using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.Exceptions;

namespace Akinci.Application.UseCases.PauseDownload;

public sealed record PauseDownloadCommand(Guid TaskId);

public sealed class PauseDownloadCommandHandler
{
    private readonly IDownloadRepository _repository;

    public PauseDownloadCommandHandler(IDownloadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<DownloadTask> HandleAsync(PauseDownloadCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(command.TaskId, cancellationToken)
                   ?? throw new DomainException($"İndirme görevi bulunamadı (ID: {command.TaskId}).");

        task.Pause();
        await _repository.UpdateAsync(task, cancellationToken);
        return task;
    }
}
