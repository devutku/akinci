using System.Diagnostics;
using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.UseCases.StartDownload;

public sealed record StartDownloadCommand(
    Guid TaskId,
    IProgress<DownloadProgressDto>? Progress = null);

public sealed class StartDownloadCommandHandler
{
    private readonly IDownloadRepository _repository;
    private readonly IChunkDownloader _chunkDownloader;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileHashCalculator? _hashCalculator;
    private readonly IFileSecurityScanner? _securityScanner;
    private readonly IBandwidthThrottler? _throttler;

    public StartDownloadCommandHandler(
        IDownloadRepository repository,
        IChunkDownloader chunkDownloader,
        IFileStorageService fileStorage,
        IFileHashCalculator? hashCalculator = null,
        IFileSecurityScanner? securityScanner = null,
        IBandwidthThrottler? throttler = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _chunkDownloader = chunkDownloader ?? throw new ArgumentNullException(nameof(chunkDownloader));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _hashCalculator = hashCalculator;
        _securityScanner = securityScanner;
        _throttler = throttler;
    }

    public async Task HandleAsync(StartDownloadCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(command.TaskId, cancellationToken)
                   ?? throw new DomainException($"İndirme görevi bulunamadı (ID: {command.TaskId}).");

        if (task.Status == DownloadStatus.Completed)
        {
            return;
        }

        task.Start();
        await _repository.UpdateAsync(task, cancellationToken);

        // Hız hesaplaması ve ilerleme takibi için sayaçlar
        var stopwatch = Stopwatch.StartNew();
        long lastReportedBytes = task.DownloadedBytes;
        long lastReportedTicks = stopwatch.ElapsedTicks;
        var progressLock = new object();

        void ReportProgress(DownloadSpeed speed)
        {
            if (command.Progress == null) return;

            var chunkDtos = task.Chunks.Select(c => new ChunkProgressDto(
                c.Index,
                c.Range.Start,
                c.Range.End,
                c.CurrentOffset,
                c.DownloadedBytes,
                c.ProgressPercentage,
                c.IsCompleted
            )).ToList();

            var progressDto = new DownloadProgressDto(
                task.Id,
                task.DownloadedBytes,
                task.TotalBytes,
                task.ProgressPercentage,
                speed,
                task.Status,
                chunkDtos,
                task.SecurityVerdict
            );

            command.Progress.Report(progressDto);
        }

        try
        {
            var unfinishedChunks = task.Chunks.Where(c => !c.IsCompleted).ToList();

            var workerTasks = unfinishedChunks.Select(async chunk =>
            {
                long lastPersistedOffset = chunk.CurrentOffset;
                var lastPersistTime = Stopwatch.StartNew();
                var chunkDbLock = new SemaphoreSlim(1, 1);

                await _chunkDownloader.DownloadChunkAsync(
                    task.Url,
                    chunk,
                    async (fileOffset, data, ct) =>
                    {
                        // 0. Bant Genişliği / Hız Sınırlayıcı
                        if (_throttler != null && _throttler.LimitBytesPerSecond > 0)
                        {
                            await _throttler.ThrottleAsync(data.Length, ct).ConfigureAwait(false);
                        }

                        // 1. Veriyi sparse/in-place olarak diske yaz
                        await _fileStorage.WriteAtAsync(task.DestinationFilePath, fileOffset, data, ct);

                        // 2. Chunk ilerlemesini güncelle
                        chunk.AdvanceOffset(data.Length);

                        // 3. Kalıcı depoya ofseti periyodik/kontrollü kaydet (Aşırı SQLite yazma yükünü önleme)
                        // Her tamponda değil; en az 1.5 sn geçtiyse, 2 MB veri yazıldıysa veya parça bittiyse kaydet
                        bool shouldPersist = lastPersistTime.ElapsedMilliseconds >= 1500 ||
                                             (chunk.CurrentOffset - lastPersistedOffset >= 2 * 1024 * 1024) ||
                                             chunk.IsCompleted;

                        if (shouldPersist && chunkDbLock.Wait(0))
                        {
                            try
                            {
                                await _repository.UpdateChunkOffsetAsync(task.Id, chunk.Index, chunk.CurrentOffset, ct);
                                lastPersistedOffset = chunk.CurrentOffset;
                                lastPersistTime.Restart();
                            }
                            finally
                            {
                                chunkDbLock.Release();
                            }
                        }

                        // 4. Hız hesapla ve raporla
                        DownloadSpeed currentSpeed = DownloadSpeed.Zero;
                        lock (progressLock)
                        {
                            long currentTicks = stopwatch.ElapsedTicks;
                            double elapsedSeconds = (double)(currentTicks - lastReportedTicks) / Stopwatch.Frequency;

                            if (elapsedSeconds >= 0.5) // Her 500ms'de bir hız hesapla
                            {
                                long currentBytes = task.DownloadedBytes;
                                double bytesPerSec = (currentBytes - lastReportedBytes) / elapsedSeconds;
                                currentSpeed = new DownloadSpeed(bytesPerSec);

                                lastReportedBytes = currentBytes;
                                lastReportedTicks = currentTicks;
                                ReportProgress(currentSpeed);
                            }
                        }
                    },
                    task.Credentials,
                    cancellationToken);
            });

            await Task.WhenAll(workerTasks);

            // 1. Worker'lar bittiğinde dosya tanıtıcısını (handle) HEMEN KAPAT.
            // Bu sayede Windows ve harici programlar (WinRAR, 7-Zip, oynatıcılar) dosyaya derhal erişebilir.
            _fileStorage.CloseHandle(task.DestinationFilePath);

            task.Complete();

            // Dosya tamamlandı: SHA-256 hash hesaplama ve otomatik VirusTotal güvenlik taraması
            if (_hashCalculator != null)
            {
                var sha256 = await _hashCalculator.CalculateSha256Async(task.DestinationFilePath, CancellationToken.None);
                task.SetSha256(sha256);

                if (_securityScanner != null)
                {
                    var verdict = await _securityScanner.ScanHashAsync(sha256, CancellationToken.None);
                    task.SetSecurityVerdict(verdict);
                }
            }

            await _repository.UpdateAsync(task, CancellationToken.None);
            ReportProgress(DownloadSpeed.Zero);
        }
        catch (OperationCanceledException)
        {
            _fileStorage.CloseHandle(task.DestinationFilePath);
            task.Pause();
            await _repository.UpdateAsync(task, CancellationToken.None);
            ReportProgress(DownloadSpeed.Zero);
        }
        catch (Exception ex)
        {
            _fileStorage.CloseHandle(task.DestinationFilePath);
            task.Fail(ex.Message);
            await _repository.UpdateAsync(task, CancellationToken.None);
            ReportProgress(DownloadSpeed.Zero);
            throw;
        }
        finally
        {
            // İndirme nasıl sonlanırsa sonlansın dosya tanıtıcısının açık kalmamasını garanti et
            _fileStorage.CloseHandle(task.DestinationFilePath);
        }
    }
}
