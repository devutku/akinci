using System.Buffers;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;
using FluentFTP;

namespace Akinci.Infrastructure.Ftp;

/// <summary>
/// FluentFTP kullanarak FTP/FTPS sunucularından REST komutu ile belirli aralıkları indiren servis.
/// </summary>
public sealed class FtpChunkDownloader
{
    private const int BufferSize = 128 * 1024; // 128 KB

    public async Task DownloadChunkAsync(
        DownloadUrl url,
        Chunk chunk,
        Func<long, ReadOnlyMemory<byte>, CancellationToken, Task> onDataReceived,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        if (chunk.IsCompleted)
        {
            return;
        }

        var (host, port, remotePath) = FtpMetadataService.ParseFtpUrl(url);
        var (user, pass) = FtpMetadataService.ResolveCredentials(credentials);

        var config = new FtpConfig
        {
            ConnectTimeout = 15000,
            ReadTimeout = 30000,
            DataConnectionType = FtpDataConnectionType.PASV
        };

        await using var client = new AsyncFtpClient(host, user, pass, port, config);

        if (url.Uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
        {
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            client.Config.ValidateAnyCertificate = true;
        }

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        long startOffset = chunk.CurrentOffset;
        long endOffset = chunk.Range.End;
        long totalBytesToRead = endOffset - startOffset + 1;

        await using var ftpStream = await client.OpenRead(
            remotePath,
            FtpDataType.Binary,
            restart: startOffset,
            token: cancellationToken).ConfigureAwait(false);

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            long currentOffset = startOffset;
            long totalBytesReadSoFar = 0;

            while (totalBytesReadSoFar < totalBytesToRead)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int maxRead = (int)Math.Min(BufferSize, totalBytesToRead - totalBytesReadSoFar);
                int bytesRead = await ftpStream.ReadAsync(rentedBuffer.AsMemory(0, maxRead), cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    break;
                }

                await onDataReceived(currentOffset, rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

                currentOffset += bytesRead;
                totalBytesReadSoFar += bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            await client.Disconnect(cancellationToken).ConfigureAwait(false);
        }
    }
}
