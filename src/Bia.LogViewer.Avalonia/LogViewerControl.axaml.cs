using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Bia.LogViewer.Core;
using ObservableCollections;

namespace Bia.LogViewer.Avalonia;

public sealed partial class LogViewerControl : UserControl
{
    private DataGrid? _grid;
    private LogViewerViewModel? _vm;
    private ObservableList<LogModel>? _subscribedEntries;

    public LogViewerControl()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        DetachEntries();

        _vm = DataContext as LogViewerViewModel;
        _grid ??= this.FindControl<DataGrid>("LogDataGrid");

        if (_vm is null)
            return;

        _vm.PropertyChanged += OnVmPropertyChanged;
        AttachEntries(_vm.FilteredEntries);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            string.Equals(e.PropertyName, nameof(LogViewerViewModel.FilteredEntries), StringComparison.Ordinal)
            && _vm is not null
        )
        {
            DetachEntries();
            AttachEntries(_vm.FilteredEntries);
        }
    }

    private void AttachEntries(ObservableList<LogModel>? entries)
    {
        if (entries is null)
            return;
        _subscribedEntries = entries;
        entries.CollectionChanged += OnEntriesChanged;
    }

    private void DetachEntries()
    {
        if (_subscribedEntries is null)
            return;
        _subscribedEntries.CollectionChanged -= OnEntriesChanged;
        _subscribedEntries = null;
    }

    private void OnEntriesChanged(in NotifyCollectionChangedEventArgs<LogModel> e)
    {
        if (_vm?.AutoScroll != true || _grid is null)
            return;
        if (e.Action != NotifyCollectionChangedAction.Add)
            return;

        var filtered = _vm.FilteredEntries;
        if (filtered is null || filtered.Count == 0)
            return;

        var last = filtered[filtered.Count - 1];
        _grid.ScrollIntoView(last, null);
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;
        DetachEntries();
        _vm = null;
    }
}
