namespace Akinci.Domain.ValueObjects;

/// <summary>
/// İndirilen dosyanın VirusTotal veya güvenlik taraması sonucunu temsil eden Değer Nesnesi (Value Object).
/// </summary>
public sealed record SecurityVerdict
{
    public string Sha256Hash { get; }
    public bool IsScanned { get; }
    public int MaliciousVotes { get; }
    public int SuspiciousVotes { get; }
    public int HarmlessVotes { get; }
    public int UndetectedVotes { get; }
    public string? ScanUrl { get; }
    public string? CustomMessage { get; }

    public int TotalEngines => MaliciousVotes + SuspiciousVotes + HarmlessVotes + UndetectedVotes;
    public bool IsSafe => IsScanned && MaliciousVotes == 0 && SuspiciousVotes == 0;
    public bool HasThreat => MaliciousVotes > 0 || SuspiciousVotes > 0;

    public SecurityVerdict(
        string sha256Hash,
        bool isScanned,
        int maliciousVotes = 0,
        int suspiciousVotes = 0,
        int harmlessVotes = 0,
        int undetectedVotes = 0,
        string? scanUrl = null,
        string? customMessage = null)
    {
        Sha256Hash = sha256Hash ?? throw new ArgumentNullException(nameof(sha256Hash));
        IsScanned = isScanned;
        MaliciousVotes = Math.Max(0, maliciousVotes);
        SuspiciousVotes = Math.Max(0, suspiciousVotes);
        HarmlessVotes = Math.Max(0, harmlessVotes);
        UndetectedVotes = Math.Max(0, undetectedVotes);
        ScanUrl = scanUrl;
        CustomMessage = customMessage;
    }

    public static SecurityVerdict Unscanned(string sha256Hash) =>
        new(sha256Hash, isScanned: false, customMessage: "Henüz taranmadı");

    public static SecurityVerdict NotFound(string sha256Hash) =>
        new(sha256Hash, isScanned: true, customMessage: "VirusTotal veritabanında kaydı yok (Bilinmeyen/Yeni dosya)");

    public static SecurityVerdict Clean(string sha256Hash, int harmlessVotes, int totalEngines, string? scanUrl = null) =>
        new(sha256Hash, isScanned: true, 0, 0, harmlessVotes, totalEngines - harmlessVotes, scanUrl);

    public static SecurityVerdict Infected(string sha256Hash, int maliciousVotes, int totalEngines, string? scanUrl = null) =>
        new(sha256Hash, isScanned: true, maliciousVotes, 0, totalEngines - maliciousVotes, 0, scanUrl);

    public string FormatBadge()
    {
        if (!IsScanned)
        {
            return "Tarama Bekleniyor";
        }

        if (!string.IsNullOrEmpty(CustomMessage))
        {
            return CustomMessage;
        }

        if (HasThreat)
        {
            return TotalEngines > 0
                ? $"{MaliciousVotes}/{TotalEngines} motor zararlı uyarısı verdi!"
                : $"{MaliciousVotes} motor zararlı uyarısı verdi!";
        }

        return TotalEngines > 0
            ? $"{HarmlessVotes}/{TotalEngines} antivirüs motorundan geçti, güvenli"
            : $"{HarmlessVotes} antivirüs motorundan geçti, güvenli";
    }

    public override string ToString() => FormatBadge();
}
