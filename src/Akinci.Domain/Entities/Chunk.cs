using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;

namespace Akinci.Domain.Entities;

/// <summary>
/// Çok kanallı indirmede bağımsız olarak indirilen bir dosya parçasını (Chunk) temsil eden Domain Varlığı (Entity).
/// </summary>
public sealed class Chunk
{
    public Guid Id { get; }
    public Guid DownloadTaskId { get; }
    public int Index { get; }
    public ByteRange Range { get; private set; }
    public long CurrentOffset { get; private set; }
    private bool _isStreamCompleted;

    /// <summary>
    /// Parçanın indirilip indirilmediğini belirtir.
    /// CurrentOffset, Range.End değerini 1 bayt geçtiğinde veya akış sonlandığında tüm baytlar alınmış demektir.
    /// </summary>
    public bool IsCompleted => _isStreamCompleted || CurrentOffset > Range.End;

    /// <summary>
    /// Boyutu baştan bilinmeyen açık uçlu akışlarda sunucu akışı kapattığında parçayı tamamlar.
    /// </summary>
    public void CompleteStream(long totalBytesRead)
    {
        CurrentOffset = Range.Start + totalBytesRead;
        _isStreamCompleted = true;
    }

    /// <summary>
    /// Bu parça için şu ana kadar başarıyla indirilen bayt miktarı.
    /// </summary>
    public long DownloadedBytes => Math.Clamp(CurrentOffset - Range.Start, 0, Range.TotalBytes);

    /// <summary>
    /// Bu parçanın tamamlanması için kalan bayt miktarı.
    /// </summary>
    public long RemainingBytes => Range.TotalBytes - DownloadedBytes;

    /// <summary>
    /// Parçanın yüzde kaçının tamamlandığı (0.0 - 100.0).
    /// </summary>
    public double ProgressPercentage => Range.TotalBytes == 0
        ? 100.0
        : Math.Clamp((double)DownloadedBytes / Range.TotalBytes * 100.0, 0.0, 100.0);

    public Chunk(Guid downloadTaskId, int index, ByteRange range)
    {
        Id = Guid.NewGuid();
        DownloadTaskId = downloadTaskId;
        Index = index;
        Range = range ?? throw new ArgumentNullException(nameof(range));
        CurrentOffset = range.Start;
    }

    /// <summary>
    /// Veritabanından veya kalıcı durumdan yeniden yüklerken kullanılan kurucu metot.
    /// </summary>
    public Chunk(Guid id, Guid downloadTaskId, int index, ByteRange range, long currentOffset)
    {
        Id = id;
        DownloadTaskId = downloadTaskId;
        Index = index;
        Range = range ?? throw new ArgumentNullException(nameof(range));

        if (currentOffset < range.Start || currentOffset > range.End + 1)
        {
            throw new InvalidByteRangeException(range.Start, range.End,
                $"Kayıtlı ofset ({currentOffset}) aralık sınırları [{range.Start}, {range.End + 1}] dışında.");
        }

        CurrentOffset = currentOffset;
    }

    /// <summary>
    /// İndirilen bayt miktarını ofsete ekler ve ilerlemeyi günceller.
    /// </summary>
    public void AdvanceOffset(long byteCount)
    {
        if (byteCount <= 0)
        {
            throw new DomainException($"İlerletilecek bayt miktarı pozitif olmalıdır. Verilen: {byteCount}");
        }

        if (IsCompleted)
        {
            throw new DomainException($"Zaten tamamlanmış bir chunk (#{Index}) ilerletilemez.");
        }

        var newOffset = CurrentOffset + byteCount;
        if (newOffset > Range.End + 1)
        {
            throw new InvalidByteRangeException(Range.Start, Range.End,
                $"Chunk #{Index} taşma hatası: Yeni ofset ({newOffset}) parça bitişini ({Range.End + 1}) aşıyor.");
        }

        CurrentOffset = newOffset;
    }

    /// <summary>
    /// Duraklatma/Devam etme senaryolarında ofseti doğrudan ayarlar.
    /// </summary>
    public void SetOffset(long newOffset)
    {
        if (newOffset < Range.Start || newOffset > Range.End + 1)
        {
            throw new InvalidByteRangeException(Range.Start, Range.End,
                $"Ofset ({newOffset}) aralık sınırları [{Range.Start}, {Range.End + 1}] dışında.");
        }

        CurrentOffset = newOffset;
    }

    /// <summary>
    /// Parçayı sıfırdan yeniden indirmek için ofseti başlangıca çeker.
    /// </summary>
    public void Reset()
    {
        CurrentOffset = Range.Start;
    }

    /// <summary>
    /// Dinamik parça bölme (Work Stealing) sırasında bu parçanın bitiş ofsetini orta noktaya daraltır.
    /// </summary>
    public void UpdateEndOffset(long newEnd)
    {
        if (newEnd < CurrentOffset || newEnd >= Range.End)
        {
            throw new DomainException($"Geçersiz yeni bitiş ofseti: {newEnd}. Mevcut ofset: {CurrentOffset}, Mevcut bitiş: {Range.End}");
        }

        Range = new ByteRange(Range.Start, newEnd);
    }
}
