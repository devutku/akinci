using System.Text;
using System.Text.Json;

namespace Akinci.NativeHost;

/// <summary>
/// Chrome, Firefox ve Edge tarayıcıları için Native Messaging Host protokol uygulayıcısı.
/// STDIN üzerinden 4-bayt uzunluk başlığı + JSON mesajını okur, yerel Akıncı IPC servisine iletir.
/// </summary>
public static class Program
{
    private const string IpcEndpoint = "http://127.0.0.1:49500/add-download/";

    public static async Task Main(string[] args)
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new BinaryReader(stdin);
        using var writer = new BinaryWriter(stdout);

        try
        {
            while (true)
            {
                // 1. 4-bayt uzunluk başlığını oku (Little-endian 32-bit int)
                int messageLength;
                try
                {
                    messageLength = reader.ReadInt32();
                }
                catch (EndOfStreamException)
                {
                    break; // Tarayıcı bağlantıyı kapattı
                }

                if (messageLength <= 0 || messageLength > 1024 * 1024)
                {
                    break;
                }

                // 2. JSON gövdesini oku
                byte[] messageBytes = reader.ReadBytes(messageLength);
                string jsonMessage = Encoding.UTF8.GetString(messageBytes);

                // 3. Yerel Akıncı IPC servisine POST at
                bool success = await ForwardToAkinciAsync(jsonMessage);

                // 4. Yanıtı tarayıcıya geri gönder
                var responseJson = success
                    ? "{\"status\":\"success\",\"message\":\"İndirme Akıncı'ya aktarıldı.\"}"
                    : "{\"status\":\"error\",\"message\":\"Akıncı masaüstü uygulamasına ulaşılamadı. Uygulamanın açık olduğundan emin olun.\"}";

                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                writer.Write(responseBytes.Length);
                writer.Write(responseBytes);
                writer.Flush();
            }
        }
        catch (Exception ex)
        {
            var errJson = $"{{\"status\":\"fatal\",\"error\":\"{ex.Message}\"}}";
            byte[] errBytes = Encoding.UTF8.GetBytes(errJson);
            writer.Write(errBytes.Length);
            writer.Write(errBytes);
            writer.Flush();
        }
    }

    private static async Task<bool> ForwardToAkinciAsync(string jsonPayload)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(IpcEndpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
