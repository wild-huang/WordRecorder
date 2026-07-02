using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordRecorder.Models;
using WordRecorder.Services;

namespace WordRecorder.ViewModels;

public partial class DateGroup : ObservableObject
{
    [ObservableProperty]
    private string _dateKey = string.Empty;

    [ObservableProperty]
    private string _dateDisplay = string.Empty;

    public ObservableCollection<Word> Words { get; } = new();
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly YoudaoDictService _dictService = new();
    private readonly ExportService _exportService = new();
    private readonly SettingsService _settingsService = new();
    private readonly AiService _aiService = new();
    private readonly LingoesDictService _lingoesDictService = new();
    private readonly string _dataFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WordRecorder", "words.json");
    private CancellationTokenSource? _suggestCts;
    private bool _isUpdatingColor;

    [ObservableProperty]
    private string _currentInput = string.Empty;

    [ObservableProperty]
    private bool _isLooking;

    [ObservableProperty]
    private string _statusText = "输入单词后按回车添加";

    [ObservableProperty]
    private Word? _selectedWord;

    [ObservableProperty]
    private AppSettings _settings = AppSettings.Default;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private string _suggestedWord = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _suggestedWords = new();

    [ObservableProperty]
    private double _redChannel = 0;

    [ObservableProperty]
    private double _greenChannel = 120;

    [ObservableProperty]
    private double _blueChannel = 212;

    [ObservableProperty]
    private string _hexInput = "#FF0078D4";

    [ObservableProperty]
    private bool? _wordExists; // null = 未检查, true = 存在, false = 不存在

    public ObservableCollection<Word> Words { get; } = new();
    public ObservableCollection<DateGroup> DateGroups { get; } = new();

    // 晚霞配色方案 - 低饱和度粉蓝撞色
    public ObservableCollection<ColorPreset> SunsetPresets { get; } = new()
    {
        new("薄雾粉", "#E8B4B8"),
        new("暮色蓝", "#7EB5D6"),
        new("淡紫霞", "#C5A3CF"),
        new("天空蓝", "#A8C8E8"),
        new("玫瑰灰", "#D4A5A5"),
        new("雾蓝", "#8BAFCB"),
        new("藕粉", "#E0C4C8"),
        new("青瓷蓝", "#9BBDD2"),
    };

    public MainWindowViewModel()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dataFilePath)!);
        LoadSettingsAndWords();
        SyncColorFromSettings();
    }

    partial void OnCurrentInputChanged(string value)
    {
        _ = UpdateSuggestionsAsync(value);
    }

    partial void OnRedChannelChanged(double value)
    {
        if (!_isUpdatingColor)
        {
            _isUpdatingColor = true;
            UpdateHexFromRgb();
            _isUpdatingColor = false;
        }
    }

    partial void OnGreenChannelChanged(double value)
    {
        if (!_isUpdatingColor)
        {
            _isUpdatingColor = true;
            UpdateHexFromRgb();
            _isUpdatingColor = false;
        }
    }

    partial void OnBlueChannelChanged(double value)
    {
        if (!_isUpdatingColor)
        {
            _isUpdatingColor = true;
            UpdateHexFromRgb();
            _isUpdatingColor = false;
        }
    }

    partial void OnHexInputChanged(string value)
    {
        if (!_isUpdatingColor)
        {
            _isUpdatingColor = true;
            UpdateRgbFromHex(value);
            _isUpdatingColor = false;
        }
    }

    private void UpdateHexFromRgb()
    {
        var color = Color.FromArgb(255, (byte)RedChannel, (byte)GreenChannel, (byte)BlueChannel);
        HexInput = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void UpdateRgbFromHex(string hex)
    {
        try
        {
            if (!string.IsNullOrEmpty(hex) && hex.StartsWith("#"))
            {
                var color = Color.Parse(hex);
                RedChannel = color.R;
                GreenChannel = color.G;
                BlueChannel = color.B;
            }
        }
        catch
        {
            // Invalid hex, ignore
        }
    }

    private void SyncColorFromSettings()
    {
        _isUpdatingColor = true;
        HexInput = Settings.AccentColor;
        UpdateRgbFromHex(Settings.AccentColor);
        _isUpdatingColor = false;
    }

    [RelayCommand]
    private void ApplyCustomColor()
    {
        _isUpdatingColor = true;
        var color = Color.FromArgb(255, (byte)RedChannel, (byte)GreenChannel, (byte)BlueChannel);
        Settings.AccentColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        _isUpdatingColor = false;
    }

    [RelayCommand]
    private void ApplyPresetColor(string? colorHex)
    {
        if (!string.IsNullOrEmpty(colorHex))
        {
            _isUpdatingColor = true;
            Settings.AccentColor = colorHex;
            HexInput = colorHex;
            UpdateRgbFromHex(colorHex);
            _isUpdatingColor = false;
        }
    }

    private async Task UpdateSuggestionsAsync(string input)
    {
        _suggestCts?.Cancel();
        _suggestCts = new CancellationTokenSource();
        var token = _suggestCts.Token;

        if (string.IsNullOrEmpty(input) || input.Length < 2)
        {
            WordExists = null;
            SuggestedWord = string.Empty;
            SuggestedWords.Clear();
            return;
        }

        // 指示器：O(1) 精确匹配，即时
        WordExists = (Settings.DictEnabled && _lingoesDictService.IsLoaded)
            ? _lingoesDictService.ContainsWord(input)
            : null;

        // 防抖
        try { await Task.Delay(80, token); } catch { return; }
        if (token.IsCancellationRequested) return;

        SuggestedWord = string.Empty;
        SuggestedWords.Clear();

        try
        {
            // 本地词典二分查找（前缀匹配）
            var dictTask = (Settings.DictEnabled && _lingoesDictService.IsLoaded)
                ? Task.Run(() => _lingoesDictService.FindSimilar(input, 5), token)
                : Task.FromResult(new List<string>());

            // 有道查词（指示器用）
            var youdaoTask = Task.Run(async () => await CheckYoudaoWordAsync(input, token), token);

            // AI 推荐词
            var aiTask = (Settings.AiEnabled && input.Length >= 4)
                ? Task.Run(async () => await GetAiSuggestionsAsync(input, token), token)
                : Task.FromResult(new List<string>());

            await Task.WhenAll(dictTask, youdaoTask, aiTask);
            if (token.IsCancellationRequested) return;

            var suggestions = dictTask.Result;

            // 有道确认单词存在 → 绿点
            if (youdaoTask.Result)
                WordExists = true;

            // AI 推荐词（纠错/补全）
            foreach (var s in aiTask.Result)
                if (!suggestions.Contains(s, StringComparer.OrdinalIgnoreCase))
                    suggestions.Add(s);

            if (!token.IsCancellationRequested)
            {
                SuggestedWords = new ObservableCollection<string>(suggestions.Take(5));
                if (SuggestedWords.Count > 0)
                    SuggestedWord = SuggestedWords[0];
            }
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private async Task<bool> CheckYoudaoWordAsync(string input, CancellationToken token)
    {
        try
        {
            var word = await _dictService.LookupWordAsync(input);
            return !string.IsNullOrEmpty(word.Translation) && word.Translation != "(查询失败)";
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<string>> GetAiSuggestionsAsync(string input, CancellationToken token)
    {
        try
        {
            _aiService.Configure(Settings.AiEndpoint, Settings.AiApiKey, Settings.AiModel);
            return await _aiService.SuggestWordsAsync(input, 5);
        }
        catch
        {
            return new List<string>();
        }
    }

    private async void LoadSettingsAndWords()
    {
        Settings = await _settingsService.LoadAsync();
        ApplyTheme();
        SyncColorFromSettings();

        // Load dictionary if configured
        if (Settings.DictEnabled && !string.IsNullOrEmpty(Settings.LingoesDictPath))
        {
            try
            {
                StatusText = "正在加载词典...";
                await _lingoesDictService.ImportFromFileAsync(Settings.LingoesDictPath);
                
                if (_lingoesDictService.IsLoaded)
                    StatusText = $"词典已加载: {_lingoesDictService.WordCount} 个单词";
                else
                    StatusText = "词典加载失败，请重新导入";
            }
            catch (Exception ex)
            {
                StatusText = $"词典加载失败: {ex.Message}";
            }
        }

        LoadWords();
    }

    private async void LoadWords()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = await File.ReadAllTextAsync(_dataFilePath);
                var words = JsonSerializer.Deserialize<Word[]>(json);
                if (words != null)
                {
                    foreach (var word in words)
                        Words.Add(word);
                    RebuildDateGroups();
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private async void SaveWords()
    {
        try
        {
            var json = JsonSerializer.Serialize(Words.ToArray(), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private void RebuildDateGroups()
    {
        DateGroups.Clear();
        var groups = Words
            .GroupBy(w => w.AddedTime.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in groups)
        {
            var dateGroup = new DateGroup
            {
                DateKey = group.Key.ToString("yyyy-MM-dd"),
                DateDisplay = GetDateDisplay(group.Key)
            };
            foreach (var word in group.OrderByDescending(w => w.AddedTime))
            {
                dateGroup.Words.Add(word);
            }
            DateGroups.Add(dateGroup);
        }
    }

    private string GetDateDisplay(DateTime date)
    {
        var today = DateTime.Today;
        if (date == today) return "今天";
        if (date == today.AddDays(-1)) return "昨天";
        if (date == today.AddDays(-2)) return "前天";
        return date.ToString("yyyy年MM月dd日 dddd");
    }

    [RelayCommand]
    private async Task AddWord()
    {
        var term = CurrentInput.Trim();
        if (string.IsNullOrEmpty(term))
            return;

        CurrentInput = string.Empty;
        SuggestedWord = string.Empty;
        SuggestedWords.Clear();
        WordExists = null;

        // 检查是否已存在该单词
        var existingWord = Words.FirstOrDefault(w => 
            w.Term.Equals(term, StringComparison.OrdinalIgnoreCase));

        if (existingWord != null)
        {
            // 已存在，增加查询次数并移动到顶部
            existingWord.LookupCount++;
            existingWord.LastLookupTime = DateTime.Now;
            
            Words.Remove(existingWord);
            Words.Insert(0, existingWord);
            
            // 更新日期分组
            RebuildDateGroups();
            
            SaveWords();
            WordExists = true;
            StatusText = $"已更新: {existingWord.Term} (第 {existingWord.LookupCount} 次查询)";
            return;
        }

        IsLooking = true;
        StatusText = $"正在查询: {term}...";

        try
        {
            var word = await _dictService.LookupWordAsync(term);

            // 查询成功，根据翻译结果判断单词是否存在
            bool hasTranslation = !string.IsNullOrEmpty(word.Translation) && word.Translation != "(查询失败)";
            WordExists = hasTranslation;

            // Check if this might be a misspelling
            if (Settings.DictEnabled && _lingoesDictService.IsLoaded)
            {
                var exactMatch = _lingoesDictService.FindSimilar(term, 1)
                    .FirstOrDefault(w => w.Equals(term, StringComparison.OrdinalIgnoreCase));

                if (exactMatch == null)
                {
                    // Word not found in dictionary, might be misspelled
                    word.IsPossibleMistake = true;
                    var corrections = _lingoesDictService.FindSimilar(term, 3);
                    if (corrections.Count > 0)
                    {
                        word.SuggestedCorrection = corrections[0];
                        word.PossibleWords = corrections;
                    }
                }
            }

            Words.Insert(0, word);

            // Update date groups
            var dateKey = word.AddedTime.Date;
            var existingGroup = DateGroups.FirstOrDefault(g => g.DateKey == dateKey.ToString("yyyy-MM-dd"));
            if (existingGroup == null)
            {
                existingGroup = new DateGroup
                {
                    DateKey = dateKey.ToString("yyyy-MM-dd"),
                    DateDisplay = GetDateDisplay(dateKey)
                };
                DateGroups.Insert(0, existingGroup);
            }
            existingGroup.Words.Insert(0, word);

            SaveWords();
            StatusText = $"已添加: {word.Term} - {word.Translation}";
        }
        catch (Exception ex)
        {
            WordExists = false;
            StatusText = $"查询失败: {ex.Message}";
        }
        finally
        {
            IsLooking = false;
        }
    }

    [RelayCommand]
    private void SelectSuggestion(string? word)
    {
        if (!string.IsNullOrEmpty(word))
        {
            CurrentInput = word;
            SuggestedWord = string.Empty;
            SuggestedWords.Clear();
        }
    }

    [RelayCommand]
    private void DeleteWord(Word? word)
    {
        if (word == null) return;

        Words.Remove(word);

        // Remove from date group
        var dateKey = word.AddedTime.Date.ToString("yyyy-MM-dd");
        var group = DateGroups.FirstOrDefault(g => g.DateKey == dateKey);
        if (group != null)
        {
            group.Words.Remove(word);
            if (group.Words.Count == 0)
                DateGroups.Remove(group);
        }

        SaveWords();
        StatusText = $"已删除: {word.Term}";
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private async Task ApplySettings()
    {
        await _settingsService.SaveAsync(Settings);
        ApplyTheme();

        // Reload dictionary if path changed
        if (Settings.DictEnabled && !string.IsNullOrEmpty(Settings.LingoesDictPath))
        {
            try
            {
                await _lingoesDictService.ImportFromFileAsync(Settings.LingoesDictPath);
                StatusText = $"词典已加载: {_lingoesDictService.WordCount} 个单词";
            }
            catch (Exception ex)
            {
                StatusText = $"词典加载失败: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ImportDictionary(Window window)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择词典文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("词典文件") { Patterns = new[] { "*.txt", "*.csv", "*.ldx" } },
                new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
            }
        });

        if (files == null || files.Count == 0) return;

        var file = files[0];
        var filePath = file.TryGetLocalPath();
        if (filePath == null) return;

        try
        {
            await _lingoesDictService.ImportFromFileAsync(filePath);
            Settings.LingoesDictPath = filePath;
            Settings.DictEnabled = true;
            await _settingsService.SaveAsync(Settings);
            StatusText = $"词典已导入: {_lingoesDictService.WordCount} 个单词";
        }
        catch (Exception ex)
        {
            StatusText = $"导入失败: {ex.Message}";
        }
    }

    private void ApplyTheme()
    {
        if (Application.Current == null) return;

        Application.Current.RequestedThemeVariant = Settings.Theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };

        // Apply accent color
        if (App.Instance != null)
        {
            App.Instance.ApplyAccentColor(Settings.AccentColor);
        }
    }

    [RelayCommand]
    private async Task Export(Window window)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出生词本",
            SuggestedFileName = "生词本",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("Excel 文件") { Patterns = new[] { "*.xlsx" } },
                new FilePickerFileType("Word 文件") { Patterns = new[] { "*.docx" } },
                new FilePickerFileType("PDF 文件") { Patterns = new[] { "*.pdf" } },
                new FilePickerFileType("PNG 图片") { Patterns = new[] { "*.png" } },
            }
        });

        if (file == null) return;

        var filePath = file.TryGetLocalPath();
        if (filePath == null) return;

        StatusText = "正在导出...";
        try
        {
            var ext = Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".txt":
                    await _exportService.ExportToTxtAsync(Words, filePath);
                    break;
                case ".xlsx":
                    await _exportService.ExportToXlsxAsync(Words, filePath);
                    break;
                case ".docx":
                    await _exportService.ExportToDocxAsync(Words, filePath);
                    break;
                case ".pdf":
                    await _exportService.ExportToPdfAsync(Words, filePath);
                    break;
                case ".png":
                    await _exportService.ExportToPngAsync(Words, filePath);
                    break;
                default:
                    await _exportService.ExportToTxtAsync(Words, filePath);
                    break;
            }
            StatusText = $"已导出到: {filePath}";
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败: {ex.Message}";
        }
    }
}

public record ColorPreset(string Name, string Hex)
{
    public Avalonia.Media.Color Color => Avalonia.Media.Color.Parse(Hex);
}
