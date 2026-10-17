using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class ByteRangeTests
{
    [Fact]
    public void Constructor_WithValidRange_SetsPropertiesCorrectly()
    {
        var range = new ByteRange(0, 1023);

        Assert.Equal(0, range.Start);
        Assert.Equal(1023, range.End);
        Assert.Equal(1024, range.TotalBytes);
    }

    [Fact]
    public void Constructor_WithNegativeStart_ThrowsInvalidByteRangeException()
    {
        Assert.Throws<InvalidByteRangeException>(() => new ByteRange(-1, 100));
    }

    [Fact]
    public void Constructor_WithEndLessThanStart_ThrowsInvalidByteRangeException()
    {
        Assert.Throws<InvalidByteRangeException>(() => new ByteRange(100, 50));
    }

    [Fact]
    public void Contains_WithinRange_ReturnsTrue()
    {
        var range = new ByteRange(100, 200);

        Assert.True(range.Contains(100));
        Assert.True(range.Contains(150));
        Assert.True(range.Contains(200));
    }

    [Fact]
    public void Contains_OutsideRange_ReturnsFalse()
    {
        var range = new ByteRange(100, 200);

        Assert.False(range.Contains(99));
        Assert.False(range.Contains(201));
    }
}
