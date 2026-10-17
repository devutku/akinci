namespace Akinci.Application.DTOs;

/// <summary>
/// Sunucudan (HEAD/GET sorgusu ile) elde edilen dosya üstverisi.
/// </summary>
public sealed record FileMetadataDto(
    long? ContentLength,
    bool SupportsRangeRequests,
    string? SuggestedFileName,
    string? ETag,
    string? ContentType);
