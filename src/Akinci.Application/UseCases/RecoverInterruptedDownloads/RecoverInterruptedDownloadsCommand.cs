using Akinci.Application.Ports;
using Akinci.Domain.Enums;

namespace Akinci.Application.UseCases.RecoverInterruptedDownloads;

/// <summary>
/// Uygulama kapanırken (çökme, zorla sonlandırma veya eski sürümlerin kapanış davranışı nedeniyle)
/// "İndiriliyor" durumunda kalmış görevleri açılışta "Duraklatıldı" durumuna çeker.
/// Böylece görevler kullanıcı tarafından devam ettirilebilir ve eşzamanlı indirme kuyruğunu tıkamaz.
/// </summary>
public sealed class RecoverInterruptedDownloadsCommandHandler
{
    private readonly IDownloadRepository _repository;

    public RecoverInterruptedDownloadsCommandHandler(IDownloadRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <returns>Duraklatılan görev sayısı.</returns>
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.GetAllAsync(cancellationToken);
        int recovered = 0;

        foreach (var task in tasks.Where(t => t.Status == DownloadStatus.Downloading))
        {
            task.Pause();
            await _repository.UpdateAsync(task, cancellationToken);
            recovered++;
        }

        return recovered;
    }
}
