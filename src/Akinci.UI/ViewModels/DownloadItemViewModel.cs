using System.Collections.ObjectModel;
using System.Diagnostics;
using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Application.UseCases.PauseDownload;
using Akinci.Application.UseCases.ResumeDownload;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Akinci.UI.ViewModels;

public partial class DownloadItemViewModel : ObservableObject
{
    private readonly StartDownloadCommandHandler _startHandler;
    private readonly PauseDownloadCommandHandler _pauseHandler;
    private readonly ResumeDownloadCommandHandler _resumeHandler;
    private readonly IDownloadRepository _repository;
    private CancellationTokenSource? _downloadCts;

    public DownloadTask Task { get; }
    public Guid Id => Task.Id;
    public string Url => Task.Url.Value;
    public string DestinationFilePath => Task.DestinationFilePath;
    public string FileName => Path.GetFileName(Task.DestinationFilePath);

    [ObservableProperty]
    private DownloadStatus _status;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _speedText = "0 B/s";

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private string _statusText = "Beklemede";

    [ObservableProperty]
    private string _statusColor = "#9E9E9E"; // Gri

    [ObservableProperty]
    private SecurityVerdict? _securityVerdict;

    [ObservableProperty]
    private string _securityBadgeText = "Tarama Bekleniyor";

    [ObservableProperty]
    private bool _hasThreat;

    [ObservableProperty]
    private bool _isSafe;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    public ObservableCollection<ChunkViewModel> Chunks { get; } = new();

    public event Action<DownloadItemViewModel>? Deleted;

    public DownloadItemViewModel(
        DownloadTask task,
        StartDownloadCommandHandler startHandler,
        PauseDownloadCommandHandler pauseHandler,
        ResumeDownloadCommandHandler resumeHandler,
        IDownloadRepository repository)
    {
        Task = task;
        _startHandler = startHandler;
        _pauseHandler = pauseHandler;
        _resumeHandler = resumeHandler;
        _repository = repository;

        _status = task.Status;
        _progressPercentage = task.ProgressPercentage;
        _securityVerdict = task.SecurityVerdict;

        // Chunk'ları ekle
        foreach (var chunk in task.Chunks)
        {
            Chunks.Add(new ChunkViewModel(chunk.Index, chunk.Range.Start, chunk.Range.End, chunk.CurrentOffset));
        }

        UpdateStatusState(task.Status);
        UpdateProgressText(task.DownloadedBytes, task.TotalBytes);
        UpdateSecurityDisplay(task.SecurityVerdict);
    }

    public void UpdateFromProgress(DownloadProgressDto dto)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ProgressPercentage = dto.ProgressPercentage;
            SpeedText = dto.Speed.FormatHumanReadable();
            UpdateProgressText(dto.DownloadedBytes, dto.TotalBytes);
            UpdateStatusState(dto.Status);

            // Chunk'ları güncelle
            foreach (var chunkDto in dto.Chunks)
            {
                if (chunkDto.Index < Chunks.Count)
                {
                    Chunks[chunkDto.Index].UpdateProgress(chunkDto.CurrentOffset);
                }
            }

            if (dto.SecurityVerdict != null)
            {
                UpdateSecurityDisplay(dto.SecurityVerdict);
            }
        });
    }

    private void UpdateProgressText(long downloaded, long? total)
    {
        string downloadedMb = $"{downloaded / (1024.0 * 1024.0):F2} MB";
        string totalMb = total.HasValue ? $"{total.Value / (1024.0 * 1024.0):F2} MB" : "Bilinmiyor";
        ProgressText = $"{downloadedMb} / {totalMb} ({ProgressPercentage:F1}%)";
    }

    private void UpdateStatusState(DownloadStatus status)
    {
        Status = status;
        CanPause = status == DownloadStatus.Downloading;
        CanResume = status is DownloadStatus.Paused or DownloadStatus.Failed;

        (StatusText, StatusColor) = status switch
        {
            DownloadStatus.Downloading => ("İndiriliyor", "#2196F3"),  // Mavi
            DownloadStatus.Completed => ("Tamamlandı", "#4CAF50"),    // Yeşil
            DownloadStatus.Paused => ("Duraklatıldı", "#FF9800"),     // Turuncu
            DownloadStatus.Failed => ("Hata Oluştu", "#F44336"),      // Kırmızı
            DownloadStatus.Canceled => ("İptal Edildi", "#9E9E9E"),  // Gri
            _ => ("Beklemede", "#9E9E9E")
        };
    }

    private void UpdateSecurityDisplay(SecurityVerdict? verdict)
    {
        SecurityVerdict = verdict;
        if (verdict == null)
        {
            SecurityBadgeText = "Tarama Bekleniyor";
            HasThreat = false;
            IsSafe = false;
            return;
        }

        SecurityBadgeText = verdict.FormatBadge();
        HasThreat = verdict.HasThreat;
        IsSafe = verdict.IsSafe;
    }

    public async Task StartDownloadAsync()
    {
        _downloadCts = new CancellationTokenSource();
        UpdateStatusState(DownloadStatus.Downloading);

        var progress = new Progress<DownloadProgressDto>(UpdateFromProgress);
        try
        {
            await _startHandler.HandleAsync(new StartDownloadCommand(Task.Id, progress), _downloadCts.Token);
            UpdateStatusState(Task.Status);
            UpdateSecurityDisplay(Task.SecurityVerdict);
        }
        catch (OperationCanceledException)
        {
            UpdateStatusState(DownloadStatus.Paused);
        }
        catch (Exception ex)
        {
            UpdateStatusState(DownloadStatus.Failed);
            StatusText = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task PauseAsync()
    {
        if (_downloadCts != null)
        {
            _downloadCts.Cancel();
        }
        await _pauseHandler.HandleAsync(new PauseDownloadCommand(Task.Id));
        UpdateStatusState(DownloadStatus.Paused);
    }

    [RelayCommand]
    public async Task ResumeAsync()
    {
        _downloadCts = new CancellationTokenSource();
        UpdateStatusState(DownloadStatus.Downloading);

        var progress = new Progress<DownloadProgressDto>(UpdateFromProgress);
        try
        {
            await _resumeHandler.HandleAsync(new ResumeDownloadCommand(Task.Id, progress), _downloadCts.Token);
            UpdateStatusState(Task.Status);
            UpdateSecurityDisplay(Task.SecurityVerdict);
        }
        catch (OperationCanceledException)
        {
            UpdateStatusState(DownloadStatus.Paused);
        }
        catch (Exception ex)
        {
            UpdateStatusState(DownloadStatus.Failed);
            StatusText = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenFolder()
    {
        var directory = Path.GetDirectoryName(DestinationFilePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public void OpenSecurityReport()
    {
        if (!string.IsNullOrEmpty(SecurityVerdict?.ScanUrl))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SecurityVerdict.ScanUrl,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (CanPause)
        {
            await PauseAsync();
        }

        await _repository.DeleteAsync(Task.Id);
        Deleted?.Invoke(this);
    }
}
