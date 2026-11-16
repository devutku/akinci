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

    public bool IsHls => IsHttp && (Uri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
                                    Uri.Query.Contains(".m3u8", StringComparison.OrdinalIgnoreCase));

    public static DownloadUrl Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw InvalidDownloadUrlException.Empty();
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            throw InvalidDownloadUrlException.InvalidFormat(url);
        }

        if (!SupportedSchemes.Contains(uri.Scheme))
        {
            throw InvalidDownloadUrlException.UnsupportedScheme(url, uri.Scheme);
        }

        return new DownloadUrl(uri);
    }

    public string GetFileNameFromUrl()
    {
        var path = Uri.AbsolutePath;
        var fileName = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return IsHls ? "stream_recording.ts" : "download.bin";
        }

        if (IsHls && fileName.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            return Path.ChangeExtension(fileName, ".ts");
        }

        return fileName;
    }

    public override string ToString() => Value;
}
