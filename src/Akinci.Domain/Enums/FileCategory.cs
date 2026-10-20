namespace Akinci.Domain.Enums;

/// <summary>
/// İndirilen dosyaların uzantılarına göre sınıflandırıldığı dosya kategorileri.
/// </summary>
public enum FileCategory
{
    All,
    Video,
    Audio,
    Document,
    Program,
    Archive,
    Other
}

/// <summary>
/// Dosya adı veya yolundan dosya kategorisini yüksek performansla tespit eden yardımcı sınıf.
/// </summary>
public static class FileCategoryDetector
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".m3u8", ".3gp", ".mpeg", ".mpg"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus", ".alac", ".mid", ".midi"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp",
        ".csv", ".epub", ".mobi", ".md", ".xml", ".json", ".log"
    };

    private static readonly HashSet<string> ProgramExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".msi", ".iso", ".dmg", ".pkg", ".apk", ".deb", ".rpm", ".AppImage", ".bat", ".cmd", ".sh",
        ".ps1", ".jar", ".vbs"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".z", ".tgz", ".tbz2", ".cab", ".lz"
    };

    public static FileCategory Detect(string? fileNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            return FileCategory.Other;
        }

        string ext;
        try
        {
            ext = Path.GetExtension(fileNameOrPath).Trim().ToLowerInvariant();
        }
        catch
        {
            return FileCategory.Other;
        }

        if (string.IsNullOrEmpty(ext))
        {
            return FileCategory.Other;
        }

        if (VideoExtensions.Contains(ext)) return FileCategory.Video;
        if (AudioExtensions.Contains(ext)) return FileCategory.Audio;
        if (DocumentExtensions.Contains(ext)) return FileCategory.Document;
        if (ProgramExtensions.Contains(ext)) return FileCategory.Program;
        if (ArchiveExtensions.Contains(ext)) return FileCategory.Archive;

        return FileCategory.Other;
    }
}
