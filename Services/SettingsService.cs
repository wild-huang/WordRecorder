using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WordRecorder.Models;

namespace WordRecorder.Services;

public class SettingsService
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WordRecorder", "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.Default;
            }
        }
        catch
        {
            // Ignore
        }
        return AppSettings.Default;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Ignore
        }
    }
}
