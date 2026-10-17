using Akinci.Domain.Exceptions;

namespace Akinci.Domain.ValueObjects;

/// <summary>
/// Doğrulanmış bir indirme URL'ini temsil eden Değer Nesnesi (Value Object).
/// Yalnızca HTTP ve HTTPS şemalarını kabul eder.
/// </summary>
public sealed record DownloadUrl
{
    public string Value { get; }
    public Uri Uri { get; }

    private DownloadUrl(Uri uri)
    {
        Uri = uri;
        Value = uri.AbsoluteUri;
    }

    public static DownloadUrl Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("İndirme URL'i boş olamaz.");
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            throw new DomainException($"Geçersiz URL formatı: '{url}'.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new DomainException($"Desteklenmeyen URL şeması: '{uri.Scheme}'. Yalnızca HTTP ve HTTPS desteklenir.");
        }

        return new DownloadUrl(uri);
    }

    public string GetFileNameFromUrl()
    {
        var path = Uri.AbsolutePath;
        var fileName = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(fileName) ? "download.bin" : fileName;
    }

    public override string ToString() => Value;
}
