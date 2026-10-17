using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class SecurityVerdictTests
{
    private const string SampleHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Fact]
    public void Clean_Verdict_MarksAsSafeAndFormatsBadge()
    {
        var verdict = SecurityVerdict.Clean(SampleHash, harmlessVotes: 68, totalEngines: 70);

        Assert.True(verdict.IsScanned);
        Assert.True(verdict.IsSafe);
        Assert.False(verdict.HasThreat);
        Assert.Equal(0, verdict.MaliciousVotes);
        Assert.Equal(70, verdict.TotalEngines);
        Assert.Equal("70 antivirüs motorundan geçti, güvenli", verdict.FormatBadge());
    }

    [Fact]
    public void Infected_Verdict_MarksAsThreatAndFormatsBadge()
    {
        var verdict = SecurityVerdict.Infected(SampleHash, maliciousVotes: 5, totalEngines: 70);

        Assert.True(verdict.IsScanned);
        Assert.False(verdict.IsSafe);
        Assert.True(verdict.HasThreat);
        Assert.Equal(5, verdict.MaliciousVotes);
        Assert.Equal("5 antivirüs motoru zararlı uyarısı verdi!", verdict.FormatBadge());
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
