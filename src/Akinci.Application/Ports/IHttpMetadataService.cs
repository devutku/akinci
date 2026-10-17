using Akinci.Application.DTOs;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.Ports;

/// <summary>
/// Uzak HTTP sunucusunun yeteneklerini (Content-Length, Accept-Ranges vb.) sorgulayan port arayüzü.
/// </summary>
public interface IHttpMetadataService
{
    /// <summary>
    /// Verilen URL için HTTP başlıklarını sorgulayarak dosya üstverisini döner.
    /// </summary>
    Task<FileMetadataDto> GetMetadataAsync(DownloadUrl url, CancellationToken cancellationToken = default);
}
