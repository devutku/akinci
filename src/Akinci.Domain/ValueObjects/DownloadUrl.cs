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

    private static readonly HashSet<string> SupportedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        Uri.UriSchemeFtp,
        "ftps"
    };

    public bool IsHttp => Uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                          Uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    public bool IsFtp => Uri.Scheme.Equals(Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase) ||
                         Uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase);

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

        if (!SupportedSchemes.Contains(uri.Scheme))
        {
            throw new DomainException($"Desteklenmeyen URL şeması: '{uri.Scheme}'. Yalnızca HTTP, HTTPS, FTP ve FTPS desteklenir.");
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
