using System.Text.Json;

namespace Akinci.UI.Services;

public sealed class AppSettings
{
    public string DefaultDownloadDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    public int DefaultConnections { get; set; } = 8;

    public string? VirusTotalApiKey { get; set; } = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");
}

public sealed class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;

    public AppSettings Current => _currentSettings;

    public SettingsService()
    {
        var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

        _settingsFilePath = Path.Combine(appDir, "settings.json");
        _currentSettings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Varsayılan ayarlarla devam et
        }

        var defaultSettings = new AppSettings();
        SaveSettings(defaultSettings);
        return defaultSettings;
    }

    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings;
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Kaydetme hatası
        }
    }
}
