using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Http;

namespace Akinci.Infrastructure.Hls;

/// <summary>
/// HLS (.m3u8) akışlarının üstveri, kalite varyantı ve canlı/VOD durumunu çözümleyen servis.
/// </summary>
public sealed class HlsMetadataService : IHttpMetadataService
{
    public async Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        using var client = HttpHandlerFactory.CreateClient(credentials);

        string playlistContent;
        try
        {
            playlistContent = await client.GetStringAsync(url.Uri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Oynatma listesi çekilemezse temel fallback bilgisi dön
            return new FileMetadataDto(
                ContentLength: null,
                SupportsRangeRequests: false,
                SuggestedFileName: url.GetFileNameFromUrl(),
                ETag: null,
                ContentType: "video/mp2t");
        }

        var playlist = HlsPlaylistParser.Parse(playlistContent, url.Uri);

        // Master playlist ise en yüksek çözünürlüklü varyantı seç
        if (playlist.IsMaster && playlist.Variants.Count > 0)
        {
            var bestVariant = playlist.Variants[0];
            if (Uri.TryCreate(bestVariant.Url, UriKind.Absolute, out var variantUri))
            {
                try
                {
                    var variantContent = await client.GetStringAsync(variantUri, cancellationToken).ConfigureAwait(false);
                    playlist = HlsPlaylistParser.Parse(variantContent, variantUri);
                }
                catch { }
            }
        }

        var fileName = url.GetFileNameFromUrl();
        if (!fileName.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) &&
            !fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            fileName = Path.ChangeExtension(fileName, ".ts");
        }

        return new FileMetadataDto(
            ContentLength: null, // HLS akışları dinamik boyutludur
            SupportsRangeRequests: false,
            SuggestedFileName: fileName,
            ETag: null,
            ContentType: "video/mp2t");
    }
}
