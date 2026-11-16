namespace Akinci.Domain.Exceptions;

/// <summary>
/// İndirme URL'inin neden reddedildiği. Arayüz bu koda göre kendi dilinde mesaj gösterir.
/// </summary>
public enum InvalidDownloadUrlReason
{
    Empty,
    InvalidFormat,
    UnsupportedScheme
}

/// <summary>
/// İndirme URL'i boş, biçimsiz veya desteklenmeyen bir şemaya sahip olduğunda fırlatılır.
/// </summary>
public sealed class InvalidDownloadUrlException : DomainException
{
    public InvalidDownloadUrlReason Reason { get; }

    /// <summary>Kullanıcının girdiği URL (boş olabilir).</summary>
    public string? Url { get; }

    /// <summary>Yalnızca <see cref="InvalidDownloadUrlReason.UnsupportedScheme"/> için dolu.</summary>
    public string? Scheme { get; }

    private InvalidDownloadUrlException(InvalidDownloadUrlReason reason, string? url, string? scheme, string message)
        : base(message)
    {
        Reason = reason;
        Url = url;
        Scheme = scheme;
    }

    public static InvalidDownloadUrlException Empty() =>
        new(InvalidDownloadUrlReason.Empty, null, null, "İndirme URL'i boş olamaz.");

    public static InvalidDownloadUrlException InvalidFormat(string url) =>
        new(InvalidDownloadUrlReason.InvalidFormat, url, null, $"Geçersiz URL formatı: '{url}'.");

    public static InvalidDownloadUrlException UnsupportedScheme(string url, string scheme) =>
        new(InvalidDownloadUrlReason.UnsupportedScheme, url, scheme,
            $"Desteklenmeyen URL şeması: '{scheme}'. Yalnızca HTTP, HTTPS, FTP ve FTPS desteklenir.");
}
