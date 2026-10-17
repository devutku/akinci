using System.Security.Cryptography;
using Akinci.Application.Ports;

namespace Akinci.Infrastructure.Security;

/// <summary>
/// Dosyayı diskten yüksek performanslı sequential akışla okuyarak SHA-256 hash'ini hesaplayan servis.
/// </summary>
public sealed class StreamingFileHashCalculator : IFileHashCalculator
{
    private const int ReadBufferSize = 1024 * 1024; // 1 MB okuma tamponu

    public async Task<string> CalculateSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hash hesaplanacak dosya bulunamadı: {filePath}", filePath);
        }

        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            ReadBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
