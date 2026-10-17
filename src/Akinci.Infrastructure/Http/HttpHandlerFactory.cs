using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;

namespace Akinci.Infrastructure.Http;

/// <summary>
/// Yüksek performanslı SocketsHttpHandler ve kimlik doğrulama ayarlarını yapılandıran fabrika sınıfı.
/// </summary>
public static class HttpHandlerFactory
{
    public static SocketsHttpHandler CreateHandler(DownloadCredentials? credentials = null)
    {
        var handler = new SocketsHttpHandler
        {
            // Çok kanallı paralel indirmeler için soket havuzu ayarları
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 128,
            EnableMultipleHttp2Connections = true,

            // Parça ve Range indirmelerinde boyut bütünlüğü için otomatik açmayı kapatıyoruz
            AutomaticDecompression = DecompressionMethods.None,

            // Yeniden yönlendirmeleri (301, 302, 307, 308) otomatik takip et
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        };

        // Eğer tarayıcıdan özel Cookie header (cf_clearance vb.) aktarılmışsa,
        // CookieContainer'ın kısıtlamalarına takılmadan doğrudan başlık olarak iletilmesi için UseCookies = false yapılır.
        if (!string.IsNullOrWhiteSpace(credentials?.CookieHeader))
        {
            handler.UseCookies = false;
        }

        ConfigureAuthentication(handler, credentials);

        return handler;
    }

    public static HttpClient CreateClient(DownloadCredentials? credentials = null)
    {
        var handler = CreateHandler(credentials);
        var client = new HttpClient(handler, disposeHandler: true);

        // 1. Modern Chromium User-Agent (veya özel aktarılan tarayıcı User-Agent)
        var userAgent = !string.IsNullOrWhiteSpace(credentials?.UserAgent)
            ? credentials.UserAgent
            : "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

        // 2. Accept & Dil Başlıkları
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");

        // 3. Client Hints (Chromium Fingerprint - Cloudflare/WAF bot korumasını aşmak için zorunlu)
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", "\"Chromium\";v=\"130\", \"Google Chrome\";v=\"130\", \"Not?A_Brand\";v=\"99\"");
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");

        // 4. Fetch Metadata (Sec-Fetch-*)
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

        // 5. Referer (Hotlink korumaları ve sayfa bazlı yönlendirmeler için)
        if (!string.IsNullOrWhiteSpace(credentials?.Referer))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", credentials.Referer);
        }

        // 6. Cookie (Cloudflare cf_clearance, oturum ve indirme anahtarları)
        if (!string.IsNullOrWhiteSpace(credentials?.CookieHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", credentials.CookieHeader);
        }

        // 7. HTTP Basic doğrulaması varsa Authorization başlığını doğrudan ekle
        if (credentials is { Type: AuthenticationType.Basic } &&
            !string.IsNullOrEmpty(credentials.UserName))
        {
            var raw = $"{credentials.UserName}:{credentials.Password ?? ""}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        return client;
    }

    private static void ConfigureAuthentication(SocketsHttpHandler handler, DownloadCredentials? credentials)
    {
        if (credentials == null || credentials.Type == AuthenticationType.None)
        {
            return;
        }

        if (credentials.UseDefaultCredentials)
        {
            // Windows oturum açma kimlik bilgileri (Negotiate, Kerberos, NTLM)
            handler.Credentials = CredentialCache.DefaultNetworkCredentials;
            handler.PreAuthenticate = true;
            return;
        }

        switch (credentials.Type)
        {
            case AuthenticationType.Basic:
                if (!string.IsNullOrEmpty(credentials.UserName))
                {
                    handler.Credentials = new NetworkCredential(credentials.UserName, credentials.Password ?? "");
                    handler.PreAuthenticate = true;
                }
                break;

            case AuthenticationType.Ntlm:
                if (!string.IsNullOrEmpty(credentials.UserName))
                {
                    handler.Credentials = new NetworkCredential(
                        credentials.UserName,
                        credentials.Password ?? "",
                        credentials.Domain ?? "");
                    handler.PreAuthenticate = true;
                }
                break;

            case AuthenticationType.Kerberos:
            case AuthenticationType.Negotiate:
                if (!string.IsNullOrEmpty(credentials.UserName))
                {
                    handler.Credentials = new NetworkCredential(
                        credentials.UserName,
                        credentials.Password ?? "",
                        credentials.Domain ?? "");
                    handler.PreAuthenticate = true;
                }
                break;
        }
    }
}
