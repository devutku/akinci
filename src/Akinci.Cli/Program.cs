using System.Diagnostics;
using Akinci.Application.DTOs;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Adapters;
using Akinci.Infrastructure.Persistence;
using Akinci.Infrastructure.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
    ___    __ __ ____ _   __ _____ ____
   /   |  / // //  _// | / // ___//  _/
  / /| | / // / / / /  |/ // /    / /  
 / ___ |/ // /_/ / / /|  // /___ _/ /   
/_/  |_/_//_//___//_/ |_/ \____//___/   
Clean Core Çok Kanallı İndirme Motoru
");
Console.ResetColor();

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Kullanım: akinci <URL> [seçenekler]");
    Console.WriteLine();
    Console.WriteLine("Seçenekler:");
    Console.WriteLine("  -o, --output <dizin>       Hedef indirme dizini (Varsayılan: mevcut dizin)");
    Console.WriteLine("  -c, --connections <sayı>   Paralel bağlantı/parça sayısı (Varsayılan: 8)");
    Console.WriteLine("  -n, --name <isim>          Özel dosya adı");
    Console.WriteLine("  -u, --user <kullanıcı>     Kimlik doğrulama kullanıcı adı");
    Console.WriteLine("  -p, --pass <şifre>         Kimlik doğrulama parolası");
    Console.WriteLine("  -a, --auth <tür>           Kimlik doğrulama türü: basic, ntlm, kerberos, negotiate");
    Console.WriteLine();
    Console.WriteLine("Örnek:");
    Console.WriteLine("  akinci https://speed.hetzner.de/100MB.bin -c 8 -o downloads");
    return 0;
}

string url = args[0];
string outputDir = Directory.GetCurrentDirectory();
int connections = 8;
string? customFileName = null;
string? userName = null;
string? password = null;
string authType = "basic";

for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-o" or "--output" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "-c" or "--connections" when i + 1 < args.Length:
            _ = int.TryParse(args[++i], out connections);
            break;
        case "-n" or "--name" when i + 1 < args.Length:
            customFileName = args[++i];
            break;
        case "-u" or "--user" when i + 1 < args.Length:
            userName = args[++i];
            break;
        case "-p" or "--pass" when i + 1 < args.Length:
            password = args[++i];
            break;
        case "-a" or "--auth" when i + 1 < args.Length:
            authType = args[++i].ToLowerInvariant();
            break;
    }
}

DownloadCredentials? credentials = null;
if (!string.IsNullOrEmpty(userName))
{
    credentials = authType switch
    {
        "ntlm" => DownloadCredentials.Ntlm(userName, password ?? ""),
        "kerberos" => DownloadCredentials.Kerberos(userName, password ?? ""),
        "negotiate" => DownloadCredentials.Negotiate(userName, password ?? ""),
        _ => DownloadCredentials.Basic(userName, password ?? "")
    };
}

// Clean Core Altyapı Servisleri
var metadataService = new ProtocolMetadataService();
var chunkDownloader = new ProtocolChunkDownloader();
using var fileStorage = new RandomAccessFileStorageService();
var repository = new InMemoryDownloadRepository();

var createHandler = new CreateDownloadCommandHandler(metadataService, repository, fileStorage);
var startHandler = new StartDownloadCommandHandler(repository, chunkDownloader, fileStorage);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("\n[DURAKLATILIYOR] İndirme duraklatılıyor...");
    cts.Cancel();
};

try
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[1/3] Sunucu sorgulanıyor: {url}");
    Console.ResetColor();

    var createCommand = new CreateDownloadCommand(
        Url: url,
        DestinationDirectory: outputDir,
        RequestedConnections: connections,
        CustomFileName: customFileName,
        Credentials: credentials);

    var task = await createHandler.HandleAsync(createCommand, cts.Token);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[2/3] Hedef Dosya: {task.DestinationFilePath}");
    Console.WriteLine($"      Toplam Boyut: {(task.TotalBytes.HasValue ? $"{task.TotalBytes.Value / (1024.0 * 1024.0):F2} MB" : "Bilinmiyor")}");
    Console.WriteLine($"      Range Desteği: {(task.SupportsRangeRequests ? "VAR (Çok Kanallı)" : "YOK (Tek Kanal)")}");
    Console.WriteLine($"      Parça Sayısı: {task.Chunks.Count}");
    Console.ResetColor();

    Console.WriteLine($"[3/3] İndirme başlatılıyor...\n");

    var lastReport = Stopwatch.StartNew();
    var progress = new Progress<DownloadProgressDto>(report =>
    {
        if (lastReport.ElapsedMilliseconds < 100 && report.ProgressPercentage < 100) return;
        lastReport.Restart();

        int barWidth = 30;
        int filled = (int)(report.ProgressPercentage / 100.0 * barWidth);
        string progressBar = new string('█', filled) + new string('░', barWidth - filled);

        string downloadedMb = $"{report.DownloadedBytes / (1024.0 * 1024.0):F2}";
        string totalMb = report.TotalBytes.HasValue ? $"{report.TotalBytes.Value / (1024.0 * 1024.0):F2} MB" : "Bilinmiyor";

        Console.Write($"\r[{progressBar}] {report.ProgressPercentage,5:F1}% | {downloadedMb} / {totalMb} | {report.Speed.FormatHumanReadable(),10}  ");
    });

    var startCommand = new StartDownloadCommand(task.Id, progress);
    var stopwatch = Stopwatch.StartNew();

    await startHandler.HandleAsync(startCommand, cts.Token);
    stopwatch.Stop();

    Console.WriteLine("\n");
    if (task.Status == DownloadStatus.Completed)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✔ İNDİRME TAMAMLANDI! Süre: {stopwatch.Elapsed.TotalSeconds:F2} sn");
        Console.WriteLine($"  Kaydedilen Konum: {task.DestinationFilePath}");
        Console.ResetColor();
    }
    else if (task.Status == DownloadStatus.Paused)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⏸ İndirme duraklatıldı. Kaldığı bayt: {task.DownloadedBytes}");
        Console.ResetColor();
    }

    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nİndirme kullanıcı tarafından iptal edildi veya duraklatıldı.");
    return 130;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ HATA: {ex.Message}");
    Console.ResetColor();
    return 1;
}
