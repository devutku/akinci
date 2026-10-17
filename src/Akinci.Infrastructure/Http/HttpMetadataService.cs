using System.Net;
using System.Net.Http.Headers;
using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.ValueObjects;

namespace Akinci.Infrastructure.Http;

/// <summary>
/// SocketsHttpHandler kullanarak HTTP/HTTPS sunucularından dosya üstverisini ve Range desteğini sorgulayan servis.
/// </summary>
public sealed class HttpMetadataService : IHttpMetadataService
{
    public async Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        using var client = HttpHandlerFactory.CreateClient(credentials);

        // 1. Önce hafif bir HEAD isteği deneyelim
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url.Uri);
            using var response = await client.SendAsync(
                headRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var meta = ExtractMetadata(response, url);
                // Sunucu HEAD isteğine gerçekten pozitif bir boyut döndüyse bunu kullanabiliriz
                if (meta.ContentLength.HasValue && meta.ContentLength.Value > 0)
                {
                    return meta;
                }
            }
        }
        catch
        {
            // HEAD başarısız olursa veya engellenirse doğrudan GET fallback'e geç
        }

        // 2. HEAD boyutu vermediyse veya engellendiyse: ResponseHeadersRead ile GET isteği at (dosyayı indirmeden sadece başlıkları oku)
        return await QueryViaGetHeadersAsync(client, url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// HEAD isteği dosya boyutu vermediğinde veya engellendiğinde, gövdeyi (dosyayı) indirmeden
    /// yalnızca HTTP başlıkları geldiği an dönen hafif GET isteğiyle boyutu ve Range desteğini çeker.
    /// </summary>
    private static async Task<FileMetadataDto> QueryViaGetHeadersAsync(
        HttpClient client,
        DownloadUrl url,
        CancellationToken cancellationToken)
    {
        // Adım A: Önce 'Range: bytes=0-0' ile dene (Boyutu ve Range desteğini aynı anda anlamak için en hafif yöntem)
        try
        {
            using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url.Uri);
            rangeRequest.Headers.Range = new RangeHeaderValue(0, 0);

            using var response = await client.SendAsync(
                rangeRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                long? totalBytes = null;
                bool supportsRange = false;

                if (response.StatusCode == HttpStatusCode.PartialContent)
                {
                    supportsRange = true;
                    totalBytes = response.Content.Headers.ContentRange?.Length;
                }
                else
                {
                    totalBytes = response.Content.Headers.ContentLength;
                    supportsRange = response.Headers.AcceptRanges.Contains("bytes");
                }

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    var fileName = ExtractFileName(response, url);
                    var etag = response.Headers.ETag?.Tag;
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    return new FileMetadataDto(totalBytes, supportsRange, fileName, etag, contentType);
                }
            }
        }
        catch
        {
            // Range GET başarısız olursa düz GET'e geç
        }

        // Adım B: Düz standart GET isteği (ResponseHeadersRead: Gövdeyi indirmeden sadece HTTP başlıklarını okur)
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, url.Uri);
        using var getResponse = await client.SendAsync(
            getRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        getResponse.EnsureSuccessStatusCode();

        long? length = getResponse.Content.Headers.ContentLength;
        bool canRange = getResponse.Headers.AcceptRanges.Contains("bytes");
        var name = ExtractFileName(getResponse, url);
        var tag = getResponse.Headers.ETag?.Tag;
        var type = getResponse.Content.Headers.ContentType?.MediaType;

        return new FileMetadataDto(length, canRange, name, tag, type);
    }

    private static FileMetadataDto ExtractMetadata(HttpResponseMessage response, DownloadUrl url)
    {
        long? contentLength = response.Content.Headers.ContentLength;
        bool supportsRange = response.Headers.AcceptRanges.Contains("bytes");
        var fileName = ExtractFileName(response, url);
        var etag = response.Headers.ETag?.Tag;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        return new FileMetadataDto(contentLength, supportsRange, fileName, etag, contentType);
    }

    private static string? ExtractFileName(HttpResponseMessage response, DownloadUrl url)
    {
        // 1. Content-Disposition başlığında filename* veya filename var mı?
        var disposition = response.Content.Headers.ContentDisposition;
        if (disposition != null)
        {
            if (!string.IsNullOrWhiteSpace(disposition.FileNameStar))
            {
                return disposition.FileNameStar.Trim('\"');
            }

            if (!string.IsNullOrWhiteSpace(disposition.FileName))
            {
                return disposition.FileName.Trim('\"');
            }
        }

        // 2. URL'den çıkar
        return url.GetFileNameFromUrl();
    }
}
