using System.Globalization;
using Akinci.Domain.ValueObjects;
using Akinci.UI.Localization;

namespace Akinci.UI.UnitTests;

/// <summary>
/// <see cref="Loc.Current"/> statik olduğu için dili değiştiren testler aynı koleksiyonda sırayla çalışır.
/// </summary>
[CollectionDefinition("Localization", DisableParallelization = true)]
public sealed class LocalizationCollection;

[Collection("Localization")]
public sealed class LocalizationTests : IDisposable
{
    private const string SampleHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    public void Dispose() => Loc.Initialize(Loc.TurkishCode);

    [Theory]
    [InlineData("tr", "en-US", AppLanguage.Turkish)]
    [InlineData("en", "tr-TR", AppLanguage.English)]
    [InlineData(null, "tr-TR", AppLanguage.Turkish)]
    [InlineData(null, "en-US", AppLanguage.English)]
    [InlineData(null, "de-DE", AppLanguage.English)]
    [InlineData("xx", "tr-TR", AppLanguage.Turkish)]
    public void Resolve_UsesSettingFirstThenWindowsLanguage(string? setting, string systemCulture, AppLanguage expected)
    {
        Assert.Equal(expected, Loc.Resolve(setting, new CultureInfo(systemCulture)));
    }

    [Fact]
    public void Strings_FollowSelectedLanguage()
    {
        Loc.Initialize(Loc.EnglishCode);
        Assert.Equal("Settings", Strings.Settings);
        Assert.Equal("3 connections", Strings.ConnectionCount(3));
        Assert.Equal("1 connection", Strings.ConnectionCount(1));

        Loc.Initialize(Loc.TurkishCode);
        Assert.Equal("Ayarlar", Strings.Settings);
        Assert.Equal("3 bağlantı", Strings.ConnectionCount(3));
    }

    [Fact]
    public void SecurityBadge_IsWrittenInSelectedLanguage()
    {
        var noKey = SecurityVerdict.NoApiKey(SampleHash);
        var infected = SecurityVerdict.Infected(SampleHash, maliciousVotes: 5, totalEngines: 70);

        Loc.Initialize(Loc.EnglishCode);
        Assert.Equal("No VirusTotal API key (scan skipped)", SecurityVerdictText.Format(noKey));
        Assert.Equal("5/70 engines flagged this file!", SecurityVerdictText.Format(infected));
        Assert.Equal("Awaiting scan", SecurityVerdictText.Format(null));

        Loc.Initialize(Loc.TurkishCode);
        Assert.Equal("VirusTotal API anahtarı girilmedi (Tarama atlandı)", SecurityVerdictText.Format(noKey));
        Assert.Equal("5/70 motor zararlı uyarısı verdi!", SecurityVerdictText.Format(infected));
    }

    [Fact]
    public void SecurityBadge_InTurkish_MatchesCoreBadgeForScannedResults()
    {
        Loc.Initialize(Loc.TurkishCode);

        var clean = SecurityVerdict.Clean(SampleHash, harmlessVotes: 68, totalEngines: 70);
        var notFound = SecurityVerdict.NotFound(SampleHash);
        var noEngines = new SecurityVerdict(SampleHash, isScanned: true);

        Assert.Equal(clean.FormatBadge(), SecurityVerdictText.Format(clean));
        Assert.Equal(notFound.FormatBadge(), SecurityVerdictText.Format(notFound));
        Assert.Equal(noEngines.FormatBadge(), SecurityVerdictText.Format(noEngines));
    }
}
