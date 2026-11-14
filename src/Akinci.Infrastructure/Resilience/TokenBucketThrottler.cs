using System.Diagnostics;
using Akinci.Application.Ports;

namespace Akinci.Infrastructure.Resilience;

/// <summary>
/// Yüksek eşzamanlı parçalı indirmelerde bant genişliğini CPU dostu Token Bucket algoritmasıyla sınırlayan servis.
/// </summary>
public sealed class TokenBucketThrottler : IBandwidthThrottler
{
    private long _limitBytesPerSecond;
    private double _availableTokens;
    private long _lastRefillTicks;
    private readonly object _lock = new();

    public long LimitBytesPerSecond
    {
        get => Interlocked.Read(ref _limitBytesPerSecond);
        set
        {
            Interlocked.Exchange(ref _limitBytesPerSecond, Math.Max(0, value));
            lock (_lock)
            {
                _availableTokens = Math.Min(_availableTokens, Math.Max(0, value));
            }
        }
    }

    public void SetLimit(long limitBytesPerSecond)
    {
        LimitBytesPerSecond = limitBytesPerSecond;
    }

    public TokenBucketThrottler(long limitBytesPerSecond = 0)
    {
        _limitBytesPerSecond = Math.Max(0, limitBytesPerSecond);
        _availableTokens = _limitBytesPerSecond;
        _lastRefillTicks = Stopwatch.GetTimestamp();
    }

    public async ValueTask ThrottleAsync(int bytes, CancellationToken cancellationToken = default)
    {
        long remaining = bytes;

        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Limit çalışma sırasında değişebilir (ör. 0 = sınırsız yapılırsa hemen çık)
            var limit = LimitBytesPerSecond;
            if (limit <= 0)
            {
                return;
            }

            // Kova kapasitesi saniyelik limit kadardır; limitten büyük istekler (128 KB tampon,
            // tüm HLS segmenti vb.) asla tek seferde karşılanamaz. Bu yüzden isteği en fazla
            // kova kapasitesi kadar dilimlerle tüketiyoruz.
            long portion = Math.Min(remaining, limit);
            int delayMs = 0;

            lock (_lock)
            {
                var now = Stopwatch.GetTimestamp();
                double elapsedSeconds = (double)(now - _lastRefillTicks) / Stopwatch.Frequency;
                if (elapsedSeconds > 0)
                {
                    double tokensToAdd = elapsedSeconds * limit;
                    _availableTokens = Math.Min(limit, _availableTokens + tokensToAdd);
                    _lastRefillTicks = now;
                }

                if (_availableTokens >= portion)
                {
                    _availableTokens -= portion;
                    remaining -= portion;
                    continue;
                }

                // Yetersiz token: Ne kadar beklemeliyiz?
                double needed = portion - _availableTokens;
                delayMs = (int)Math.Ceiling(needed / limit * 1000.0);
                delayMs = Math.Clamp(delayMs, 1, 500);
            }

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
