using System.Globalization;
using Akinci.Domain.ValueObjects;

namespace Akinci.Cli;

/// <summary>
/// Komut satırı arayüzünün metinleri. Dil, masaüstü uygulamasındaki <c>Loc.Resolve</c> ile aynı kurala göre seçilir:
/// AKINCI_LANG ortam değişkeni "tr" veya "en" ise o dil, değilse Windows arayüz dili Türkçe ise Türkçe, diğer dillerde İngilizce.
/// </summary>
internal static class CliText
{
    public const string LanguageVariable = "AKINCI_LANG";

    public static bool IsTurkish { get; } =
        ResolveTurkish(Environment.GetEnvironmentVariable(LanguageVariable), CultureInfo.CurrentUICulture);

    internal static bool ResolveTurkish(string? languageSetting, CultureInfo systemCulture) =>
        languageSetting?.Trim().ToLowerInvariant() switch
        {
            "tr" => true,
            "en" => false,
            _ => systemCulture.TwoLetterISOLanguageName == "tr"
        };

    private static string T(string turkish, string english) => IsTurkish ? turkish : english;

    public static string Tagline => T("Clean Core Çok Kanallı İndirme Motoru", "Clean Core Multi-Connection Download Engine");

    public static string Help => IsTurkish
        ? """
          Kullanım: akinci <URL> [seçenekler]

          Seçenekler:
            -o, --output <dizin>       Hedef indirme dizini (Varsayılan: mevcut dizin)
            -c, --connections <sayı>   Paralel bağlantı/parça sayısı (Varsayılan: 8)
            -n, --name <isim>          Özel dosya adı
            -u, --user <kullanıcı>     Kimlik doğrulama kullanıcı adı
            -p, --pass <şifre>         Kimlik doğrulama parolası
            -a, --auth <tür>           Kimlik doğrulama türü: basic, ntlm, kerberos, negotiate
            --vt-key <key>             VirusTotal API anahtarı (Varsayılan: $VIRUSTOTAL_API_KEY)

          Dil: Windows dilini izler. Değiştirmek için AKINCI_LANG=tr veya AKINCI_LANG=en

          Örnek:
            akinci https://speed.hetzner.de/100MB.bin -c 8 -o downloads
          """
        : """
          Usage: akinci <URL> [options]

          Options:
            -o, --output <dir>         Destination directory (default: current directory)
            -c, --connections <n>      Number of parallel connections/chunks (default: 8)
            -n, --name <name>          Custom file name
            -u, --user <user>          User name for authentication
            -p, --pass <password>      Password for authentication
            -a, --auth <type>          Authentication type: basic, ntlm, kerberos, negotiate
            --vt-key <key>             VirusTotal API key (default: $VIRUSTOTAL_API_KEY)

          Language: follows the Windows display language. Override with AKINCI_LANG=tr or AKINCI_LANG=en

          Example:
            akinci https://speed.hetzner.de/100MB.bin -c 8 -o downloads
          """;

    public static string Pausing => T("[DURAKLATILIYOR] İndirme duraklatılıyor...", "[PAUSING] Pausing the download...");
    public static string QueryingServer(string url) => T($"[1/3] Sunucu sorgulanıyor: {url}", $"[1/3] Querying server: {url}");
    public static string TargetFile(string path) => T($"[2/3] Hedef Dosya: {path}", $"[2/3] Target file: {path}");
    public static string TotalSize(string size) => T($"      Toplam Boyut: {size}", $"      Total size: {size}");
    public static string RangeSupport(bool supported) => supported
        ? T("      Range Desteği: VAR (Çok Kanallı)", "      Range support: YES (multi-connection)")
        : T("      Range Desteği: YOK (Tek Kanal)", "      Range support: NO (single connection)");
    public static string ChunkCount(int count) => T($"      Parça Sayısı: {count}", $"      Chunks: {count}");
    public static string Starting => T("[3/3] İndirme başlatılıyor...", "[3/3] Starting download...");
    public static string Unknown => T("Bilinmiyor", "Unknown");
    public static string Completed(double seconds) =>
        T($"✔ İNDİRME TAMAMLANDI! Süre: {seconds:F2} sn", $"✔ DOWNLOAD COMPLETE! Time: {seconds:F2} s");
    public static string SavedTo(string path) => T($"  Kaydedilen Konum: {path}", $"  Saved to: {path}");
    public static string SecurityThreatTag => T("GÜVENLİK TEHLİKESİ", "SECURITY THREAT");
    public static string SecurityTag => T("GÜVENLİK", "SECURITY");
    public static string Report(string url) => T($"  Rapor: {url}", $"  Report: {url}");
    public static string Paused(long bytes) =>
        T($"⏸ İndirme duraklatıldı. Kaldığı bayt: {bytes}", $"⏸ Download paused at byte {bytes}.");
    public static string Cancelled => T("İndirme kullanıcı tarafından iptal edildi veya duraklatıldı.", "The download was cancelled or paused by the user.");
    public static string Error(string message) => T($"❌ HATA: {message}", $"❌ ERROR: {message}");

    /// <summary>
    /// VirusTotal sonucunu CLI dilinde yazar. Türkçede çekirdekteki <see cref="SecurityVerdict.FormatBadge"/> kullanılır;
    /// İngilizcede metin oy sayılarından ve API anahtarı olup olmadığından üretilir, çünkü çekirdeğin özel mesajları Türkçedir.
    /// </summary>
    public static string SecurityVerdictText(SecurityVerdict verdict, bool hasApiKey)
    {
        if (IsTurkish)
        {
            return verdict.FormatBadge();
        }

        if (verdict.HasThreat)
        {
            return verdict.TotalEngines > 0
                ? $"{verdict.MaliciousVotes}/{verdict.TotalEngines} engines flagged this file as malicious!"
                : $"{verdict.MaliciousVotes} engines flagged this file as malicious!";
        }

        if (verdict.IsSafe)
        {
            return $"Passed {verdict.HarmlessVotes}/{verdict.TotalEngines} antivirus engines, safe";
        }

        if (verdict.IsScanned)
        {
            // Taranmış ama motor sonucu yok: özel mesaj varsa dosya VirusTotal'da bulunamamıştır
            return verdict.CustomMessage != null
                ? "Not in the VirusTotal database (unknown or new file)"
                : "No antivirus engine reported a result";
        }

        return hasApiKey
            ? "VirusTotal scan could not be completed"
            : "No VirusTotal API key given (scan skipped)";
    }
}
