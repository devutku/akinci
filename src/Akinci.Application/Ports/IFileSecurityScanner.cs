using Akinci.Domain.ValueObjects;

namespace Akinci.Application.Ports;

/// <summary>
/// Dosya SHA-256 hash'i üzerinden VirusTotal veya güvenlik istihbarat servislerine sorgu atan port arayüzü.
/// </summary>
public interface IFileSecurityScanner
{
    /// <summary>
    /// Verilen SHA-256 hash'ini sorgulayarak antivirüs motorlarının tarama sonucunu döner.
    /// </summary>
    Task<SecurityVerdict> ScanHashAsync(string sha256Hash, CancellationToken cancellationToken = default);
}
