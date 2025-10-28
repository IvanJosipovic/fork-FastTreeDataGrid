using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class ItemsControlWidget : VirtualizingPanelWidget
{
    private IEnumerable? _itemsSource;
    private INotifyCollectionChanged? _collectionChangedSource;
    private WidgetItemsSource? _dataSource;
    private readonly List<object?> _rootItems = new();
    private readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;
    private Func<object?, string?>? _itemKeySelector;
    private bool _usingExternalSource;
    private bool _preserveExpansionOnUpdate = true;

    public ItemsControlWidget()
    {
        _collectionChangedHandler = OnItemsSourceCollectionChanged;
        ItemExtent = 32;
        ClipToBounds = true;
    }

    public new IEnumerable? ItemsSource
    {
        get => _itemsSource;
        set => SetItemsSource(value);
    }

    public Func<object?, IEnumerable?>? ItemChildrenSelector { get; set; }

    public Func<object?, string?>? ItemKeySelector
    {
        get => _itemKeySelector;
        set
        {
            if (_itemKeySelector == value)
            {
                return;
            }

            _itemKeySelector = value;
            RebuildDataSource(preserveExpansion: true);
        }
    }

    public bool PreserveExpansionOnUpdate
    {
        get => _preserveExpansionOnUpdate;
        set => _preserveExpansionOnUpdate = value;
    }

    protected IReadOnlyList<object?> RootItems => _rootItems;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        if (provider is not null && Key is not null)
        {
            ApplyValue(provider.GetValue(item, Key));
        }
        else
        {
            ApplyValue(item);
        }
    }

    protected virtual void ApplyValue(object? value)
    {
        if (value is not ItemsControlWidgetValue itemsValue)
        {
            return;
        }

        var presentationChanged = false;

        if (itemsValue.ItemsSource is not null)
        {
            ItemsSource = itemsValue.ItemsSource;
        }

        if (itemsValue.ChildrenSelector is not null)
        {
            ItemChildrenSelector = itemsValue.ChildrenSelector;
            RebuildDataSource(preserveExpansion: true);
        }

        if (itemsValue.KeySelector is not null)
        {
            ItemKeySelector = itemsValue.KeySelector;
        }

        if (itemsValue.ItemTemplate is not null && !ReferenceEquals(ItemTemplate, itemsValue.ItemTemplate))
        {
            base.ItemTemplate = itemsValue.ItemTemplate;
            presentationChanged = true;
        }

        if (itemsValue.ItemFactory is not null && !ReferenceEquals(ItemFactory, itemsValue.ItemFactory))
        {
            ItemFactory = itemsValue.ItemFactory;
            presentationChanged = true;
        }

        if (itemsValue.ItemExtent.HasValue)
        {
            ItemExtent = itemsValue.ItemExtent.Value;
        }

        if (itemsValue.CrossAxisItemLength.HasValue)
        {
            CrossAxisItemLength = itemsValue.CrossAxisItemLength.Value;
        }

        if (itemsValue.BufferItemCount.HasValue)
        {
            BufferItemCount = Math.Max(0, itemsValue.BufferItemCount.Value);
        }

        if (presentationChanged)
        {
            OnItemPresentationChanged();
        }
    }

    protected virtual void OnItemPresentationChanged()
    {
        ClearRealized();
        RequestViewportUpdate();
    }

    protected virtual void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
    {
        _ = oldValue;
        _ = newValue;
    }

    protected virtual void OnItemsSnapshotChanged()
    {
    }

    protected void RefreshItems(bool preserveExpansion)
    {
        if (_usingExternalSource)
        {
            return;
        }

        if (_itemsSource is null)
        {
            return;
        }

        SnapshotItems(_itemsSource, _rootItems);
        BuildOrUpdateDataSource(_rootItems, preserveExpansion && _preserveExpansionOnUpdate);
        OnItemsSnapshotChanged();
    }

    private void SetItemsSource(IEnumerable? source)
    {
        if (ReferenceEquals(_itemsSource, source))
        {
            return;
        }

        var previous = _itemsSource;

        DetachCollectionChanged();

        _itemsSource = source;
        _rootItems.Clear();

        if (source is null)
        {
            DisposeDataSource();
            base.ItemsSource = null;
            _usingExternalSource = false;
            OnItemsSourceChanged(previous, null);
            OnItemsSnapshotChanged();
            return;
        }

        if (source is IFastTreeDataGridSource gridSource)
        {
            DisposeDataSource();
            base.ItemsSource = gridSource;
            _usingExternalSource = true;
            OnItemsSourceChanged(previous, source);
            RequestViewportUpdate();
            return;
        }

        _usingExternalSource = false;

        SnapshotItems(source, _rootItems);
        BuildOrUpdateDataSource(_rootItems, preserveExpansion: false);
        AttachCollectionChanged(source as INotifyCollectionChanged);
        OnItemsSourceChanged(previous, source);
        OnItemsSnapshotChanged();
    }

    private void RebuildDataSource(bool preserveExpansion)
    {
        if (_usingExternalSource)
        {
            return;
        }

        if (_itemsSource is null)
        {
            DisposeDataSource();
            base.ItemsSource = null;
            return;
        }

        SnapshotItems(_itemsSource, _rootItems);
        BuildOrUpdateDataSource(_rootItems, preserveExpansion);
        OnItemsSnapshotChanged();
    }

    private void BuildOrUpdateDataSource(IReadOnlyList<object?> items, bool preserveExpansion)
    {
        if (_dataSource is null)
        {
            _dataSource = new WidgetItemsSource(items, GetChildren, GetKeySelector());
            base.ItemsSource = _dataSource;
        }
        else
        {
            _dataSource.UpdateItems(items, preserveExpansion);
        }

        RequestViewportUpdate();
    }

    private void DisposeDataSource()
    {
        if (_dataSource is null)
        {
            return;
        }

        _dataSource.Dispose();
        _dataSource = null;
    }

    private void AttachCollectionChanged(INotifyCollectionChanged? notifier)
    {
        DetachCollectionChanged();

        if (notifier is null)
        {
            return;
        }

        _collectionChangedSource = notifier;
        _collectionChangedSource.CollectionChanged += _collectionChangedHandler;
    }

    private void DetachCollectionChanged()
    {
        if (_collectionChangedSource is null)
        {
            return;
        }

        _collectionChangedSource.CollectionChanged -= _collectionChangedHandler;
        _collectionChangedSource = null;
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(
            () => RefreshItems(preserveExpansion: true),
            DispatcherPriority.Background);
    }

    private IEnumerable<object?> GetChildren(object? item)
    {
        if (ItemChildrenSelector is null)
        {
            return Array.Empty<object?>();
        }

        var result = ItemChildrenSelector(item);
        return ConvertChildren(result);
    }

    private static IEnumerable<object?> ConvertChildren(object? value)
    {
        switch (value)
        {
            case null:
                return Array.Empty<object?>();
            case IEnumerable<object?> typed:
                return typed;
            case string text:
                return new object?[] { text };
            case IEnumerable enumerable:
                return enumerable.Cast<object?>();
            default:
                return Array.Empty<object?>();
        }
    }

    private static void SnapshotItems(IEnumerable source, IList<object?> target)
    {
        target.Clear();

        if (source is string text)
        {
            target.Add(text);
            return;
        }

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private Func<object?, string>? GetKeySelector()
    {
        if (_itemKeySelector is null)
        {
            return null;
        }

        return item => _itemKeySelector(item) ?? string.Empty;
    }

    private sealed class WidgetItemsSource : FastTreeDataGridDynamicSource<object?>
    {
        public WidgetItemsSource(
            IEnumerable<object?> items,
            Func<object?, IEnumerable<object?>> childrenSelector,
            Func<object?, string>? keySelector)
            : base(items ?? Array.Empty<object?>(), childrenSelector, keySelector)
        {
        }

        public void UpdateItems(IEnumerable<object?> items, bool preserveExpansion)
        {
            var snapshot = items as IList<object?> ?? items.ToList();
            ResetWithSnapshot(snapshot, preserveExpansion);
        }
    }
}
