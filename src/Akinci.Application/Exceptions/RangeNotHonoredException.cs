namespace Akinci.Application.Exceptions;

/// <summary>
/// Dosyanın ortasından bir parça istenirken sunucu kısmi içerik (HTTP 206) yerine
/// dosyanın tamamını (HTTP 200) döndürdüğünde fırlatılır. Dosyanın bozulmaması için indirme durdurulur.
/// </summary>
public sealed class RangeNotHonoredException : InvalidOperationException
{
    public int ChunkIndex { get; }
    public long Offset { get; }

    public RangeNotHonoredException(int chunkIndex, long offset)
        : base($"Sunucu kısmi içerik (HTTP 206 Partial Content) yerine dosyanın tamamını (HTTP 200 OK) döndürdü. " +
               $"Chunk #{chunkIndex} (Ofset: {offset}) için dosya bozulmasını önlemek amacıyla indirme durduruldu.")
    {
        ChunkIndex = chunkIndex;
        Offset = offset;
    }
}
