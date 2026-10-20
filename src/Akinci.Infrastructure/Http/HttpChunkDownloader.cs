using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Resilience;

namespace Akinci.Infrastructure.Http;

/// <summary>
/// SocketsHttpHandler tabanlı HTTP Range istekleri ile parçaları indiren ve geçici ağ kopmalarında
/// exponential backoff ile otomatik kaldığı ofsetten devam eden dirençli (resilient) servis.
/// </summary>
public sealed class HttpChunkDownloader : IChunkDownloader
{
    private const int BufferSize = 128 * 1024; // 128 KB yüksek verimli aktarım tamponu
    private readonly int _maxRetries;

    public HttpChunkDownloader(int maxRetries = 5)
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

        // Ağ kopmalarında otomatik exponential backoff ile yeniden deneme
        await RetryPolicy.ExecuteAsync(
            async attempt =>
            {
                if (chunk.IsCompleted) return;

                using var client = HttpHandlerFactory.CreateClient(credentials);

                long startOffset = chunk.CurrentOffset;
                long endOffset = chunk.Range.End;
                bool isUnbounded = endOffset >= long.MaxValue - 2;

                using var request = new HttpRequestMessage(HttpMethod.Get, url.Uri);
                if (!isUnbounded)
                {
                    request.Headers.Range = new RangeHeaderValue(startOffset, endOffset);
                }
                else if (startOffset > 0)
                {
                    request.Headers.Range = new RangeHeaderValue(startOffset, null);
                }

                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (response.StatusCode is not (HttpStatusCode.PartialContent or HttpStatusCode.OK))
                {
                    throw new HttpRequestException(
                        $"Chunk #{chunk.Index} indirilemedi. Sunucu HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) döndü.",
                        null,
                        response.StatusCode);
                }

                // Range Güvenlik Kontrolü: Ofset > 0 iken sunucu 200 OK (tüm dosya) dönerse veri bozulmasını engelle
                if (startOffset > 0 && response.StatusCode == HttpStatusCode.OK)
                {
                    throw new InvalidOperationException(
                        $"Sunucu kısmi içerik (HTTP 206 Partial Content) yerine dosyanın tamamını (HTTP 200 OK) döndürdü. " +
                        $"Chunk #{chunk.Index} (Ofset: {startOffset}) için dosya bozulmasını önlemek amacıyla indirme durduruldu.");
                }

                // Web sayfası (HTML) koruması: Arşiv/Binary dosyası beklenirken HTML gelmesi durumu
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && mediaType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    var fn = url.GetFileNameFromUrl().ToLowerInvariant();
                    if (fn.EndsWith(".rar") || fn.EndsWith(".zip") || fn.EndsWith(".7z") ||
                        fn.EndsWith(".iso") || fn.EndsWith(".exe") || fn.EndsWith(".bin") ||
                        fn.EndsWith(".tar") || fn.EndsWith(".gz") || fn.EndsWith(".mp4"))
                    {
                        throw new InvalidOperationException(
                            "Sunucu doğrudan dosya yerine bir web sayfası (HTML) döndürdü. Yapıştırdığınız bağlantı doğrudan dosya indirme linki (Direct Download Link) değil, bir web sayfasıdır.");
                    }
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try
                {
                    long currentOffset = startOffset;

                    if (isUnbounded)
                    {
                        // Boyut önceden bilinmiyorsa sunucu akışı kapatana kadar tüm baytları oku
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            int bytesRead = await stream.ReadAsync(rentedBuffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
                            if (bytesRead == 0)
                            {
                                break;
                            }

                            await onDataReceived(currentOffset, rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                            currentOffset += bytesRead;
                        }
                        chunk.CompleteStream(currentOffset - startOffset);
                    }
                    else
                    {
                        long totalBytesToRead = endOffset - startOffset + 1;
                        long totalBytesReadSoFar = 0;

                        while (totalBytesReadSoFar < totalBytesToRead)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            int maxRead = (int)Math.Min(BufferSize, totalBytesToRead - totalBytesReadSoFar);
                            int bytesRead = await stream.ReadAsync(rentedBuffer.AsMemory(0, maxRead), cancellationToken).ConfigureAwait(false);

                            if (bytesRead == 0)
                            {
                                break;
                            }

                            await onDataReceived(currentOffset, rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

                            currentOffset += bytesRead;
                            totalBytesReadSoFar += bytesRead;
                        }

                        if (totalBytesReadSoFar < totalBytesToRead && response.StatusCode == HttpStatusCode.PartialContent)
                        {
                            throw new IOException(
                                $"Chunk #{chunk.Index} bağlantısı erken kesildi. İndirilen: {totalBytesReadSoFar}/{totalBytesToRead}. Yeniden bağlanılıyor...");
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            },
            maxRetries: _maxRetries,
            initialDelay: TimeSpan.FromSeconds(1),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
