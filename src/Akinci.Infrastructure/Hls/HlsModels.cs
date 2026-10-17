namespace Akinci.Infrastructure.Hls;

/// <summary>
/// HLS akışındaki tek bir video/ses segmentini (.ts veya .m4s) temsil eder.
/// </summary>
public sealed record HlsSegment(
    int Index,
    double DurationSeconds,
    string Url,
    string? KeyUrl = null);

/// <summary>
/// Master playlist içindeki farklı kalite/çözünürlük akış varyantı (1080p, 720p vb.).
/// </summary>
public sealed record HlsVariant(
    int Bandwidth,
    string? Resolution,
    string Url);

/// <summary>
/// Ayrıştırılmış HLS M3U8 oynatma listesi.
/// </summary>
public sealed record HlsPlaylist(
    bool IsMaster,
    IReadOnlyList<HlsVariant> Variants,
    IReadOnlyList<HlsSegment> Segments,
    bool HasEndList,
    double TargetDuration)
{
    /// <summary>
    /// Eğer #EXT-X-ENDLIST etiketi yoksa bu bir canlı yayındır (Kick, Twitch, Canlı TV vb.).
    /// </summary>
    public bool IsLive => !HasEndList;

    /// <summary>
    /// Listedeki tüm parçaların toplam süresi (saniye).
    /// </summary>
    public double TotalDurationSeconds => Segments.Sum(s => s.DurationSeconds);
}
