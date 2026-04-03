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

    [ObservableProperty]
    private ObservableList<LogModel>? _filteredEntries;

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
        RebuildFilteredList();

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

                if (_levelMask == 0 || (_levelMask & (1 << (int)item.LogLevel)) != 0)
                    FilteredEntries?.Add(item);
                break;

            case NotifyCollectionChangedAction.Reset:
                Array.Clear(_levelCounts, 0, _levelCounts.Length);
                SyncCountProperties();
                RebuildFilteredList();
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
        RebuildFilteredList();
    }

    /// <summary>
    /// Rebuilds the filtered list from the source entries.
    /// Allocates one <see cref="ObservableList{T}"/> per call — unavoidable since
    /// the DataGrid is bound to it. The intermediate array is sized exactly to avoid
    /// list-resize copies.
    /// </summary>
    private void RebuildFilteredList()
    {
        if (_entries is null)
            return;

        if (_levelMask == 0)
        {
            // No filter — shallow copy; single array allocation sized to count.
            FilteredEntries = new ObservableList<LogModel>(_entries);
            return;
        }

        // Pre-count matching entries to allocate the exact capacity — zero resizes.
        var count = 0;
        foreach (var entry in _entries)
        {
            if ((_levelMask & (1 << (int)entry.LogLevel)) != 0)
                count++;
        }

        var buffer = new LogModel[count];
        var idx = 0;
        foreach (var entry in _entries)
        {
            if ((_levelMask & (1 << (int)entry.LogLevel)) != 0)
                buffer[idx++] = entry;
        }

        FilteredEntries = new ObservableList<LogModel>(buffer);
    }

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
        RebuildFilteredList();
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
    }
}
