using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;

namespace Akinci.Domain.Entities;

/// <summary>
/// İndirme sürecinin tamamını yöneten Ana Varlık (Aggregate Root).
/// </summary>
public sealed class DownloadTask
{
    private readonly List<Chunk> _chunks = new();

    public Guid Id { get; }
    public DownloadUrl Url { get; private set; }
    public string DestinationFilePath { get; private set; }
    public long? TotalBytes { get; private set; }
    public bool SupportsRangeRequests { get; private set; }
    public DownloadStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? PausedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<Chunk> Chunks => _chunks.AsReadOnly();

    /// <summary>
    /// Tüm parçalarda şu ana kadar başarıyla indirilen toplam bayt sayısı.
    /// </summary>
    public long DownloadedBytes => _chunks.Count == 0 ? 0 : _chunks.Sum(c => c.DownloadedBytes);

    /// <summary>
    /// Toplam indirme ilerleme yüzdesi (0.0 - 100.0).
    /// </summary>
    public double ProgressPercentage
    {
        get
        {
            if (TotalBytes is null or <= 0)
            {
                return Status == DownloadStatus.Completed ? 100.0 : 0.0;
            }

            return Math.Clamp((double)DownloadedBytes / TotalBytes.Value * 100.0, 0.0, 100.0);
        }
    }

    /// <summary>
    /// Tüm parçaların başarıyla bitip bitmediğini kontrol eder.
    /// </summary>
    public bool IsAllChunksCompleted => _chunks.Count > 0 && _chunks.All(c => c.IsCompleted);

    public DownloadCredentials? Credentials { get; private set; }
    public string? Sha256Hash { get; private set; }
    public SecurityVerdict? SecurityVerdict { get; private set; }

    public DownloadTask(
        Guid id,
        DownloadUrl url,
        string destinationFilePath,
        long? totalBytes = null,
        bool supportsRangeRequests = false,
        DownloadStatus status = DownloadStatus.Pending,
        DownloadCredentials? credentials = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Url = url ?? throw new ArgumentNullException(nameof(url));
        DestinationFilePath = string.IsNullOrWhiteSpace(destinationFilePath)
            ? throw new ArgumentNullException(nameof(destinationFilePath))
            : destinationFilePath;
        TotalBytes = totalBytes;
        SupportsRangeRequests = supportsRangeRequests;
        Status = status;
        Credentials = credentials;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Yeni bir indirme görevi oluşturmak için fabrika metodu.
    /// </summary>
    public static DownloadTask Create(
        DownloadUrl url,
        string destinationFilePath,
        long? totalBytes = null,
        bool supportsRangeRequests = false,
        DownloadCredentials? credentials = null)
    {
        return new DownloadTask(
            Guid.NewGuid(),
            url,
            destinationFilePath,
            totalBytes,
            supportsRangeRequests,
            DownloadStatus.Pending,
            credentials);
    }

    /// <summary>
    /// Göreve kimlik doğrulama bilgilerini atar veya günceller.
    /// </summary>
    public void SetCredentials(DownloadCredentials? credentials)
    {
        Credentials = credentials;
    }

    /// <summary>
    /// İndirme tamamlandığında hesaplanan SHA-256 hash değerini atar.
    /// </summary>
    public void SetSha256(string sha256)
    {
        Sha256Hash = sha256;
    }

    /// <summary>
    /// VirusTotal veya güvenlik taraması sonucunu kaydeder.
    /// </summary>
    public void SetSecurityVerdict(SecurityVerdict verdict)
    {
        SecurityVerdict = verdict;
    }

    /// <summary>
    /// Sunucudan HEAD / Range doğrulaması alındıktan sonra metadata'yı günceller.
    /// </summary>
    public void UpdateMetadata(long totalBytes, bool supportsRangeRequests, string? newDestinationFilePath = null)
    {
        if (totalBytes < 0)
        {
            throw new DomainException($"Dosya boyutu negatif olamaz. Verilen: {totalBytes}");
        }

        TotalBytes = totalBytes;
        SupportsRangeRequests = supportsRangeRequests;

        if (!string.IsNullOrWhiteSpace(newDestinationFilePath))
        {
            DestinationFilePath = newDestinationFilePath;
        }
    }

    /// <summary>
    /// İstenen paralel bağlantı sayısına göre dosyayı kesinlikle örtüşmeyen ve bayt kaçırmayan parçalara böler.
    /// </summary>
    public void InitializeChunks(int requestedConnections)
    {
        if (requestedConnections < 1)
        {
            throw new DomainException("Bağlantı sayısı en az 1 olmalıdır.");
        }

        _chunks.Clear();

        // Boyut bilinmiyorsa veya sunucu Range desteklemiyorsa tek parçada indirmek zorundayız
        if (!SupportsRangeRequests || TotalBytes is null or <= 0)
        {
            var singleRange = new ByteRange(0, Math.Max(0, (TotalBytes ?? 1) - 1));
            _chunks.Add(new Chunk(Id, 0, singleRange));
            return;
        }

        long total = TotalBytes.Value;
        int chunkCount = (int)Math.Min(requestedConnections, total);

        long baseChunkSize = total / chunkCount;
        long remainder = total % chunkCount;

        long currentStart = 0;
        for (int i = 0; i < chunkCount; i++)
        {
            long chunkSize = baseChunkSize + (i < remainder ? 1 : 0);
            long end = currentStart + chunkSize - 1;

            _chunks.Add(new Chunk(Id, i, new ByteRange(currentStart, end)));
            currentStart = end + 1;
        }
    }

    /// <summary>
    /// Kalıcı depodan (SQLite vb.) daha önceden var olan parçaları yükler.
    /// </summary>
    public void LoadChunks(IEnumerable<Chunk> chunks)
    {
        _chunks.Clear();
        _chunks.AddRange(chunks.OrderBy(c => c.Index));
    }

    /// <summary>
    /// İndirmeyi başlatır veya duraklatılmışsa devam ettirir.
    /// </summary>
    public void Start()
    {
        if (Status == DownloadStatus.Downloading)
        {
            return;
        }

        if (Status is not (DownloadStatus.Pending or DownloadStatus.Paused or DownloadStatus.Failed))
        {
            throw new InvalidStateTransitionException(Status, DownloadStatus.Downloading);
        }

        Status = DownloadStatus.Downloading;
        StartedAt ??= DateTime.UtcNow;
        FailureReason = null;
    }

    /// <summary>
    /// Devam eden indirmeyi duraklatır.
    /// </summary>
    public void Pause()
    {
        if (Status == DownloadStatus.Paused)
        {
            return;
        }

        if (Status is not (DownloadStatus.Downloading or DownloadStatus.Pending))
        {
            throw new InvalidStateTransitionException(Status, DownloadStatus.Paused);
        }

        Status = DownloadStatus.Paused;
        PausedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Tüm parçaların eksiksiz indirildiğini doğrulayarak indirmeyi tamamlar.
    /// </summary>
    public void Complete()
    {
        if (Status == DownloadStatus.Completed)
        {
            return;
        }

        if (_chunks.Count > 0 && !_chunks.All(c => c.IsCompleted))
        {
            throw new DomainException("Tüm parçalar tamamlanmadan indirme tamamlandı olarak işaretlenemez.");
        }

        Status = DownloadStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        FailureReason = null;
    }

    /// <summary>
    /// Hata oluştuğunda durumu günceller ve hata gerekçesini kaydeder.
    /// </summary>
    public void Fail(string reason)
    {
        Status = DownloadStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>
    /// İndirmeyi kullanıcı isteğiyle iptal eder.
    /// </summary>
    public void Cancel()
    {
        if (Status == DownloadStatus.Canceled)
        {
            return;
        }

        Status = DownloadStatus.Canceled;
    }
}
