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
        using var headRequest = new HttpRequestMessage(HttpMethod.Head, url.Uri);
        try
        {
            using var response = await client.SendAsync(
                headRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return ExtractMetadata(response, url);
            }

            // Bazı sunucular HEAD isteğini reddeder (405 Method Not Allowed)
            if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                return await QueryViaRangeGetAsync(client, url, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (HttpRequestException)
        {
            // HEAD başarısız olursa GET fallback'i dene
            return await QueryViaRangeGetAsync(client, url, cancellationToken).ConfigureAwait(false);
        }

        // Eğer buraya gelinirse GET fallback ile tekrar sorgulayalım
        return await QueryViaRangeGetAsync(client, url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// HEAD isteği desteklenmediğinde 'Range: bytes=0-0' GET isteği atarak boyutu ve Range desteğini sorgular.
    /// </summary>
    private static async Task<FileMetadataDto> QueryViaRangeGetAsync(
        HttpClient client,
        DownloadUrl url,
        CancellationToken cancellationToken)
    {
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, url.Uri);
        getRequest.Headers.Range = new RangeHeaderValue(0, 0);

        using var response = await client.SendAsync(
            getRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        long? totalBytes = null;
        bool supportsRange = false;

        if (response.StatusCode == HttpStatusCode.PartialContent)
        {
            supportsRange = true;
            if (response.Content.Headers.ContentRange?.Length != null)
            {
                totalBytes = response.Content.Headers.ContentRange.Length;
            }
        }
        else
        {
            totalBytes = response.Content.Headers.ContentLength;
            supportsRange = response.Headers.AcceptRanges.Contains("bytes");
        }

        var fileName = ExtractFileName(response, url);
        var etag = response.Headers.ETag?.Tag;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        return new FileMetadataDto(totalBytes, supportsRange, fileName, etag, contentType);
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
