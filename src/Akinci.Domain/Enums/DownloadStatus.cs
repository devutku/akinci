namespace Akinci.Domain.Enums;

/// <summary>
/// İndirme görevinin yaşam döngüsü durumları.
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// İndirme görevi oluşturuldu, başlatılmayı bekliyor.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// İndirme aktif olarak devam ediyor.
    /// </summary>
    Downloading = 1,

    /// <summary>
    /// Kullanıcı veya sistem tarafından duraklatıldı.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Tüm parçalar başarıyla indirildi ve doğrulandı.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Bir ağ veya disk hatası nedeniyle durdu.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Kullanıcı tarafından iptal edildi.
    /// </summary>
    Canceled = 5
}
