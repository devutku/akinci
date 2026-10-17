using System.Globalization;
using System.Text.RegularExpressions;

namespace Akinci.Infrastructure.Hls;

/// <summary>
/// HLS (.m3u8) Master ve Media oynatma listelerini ayrıştıran yüksek performanslı parser.
/// </summary>
public static class HlsPlaylistParser
{
    private static readonly Regex BandwidthRegex = new(@"BANDWIDTH=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ResolutionRegex = new(@"RESOLUTION=(\d+x\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static HlsPlaylist Parse(string m3u8Content, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(m3u8Content))
        {
            return new HlsPlaylist(false, Array.Empty<HlsVariant>(), Array.Empty<HlsSegment>(), true, 0);
        }

        var lines = m3u8Content
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        bool isMaster = lines.Any(l => l.StartsWith("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase));

        if (isMaster)
        {
            return ParseMasterPlaylist(lines, baseUri);
        }

        return ParseMediaPlaylist(lines, baseUri);
    }

    private static HlsPlaylist ParseMasterPlaylist(List<string> lines, Uri baseUri)
    {
        var variants = new List<HlsVariant>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
            {
                int bandwidth = 0;
                string? resolution = null;

                var bwMatch = BandwidthRegex.Match(line);
                if (bwMatch.Success && int.TryParse(bwMatch.Groups[1].Value, out var bw))
                {
                    bandwidth = bw;
                }

                var resMatch = ResolutionRegex.Match(line);
                if (resMatch.Success)
                {
                    resolution = resMatch.Groups[1].Value;
                }

                // Bir sonraki satır varyantın URL'idir
                if (i + 1 < lines.Count && !lines[i + 1].StartsWith("#"))
                {
                    var variantUrl = ResolveUrl(lines[i + 1], baseUri);
                    variants.Add(new HlsVariant(bandwidth, resolution, variantUrl));
                    i++;
                }
            }
        }

        // En yüksek çözünürlük / bant genişliğine göre sırala
        var sortedVariants = variants.OrderByDescending(v => v.Bandwidth).ToList();

        return new HlsPlaylist(
            IsMaster: true,
            Variants: sortedVariants,
            Segments: Array.Empty<HlsSegment>(),
            HasEndList: false,
            TargetDuration: 0);
    }

    private static HlsPlaylist ParseMediaPlaylist(List<string> lines, Uri baseUri)
    {
        var segments = new List<HlsSegment>();
        bool hasEndList = false;
        double targetDuration = 0;
        double currentSegmentDuration = 0;
        int segmentIndex = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.StartsWith("#EXT-X-ENDLIST", StringComparison.OrdinalIgnoreCase))
            {
                hasEndList = true;
                continue;
            }

            if (line.StartsWith("#EXT-X-TARGETDURATION:", StringComparison.OrdinalIgnoreCase))
            {
                var val = line.Substring("#EXT-X-TARGETDURATION:".Length).Trim();
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var td))
                {
                    targetDuration = td;
                }
                continue;
            }

            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                var val = line.Substring("#EXTINF:".Length).Split(',')[0].Trim();
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var dur))
                {
                    currentSegmentDuration = dur;
                }
                else
                {
                    currentSegmentDuration = targetDuration > 0 ? targetDuration : 4.0;
                }

                // Bir sonraki satır segmentin dosya yoludur (.ts / .m4s)
                if (i + 1 < lines.Count && !lines[i + 1].StartsWith("#"))
                {
                    var segUrl = ResolveUrl(lines[i + 1], baseUri);
                    segments.Add(new HlsSegment(segmentIndex++, currentSegmentDuration, segUrl));
                    i++;
                }
            }
        }

        return new HlsPlaylist(
            IsMaster: false,
            Variants: Array.Empty<HlsVariant>(),
            Segments: segments,
            HasEndList: hasEndList,
            TargetDuration: targetDuration);
    }

    private static string ResolveUrl(string pathOrUrl, Uri baseUri)
    {
        if (Uri.IsWellFormedUriString(pathOrUrl, UriKind.Absolute))
        {
            return pathOrUrl;
        }

        if (Uri.TryCreate(baseUri, pathOrUrl, out var combined))
        {
            return combined.AbsoluteUri;
        }

        return pathOrUrl;
    }
}
