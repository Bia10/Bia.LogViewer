using System.Collections.Specialized;
using Bia.LogViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ObservableCollections;

namespace Bia.LogViewer.Avalonia;

public sealed partial class LogViewerViewModel : ObservableObject, IDisposable
{
    private readonly IClipboardService _clipboardService;
    private readonly IReadOnlyObservableList<LogModel>? _entries;
    private LogModel _selectedLogItem;
    private bool _hasSelection;
    private bool _isDisposed;

    // O(1) per-log counter array: indexed by (int)LogLevel (0=Trace … 5=Critical, 6=None)
    private readonly int[] _levelCounts = new int[7];

    // Bitmask over LogLevel values — avoids HashSet allocation per toggle.
    // Bit i is set ⟺ LogLevel i is selected. 0 means "show all".
    private int _levelMask;

    [ObservableProperty]
    private int _errorsCount;

    [ObservableProperty]
    private int _warningsCount;

    [ObservableProperty]
    private int _messagesCount;

    [ObservableProperty]
    private int _criticalCount;

    private ISynchronizedView<LogModel, LogModel>? _filteredView;

    public INotifyCollectionChangedSynchronizedViewList<LogModel>? FilteredView { get; private set; }

    public bool IsInformationSelected
    {
        get => HasLevelFlag(LogLevel.Information);
        set => SetLevelFlag(LogLevel.Information, value);
    }

    public bool IsWarningSelected
    {
        get => HasLevelFlag(LogLevel.Warning);
        set => SetLevelFlag(LogLevel.Warning, value);
    }

    public bool IsErrorSelected
    {
        get => HasLevelFlag(LogLevel.Error);
        set => SetLevelFlag(LogLevel.Error, value);
    }

    public bool IsCriticalSelected
    {
        get => HasLevelFlag(LogLevel.Critical);
        set => SetLevelFlag(LogLevel.Critical, value);
    }

    public LogViewerViewModel(ILogEntrySource entrySource, IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
        _entries = entrySource.Entries;

        AutoScroll = true;
        CopyOnSelect = false;

        if (_entries is null)
            return;

        // Seed counts from existing entries — no allocation
        foreach (var entry in _entries)
            _levelCounts[(int)entry.LogLevel]++;

        SyncCountProperties();

        _filteredView = _entries.CreateView(static entry => entry);
        _filteredView.AttachFilter(PassesFilter);
        FilteredView = _filteredView.ToNotifyCollectionChanged();

        _entries.CollectionChanged += OnLogCollectionChanged;
    }

    public bool AutoScroll { get; init; }
    public bool CopyOnSelect { get; init; }

    public LogModel SelectedLogItem
    {
        get => _selectedLogItem;
        set
        {
            if (_hasSelection && _selectedLogItem == value)
                return;

            _selectedLogItem = value;
            _hasSelection = true;
            if (CopyOnSelect)
                _ = CopySelectedLogAsync();
        }
    }

    private void OnLogCollectionChanged(in NotifyCollectionChangedEventArgs<LogModel> e)
    {
        if (_entries is null)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var item = e.NewItem;
                _levelCounts[(int)item.LogLevel]++;
                SyncCountProperties();
                break;

            case NotifyCollectionChangedAction.Reset:
                Array.Clear(_levelCounts, 0, _levelCounts.Length);
                SyncCountProperties();
                _filteredView?.AttachFilter(PassesFilter);
                break;
        }
    }

    private async Task CopySelectedLogAsync()
    {
        if (_hasSelection && SelectedLogItem.Message is not null)
            await _clipboardService.CopyToClipboardAsync(SelectedLogItem.Message).ConfigureAwait(true);
    }

    [RelayCommand]
    private void ToggleLogLevelFilter(LogLevel logLevel)
    {
        _levelMask ^= 1 << (int)logLevel;
        OnPropertyChanged(nameof(IsInformationSelected));
        OnPropertyChanged(nameof(IsWarningSelected));
        OnPropertyChanged(nameof(IsErrorSelected));
        OnPropertyChanged(nameof(IsCriticalSelected));
        _filteredView?.AttachFilter(PassesFilter);
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="entry"/> matches the active level mask.
    /// When no levels are selected (<see cref="_levelMask"/> == 0) every entry passes.
    /// </summary>
    private bool PassesFilter(LogModel entry) =>
        _levelMask == 0 || (_levelMask & (1 << (int)entry.LogLevel)) != 0;

    private bool HasLevelFlag(LogLevel level) => (_levelMask & (1 << (int)level)) != 0;

    private void SetLevelFlag(LogLevel level, bool isSelected)
    {
        var bit = 1 << (int)level;
        var prev = _levelMask;

        if (isSelected)
            _levelMask |= bit;
        else
            _levelMask &= ~bit;

        if (_levelMask == prev)
            return;

        OnPropertyChanged(nameof(IsInformationSelected));
        OnPropertyChanged(nameof(IsWarningSelected));
        OnPropertyChanged(nameof(IsErrorSelected));
        OnPropertyChanged(nameof(IsCriticalSelected));
        _filteredView?.AttachFilter(PassesFilter);
    }

    private void SyncCountProperties()
    {
        ErrorsCount = _levelCounts[(int)LogLevel.Error];
        WarningsCount = _levelCounts[(int)LogLevel.Warning];
        MessagesCount = _levelCounts[(int)LogLevel.Information];
        CriticalCount = _levelCounts[(int)LogLevel.Critical];
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        if (_entries is not null)
            _entries.CollectionChanged -= OnLogCollectionChanged;

        _filteredView?.Dispose();
        FilteredView?.Dispose();
    }
}
