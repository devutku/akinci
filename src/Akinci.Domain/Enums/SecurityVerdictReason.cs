namespace Akinci.Domain.Enums;

/// <summary>
/// Güvenlik taramasının neden sayılarla ifade edilemediğini belirten dil bağımsız kod.
/// Arayüz, kullanıcıya gösterilecek metni bu koddan kendi dilinde üretir.
/// Değerler veritabanına kaydedildiği için değiştirilmemelidir.
/// </summary>
public enum SecurityVerdictReason
{
    /// <summary>
    /// Özel bir durum yok: sonuç motor sayılarıyla (veya "henüz taranmadı" ile) ifade edilir.
    /// </summary>
    None = 0,

    /// <summary>
    /// Dosya VirusTotal veritabanında bulunamadı (bilinmeyen/yeni dosya).
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// VirusTotal API anahtarı girilmediği için tarama atlandı.
    /// </summary>
    NoApiKey = 2,

    /// <summary>
    /// VirusTotal API istek limiti aşıldı.
    /// </summary>
    RateLimited = 3,

    /// <summary>
    /// VirusTotal yanıtı beklenen biçimde değildi.
    /// </summary>
    InvalidResponse = 4,

    /// <summary>
    /// VirusTotal'a bağlanılamadı.
    /// </summary>
    ConnectionError = 5
}
