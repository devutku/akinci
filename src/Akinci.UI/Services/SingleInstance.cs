using System.Net.Http;

namespace Akinci.UI.Services;

/// <summary>
/// Akıncı'nın aynı anda tek örnek çalışmasını sağlar ve çalışan örneğe yerel kontrol komutları gönderir.
/// Kurulum programı da aynı mutex adıyla uygulamanın açık olup olmadığını anlar.
/// </summary>
internal static class SingleInstance
{
    public const string MutexName = @"Local\Akinci.SingleInstance";
    private const string ControlEndpoint = "http://127.0.0.1:49500/control/";

    /// <summary>
    /// Tek örnek kilidini almaya çalışır. Başka bir örnek kilidi tutuyorsa <c>false</c> döner.
    /// Kilit, dönen mutex süreç boyunca canlı tutulduğu sürece geçerlidir.
    /// </summary>
    public static bool TryAcquire(out Mutex mutex)
    {
        mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        return createdNew;
    }

    /// <summary>
    /// Çalışan örneğe <paramref name="command"/> ("shutdown" veya "activate") komutunu gönderir.
    /// İstek, <c>%USERPROFILE%\.akinci\ipc.token</c> ile doğrulanır. Başarılıysa <c>true</c> döner.
    /// </summary>
    public static async Task<bool> SendControlAsync(string command)
    {
        try
        {
            var tokenPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci", "ipc.token");
            if (!File.Exists(tokenPath))
            {
                return false;
            }

            var token = (await File.ReadAllTextAsync(tokenPath)).Trim();
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ControlEndpoint}{command}/");
            request.Headers.Add("X-Akinci-Token", token);

            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
