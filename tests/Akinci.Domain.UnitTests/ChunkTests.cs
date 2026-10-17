using Akinci.Domain.Entities;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class ChunkTests
{
    [Fact]
    public void Constructor_InitializesStateCorrectly()
    {
        var range = new ByteRange(100, 199);
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        Assert.Equal(0, chunk.Index);
        Assert.Equal(100, chunk.CurrentOffset);
        Assert.Equal(0, chunk.DownloadedBytes);
        Assert.Equal(100, chunk.RemainingBytes);
        Assert.False(chunk.IsCompleted);
        Assert.Equal(0.0, chunk.ProgressPercentage);
    }

    [Fact]
    public void AdvanceOffset_WithValidBytes_IncrementsCurrentOffsetAndDownloadedBytes()
    {
        var range = new ByteRange(0, 99); // 100 bytes
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        chunk.AdvanceOffset(40);

        Assert.Equal(40, chunk.CurrentOffset);
        Assert.Equal(40, chunk.DownloadedBytes);
        Assert.Equal(60, chunk.RemainingBytes);
        Assert.Equal(40.0, chunk.ProgressPercentage);
        Assert.False(chunk.IsCompleted);
    }

    [Fact]
    public void AdvanceOffset_UntilEnd_MarksChunkAsCompleted()
    {
        var range = new ByteRange(0, 99); // 100 bytes
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        chunk.AdvanceOffset(100);

        Assert.Equal(100, chunk.CurrentOffset);
        Assert.Equal(100, chunk.DownloadedBytes);
        Assert.Equal(0, chunk.RemainingBytes);
        Assert.Equal(100.0, chunk.ProgressPercentage);
        Assert.True(chunk.IsCompleted);
    }

    [Fact]
    public void AdvanceOffset_PastRangeEnd_ThrowsInvalidByteRangeException()
    {
        var range = new ByteRange(0, 99);
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        Assert.Throws<InvalidByteRangeException>(() => chunk.AdvanceOffset(101));
    }

    [Fact]
    public void AdvanceOffset_WhenAlreadyCompleted_ThrowsDomainException()
    {
        var range = new ByteRange(0, 49);
        var chunk = new Chunk(Guid.NewGuid(), 0, range);
        chunk.AdvanceOffset(50); // Completed

        Assert.Throws<DomainException>(() => chunk.AdvanceOffset(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AdvanceOffset_WithZeroOrNegativeBytes_ThrowsDomainException(long invalidBytes)
    {
        var range = new ByteRange(0, 99);
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        Assert.Throws<DomainException>(() => chunk.AdvanceOffset(invalidBytes));
    }

    [Fact]
    public void SetOffset_WithValidOffset_UpdatesOffset()
    {
        var range = new ByteRange(100, 199);
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        chunk.SetOffset(150);

        Assert.Equal(150, chunk.CurrentOffset);
        Assert.Equal(50, chunk.DownloadedBytes);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(201)]
    public void SetOffset_OutsideValidRange_ThrowsInvalidByteRangeException(long invalidOffset)
    {
        var range = new ByteRange(100, 199);
        var chunk = new Chunk(Guid.NewGuid(), 0, range);

        Assert.Throws<InvalidByteRangeException>(() => chunk.SetOffset(invalidOffset));
    }
}
