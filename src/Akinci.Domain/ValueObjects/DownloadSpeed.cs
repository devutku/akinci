namespace Akinci.Domain.ValueObjects;

/// <summary>
/// Saniyede indirilen bayt miktarını temsil eden Değer Nesnesi (Value Object).
/// </summary>
public sealed record DownloadSpeed
{
    public static readonly DownloadSpeed Zero = new(0);

    public double BytesPerSecond { get; }

    public DownloadSpeed(double bytesPerSecond)
    {
        BytesPerSecond = Math.Max(0, bytesPerSecond);
    }

    /// <summary>
    /// B/s, KB/s, MB/s veya GB/s formatında insan tarafından okunabilir gösterim üretir.
    /// </summary>
    public string FormatHumanReadable()
    {
        const double kb = 1024.0;
        const double mb = kb * 1024.0;
        const double gb = mb * 1024.0;

        return BytesPerSecond switch
        {
            >= gb => $"{BytesPerSecond / gb:F2} GB/s",
            >= mb => $"{BytesPerSecond / mb:F2} MB/s",
            >= kb => $"{BytesPerSecond / kb:F1} KB/s",
            _ => $"{BytesPerSecond:F0} B/s"
        };
    }

    public override string ToString() => FormatHumanReadable();
}
