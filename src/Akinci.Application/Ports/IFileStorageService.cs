namespace Akinci.Application.Ports;

/// <summary>
/// Dosya diski üzerinde sparse (in-place) yazım, pre-allocation ve dosya işlemlerini yöneten port arayüzü.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Hedef dosyayı oluşturur ve toplam boyut biliniyorsa diske yer tahsis eder (pre-allocation).
    /// </summary>
    Task PreAllocateAsync(string filePath, long totalBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen dosya ofsetine eşzamanlı/güvenli olarak veri bloğunu yazar (RandomAccess).
    /// </summary>
    Task WriteAtAsync(string filePath, long fileOffset, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyanın diskte var olup olmadığını döner.
    /// </summary>
    bool FileExists(string filePath);

    /// <summary>
    /// Mevcut dosyanın boyutunu bayt cinsinden döner.
    /// </summary>
    long GetFileSize(string filePath);

    /// <summary>
    /// Dosyayı diskten siler.
    /// </summary>
    void DeleteFile(string filePath);
}
