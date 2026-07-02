using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordRecorder.Services;

public class DictionaryService
{
    private readonly Dictionary<string, List<string>> _wordDict = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _dictPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WordRecorder", "dictionary.txt");

    public int WordCount => _wordDict.Count;

    public async Task LoadDictionaryAsync()
    {
        if (!File.Exists(_dictPath))
            return;

        try
        {
            var lines = await File.ReadAllLinesAsync(_dictPath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Lingoes/灵格斯词霸格式: word\ttranslation
                var parts = line.Split('\t', 2);
                if (parts.Length >= 1)
                {
                    var word = parts[0].Trim().ToLower();
                    var translation = parts.Length > 1 ? parts[1].Trim() : "";

                    if (!string.IsNullOrEmpty(word) && !_wordDict.ContainsKey(word))
                    {
                        _wordDict[word] = new List<string> { translation };
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    public async Task ImportFromLingoesAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            var newWords = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('\t', 2);
                if (parts.Length >= 1)
                {
                    var word = parts[0].Trim().ToLower();
                    var translation = parts.Length > 1 ? parts[1].Trim() : "";

                    if (!string.IsNullOrEmpty(word) && !_wordDict.ContainsKey(word))
                    {
                        _wordDict[word] = new List<string> { translation };
                        newWords.Add($"{word}\t{translation}");
                    }
                }
            }

            // Append to dictionary file
            var dir = Path.GetDirectoryName(_dictPath)!;
            Directory.CreateDirectory(dir);
            await File.AppendAllLinesAsync(_dictPath, newWords, Encoding.UTF8);
        }
        catch
        {
            // Ignore errors
        }
    }

    public List<string> GetSuggestions(string prefix, int count = 5)
    {
        if (string.IsNullOrEmpty(prefix) || prefix.Length < 2)
            return new List<string>();

        return _wordDict.Keys
            .Where(w => w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Take(count)
            .ToList();
    }

    public string? GetTranslation(string word)
    {
        if (_wordDict.TryGetValue(word.ToLower(), out var translations))
        {
            return translations.FirstOrDefault();
        }
        return null;
    }
}
