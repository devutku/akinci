using System.Diagnostics;
using Akinci.Infrastructure.Resilience;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class TokenBucketThrottlerTests
{
    [Fact]
    public async Task ThrottleAsync_WhenLimitIsZero_DoesNotDelay()
    {
        var throttler = new TokenBucketThrottler(0);
        var sw = Stopwatch.StartNew();

        await throttler.ThrottleAsync(1_000_000); // 1 MB
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task ThrottleAsync_WhenLimitIsSet_ThrottlesProperly()
    {
        // 50 KB/s hız limiti
        long limitBps = 50 * 1024;
        var throttler = new TokenBucketThrottler(limitBps);

        // İlk token kovasını tüketelim
        await throttler.ThrottleAsync((int)limitBps);

        var sw = Stopwatch.StartNew();
        // 25 KB daha veri iste -> yaklaşık 500 ms beklemesi gerekir
        await throttler.ThrottleAsync((int)(limitBps / 2));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 350, $"Bekleme süresi en az 350ms olmalıydı ancak {sw.ElapsedMilliseconds}ms sürdü.");
    }

    [Fact]
    public async Task ThrottleAsync_WhenDynamicLimitUpdated_ReflectsNewRate()
    {
        var throttler = new TokenBucketThrottler(10 * 1024); // 10 KB/s
        Assert.Equal(10 * 1024, throttler.LimitBytesPerSecond);

        // Hız limitini 0 yap (sınırsız)
        throttler.SetLimit(0);
        Assert.Equal(0, throttler.LimitBytesPerSecond);

        var sw = Stopwatch.StartNew();
        await throttler.ThrottleAsync(500_000);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task ThrottleAsync_WhenCancellationTokenCanceled_ThrowsOperationCanceledException()
    {
        var throttler = new TokenBucketThrottler(1024); // Çok yavaş: 1 KB/s
        // Kovayı boşalt
        await throttler.ThrottleAsync(1024);

        // Limit içinde kalan bir istek (~1 sn bekleme gerektirir); 50 ms sonra iptal edilmeli
        using var cts = new CancellationTokenSource(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await throttler.ThrottleAsync(1024, cts.Token);
        });
    }

    [Fact]
    public async Task ThrottleAsync_WhenRequestExceedsLimit_CompletesAtLimitedRate()
    {
        // HTTP/FTP indiricileri 128 KB'a kadar, HLS ise tüm segmenti tek seferde iletir.
        // Saniyelik limitten büyük istekler sonsuza dek beklememeli, limit hızında tamamlanmalı.
        long limitBps = 100 * 1024; // 100 KB/s
        var throttler = new TokenBucketThrottler(limitBps);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var sw = Stopwatch.StartNew();

        // Kova dolu (100 KB) -> kalan 150 KB yaklaşık 1.5 sn bekleme gerektirir
        await throttler.ThrottleAsync((int)(limitBps * 5 / 2), cts.Token);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 1000, $"İstek sınırlanmadan geçti: {sw.ElapsedMilliseconds}ms");
    }
}
