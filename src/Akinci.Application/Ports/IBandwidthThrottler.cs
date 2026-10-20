namespace Akinci.Application.Ports;

/// <summary>
/// Küresel veya görev bazlı indirme bant genişliğini (hızını) sınırlayan port arayüzü.
/// </summary>
public interface IBandwidthThrottler
{
    /// <summary>
    /// Saniye başına izin verilen maksimum bayt miktarı (0 veya negatif ise sınırsız).
    /// </summary>
    long LimitBytesPerSecond { get; set; }

    /// <summary>
    /// Saniye başına hız limitini günceller.
    /// </summary>
    void SetLimit(long limitBytesPerSecond);

    /// <summary>
    /// Alınan bayt miktarına göre gerekirse akışı asenkron olarak geciktirir (hız sınırlar).
    /// </summary>
    ValueTask ThrottleAsync(int bytes, CancellationToken cancellationToken = default);
}
