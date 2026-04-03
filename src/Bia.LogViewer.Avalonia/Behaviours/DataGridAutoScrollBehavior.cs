using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Bia.LogViewer.Avalonia.Behaviours;

/// <summary>
/// Avalonia behavior that automatically scrolls a DataGrid to the last item
/// when new entries are added to the bound <see cref="Items"/> collection and
/// <see cref="AutoScroll"/> is <see langword="true"/>.
/// Use this instead of code-behind <c>CollectionChanged</c> subscriptions to keep views
/// free of direct ViewModel coupling.
/// </summary>
public sealed class DataGridAutoScrollBehavior : Behavior<DataGrid>
{
    /// <summary>Collection to observe for new items. Typically bound to a ViewModel property.</summary>
    public static readonly StyledProperty<INotifyCollectionChanged?> ItemsProperty = AvaloniaProperty.Register<
        DataGridAutoScrollBehavior,
        INotifyCollectionChanged?
    >(nameof(Items));

    /// <summary>When <see langword="true"/>, the DataGrid scrolls to the last item on each Add.</summary>
    public static readonly StyledProperty<bool> AutoScrollProperty = AvaloniaProperty.Register<
        DataGridAutoScrollBehavior,
        bool
    >(nameof(AutoScroll), defaultValue: true);

    private INotifyCollectionChanged? _subscribedCollection;

    static DataGridAutoScrollBehavior()
    {
        ItemsProperty.Changed.AddClassHandler<DataGridAutoScrollBehavior>((b, e) => b.OnItemsChanged(e));
    }

    public INotifyCollectionChanged? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public bool AutoScroll
    {
        get => GetValue(AutoScrollProperty);
        set => SetValue(AutoScrollProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (Items is { } initial)
        {
            _subscribedCollection = initial;
            initial.CollectionChanged += OnCollectionChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        DetachCollection();
    }

    private void OnItemsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        DetachCollection();
        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            _subscribedCollection = newCollection;
            newCollection.CollectionChanged += OnCollectionChanged;
        }
    }

    private void DetachCollection()
    {
        if (_subscribedCollection is null)
            return;
        _subscribedCollection.CollectionChanged -= OnCollectionChanged;
        _subscribedCollection = null;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!AutoScroll)
            return;
        if (e.Action != NotifyCollectionChangedAction.Add)
            return;
        if (AssociatedObject is null)
            return;

        // Prefer the last item from the event's NewItems list; fall back to iterating the source.
        object? last = e.NewItems?.Count > 0 ? e.NewItems[e.NewItems.Count - 1] : null;

        if (last is null && sender is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
                last = item;
        }

        if (last is not null)
            AssociatedObject.ScrollIntoView(last, null);
    }
}
