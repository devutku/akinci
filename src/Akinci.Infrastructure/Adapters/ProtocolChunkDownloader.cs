using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Ftp;
using Akinci.Infrastructure.Hls;
using Akinci.Infrastructure.Http;

namespace Akinci.Infrastructure.Adapters;

/// <summary>
/// HTTP(S), FTP(S) ve HLS (.m3u8) protokollerini URL şemasına göre uygun parça indirme motoruna yönlendiren port adaptörü.
/// </summary>
public sealed class ProtocolChunkDownloader : IChunkDownloader
{
    private readonly HttpChunkDownloader _httpDownloader;
    private readonly FtpChunkDownloader _ftpDownloader;
    private readonly HlsChunkDownloader _hlsDownloader;

    public ProtocolChunkDownloader(
        HttpChunkDownloader? httpDownloader = null,
        FtpChunkDownloader? ftpDownloader = null,
        HlsChunkDownloader? hlsDownloader = null)
    {
        _httpDownloader = httpDownloader ?? new HttpChunkDownloader();
        _ftpDownloader = ftpDownloader ?? new FtpChunkDownloader();
        _hlsDownloader = hlsDownloader ?? new HlsChunkDownloader();
    }

    public Task DownloadChunkAsync(
        DownloadUrl url,
        Chunk chunk,
        Func<long, ReadOnlyMemory<byte>, CancellationToken, Task> onDataReceived,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        if (url.IsHls)
        {
            return _hlsDownloader.DownloadChunkAsync(url, chunk, onDataReceived, credentials, cancellationToken);
        }

        if (url.IsFtp)
        {
            return _ftpDownloader.DownloadChunkAsync(url, chunk, onDataReceived, credentials, cancellationToken);
        }

        return _httpDownloader.DownloadChunkAsync(url, chunk, onDataReceived, credentials, cancellationToken);
    }
}
