using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.Ports;

/// <summary>
/// Belirli bir Chunk'ı HTTP Range protokolü üzerinden indiren port arayüzü.
/// </summary>
public interface IChunkDownloader
{
    /// <summary>
    /// Chunk'ın mevcut ofsetinden aralık sonuna kadar olan veriyi parça parça indirir
    /// ve her okunan bayt bloğunu (ofset, veri) callback fonksiyonuna iletir.
    /// </summary>
    /// <param name="url">İndirme bağlantısı</param>
    /// <param name="chunk">İndirilecek parça varlığı</param>
    /// <param name="onDataReceived">Okunan her blokta çalıştırılacak fonksiyon: (yazılacakOfset, veri, iptalJetonu)</param>
    /// <param name="credentials">Kimlik doğrulama bilgileri (Basic, NTLM, Kerberos, Negotiate)</param>
    /// <param name="cancellationToken">İptal jetonu</param>
    Task DownloadChunkAsync(
        DownloadUrl url,
        Chunk chunk,
        Func<long, ReadOnlyMemory<byte>, CancellationToken, Task> onDataReceived,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default);
}
