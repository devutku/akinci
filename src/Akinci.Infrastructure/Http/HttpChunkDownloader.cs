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

                using var request = new HttpRequestMessage(HttpMethod.Get, url.Uri);
                request.Headers.Range = new RangeHeaderValue(startOffset, endOffset);

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

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try
                {
                    long currentOffset = startOffset;
                    long totalBytesToRead = endOffset - startOffset + 1;
                    long totalBytesReadSoFar = 0;

                    while (totalBytesReadSoFar < totalBytesToRead)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int maxRead = (int)Math.Min(BufferSize, totalBytesToRead - totalBytesReadSoFar);
                        int bytesRead = await stream.ReadAsync(rentedBuffer.AsMemory(0, maxRead), cancellationToken).ConfigureAwait(false);

                        if (bytesRead == 0)
                        {
                            break; // Akış erken kapandıysa döngüden çıkıp retry ile kalan baytları isteyeceğiz
                        }

                        // Diske in-place yazım fonksiyonunu tetikle
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
