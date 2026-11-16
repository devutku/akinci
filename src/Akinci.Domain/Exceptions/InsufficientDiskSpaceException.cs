namespace Akinci.Domain.Exceptions;

/// <summary>
/// Hedef sürücüde dosya için yeterli boş alan olmadığında fırlatılır.
/// </summary>
public sealed class InsufficientDiskSpaceException : DomainException
{
    public long RequiredBytes { get; }
    public long AvailableBytes { get; }

    public InsufficientDiskSpaceException(long requiredBytes, long availableBytes)
        : base($"Hedef sürücüde yetersiz disk alanı. Gerekli: {FormatBytes(requiredBytes)}, Kullanılabilir: {FormatBytes(availableBytes)}.")
    {
        RequiredBytes = requiredBytes;
        AvailableBytes = availableBytes;
    }

    /// <summary>Bayt sayısını "1.5 GB" gibi okunur biçime çevirir (geçerli kültürün ondalık ayırıcısıyla).</summary>
    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dbl = bytes;
        while (dbl >= 1024 && i < suffixes.Length - 1)
        {
            dbl /= 1024;
            i++;
        }
        return $"{dbl:0.##} {suffixes[i]}";
    }
}
