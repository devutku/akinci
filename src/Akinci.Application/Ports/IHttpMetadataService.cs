using Akinci.Application.DTOs;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.Ports;

/// <summary>
/// Uzak HTTP sunucusunun yeteneklerini (Content-Length, Accept-Ranges vb.) sorgulayan port arayüzü.
/// </summary>
public interface IHttpMetadataService
{
    /// <summary>
    /// Verilen URL için üstveri sorgulaması yapar (HTTP HEAD / Range / FTP SIZE desteği).
    /// </summary>
    Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default);
}
