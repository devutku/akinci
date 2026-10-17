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

        ConfigureAuthentication(handler, credentials);

        return handler;
    }

    public static HttpClient CreateClient(DownloadCredentials? credentials = null)
    {
        var handler = CreateHandler(credentials);
        var client = new HttpClient(handler, disposeHandler: true);

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Akinci/1.0");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        client.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");

        // HTTP Basic doğrulaması varsa Authorization başlığını doğrudan ekle
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
