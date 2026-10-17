using CommunityToolkit.Mvvm.ComponentModel;

namespace Akinci.UI.ViewModels;

/// <summary>
/// Arayüzde tek bir parçanın canlı durumunu gösteren ViewModel.
/// </summary>
public partial class ChunkViewModel : ObservableObject
{
    public int Index { get; }
    public long Start { get; }
    public long End { get; }
    public long TotalBytes => End - Start + 1;

    [ObservableProperty]
    private long _currentOffset;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _isCompleted;

    public string Title => $"Parça #{Index + 1}";
    public string RangeText => $"{Start / 1024.0:F0} KB - {End / 1024.0:F0} KB";

    public ChunkViewModel(int index, long start, long end, long currentOffset)
    {
        Index = index;
        Start = start;
        End = end;
        _currentOffset = currentOffset;
        UpdateProgress(currentOffset);
    }

    public void UpdateProgress(long newOffset)
    {
        CurrentOffset = newOffset;
        long downloaded = Math.Clamp(newOffset - Start, 0, TotalBytes);
        ProgressPercentage = TotalBytes == 0 ? 100.0 : Math.Clamp((double)downloaded / TotalBytes * 100.0, 0.0, 100.0);
        IsCompleted = newOffset > End;
    }
}
