using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.UseCases.CreateDownload;

public sealed record CreateDownloadCommand(
    string Url,
    string DestinationDirectory,
    int RequestedConnections = 8,
    string? CustomFileName = null);

public sealed class CreateDownloadCommandHandler
{
    private readonly IHttpMetadataService _metadataService;
    private readonly IDownloadRepository _repository;
    private readonly IFileStorageService _fileStorage;

    public CreateDownloadCommandHandler(
        IHttpMetadataService metadataService,
        IDownloadRepository repository,
        IFileStorageService fileStorage)
    {
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<DownloadTask> HandleAsync(CreateDownloadCommand command, CancellationToken cancellationToken = default)
    {
        var downloadUrl = DownloadUrl.Create(command.Url);

        // 1. Sunucudan üstveriyi sorgula
        var metadata = await _metadataService.GetMetadataAsync(downloadUrl, cancellationToken);

        // 2. Dosya adını ve hedef yolunu belirle
        var fileName = !string.IsNullOrWhiteSpace(command.CustomFileName)
            ? command.CustomFileName
            : !string.IsNullOrWhiteSpace(metadata.SuggestedFileName)
                ? metadata.SuggestedFileName
                : downloadUrl.GetFileNameFromUrl();

        var destinationPath = Path.Combine(command.DestinationDirectory, fileName);

        // 3. Domain varlığını oluştur
        var task = DownloadTask.Create(
            downloadUrl,
            destinationPath,
            metadata.ContentLength,
            metadata.SupportsRangeRequests);

        // 4. Parçaları hesapla ve ata
        task.InitializeChunks(command.RequestedConnections);

        // 5. Boyut biliniyorsa diskte yer tahsis et (pre-allocation)
        if (metadata.ContentLength is > 0)
        {
            await _fileStorage.PreAllocateAsync(destinationPath, metadata.ContentLength.Value, cancellationToken);
        }

        // 6. Durumu veritabanına kaydet
        await _repository.AddAsync(task, cancellationToken);

        return task;
    }
}
