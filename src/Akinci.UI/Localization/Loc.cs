using System.Globalization;

namespace Akinci.UI.Localization;

public enum AppLanguage
{
    Turkish,
    English
}

/// <summary>
/// Arayüz dilini belirler. Dil uygulama açılışında bir kez seçilir; değişiklik yeniden başlatınca uygulanır.
/// </summary>
public static class Loc
{
    /// <summary>Ayarlarda saklanan dil kodları. <c>null</c> sistem dilini izler.</summary>
    public const string TurkishCode = "tr";
    public const string EnglishCode = "en";

    public static AppLanguage Current { get; private set; } = AppLanguage.Turkish;

    public static void Initialize(string? languageSetting)
    {
        Current = Resolve(languageSetting, CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Ayardaki dil koduna göre dili seçer. Kod yoksa Windows dili Türkçe ise Türkçe, değilse İngilizce kullanılır.
    /// </summary>
    public static AppLanguage Resolve(string? languageSetting, CultureInfo systemCulture) => languageSetting switch
    {
        TurkishCode => AppLanguage.Turkish,
        EnglishCode => AppLanguage.English,
        _ => systemCulture.TwoLetterISOLanguageName == "tr" ? AppLanguage.Turkish : AppLanguage.English
    };

    internal static string T(string turkish, string english) =>
        Current == AppLanguage.Turkish ? turkish : english;
}
