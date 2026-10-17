namespace Akinci.Domain.Exceptions;

/// <summary>
/// Domain katmanındaki iş mantığı kural ihlalleri için temel istisna sınıfı.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
