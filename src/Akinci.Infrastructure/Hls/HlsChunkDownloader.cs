using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Http;
using Akinci.Infrastructure.Resilience;

namespace Akinci.Infrastructure.Hls;

/// <summary>
/// HLS (.m3u8) akışlarındaki video/ses segmentlerini sırayla indirip dosya havuzuna aktaran,
/// hem VOD hem de canlı yayın kaydı (rolling window) destekleyen parça indirme motoru.
/// </summary>
public sealed class HlsChunkDownloader : IChunkDownloader
{
    private readonly int _maxRetries;

    public HlsChunkDownloader(int maxRetries = 3)
    {
        _maxRetries = maxRetries;
    }

    public async Task DownloadChunkAsync(
        DownloadUrl url,
        Chunk chunk,
        Func<long, ReadOnlyMemory<byte>, CancellationToken, Task> onDataReceived,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        if (chunk.IsCompleted)
        {
            return;
        }

        using var client = HttpHandlerFactory.CreateClient(credentials);

        // 1. Ana oynatma listesini çöz
        var effectiveUri = url.Uri;
        string playlistContent = await client.GetStringAsync(effectiveUri, cancellationToken).ConfigureAwait(false);
        var playlist = HlsPlaylistParser.Parse(playlistContent, effectiveUri);

        // Master playlist ise en kaliteli varyantı seç
        if (playlist.IsMaster && playlist.Variants.Count > 0)
        {
            var best = playlist.Variants[0];
            effectiveUri = new Uri(best.Url);
            playlistContent = await client.GetStringAsync(effectiveUri, cancellationToken).ConfigureAwait(false);
            playlist = HlsPlaylistParser.Parse(playlistContent, effectiveUri);
        }

        long fileOffset = chunk.CurrentOffset;
        var downloadedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 2. VOD veya Canlı Yayın Döngüsü
        while (!cancellationToken.IsCancellationRequested)
        {
            var newSegments = playlist.Segments
                .Where(s => !downloadedUrls.Contains(s.Url))
                .ToList();

            foreach (var segment in newSegments)
            {
                if (cancellationToken.IsCancellationRequested) break;

                byte[]? segmentData = null;

                // Segmenti dirençli (exponential backoff) indir
                await RetryPolicy.ExecuteAsync(
                    async attempt =>
                    {
                        using var segResponse = await client.GetAsync(
                            segment.Url,
                            HttpCompletionOption.ResponseContentRead,
                            cancellationToken).ConfigureAwait(false);

                        segResponse.EnsureSuccessStatusCode();
                        segmentData = await segResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                    },
                    maxRetries: _maxRetries,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (segmentData != null && segmentData.Length > 0)
                {
                    // Diske sparse/ardışık olarak yaz
                    await onDataReceived(fileOffset, segmentData, cancellationToken).ConfigureAwait(false);
                    fileOffset += segmentData.Length;
                    downloadedUrls.Add(segment.Url);
                }
            }

            // Eğer oynatma listesinde #EXT-X-ENDLIST varsa (VOD) veya tüm parçalar bittiyse tamamla
            if (playlist.HasEndList)
            {
                break;
            }

            // Canlı Yayın (Rolling Window): Yeni segmentlerin sunucuya eklenmesi için hedef süre kadar bekle
            var waitSec = playlist.TargetDuration > 0 ? playlist.TargetDuration : 2.0;
            var delayMs = (int)(Math.Max(1.0, waitSec) * 1000);

            try
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Güncel M3U8 listesini yeniden çek
            try
            {
                playlistContent = await client.GetStringAsync(effectiveUri, cancellationToken).ConfigureAwait(false);
                playlist = HlsPlaylistParser.Parse(playlistContent, effectiveUri);
            }
            catch
            {
                // Geçici liste çekme hatalarında döngüyü hemen kırma, sonraki adımda tekrar dene
            }
        }

        // Açık uçlu HLS akışını tamamlandı olarak işaretle
        chunk.CompleteStream(fileOffset - chunk.Range.Start);
    }
}
