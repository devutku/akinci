using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.UseCases.CreateDownload;

public sealed record CreateDownloadCommand(
    string Url,
    string DestinationDirectory,
    int RequestedConnections = 8,
    string? CustomFileName = null,
    DownloadCredentials? Credentials = null);

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
        var metadata = await _metadataService.GetMetadataAsync(downloadUrl, command.Credentials, cancellationToken);

        // 2. Dosya adını ve hedef yolunu belirle (Aynı isimde dosya varsa çakışmayı önle)
        var fileName = !string.IsNullOrWhiteSpace(command.CustomFileName)
            ? command.CustomFileName
            : !string.IsNullOrWhiteSpace(metadata.SuggestedFileName)
                ? metadata.SuggestedFileName
                : downloadUrl.GetFileNameFromUrl();

        var initialDestinationPath = Path.Combine(command.DestinationDirectory, fileName);
        var destinationPath = EnsureUniqueFilePath(initialDestinationPath);

        // 3. Yeterli disk alanı kontrolü
        if (metadata.ContentLength is > 0)
        {
            var freeSpace = _fileStorage.GetAvailableFreeSpace(command.DestinationDirectory);
            if (freeSpace.HasValue && freeSpace.Value < metadata.ContentLength.Value)
            {
                throw new DomainException(
                    $"Hedef sürücüde yetersiz disk alanı. Gerekli: {FormatBytes(metadata.ContentLength.Value)}, Kullanılabilir: {FormatBytes(freeSpace.Value)}.");
            }
        }

        // 4. Domain varlığını oluştur
        var task = DownloadTask.Create(
            downloadUrl,
            destinationPath,
            metadata.ContentLength,
            metadata.SupportsRangeRequests,
            command.Credentials);

        // 5. Parçaları hesapla ve ata
        task.InitializeChunks(command.RequestedConnections);

        // 6. Boyut biliniyorsa diskte yer tahsis et (pre-allocation)
        if (metadata.ContentLength is > 0)
        {
            await _fileStorage.PreAllocateAsync(destinationPath, metadata.ContentLength.Value, cancellationToken);
        }

        // 7. Durumu veritabanına kaydet
        await _repository.AddAsync(task, cancellationToken);

        return task;
    }

    private string EnsureUniqueFilePath(string destinationPath)
    {
        if (!_fileStorage.FileExists(destinationPath))
        {
            return destinationPath;
        }

        var directory = Path.GetDirectoryName(destinationPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(destinationPath);
        var extension = Path.GetExtension(destinationPath);

        int counter = 1;
        string candidatePath;
        do
        {
            candidatePath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
            counter++;
        } while (_fileStorage.FileExists(candidatePath));

        return candidatePath;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dbl = bytes;
        while (dbl >= 1024 && i < suffixes.Length - 1)
        {
            dbl /= 1024;
            i++;
        }
        return $"{dbl:0.##} {suffixes[i]}";
    }
}
