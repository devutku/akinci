using System.Net;
using System.Security.Cryptography;
using System.Text;
using Akinci.Infrastructure.Security;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class SecurityIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly StreamingFileHashCalculator _calculator;

    public SecurityIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Akinci_Security_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _calculator = new StreamingFileHashCalculator();
    }

    [Fact]
    public async Task CalculateSha256Async_CalculatesExactHashFromStream()
    {
        var filePath = Path.Combine(_tempDirectory, "testfile.bin");
        byte[] payload = new byte[1024 * 512]; // 512 KB
        RandomNumberGenerator.Fill(payload);
        await File.WriteAllBytesAsync(filePath, payload);

        string expectedHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        string actualHash = await _calculator.CalculateSha256Async(filePath);

        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public async Task VirusTotalSecurityScanner_WithMockResponse_ParsesStatsCorrectly()
    {
        var mockHandler = new MockVirusTotalHttpMessageHandler();
        using var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://www.virustotal.com/") };
        var scanner = new VirusTotalSecurityScanner(httpClient, apiKey: "dummy-test-key");

        var verdict = await scanner.ScanHashAsync("275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f");

        Assert.True(verdict.IsScanned);
        Assert.Equal(68, verdict.HarmlessVotes);
        Assert.Equal(0, verdict.MaliciousVotes);
        Assert.True(verdict.IsSafe);
        Assert.Contains("güvenli", verdict.FormatBadge());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try { Directory.Delete(_tempDirectory, true); } catch { }
        }
    }

    private class MockVirusTotalHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string json = """
            {
                "data": {
                    "id": "275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f",
                    "type": "file",
                    "attributes": {
                        "last_analysis_stats": {
                            "harmless": 68,
                            "type-unsupported": 0,
                            "suspicious": 0,
                            "confirmed-timeout": 0,
                            "timeout": 0,
                            "failure": 0,
                            "malicious": 0,
                            "undetected": 2
                        }
                    }
                }
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
