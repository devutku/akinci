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

    public StartDownloadCommandHandler(
        IDownloadRepository repository,
        IChunkDownloader chunkDownloader,
        IFileStorageService fileStorage)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _chunkDownloader = chunkDownloader ?? throw new ArgumentNullException(nameof(chunkDownloader));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
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
                chunkDtos
            );

            command.Progress.Report(progressDto);
        }

        try
        {
            var unfinishedChunks = task.Chunks.Where(c => !c.IsCompleted).ToList();

            var workerTasks = unfinishedChunks.Select(async chunk =>
            {
                await _chunkDownloader.DownloadChunkAsync(
                    task.Url,
                    chunk,
                    async (fileOffset, data, ct) =>
                    {
                        // 1. Veriyi sparse/in-place olarak diske yaz
                        await _fileStorage.WriteAtAsync(task.DestinationFilePath, fileOffset, data, ct);

                        // 2. Chunk ilerlemesini güncelle
                        chunk.AdvanceOffset(data.Length);

                        // 3. Kalıcı depoya ofseti kaydet
                        await _repository.UpdateChunkOffsetAsync(task.Id, chunk.Index, chunk.CurrentOffset, ct);

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

            task.Complete();
            await _repository.UpdateAsync(task, CancellationToken.None);
            ReportProgress(DownloadSpeed.Zero);
        }
        catch (OperationCanceledException)
        {
            task.Pause();
            await _repository.UpdateAsync(task, CancellationToken.None);
            ReportProgress(DownloadSpeed.Zero);
        }
        catch (Exception ex)
        {
            task.Fail(ex.Message);
            await _repository.UpdateAsync(task, CancellationToken.None);
            ReportProgress(DownloadSpeed.Zero);
            throw;
        }
    }
}
