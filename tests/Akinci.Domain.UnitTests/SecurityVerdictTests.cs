using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class SecurityVerdictTests
{
    private const string SampleHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Theory]
    [InlineData(68, 70)]
    [InlineData(72, 72)]
    [InlineData(50, 65)]
    public void Clean_Verdict_MarksAsSafeAndFormatsBadge(int harmlessVotes, int totalEngines)
    {
        var verdict = SecurityVerdict.Clean(SampleHash, harmlessVotes, totalEngines);

        Assert.True(verdict.IsScanned);
        Assert.True(verdict.IsSafe);
        Assert.False(verdict.HasThreat);
        Assert.Equal(0, verdict.MaliciousVotes);
        Assert.Equal(totalEngines, verdict.TotalEngines);
        Assert.Equal($"{harmlessVotes}/{totalEngines} antivirüs motorundan geçti, güvenli", verdict.FormatBadge());
    }

    [Theory]
    [InlineData(5, 70)]
    [InlineData(1, 68)]
    [InlineData(14, 75)]
    public void Infected_Verdict_MarksAsThreatAndFormatsBadge(int maliciousVotes, int totalEngines)
    {
        var verdict = SecurityVerdict.Infected(SampleHash, maliciousVotes, totalEngines);

        Assert.True(verdict.IsScanned);
        Assert.False(verdict.IsSafe);
        Assert.True(verdict.HasThreat);
        Assert.Equal(maliciousVotes, verdict.MaliciousVotes);
        Assert.Equal(totalEngines, verdict.TotalEngines);
        Assert.Equal($"{maliciousVotes}/{totalEngines} motor zararlı uyarısı verdi!", verdict.FormatBadge());
    }

    [Fact]
    public void NotFound_Verdict_FormatsInformativeBadge()
    {
        var verdict = SecurityVerdict.NotFound(SampleHash);

        Assert.True(verdict.IsScanned);
        Assert.False(verdict.HasThreat);
        Assert.Contains("VirusTotal veritabanında kaydı yok", verdict.FormatBadge());
    }
}
