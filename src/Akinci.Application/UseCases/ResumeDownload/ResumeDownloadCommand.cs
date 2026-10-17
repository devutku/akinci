using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;

namespace Akinci.Application.UseCases.ResumeDownload;

public sealed record ResumeDownloadCommand(
    Guid TaskId,
    IProgress<DownloadProgressDto>? Progress = null);

public sealed class ResumeDownloadCommandHandler
{
    private readonly StartDownloadCommandHandler _startDownloadHandler;
    private readonly IDownloadRepository _repository;

    public ResumeDownloadCommandHandler(
        IDownloadRepository repository,
        IChunkDownloader chunkDownloader,
        IFileStorageService fileStorage,
        IFileHashCalculator? hashCalculator = null,
        IFileSecurityScanner? securityScanner = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _startDownloadHandler = new StartDownloadCommandHandler(
            repository,
            chunkDownloader,
            fileStorage,
            hashCalculator,
            securityScanner);
    }

    public async Task HandleAsync(ResumeDownloadCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(command.TaskId, cancellationToken)
                   ?? throw new DomainException($"Devam ettirilecek indirme görevi bulunamadı (ID: {command.TaskId}).");

        if (task.Status == DownloadStatus.Completed)
        {
            return;
        }

        if (task.Status is not (DownloadStatus.Paused or DownloadStatus.Failed))
        {
            throw new InvalidStateTransitionException(task.Status, DownloadStatus.Downloading);
        }

        // Yarım kalan indirmeyi kaldığı ofsetlerden devam ettir
        await _startDownloadHandler.HandleAsync(
            new StartDownloadCommand(command.TaskId, command.Progress),
            cancellationToken);
    }
}
