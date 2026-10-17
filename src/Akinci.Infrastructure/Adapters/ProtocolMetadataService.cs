using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Ftp;
using Akinci.Infrastructure.Http;

namespace Akinci.Infrastructure.Adapters;

/// <summary>
/// HTTP(S) ve FTP(S) protokollerini URL şemasına göre uygun servis implementasyonuna yönlendiren port adaptörü.
/// </summary>
public sealed class ProtocolMetadataService : IHttpMetadataService
{
    private readonly HttpMetadataService _httpMetadataService;
    private readonly FtpMetadataService _ftpMetadataService;

    public ProtocolMetadataService(
        HttpMetadataService? httpMetadataService = null,
        FtpMetadataService? ftpMetadataService = null)
    {
        _httpMetadataService = httpMetadataService ?? new HttpMetadataService();
        _ftpMetadataService = ftpMetadataService ?? new FtpMetadataService();
    }

    public Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        if (url.IsFtp)
        {
            return _ftpMetadataService.GetMetadataAsync(url, credentials, cancellationToken);
        }

        return _httpMetadataService.GetMetadataAsync(url, credentials, cancellationToken);
    }
}
