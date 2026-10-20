using System.Collections.Concurrent;
using Akinci.Application.Ports;
using Microsoft.Win32.SafeHandles;

namespace Akinci.Infrastructure.Storage;

/// <summary>
/// .NET RandomAccess API'sini kullanarak dosyaya in-place (sparse/ofsetli) ve eşzamanlı yazım yapan disk depolama servisi.
/// </summary>
public sealed class RandomAccessFileStorageService : IFileStorageService, IDisposable
{
    private readonly ConcurrentDictionary<string, SafeFileHandle> _openHandles = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    private SafeFileHandle GetOrCreateHandle(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _openHandles.GetOrAdd(filePath, path =>
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return File.OpenHandle(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite | FileShare.Delete,
                FileOptions.Asynchronous);
        });
    }

    public Task PreAllocateAsync(string filePath, long totalBytes, CancellationToken cancellationToken = default)
    {
        var handle = GetOrCreateHandle(filePath);
        RandomAccess.SetLength(handle, totalBytes);
        return Task.CompletedTask;
    }

    public async Task WriteAtAsync(
        string filePath,
        long fileOffset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        var handle = GetOrCreateHandle(filePath);
        await RandomAccess.WriteAsync(handle, data, fileOffset, cancellationToken).ConfigureAwait(false);
    }

    public bool FileExists(string filePath) => File.Exists(filePath);

    public long GetFileSize(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        return new FileInfo(filePath).Length;
    }

    public void DeleteFile(string filePath)
    {
        CloseHandle(filePath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public void CloseHandle(string filePath)
    {
        if (_openHandles.TryRemove(filePath, out var handle))
        {
            handle.Dispose();
        }
    }

    public long? GetAvailableFreeSpace(string directoryPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(directoryPath);
            var root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrEmpty(root))
            {
                var drive = new DriveInfo(root);
                return drive.AvailableFreeSpace;
            }
        }
        catch
        {
            // Ağ yolları veya yetki kısıtlamalarında yoksay
        }
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var handle in _openHandles.Values)
        {
            handle.Dispose();
        }
        _openHandles.Clear();
    }
}
