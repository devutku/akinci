using Akinci.Application.DTOs;
using Akinci.Application.UnitTests.Fakes;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Domain.Enums;
using Xunit;

namespace Akinci.Application.UnitTests.UseCases;

public class CreateDownloadCommandHandlerTests
{
    private readonly InMemoryDownloadRepository _repository = new();
    private readonly FakeHttpMetadataService _metadataService = new();
    private readonly FakeFileStorageService _fileStorage = new();

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesTaskWithChunksAndPreAllocates()
    {
        _metadataService.MetadataToReturn = new FileMetadataDto(
            ContentLength: 1000,
            SupportsRangeRequests: true,
            SuggestedFileName: "archive.zip",
            ETag: "\"abc\"",
            ContentType: "application/zip");

        var handler = new CreateDownloadCommandHandler(_metadataService, _repository, _fileStorage);
        var command = new CreateDownloadCommand(
            Url: "https://example.com/archive.zip",
            DestinationDirectory: @"C:\Downloads",
            RequestedConnections: 4);

        var task = await handler.HandleAsync(command);

        Assert.NotNull(task);
        Assert.Equal(DownloadStatus.Pending, task.Status);
        Assert.Equal(@"C:\Downloads\archive.zip", task.DestinationFilePath);
        Assert.Equal(1000, task.TotalBytes);
        Assert.Equal(4, task.Chunks.Count);

        // Pre-allocation check
        Assert.Contains(task.DestinationFilePath, _fileStorage.PreAllocatedFiles);

        // Repository check
        var savedTask = await _repository.GetByIdAsync(task.Id);
        Assert.NotNull(savedTask);
    }

    [Fact]
    public async Task HandleAsync_WithCustomFileName_OverridesServerSuggestedName()
    {
        _metadataService.MetadataToReturn = new FileMetadataDto(
            ContentLength: 500,
            SupportsRangeRequests: true,
            SuggestedFileName: "server-suggested.bin",
            ETag: null,
            ContentType: null);

        var handler = new CreateDownloadCommandHandler(_metadataService, _repository, _fileStorage);
        var command = new CreateDownloadCommand(
            Url: "https://example.com/file",
            DestinationDirectory: @"C:\Downloads",
            RequestedConnections: 2,
            CustomFileName: "custom_name.iso");

        var task = await handler.HandleAsync(command);

        Assert.Equal(@"C:\Downloads\custom_name.iso", task.DestinationFilePath);
    }

    [Fact]
    public async Task HandleAsync_WhenRangeNotSupported_CreatesSingleChunk()
    {
        _metadataService.MetadataToReturn = new FileMetadataDto(
            ContentLength: 2048,
            SupportsRangeRequests: false,
            SuggestedFileName: "stream.mp4",
            ETag: null,
            ContentType: "video/mp4");

        var handler = new CreateDownloadCommandHandler(_metadataService, _repository, _fileStorage);
        var command = new CreateDownloadCommand(
            Url: "https://example.com/stream.mp4",
            DestinationDirectory: @"C:\Downloads",
            RequestedConnections: 8);

        var task = await handler.HandleAsync(command);

        Assert.Single(task.Chunks);
        Assert.Equal(0, task.Chunks[0].Range.Start);
        Assert.Equal(2047, task.Chunks[0].Range.End);
    }

    [Fact]
    public async Task HandleAsync_WhenFileAlreadyExists_GeneratesUniqueNumberedFileName()
    {
        _fileStorage.Files[@"C:\Downloads\document.pdf"] = new byte[100];

        _metadataService.MetadataToReturn = new FileMetadataDto(
            ContentLength: 500,
            SupportsRangeRequests: true,
            SuggestedFileName: "document.pdf",
            ETag: null,
            ContentType: "application/pdf");

        var handler = new CreateDownloadCommandHandler(_metadataService, _repository, _fileStorage);
        var command = new CreateDownloadCommand(
            Url: "https://example.com/document.pdf",
            DestinationDirectory: @"C:\Downloads",
            RequestedConnections: 2);

        var task = await handler.HandleAsync(command);

        Assert.Equal(@"C:\Downloads\document (1).pdf", task.DestinationFilePath);
    }

    [Fact]
    public async Task HandleAsync_WhenDiskSpaceInsufficient_ThrowsDomainException()
    {
        _fileStorage.AvailableFreeSpace = 500; // Sadece 500 bayt boş alan var

        _metadataService.MetadataToReturn = new FileMetadataDto(
            ContentLength: 2000, // 2000 bayt gerekli
            SupportsRangeRequests: true,
            SuggestedFileName: "large-file.bin",
            ETag: null,
            ContentType: "application/octet-stream");

        var handler = new CreateDownloadCommandHandler(_metadataService, _repository, _fileStorage);
        var command = new CreateDownloadCommand(
            Url: "https://example.com/large-file.bin",
            DestinationDirectory: @"C:\Downloads",
            RequestedConnections: 2);

        var ex = await Assert.ThrowsAsync<Akinci.Domain.Exceptions.DomainException>(() => handler.HandleAsync(command));
        Assert.Contains("yetersiz disk alanı", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
