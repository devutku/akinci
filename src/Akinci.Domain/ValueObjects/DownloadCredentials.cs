using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;

namespace Akinci.Domain.ValueObjects;

/// <summary>
/// İndirme istekleri için kimlik doğrulama bilgilerini (Basic, NTLM, Kerberos, Negotiate) temsil eden Değer Nesnesi.
/// </summary>
public sealed record DownloadCredentials
{
    public AuthenticationType Type { get; }
    public string? UserName { get; }
    public string? Password { get; }
    public string? Domain { get; }
    public bool UseDefaultCredentials { get; }

    public static readonly DownloadCredentials Anonymous = new(AuthenticationType.None, null, null, null, false);

    private DownloadCredentials(
        AuthenticationType type,
        string? userName,
        string? password,
        string? domain,
        bool useDefaultCredentials)
    {
        Type = type;
        UserName = userName;
        Password = password;
        Domain = domain;
        UseDefaultCredentials = useDefaultCredentials;
    }

    /// <summary>
    /// Standart Basic (Kullanıcı Adı / Parola) doğrulaması oluşturur (HTTP Basic & FTP girişi).
    /// </summary>
    public static DownloadCredentials Basic(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("Basic kimlik doğrulama için kullanıcı adı boş olamaz.");
        }

        return new DownloadCredentials(AuthenticationType.Basic, userName.Trim(), password ?? string.Empty, null, false);
    }

    /// <summary>
    /// NTLM Windows ağ doğrulaması oluşturur.
    /// </summary>
    public static DownloadCredentials Ntlm(string userName, string password, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("NTLM kimlik doğrulama için kullanıcı adı boş olamaz.");
        }

        return new DownloadCredentials(AuthenticationType.Ntlm, userName.Trim(), password ?? string.Empty, domain?.Trim(), false);
    }

    /// <summary>
    /// Kerberos doğrulaması oluşturur.
    /// </summary>
    public static DownloadCredentials Kerberos(string userName, string password, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("Kerberos kimlik doğrulama için kullanıcı adı veya kullanıcı anahtarı boş olamaz.");
        }

        return new DownloadCredentials(AuthenticationType.Kerberos, userName.Trim(), password ?? string.Empty, domain?.Trim(), false);
    }

    /// <summary>
    /// Negotiate (SPNEGO - Kerberos/NTLM uzlaşması) doğrulaması oluşturur.
    /// </summary>
    public static DownloadCredentials Negotiate(string userName, string password, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("Negotiate kimlik doğrulama için kullanıcı adı boş olamaz.");
        }

        return new DownloadCredentials(AuthenticationType.Negotiate, userName.Trim(), password ?? string.Empty, domain?.Trim(), false);
    }

    /// <summary>
    /// İşletim sisteminde oturum açmış olan mevcut Windows kullanıcısının kimlik bilgilerini otomatik kullanır.
    /// </summary>
    public static DownloadCredentials WindowsDefault(AuthenticationType type = AuthenticationType.Negotiate)
    {
        return new DownloadCredentials(type, null, null, null, true);
    }

    public override string ToString()
    {
        if (Type == AuthenticationType.None) return "Anonymous";
        if (UseDefaultCredentials) return $"{Type} (Default OS Credentials)";
        var domainPrefix = string.IsNullOrEmpty(Domain) ? "" : $"{Domain}\\";
        return $"{Type} ({domainPrefix}{UserName})";
    }
}
