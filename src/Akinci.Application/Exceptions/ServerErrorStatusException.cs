using System.Net;

namespace Akinci.Application.Exceptions;

/// <summary>
/// Sunucu bir parça isteğine başarısız bir HTTP durum koduyla yanıt verdiğinde fırlatılır.
/// <see cref="HttpRequestException.StatusCode"/> dolu olduğu için yeniden deneme politikası geçici hataları tanır.
/// </summary>
public sealed class ServerErrorStatusException : HttpRequestException
{
    public int ChunkIndex { get; }
    public string? ReasonPhrase { get; }

    public ServerErrorStatusException(int chunkIndex, HttpStatusCode statusCode, string? reasonPhrase)
        : base($"Chunk #{chunkIndex} indirilemedi. Sunucu HTTP {(int)statusCode} ({reasonPhrase}) döndü.", null, statusCode)
    {
        ChunkIndex = chunkIndex;
        ReasonPhrase = reasonPhrase;
    }
}
