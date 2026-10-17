using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Ftp;
using Akinci.Infrastructure.Hls;
using Akinci.Infrastructure.Http;

namespace Akinci.Infrastructure.Adapters;

/// <summary>
/// HTTP(S), FTP(S) ve HLS (.m3u8) protokollerini URL şemasına göre uygun servis implementasyonuna yönlendiren port adaptörü.
/// </summary>
public sealed class ProtocolMetadataService : IHttpMetadataService
{
    private readonly HttpMetadataService _httpMetadataService;
    private readonly FtpMetadataService _ftpMetadataService;
    private readonly HlsMetadataService _hlsMetadataService;

    public ProtocolMetadataService(
        HttpMetadataService? httpMetadataService = null,
        FtpMetadataService? ftpMetadataService = null,
        HlsMetadataService? hlsMetadataService = null)
    {
        _httpMetadataService = httpMetadataService ?? new HttpMetadataService();
        _ftpMetadataService = ftpMetadataService ?? new FtpMetadataService();
        _hlsMetadataService = hlsMetadataService ?? new HlsMetadataService();
    }

    public Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        if (url.IsHls)
        {
            return _hlsMetadataService.GetMetadataAsync(url, credentials, cancellationToken);
        }

        if (url.IsFtp)
        {
            return _ftpMetadataService.GetMetadataAsync(url, credentials, cancellationToken);
        }

        return _httpMetadataService.GetMetadataAsync(url, credentials, cancellationToken);
    }
}
