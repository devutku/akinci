using System.Diagnostics;
using Akinci.Application.DTOs;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.ResumeDownload;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Cli;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Adapters;
using Akinci.Infrastructure.Persistence;
using Akinci.Infrastructure.Security;
using Akinci.Infrastructure.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
    ___    __ __ ____ _   __ _____ ____
   /   |  / // //  _// | / // ___//  _/
  / /| | / // / / / /  |/ // /    / /  
 / ___ |/ // /_/ / / /|  // /___ _/ /   
/_/  |_/_//_//___//_/ |_/ \____//___/   
");
Console.WriteLine(CliText.Tagline);
Console.WriteLine();
Console.ResetColor();

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine(CliText.Help);
    return 0;
}

string url = args[0];
string outputDir = Directory.GetCurrentDirectory();
int connections = 8;
string? customFileName = null;
string? userName = null;
string? password = null;
string authType = "basic";
string? virusTotalKey = null;

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
        case "--vt-key" when i + 1 < args.Length:
            virusTotalKey = args[++i];
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
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci", "downloads.db");
var repository = new SqliteDownloadRepository(dbPath);
var hashCalculator = new StreamingFileHashCalculator();
var securityScanner = new VirusTotalSecurityScanner(apiKey: virusTotalKey);
// Tarayıcı, anahtar verilmezse VIRUSTOTAL_API_KEY değişkenine bakar; sonucu yazarken CLI de aynısını bilmeli
bool hasVirusTotalKey = !string.IsNullOrWhiteSpace(virusTotalKey ?? Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY"));

var createHandler = new CreateDownloadCommandHandler(metadataService, repository, fileStorage);
var startHandler = new StartDownloadCommandHandler(repository, chunkDownloader, fileStorage, hashCalculator, securityScanner);
var resumeHandler = new ResumeDownloadCommandHandler(repository, chunkDownloader, fileStorage, hashCalculator, securityScanner);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("\n" + CliText.Pausing);
    cts.Cancel();
};

try
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(CliText.QueryingServer(url));
    Console.ResetColor();

    var createCommand = new CreateDownloadCommand(
        Url: url,
        DestinationDirectory: outputDir,
        RequestedConnections: connections,
        CustomFileName: customFileName,
        Credentials: credentials);

    var task = await createHandler.HandleAsync(createCommand, cts.Token);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(CliText.TargetFile(task.DestinationFilePath));
    Console.WriteLine(CliText.TotalSize(task.TotalBytes.HasValue ? $"{task.TotalBytes.Value / (1024.0 * 1024.0):F2} MB" : CliText.Unknown));
    Console.WriteLine(CliText.RangeSupport(task.SupportsRangeRequests));
    Console.WriteLine(CliText.ChunkCount(task.Chunks.Count));
    Console.ResetColor();

    Console.WriteLine(CliText.Starting + "\n");

    var lastReport = Stopwatch.StartNew();
    var progress = new Progress<DownloadProgressDto>(report =>
    {
        if (lastReport.ElapsedMilliseconds < 100 && report.ProgressPercentage < 100) return;
        lastReport.Restart();

        int barWidth = 30;
        int filled = (int)(report.ProgressPercentage / 100.0 * barWidth);
        string progressBar = new string('█', filled) + new string('░', barWidth - filled);

        string downloadedMb = $"{report.DownloadedBytes / (1024.0 * 1024.0):F2}";
        string totalMb = report.TotalBytes.HasValue ? $"{report.TotalBytes.Value / (1024.0 * 1024.0):F2} MB" : CliText.Unknown;

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
        Console.WriteLine(CliText.Completed(stopwatch.Elapsed.TotalSeconds));
        Console.WriteLine(CliText.SavedTo(task.DestinationFilePath));
        Console.ResetColor();

        if (!string.IsNullOrEmpty(task.Sha256Hash))
        {
            Console.WriteLine($"  SHA-256: {task.Sha256Hash}");
        }

        if (task.SecurityVerdict != null)
        {
            var v = task.SecurityVerdict;
            var verdictText = CliText.SecurityVerdictText(v, hasVirusTotalKey);
            if (v.HasThreat)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [{CliText.SecurityThreatTag}] ⚠ {verdictText}");
            }
            else if (v.IsSafe)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [{CliText.SecurityTag}] ✔ {verdictText}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  [{CliText.SecurityTag}] ℹ {verdictText}");
            }

            if (!string.IsNullOrEmpty(v.ScanUrl))
            {
                Console.WriteLine(CliText.Report(v.ScanUrl));
            }
            Console.ResetColor();
        }
    }
    else if (task.Status == DownloadStatus.Paused)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(CliText.Paused(task.DownloadedBytes));
        Console.ResetColor();
    }

    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n" + CliText.Cancelled);
    return 130;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n" + CliText.Error(ex.Message));
    Console.ResetColor();
    return 1;
}
