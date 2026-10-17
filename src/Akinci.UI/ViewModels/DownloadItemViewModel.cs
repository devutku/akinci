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
    private readonly INotificationService? _notificationService;
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
    private bool _isIndeterminate;

    [ObservableProperty]
    private string _speedText = "0 B/s";

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private string _totalSizeText = "";

    [ObservableProperty]
    private string _downloadedAndTotalText = "";

    [ObservableProperty]
    private string _etaText = "";

    [ObservableProperty]
    private string _connectionsText = "";

    [ObservableProperty]
    private string _progressBarColor = "#38BDF8";

    [ObservableProperty]
    private string _statusText = "Beklemede";

    [ObservableProperty]
    private string _statusColor = "#94A3B8";

    [ObservableProperty]
    private string _statusTextColor = "#94A3B8";

    [ObservableProperty]
    private string _statusBgColor = "#1E293B";

    [ObservableProperty]
    private string _statusBorderColor = "#334155";

    [ObservableProperty]
    private SecurityVerdict? _securityVerdict;

    [ObservableProperty]
    private string _securityBadgeText = "Tarama Bekleniyor";

    [ObservableProperty]
    private string _securityBgColor = "#1E293B";

    [ObservableProperty]
    private string _securityBorderColor = "#334155";

    [ObservableProperty]
    private string _securityTextColor = "#94A3B8";

    [ObservableProperty]
    private bool _hasThreat;

    [ObservableProperty]
    private bool _isSafe;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private bool _isCompleted;

    public ObservableCollection<ChunkViewModel> Chunks { get; } = new();

    public event Action<DownloadItemViewModel>? Deleted;

    public DownloadItemViewModel(
        DownloadTask task,
        StartDownloadCommandHandler startHandler,
        PauseDownloadCommandHandler pauseHandler,
        ResumeDownloadCommandHandler resumeHandler,
        IDownloadRepository repository,
        INotificationService? notificationService = null)
    {
        Task = task;
        _startHandler = startHandler;
        _pauseHandler = pauseHandler;
        _resumeHandler = resumeHandler;
        _repository = repository;
        _notificationService = notificationService;

        _status = task.Status;
        _progressPercentage = task.ProgressPercentage;
        _securityVerdict = task.SecurityVerdict;

        // Chunk'ları ekle
        foreach (var chunk in task.Chunks)
        {
            Chunks.Add(new ChunkViewModel(chunk.Index, chunk.Range.Start, chunk.Range.End, chunk.CurrentOffset));
        }

        UpdateStatusState(task.Status);
        UpdateProgressMetrics(task.DownloadedBytes, task.TotalBytes, 0);
        UpdateSecurityDisplay(task.SecurityVerdict);
    }

    public void UpdateFromProgress(DownloadProgressDto dto)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ProgressPercentage = dto.ProgressPercentage;
            SpeedText = dto.Speed.FormatHumanReadable();
            UpdateProgressMetrics(dto.DownloadedBytes, dto.TotalBytes, dto.Speed.BytesPerSecond);
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

    private void UpdateProgressMetrics(long downloaded, long? total, double speedBps)
    {
        TotalSizeText = total.HasValue ? FormatBytes(total.Value) : "Boyut Belirsiz";
        string downloadedStr = FormatBytes(downloaded);
        string totalStr = total.HasValue ? FormatBytes(total.Value) : "Bilinmiyor";

        DownloadedAndTotalText = $"{downloadedStr} / {totalStr}";
        ProgressText = total.HasValue
            ? $"{downloadedStr} / {totalStr} ({ProgressPercentage:F1}%)"
            : $"{downloadedStr} indirildi";
        ConnectionsText = Chunks.Count > 0 ? $"{Chunks.Count} bağlantı" : "";

        bool hasUnknownSize = !total.HasValue || total.Value <= 0;
        IsIndeterminate = hasUnknownSize && Status == DownloadStatus.Downloading;

        UpdateEta(downloaded, total, speedBps);
    }

    private void UpdateEta(long downloaded, long? total, double speedBps)
    {
        if (Status != DownloadStatus.Downloading || speedBps <= 0 || !total.HasValue)
        {
            EtaText = Status switch
            {
                DownloadStatus.Completed => "Tamamlandı",
                DownloadStatus.Paused => "Duraklatıldı",
                DownloadStatus.Failed => "Hata",
                DownloadStatus.Canceled => "İptal",
                _ => ""
            };
            return;
        }

        long remainingBytes = Math.Max(0, total.Value - downloaded);
        double seconds = remainingBytes / speedBps;

        if (seconds < 1)
        {
            EtaText = "Kalan: < 1 sn";
        }
        else if (seconds < 60)
        {
            EtaText = $"Kalan: {(int)seconds} sn";
        }
        else if (seconds < 3600)
        {
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            EtaText = $"Kalan: {mins} dk {secs} sn";
        }
        else
        {
            int hours = (int)(seconds / 3600);
            int mins = (int)((seconds % 3600) / 60);
            EtaText = $"Kalan: {hours} sa {mins} dk";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dbl = bytes;
        while (dbl >= 1024 && i < suffixes.Length - 1)
        {
            dbl /= 1024;
            i++;
        }
        return $"{dbl:0.##} {suffixes[i]}";
    }

    private void UpdateStatusState(DownloadStatus status)
    {
        Status = status;
        CanPause = status == DownloadStatus.Downloading;
        CanResume = status is DownloadStatus.Paused or DownloadStatus.Failed or DownloadStatus.Pending;
        IsCompleted = status == DownloadStatus.Completed;
        IsIndeterminate = (!Task.TotalBytes.HasValue || Task.TotalBytes.Value <= 0) && status == DownloadStatus.Downloading;

        (StatusText, StatusTextColor, StatusBgColor, StatusBorderColor, ProgressBarColor) = status switch
        {
            DownloadStatus.Downloading => ("İndiriliyor", "#93C5FD", "#0C2B45", "#1E4E79", "#38BDF8"),  // Pastel Mavi
            DownloadStatus.Completed   => ("Tamamlandı", "#34D399", "#063A29", "#0D6348", "#10B981"),   // Pastel Yeşil
            DownloadStatus.Paused      => ("Duraklatıldı", "#94A3B8", "#1E293B", "#334155", "#475569"), // Monokrom Gri
            DownloadStatus.Failed      => ("Hata Oluştu", "#F87171", "#3C1313", "#6E2222", "#EF4444"),  // Pastel Kırmızı
            DownloadStatus.Canceled    => ("İptal Edildi", "#64748B", "#182234", "#283548", "#334155"),
            _                          => ("Beklemede", "#94A3B8", "#1E293B", "#334155", "#38BDF8")
        };
        StatusColor = StatusTextColor;
    }

    private void UpdateSecurityDisplay(SecurityVerdict? verdict)
    {
        SecurityVerdict = verdict;
        if (verdict == null)
        {
            SecurityBadgeText = "Tarama Bekleniyor";
            SecurityBgColor = "#1E293B";
            SecurityBorderColor = "#334155";
            SecurityTextColor = "#64748B";
            HasThreat = false;
            IsSafe = false;
            return;
        }

        SecurityBadgeText = verdict.FormatBadge();
        HasThreat = verdict.HasThreat;
        IsSafe = verdict.IsSafe;

        if (verdict.HasThreat)
        {
            SecurityBgColor = "#3C1313";
            SecurityBorderColor = "#6E2222";
            SecurityTextColor = "#F87171";
        }
        else if (verdict.IsSafe)
        {
            SecurityBgColor = "#063A29";
            SecurityBorderColor = "#0D6348";
            SecurityTextColor = "#34D399";
        }
        else
        {
            SecurityBgColor = "#1E293B";
            SecurityBorderColor = "#334155";
            SecurityTextColor = "#94A3B8";
        }
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
            OnDownloadFinished(true);
        }
        catch (OperationCanceledException)
        {
            UpdateStatusState(DownloadStatus.Paused);
        }
        catch (Exception ex)
        {
            UpdateStatusState(DownloadStatus.Failed);
            StatusText = $"Hata: {ex.Message}";
            OnDownloadFinished(false, ex);
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
            OnDownloadFinished(true);
        }
        catch (OperationCanceledException)
        {
            UpdateStatusState(DownloadStatus.Paused);
        }
        catch (Exception ex)
        {
            UpdateStatusState(DownloadStatus.Failed);
            StatusText = $"Hata: {ex.Message}";
            OnDownloadFinished(false, ex);
        }
    }

    private void OnDownloadFinished(bool isSuccess, Exception? ex = null)
    {
        if (isSuccess && Task.Status == DownloadStatus.Completed)
        {
            _notificationService?.ShowNotification(
                "İndirme Tamamlandı",
                $"{FileName} başarıyla indirildi.",
                NotificationType.Success);

            if (Task.SecurityVerdict != null)
            {
                if (Task.SecurityVerdict.HasThreat)
                {
                    _notificationService?.ShowNotification(
                        "Güvenlik Uyarısı!",
                        $"{FileName}: {Task.SecurityVerdict.FormatBadge()}",
                        NotificationType.Warning);
                }
                else if (Task.SecurityVerdict.IsSafe)
                {
                    _notificationService?.ShowNotification(
                        "Güvenlik Taraması Temiz",
                        $"{FileName}: {Task.SecurityVerdict.FormatBadge()}",
                        NotificationType.Success);
                }
            }
        }
        else if (!isSuccess && ex != null)
        {
            _notificationService?.ShowNotification(
                "İndirme Hatası",
                $"{FileName}: {ex.Message}",
                NotificationType.Error);
        }
    }

    [RelayCommand]
    public void OpenFile()
    {
        try
        {
            if (File.Exists(DestinationFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DestinationFilePath,
                    UseShellExecute = true
                });
            }
            else
            {
                OpenFolder();
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowNotification(
                "Dosya Açılamadı",
                $"Dosya çalıştırılamadı: {ex.Message}",
                NotificationType.Error);
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

    public Func<DownloadItemViewModel, Task<Views.DeleteDownloadOption>>? ConfirmDeleteHandler { get; set; }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        var option = Views.DeleteDownloadOption.RemoveFromListOnly;
        if (ConfirmDeleteHandler != null)
        {
            option = await ConfirmDeleteHandler(this);
            if (option == Views.DeleteDownloadOption.Cancel)
            {
                return;
            }
        }

        if (CanPause)
        {
            await PauseAsync();
        }

        if (option == Views.DeleteDownloadOption.DeleteFileAndList)
        {
            try
            {
                if (File.Exists(DestinationFilePath))
                {
                    File.Delete(DestinationFilePath);
                }

                var dir = Path.GetDirectoryName(DestinationFilePath);
                var fn = Path.GetFileName(DestinationFilePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, fn + ".*"))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch
            {
                // Dosya meşgul veya kilitli olabilir
            }
        }

        await _repository.DeleteAsync(Task.Id);
        Deleted?.Invoke(this);
    }
}
