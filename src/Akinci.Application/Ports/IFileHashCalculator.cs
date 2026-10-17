namespace Akinci.Application.Ports;

/// <summary>
/// İndirilen dosyanın akış (stream) üzerinden SHA-256 kriptografik hash'ini hesaplayan port arayüzü.
/// </summary>
public interface IFileHashCalculator
{
    /// <summary>
    /// Belirtilen dosya yolundaki veriyi stream üzerinden okuyarak hex formatında SHA-256 hash'ini üretir.
    /// </summary>
    Task<string> CalculateSha256Async(string filePath, CancellationToken cancellationToken = default);
}
