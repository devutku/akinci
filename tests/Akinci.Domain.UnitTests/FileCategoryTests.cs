using Akinci.Domain.Enums;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class FileCategoryTests
{
    [Theory]
    [InlineData("video.mp4", FileCategory.Video)]
    [InlineData("movie.MKV", FileCategory.Video)]
    [InlineData("stream.m3u8", FileCategory.Video)]
    [InlineData("song.mp3", FileCategory.Audio)]
    [InlineData("track.flac", FileCategory.Audio)]
    [InlineData("report.pdf", FileCategory.Document)]
    [InlineData("document.docx", FileCategory.Document)]
    [InlineData("setup.exe", FileCategory.Program)]
    [InlineData("installer.msi", FileCategory.Program)]
    [InlineData("archive.zip", FileCategory.Archive)]
    [InlineData("backup.tar.gz", FileCategory.Archive)]
    [InlineData("archive.7z", FileCategory.Archive)]
    [InlineData("unknown.xyz123", FileCategory.Other)]
    [InlineData("noextension", FileCategory.Other)]
    [InlineData("", FileCategory.Other)]
    [InlineData(null, FileCategory.Other)]
    public void Detect_ReturnsExpectedCategory(string? filename, FileCategory expected)
    {
        var category = FileCategoryDetector.Detect(filename);
        Assert.Equal(expected, category);
    }
}
