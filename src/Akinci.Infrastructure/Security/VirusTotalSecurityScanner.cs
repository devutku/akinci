using System.Net;
using System.Text.Json;
using Akinci.Application.Ports;
using Akinci.Domain.ValueObjects;

namespace Akinci.Infrastructure.Security;

/// <summary>
/// VirusTotal v3 REST API (GET /api/v3/files/{hash}) üzerinden dosya güvenlik taraması yapan servis.
/// </summary>
public sealed class VirusTotalSecurityScanner : IFileSecurityScanner
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public VirusTotalSecurityScanner(HttpClient? httpClient = null, string? apiKey = null)
    {
        _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri("https://www.virustotal.com/") };
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");
    }

    public async Task<SecurityVerdict> ScanHashAsync(string sha256Hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sha256Hash))
        {
            throw new ArgumentNullException(nameof(sha256Hash));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new SecurityVerdict(
                sha256Hash,
                isScanned: false,
                customMessage: "VirusTotal API anahtarı girilmedi (Tarama atlandı)");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/files/{sha256Hash}");
        request.Headers.Add("x-apikey", _apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Dosya VirusTotal veritabanında henüz görülmemiş
                return SecurityVerdict.NotFound(sha256Hash);
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                return new SecurityVerdict(
                    sha256Hash,
                    isScanned: false,
                    customMessage: "VirusTotal API istek limiti aşıldı (Rate limit)");
            }

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("attributes", out var attributes) ||
                !attributes.TryGetProperty("last_analysis_stats", out var stats))
            {
                return new SecurityVerdict(sha256Hash, isScanned: false, customMessage: "Yanıt formatı çözümlenemedi");
            }

            int malicious = stats.TryGetProperty("malicious", out var malProp) ? malProp.GetInt32() : 0;
            int suspicious = stats.TryGetProperty("suspicious", out var suspProp) ? suspProp.GetInt32() : 0;
            int harmless = stats.TryGetProperty("harmless", out var harmProp) ? harmProp.GetInt32() : 0;
            int undetected = stats.TryGetProperty("undetected", out var undetProp) ? undetProp.GetInt32() : 0;

            string? scanUrl = $"https://www.virustotal.com/gui/file/{sha256Hash}";

            return new SecurityVerdict(
                sha256Hash: sha256Hash,
                isScanned: true,
                maliciousVotes: malicious,
                suspiciousVotes: suspicious,
                harmlessVotes: harmless,
                undetectedVotes: undetected,
                scanUrl: scanUrl);
        }
        catch (HttpRequestException ex)
        {
            return new SecurityVerdict(
                sha256Hash,
                isScanned: false,
                customMessage: $"VirusTotal bağlantı hatası: {ex.Message}");
        }
    }
}
