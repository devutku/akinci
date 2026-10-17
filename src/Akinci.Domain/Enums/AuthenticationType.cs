namespace Akinci.Domain.Enums;

/// <summary>
/// İndirme isteklerinde desteklenen kimlik doğrulama mekanizmaları.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// Kimlik doğrulama yok (Anonim).
    /// </summary>
    None = 0,

    /// <summary>
    /// HTTP Basic (Base64 kodlu Kullanıcı Adı/Şifre) ve FTP standart kullanıcı girişi.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Windows NT LAN Manager (NTLM) Challenge-Response doğrulaması.
    /// </summary>
    Ntlm = 2,

    /// <summary>
    /// Ticket tabanlı Kerberos biletli doğrulaması.
    /// </summary>
    Kerberos = 3,

    /// <summary>
    /// SPNEGO (Simple and Protected GSSAPI Negotiation) - İstemci ve sunucunun Kerberos veya NTLM üzerinde uzlaşması.
    /// </summary>
    Negotiate = 4
}
