using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.UseCases.ScanSecurity;

public sealed record ScanFileSecurityCommand(Guid TaskId);

public sealed class ScanFileSecurityCommandHandler
{
    private readonly IDownloadRepository _repository;
    private readonly IFileHashCalculator _hashCalculator;
    private readonly IFileSecurityScanner _securityScanner;

    public ScanFileSecurityCommandHandler(
        IDownloadRepository repository,
        IFileHashCalculator hashCalculator,
        IFileSecurityScanner securityScanner)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _hashCalculator = hashCalculator ?? throw new ArgumentNullException(nameof(hashCalculator));
        _securityScanner = securityScanner ?? throw new ArgumentNullException(nameof(securityScanner));
    }

    public async Task<SecurityVerdict> HandleAsync(ScanFileSecurityCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(command.TaskId, cancellationToken)
                   ?? throw new DomainException($"İndirme görevi bulunamadı (ID: {command.TaskId}).");

        // 1. Dosyanın SHA-256 hash'i henüz hesaplanmadıysa hesapla
        if (string.IsNullOrEmpty(task.Sha256Hash))
        {
            var hash = await _hashCalculator.CalculateSha256Async(task.DestinationFilePath, cancellationToken);
            task.SetSha256(hash);
        }

        // 2. VirusTotal / Güvenlik servisine sorgu at
        var verdict = await _securityScanner.ScanHashAsync(task.Sha256Hash!, cancellationToken);
        task.SetSecurityVerdict(verdict);

        // 3. Kalıcı duruma kaydet
        await _repository.UpdateAsync(task, cancellationToken);

        return verdict;
    }
}
