using System.Text.Json;
using Akinci.Infrastructure.Settings;

namespace Akinci.UI.Services;

public sealed class AppSettings
{
    public string DefaultDownloadDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    public int DefaultConnections { get; set; } = 8;

    public string? VirusTotalApiKey { get; set; } = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");

    public bool ExtensionPromptDismissed { get; set; } = false;

    public int MaxConcurrentDownloads { get; set; } = 3;

    public long SpeedLimitBytesPerSecond { get; set; } = 0;

    public bool EnableClipboardMonitor { get; set; } = false;

    /// <summary>
    /// Arayüz dili: "tr", "en" veya <c>null</c> (Windows dilini izle).
    /// </summary>
    public string? Language { get; set; }
}

public sealed class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;

    public AppSettings Current => _currentSettings;

    public SettingsService()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci"))
    {
    }

    /// <summary>Ayarları belirtilen klasörde tutar (testler gerçek kullanıcı ayarlarına dokunmasın diye).</summary>
    internal SettingsService(string appDir)
    {
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

        _settingsFilePath = Path.Combine(appDir, "settings.json");
        _currentSettings = LoadSettings();

        // Kurulum programının bıraktığı VirusTotal API anahtarını (varsa) ayarlara al
        var pendingKey = PendingSettingFile.TryConsume(Path.Combine(appDir, "pending-virustotal-key"));
        if (pendingKey != null)
        {
            _currentSettings.VirusTotalApiKey = pendingKey;
            SaveSettings(_currentSettings);
        }
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
                    if (!HasLanguageKey(json))
                    {
                        // Dil seçeneğinden önceki sürümlerden gelen kullanıcı: arayüz şimdiye kadar Türkçeydi,
                        // Windows dili farklı olsa bile güncellemeden sonra Türkçe kalsın.
                        loaded.Language = Localization.Loc.TurkishCode;
                        SaveSettings(loaded);
                    }
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

    private static bool HasLanguageKey(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.Object &&
               doc.RootElement.TryGetProperty(nameof(AppSettings.Language), out _);
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
