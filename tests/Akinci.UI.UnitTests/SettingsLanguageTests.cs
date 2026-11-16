using Akinci.UI.Localization;
using Akinci.UI.Services;

namespace Akinci.UI.UnitTests;

public sealed class SettingsLanguageTests : IDisposable
{
    private readonly string _appDir = Path.Combine(Path.GetTempPath(), $"Akinci_Settings_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_appDir))
        {
            Directory.Delete(_appDir, recursive: true);
        }
    }

    private string SettingsPath => Path.Combine(_appDir, "settings.json");

    [Fact]
    public void NewInstall_FollowsWindowsLanguage()
    {
        var service = new SettingsService(_appDir);

        Assert.Null(service.Current.Language);
    }

    [Fact]
    public void SettingsFromOlderVersion_KeepTurkish()
    {
        // Dil seçeneğinden önceki sürümün yazdığı ayar dosyası: "Language" anahtarı yok
        Directory.CreateDirectory(_appDir);
        File.WriteAllText(SettingsPath, """{ "DefaultConnections": 12 }""");

        var service = new SettingsService(_appDir);

        Assert.Equal(Loc.TurkishCode, service.Current.Language);
        Assert.Equal(12, service.Current.DefaultConnections);
        Assert.Contains("\"Language\": \"tr\"", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void SavedSystemLanguageChoice_IsNotOverwritten()
    {
        Directory.CreateDirectory(_appDir);
        File.WriteAllText(SettingsPath, """{ "Language": null }""");

        var service = new SettingsService(_appDir);

        Assert.Null(service.Current.Language);
    }

    [Fact]
    public void ChosenLanguage_IsSavedAndReloaded()
    {
        var service = new SettingsService(_appDir);
        service.Current.Language = Loc.EnglishCode;
        service.SaveSettings(service.Current);

        var reloaded = new SettingsService(_appDir);

        Assert.Equal(Loc.EnglishCode, reloaded.Current.Language);
    }
}
