namespace Akinci.Domain.Exceptions;

/// <summary>
/// Geçersiz byte aralığı (başlangıç < 0 veya bitiş < başlangıç) verildiğinde fırlatılır.
/// </summary>
public sealed class InvalidByteRangeException : DomainException
{
    public InvalidByteRangeException(long start, long end, string reason)
        : base($"Geçersiz bayt aralığı [{start}-{end}]: {reason}")
    {
    }
}
