using Akinci.Domain.Entities;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class WorkStealingTests
{
    [Fact]
    public void TryStealChunk_WhenLargeRemaining_SplitsSuccessfullyWithoutOverlapOrGaps()
    {
        // 20 MB dosya
        long totalBytes = 20 * 1024 * 1024;
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/largefile.iso"),
            @"C:\downloads\largefile.iso",
            totalBytes,
            supportsRangeRequests: true);

        task.InitializeChunks(1); // 1 büyük parça: 0 - 20971519

        var initialChunk = task.Chunks[0];
        long originalEnd = initialChunk.Range.End;

        // 2 MB indirilmiş olsun
        initialChunk.AdvanceOffset(2 * 1024 * 1024);

        // Work stealing ile parçayı böl
        var stolenChunk = task.TryStealChunk(minSplitThreshold: 2 * 1024 * 1024);

        Assert.NotNull(stolenChunk);
        Assert.Equal(2, task.Chunks.Count);

        // Kesinlikle örtüşmeyen ve bayt kaçırmayan aralıklar doğrulaması
        Assert.Equal(0, initialChunk.Range.Start);
        Assert.Equal(stolenChunk.Range.Start - 1, initialChunk.Range.End);
        Assert.Equal(originalEnd, stolenChunk.Range.End);
        Assert.Equal(stolenChunk.Range.Start, stolenChunk.CurrentOffset);

        // Toplam parça baytlarının toplam dosya boyutuna eşit olduğunu doğrula
        Assert.Equal(totalBytes, initialChunk.Range.TotalBytes + stolenChunk.Range.TotalBytes);
    }

    [Fact]
    public void TryStealChunk_WhenRemainingBelowThreshold_ReturnsNull()
    {
        long totalBytes = 1024 * 1024; // 1 MB (Threshold 2 MB'den küçük)
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/smallfile.zip"),
            @"C:\downloads\smallfile.zip",
            totalBytes,
            supportsRangeRequests: true);

        task.InitializeChunks(1);

        var stolenChunk = task.TryStealChunk(minSplitThreshold: 2 * 1024 * 1024);

        Assert.Null(stolenChunk);
        Assert.Single(task.Chunks);
    }

    [Fact]
    public void TryStealChunk_WhenRangeRequestsNotSupported_ReturnsNull()
    {
        long totalBytes = 50 * 1024 * 1024;
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/stream.bin"),
            @"C:\downloads\stream.bin",
            totalBytes,
            supportsRangeRequests: false);

        task.InitializeChunks(1);

        var stolenChunk = task.TryStealChunk(minSplitThreshold: 2 * 1024 * 1024);

        Assert.Null(stolenChunk);
    }

    [Fact]
    public void UpdateEndOffset_WhenInvalidOffset_ThrowsDomainException()
    {
        var chunk = new Chunk(Guid.NewGuid(), 0, new ByteRange(0, 1000));
        chunk.AdvanceOffset(500); // CurrentOffset = 500

        // Mevcut ofsetin gerisine (400) daraltmaya çalışmak hata vermeli
        Assert.Throws<DomainException>(() => chunk.UpdateEndOffset(400));

        // Mevcut bitişten (1000) daha ileriye veya eşite genişletmeye çalışmak hata vermeli
        Assert.Throws<DomainException>(() => chunk.UpdateEndOffset(1000));
        Assert.Throws<DomainException>(() => chunk.UpdateEndOffset(1200));

        // Geçerli daraltma (750)
        chunk.UpdateEndOffset(750);
        Assert.Equal(750, chunk.Range.End);
    }
}
