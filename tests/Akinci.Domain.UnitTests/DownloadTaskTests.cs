using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class DownloadTaskTests
{
    private static DownloadTask CreateDefaultTask(long totalBytes = 1000, bool supportsRange = true)
    {
        return DownloadTask.Create(
            DownloadUrl.Create("https://example.com/testfile.iso"),
            @"C:\Downloads\testfile.iso",
            totalBytes,
            supportsRange);
    }

    [Fact]
    public void InitializeChunks_EvenDivision_CreatesExactNonOverlappingContinuousChunks()
    {
        var task = CreateDefaultTask(totalBytes: 1000, supportsRange: true);
        task.InitializeChunks(4);

        Assert.Equal(4, task.Chunks.Count);

        // Check continuity and no gaps/overlaps
        long expectedStart = 0;
        foreach (var chunk in task.Chunks)
        {
            Assert.Equal(expectedStart, chunk.Range.Start);
            Assert.Equal(250, chunk.Range.TotalBytes);
            expectedStart = chunk.Range.End + 1;
        }

        Assert.Equal(1000, expectedStart);
    }

    [Fact]
    public void InitializeChunks_UnevenDivision_DistributesRemainderProperly()
    {
        // 103 bytes split into 4 chunks: 26, 26, 26, 25 = 103 bytes total
        var task = CreateDefaultTask(totalBytes: 103, supportsRange: true);
        task.InitializeChunks(4);

        Assert.Equal(4, task.Chunks.Count);

        long totalCalculatedBytes = task.Chunks.Sum(c => c.Range.TotalBytes);
        Assert.Equal(103, totalCalculatedBytes);

        Assert.Equal(0, task.Chunks[0].Range.Start);
        Assert.Equal(25, task.Chunks[0].Range.End); // 26 bytes

        Assert.Equal(26, task.Chunks[1].Range.Start);
        Assert.Equal(51, task.Chunks[1].Range.End); // 26 bytes

        Assert.Equal(52, task.Chunks[2].Range.Start);
        Assert.Equal(77, task.Chunks[2].Range.End); // 26 bytes

        Assert.Equal(78, task.Chunks[3].Range.Start);
        Assert.Equal(102, task.Chunks[3].Range.End); // 25 bytes
    }

    [Fact]
    public void InitializeChunks_WhenRangeNotSupported_CreatesSingleChunk()
    {
        var task = CreateDefaultTask(totalBytes: 5000, supportsRange: false);
        task.InitializeChunks(8);

        Assert.Single(task.Chunks);
        Assert.Equal(0, task.Chunks[0].Range.Start);
        Assert.Equal(4999, task.Chunks[0].Range.End);
    }

    [Fact]
    public void StateTransitions_HappyPath_FollowsLifeCycle()
    {
        var task = CreateDefaultTask(totalBytes: 100, supportsRange: true);
        task.InitializeChunks(1);
        Assert.Equal(DownloadStatus.Pending, task.Status);

        task.Start();
        Assert.Equal(DownloadStatus.Downloading, task.Status);
        Assert.NotNull(task.StartedAt);

        task.Pause();
        Assert.Equal(DownloadStatus.Paused, task.Status);
        Assert.NotNull(task.PausedAt);

        task.Start();
        Assert.Equal(DownloadStatus.Downloading, task.Status);

        // Simulate complete download
        task.Chunks[0].AdvanceOffset(100);
        task.Complete();

        Assert.Equal(DownloadStatus.Completed, task.Status);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Complete_WhenChunksNotFinished_ThrowsDomainException()
    {
        var task = CreateDefaultTask(totalBytes: 100, supportsRange: true);
        task.InitializeChunks(2);
        task.Start();

        // Only first chunk finished
        task.Chunks[0].AdvanceOffset(50);

        Assert.Throws<DomainException>(() => task.Complete());
        Assert.Equal(DownloadStatus.Downloading, task.Status);
    }

    [Fact]
    public void InvalidStateTransition_ThrowsInvalidStateTransitionException()
    {
        var task = CreateDefaultTask(totalBytes: 100, supportsRange: true);
        task.InitializeChunks(1);
        task.Start();
        task.Chunks[0].AdvanceOffset(100);
        task.Complete();

        // Cannot pause completed task
        Assert.Throws<InvalidStateTransitionException>(() => task.Pause());
    }

    [Fact]
    public void ProgressCalculation_CalculatesAggregatedPercentage()
    {
        var task = CreateDefaultTask(totalBytes: 200, supportsRange: true);
        task.InitializeChunks(2); // 2 chunks of 100 bytes each

        task.Chunks[0].AdvanceOffset(50); // 50 bytes
        task.Chunks[1].AdvanceOffset(25); // 25 bytes

        Assert.Equal(75, task.DownloadedBytes);
        Assert.Equal(37.5, task.ProgressPercentage);
    }
}
