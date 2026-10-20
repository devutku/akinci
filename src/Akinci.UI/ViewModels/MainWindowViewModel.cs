using System.Collections.ObjectModel;
using Akinci.Application.Ports;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.PauseDownload;
using Akinci.Application.UseCases.ResumeDownload;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Akinci.UI.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Akinci.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly CreateDownloadCommandHandler _createHandler;
    private readonly StartDownloadCommandHandler _startHandler;
    private readonly PauseDownloadCommandHandler _pauseHandler;
    private readonly ResumeDownloadCommandHandler _resumeHandler;
    private readonly IDownloadRepository _repository;
    private readonly SettingsService _settingsService;
    private readonly INotificationService? _notificationService;
    private readonly IFileStorageService? _fileStorage;

    public ObservableCollection<DownloadItemViewModel> AllDownloads { get; } = new();
    public ObservableCollection<DownloadItemViewModel> FilteredDownloads { get; } = new();

    public Func<DownloadItemViewModel, Task<Views.DeleteDownloadOption>>? ConfirmDeleteHandler { get; set; }

    [ObservableProperty]
    private string _selectedFilter = "Tümü";

    public bool IsAllSelected => SelectedFilter == "Tümü";
    public bool IsDownloadingSelected => SelectedFilter == "İndirilenler";
    public bool IsPausedSelected => SelectedFilter == "Duraklatılanlar";
    public bool IsCompletedSelected => SelectedFilter == "Tamamlananlar";

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _totalSpeedText = "0 B/s";

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _totalCount;

    public MainWindowViewModel(
        CreateDownloadCommandHandler createHandler,
        StartDownloadCommandHandler startHandler,
        PauseDownloadCommandHandler pauseHandler,
        ResumeDownloadCommandHandler resumeHandler,
        IDownloadRepository repository,
        SettingsService settingsService,
        INotificationService? notificationService = null,
        IFileStorageService? fileStorage = null)
    {
        _createHandler = createHandler;
        _startHandler = startHandler;
        _pauseHandler = pauseHandler;
        _resumeHandler = resumeHandler;
        _repository = repository;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _fileStorage = fileStorage;

        // Periyodik global hız ve sayaç güncelleme zamanlayıcısı (1 sn)
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => UpdateAggregateStatistics();
        timer.Start();
    }

    public async Task InitializeAsync()
    {
        var tasks = await _repository.GetAllAsync();
        Dispatcher.UIThread.Post(() =>
        {
            AllDownloads.Clear();
            foreach (var task in tasks)
            {
                var itemVm = CreateItemViewModel(task);
                AllDownloads.Add(itemVm);
            }
            ApplyFilter();
            UpdateAggregateStatistics();
            ProcessQueue();
        });
    }

    private DownloadItemViewModel CreateItemViewModel(DownloadTask task)
    {
        var itemVm = new DownloadItemViewModel(task, _startHandler, _pauseHandler, _resumeHandler, _repository, _notificationService, _fileStorage);
        itemVm.Deleted += OnItemDeleted;
        itemVm.StateChanged += OnItemStateChanged;
        itemVm.ConfirmDeleteHandler = ConfirmDeleteHandler;
        return itemVm;
    }

    private void OnItemStateChanged(DownloadItemViewModel item)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateAggregateStatistics();
            ProcessQueue();
        });
    }

    private void OnItemDeleted(DownloadItemViewModel item)
    {
        item.Deleted -= OnItemDeleted;
        item.StateChanged -= OnItemStateChanged;
        AllDownloads.Remove(item);
        FilteredDownloads.Remove(item);
        UpdateAggregateStatistics();
        ProcessQueue();
    }

    public async Task AddDownloadAsync(
        string url,
        string destinationDir,
        int connections,
        string? customName,
        DownloadCredentials? credentials)
    {
        var command = new CreateDownloadCommand(url, destinationDir, connections, customName, credentials);
        var task = await _createHandler.HandleAsync(command);

        var itemVm = CreateItemViewModel(task);

        Dispatcher.UIThread.Post(() =>
        {
            AllDownloads.Insert(0, itemVm);
            ApplyFilter();
            UpdateAggregateStatistics();
            ProcessQueue();
        });
    }

    public void ProcessQueue()
    {
        int maxConcurrent = _settingsService.Current.MaxConcurrentDownloads;
        if (maxConcurrent <= 0)
        {
            var pending = AllDownloads.Where(d => d.Status == DownloadStatus.Pending).ToList();
            foreach (var item in pending)
            {
                _ = item.StartDownloadAsync();
            }
            return;
        }

        int activeCount = AllDownloads.Count(d => d.Status == DownloadStatus.Downloading);
        int availableSlots = maxConcurrent - activeCount;

        if (availableSlots > 0)
        {
            var nextToRun = AllDownloads
                .Where(d => d.Status == DownloadStatus.Pending)
                .Take(availableSlots)
                .ToList();

            foreach (var item in nextToRun)
            {
                _ = item.StartDownloadAsync();
            }
        }
    }

    partial void OnSelectedFilterChanged(string value)
    {
        OnPropertyChanged(nameof(IsAllSelected));
        OnPropertyChanged(nameof(IsDownloadingSelected));
        OnPropertyChanged(nameof(IsPausedSelected));
        OnPropertyChanged(nameof(IsCompletedSelected));
        ApplyFilter();
    }
    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    public void ApplyFilter()
    {
        FilteredDownloads.Clear();
        var query = SearchQuery?.Trim().ToLowerInvariant() ?? "";

        foreach (var item in AllDownloads)
        {
            bool matchesSearch = string.IsNullOrEmpty(query) ||
                                 item.FileName.ToLowerInvariant().Contains(query) ||
                                 item.Url.ToLowerInvariant().Contains(query);

            if (!matchesSearch) continue;

            bool matchesFilter = SelectedFilter switch
            {
                "İndirilenler" => item.Status == DownloadStatus.Downloading,
                "Duraklatılanlar" => item.Status is DownloadStatus.Paused or DownloadStatus.Failed,
                "Tamamlananlar" => item.Status == DownloadStatus.Completed,
                _ => true // "Tümü"
            };

            if (matchesFilter)
            {
                FilteredDownloads.Add(item);
            }
        }
    }

    private void UpdateAggregateStatistics()
    {
        TotalCount = AllDownloads.Count;
        ActiveCount = AllDownloads.Count(d => d.Status == DownloadStatus.Downloading);
        CompletedCount = AllDownloads.Count(d => d.Status == DownloadStatus.Completed);

        // Hızları doğrudan sayısal değerlerden topla (Kültür bağımsız ve hatasız)
        double totalBps = AllDownloads
            .Where(d => d.Status == DownloadStatus.Downloading)
            .Sum(d => d.CurrentSpeedBytesPerSecond);

        TotalSpeedText = new DownloadSpeed(totalBps).FormatHumanReadable();
    }

    [RelayCommand]
    public async Task PauseAllAsync()
    {
        var downloadingItems = AllDownloads.Where(d => d.CanPause).ToList();
        foreach (var item in downloadingItems)
        {
            await item.PauseAsync();
        }
    }

    [RelayCommand]
    public async Task ResumeAllAsync()
    {
        var pausedItems = AllDownloads.Where(d => d.CanResume).ToList();
        foreach (var item in pausedItems)
        {
            item.MarkAsPending();
        }
        ProcessQueue();
        await Task.CompletedTask;
    }
}
