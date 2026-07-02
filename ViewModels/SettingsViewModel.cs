using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordRecorder.Models;
using WordRecorder.Services;

namespace WordRecorder.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();
    private readonly AppSettings _settings;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private string _selectedAccentColor;

    public ObservableCollection<string> Themes { get; } = new()
    {
        "Default",
        "Light",
        "Dark"
    };

    public ObservableCollection<string> PresetColors { get; } = new()
    {
        "#FF0078D4", // Windows Blue
        "#FF0099BC", // Teal
        "#FF7A7574", // Neutral
        "#FF767676", // Gray
        "#FFEA005E", // Magenta
        "#FFD83427", // Red
        "#FFFF8C00", // Orange
        "#FFBBB550", // Yellow
        "#FF107C10", // Green
        "#FF0063B1", // Dark Blue
        "#FF881798", // Purple
        "#FF8764B8", // Lavender
    };

    public string ThemeDisplay => SelectedTheme switch
    {
        "Light" => "浅色",
        "Dark" => "深色",
        _ => "跟随系统"
    };

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        _selectedTheme = settings.Theme;
        _selectedAccentColor = settings.AccentColor;
    }

    [RelayCommand]
    private async Task Save()
    {
        _settings.Theme = SelectedTheme;
        _settings.AccentColor = SelectedAccentColor;
        await _settingsService.SaveAsync(_settings);
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        SelectedTheme = "Default";
        SelectedAccentColor = "#FF0078D4";
    }
}
