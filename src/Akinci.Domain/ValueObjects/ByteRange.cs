using Akinci.Domain.Exceptions;

namespace Akinci.Domain.ValueObjects;

/// <summary>
/// Dosya içindeki kapsayıcı [Start, End] bayt aralığını temsil eden Değer Nesnesi (Value Object).
/// Örnek: HTTP Range: bytes=0-1023 (Toplam 1024 bayt).
/// </summary>
public sealed record ByteRange
{
    public long Start { get; }
    public long End { get; }

    /// <summary>
    /// Bu aralıktaki toplam bayt sayısı: (End - Start + 1).
    /// </summary>
    public long TotalBytes => End - Start + 1;

    public ByteRange(long start, long end)
    {
        if (start < 0)
        {
            throw new InvalidByteRangeException(start, end, "Başlangıç ofseti negatif olamaz.");
        }

        if (end < start)
        {
            throw new InvalidByteRangeException(start, end, "Bitiş ofseti başlangıç ofsetinden küçük olamaz.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Verilen ofsetin bu aralığın içinde olup olmadığını kontrol eder.
    /// </summary>
    public bool Contains(long offset) => offset >= Start && offset <= End;

    public override string ToString() => $"{Start}-{End} ({TotalBytes:N0} bytes)";
}
