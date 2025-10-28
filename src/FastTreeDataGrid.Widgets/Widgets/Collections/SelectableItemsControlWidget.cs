using System;
using System.Collections;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class SelectableItemsControlWidget : ItemsControlWidget
{
    private int _selectedIndex = -1;
    private object? _selectedItem;

    public event EventHandler<WidgetValueChangedEventArgs<object?>>? SelectionChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetSelectedIndexInternal(value, raiseEvents: true);
    }

    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetSelectedItemInternal(value, raiseEvents: true);
    }

    protected override void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);
        CoerceSelection();
    }

    protected override void OnItemsSnapshotChanged()
    {
        base.OnItemsSnapshotChanged();
        CoerceSelection();
    }

    protected virtual void OnSelectionChanged(object? oldItem, object? newItem)
    {
        SelectionChanged?.Invoke(this, new WidgetValueChangedEventArgs<object?>(this, oldItem, newItem));
    }

    protected bool SetSelectedIndexInternal(int index, bool raiseEvents)
    {
        var count = TotalItemCount;
        if (count <= 0)
        {
            index = -1;
        }
        else
        {
            if (index < -1)
            {
                index = -1;
            }
            else if (index >= count)
            {
                index = count - 1;
            }
        }

        if (_selectedIndex == index)
        {
            if (index >= 0 && _selectedItem is null)
            {
                _selectedItem = GetItemAtIndex(index);
                RefreshSelectionVisuals();
            }

            return false;
        }

        var oldItem = _selectedItem;
        _selectedIndex = index;
        _selectedItem = index >= 0 ? GetItemAtIndex(index) : null;
        RefreshSelectionVisuals();

        if (raiseEvents)
        {
            OnSelectionChanged(oldItem, _selectedItem);
        }

        return true;
    }

    protected bool SetSelectedItemInternal(object? item, bool raiseEvents)
    {
        if (ReferenceEquals(_selectedItem, item) || Equals(_selectedItem, item))
        {
            return false;
        }

        var index = FindIndexForItem(item);
        return SetSelectedIndexInternal(index, raiseEvents);
    }

    protected virtual int FindIndexForItem(object? item)
    {
        if (item is null)
        {
            return -1;
        }

        var count = TotalItemCount;
        for (var i = 0; i < count; i++)
        {
            if (!TryGetRow(i, out var row))
            {
                continue;
            }

            if (ReferenceEquals(row.Item, item) || Equals(row.Item, item))
            {
                return i;
            }
        }

        return -1;
    }

    protected void RefreshSelectionVisuals()
    {
        foreach (var pair in RealizedWidgets)
        {
            if (pair.Value is SelectableItemWidget container)
            {
                container.UpdateSelectionState(pair.Key == _selectedIndex);
            }
        }
    }

    internal virtual void HandleContainerPointer(SelectableItemWidget container, WidgetPointerEvent e)
    {
        switch (e.Kind)
        {
            case WidgetPointerEventKind.Entered:
                container.UpdatePointerState(true);
                break;
            case WidgetPointerEventKind.Exited:
                container.UpdatePointerState(false);
                break;
            case WidgetPointerEventKind.Pressed:
                var index = GetContainerIndex(container);
                if (index >= 0)
                {
                    SetSelectedIndexInternal(index, raiseEvents: true);
                }

                break;
        }
    }

    internal int GetContainerIndex(SelectableItemWidget container)
    {
        foreach (var pair in RealizedWidgets)
        {
            if (ReferenceEquals(pair.Value, container))
            {
                return pair.Key;
            }
        }

        return -1;
    }

    protected override void PrepareWidget(Widget widget, FastTreeDataGridRow row)
    {
        base.PrepareWidget(widget, row);

        if (widget is SelectableItemWidget container)
        {
            container.BindRow(row);
            var index = GetContainerIndex(container);
            container.UpdateSelectionState(index == _selectedIndex);
            OnContainerPrepared(container, row, index);
        }
    }

    internal virtual void OnContainerPrepared(SelectableItemWidget container, FastTreeDataGridRow row, int index)
    {
        _ = container;
        _ = row;
        _ = index;
    }

    private void CoerceSelection()
    {
        var count = TotalItemCount;
        if (count <= 0)
        {
            if (_selectedIndex != -1 || _selectedItem is not null)
            {
                var oldItem = _selectedItem;
                _selectedIndex = -1;
                _selectedItem = null;
                RefreshSelectionVisuals();
                OnSelectionChanged(oldItem, null);
            }

            return;
        }

        if (_selectedIndex >= count)
        {
            SetSelectedIndexInternal(count - 1, raiseEvents: true);
            return;
        }

        if (_selectedIndex >= 0)
        {
            _selectedItem = GetItemAtIndex(_selectedIndex);
            RefreshSelectionVisuals();
            return;
        }

        if (_selectedItem is not null)
        {
            SetSelectedItemInternal(_selectedItem, raiseEvents: true);
        }
        else
        {
            RefreshSelectionVisuals();
        }
    }

    private object? GetItemAtIndex(int index)
    {
        return TryGetRow(index, out var row) ? row.Item : null;
    }

    internal abstract class SelectableItemWidget : BorderWidget
    {
        private bool _isSelected;
        private bool _isPointerOver;

        protected SelectableItemWidget(SelectableItemsControlWidget host)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            IsInteractive = true;
            PointerInput += OnPointerInput;
        }

        public SelectableItemsControlWidget Host { get; }

        public IFastTreeDataGridValueProvider? ValueProvider { get; private set; }

        public object? Item { get; private set; }

        public int Level { get; private set; }

        public bool HasChildren { get; private set; }

        public bool IsExpanded { get; private set; }

        public bool IsSelected => _isSelected;

        public bool IsPointerOver => _isPointerOver;

        internal void BindRow(FastTreeDataGridRow row)
        {
            ValueProvider = row.ValueProvider;
            Item = row.Item;
            Level = row.Level;
            HasChildren = row.HasChildren;
            IsExpanded = row.IsExpanded;
            OnRowBound(row);
        }

        internal void UpdateSelectionState(bool isSelected)
        {
            if (_isSelected == isSelected)
            {
                return;
            }

            _isSelected = isSelected;
            OnSelectionChanged(isSelected);
        }

        internal void UpdatePointerState(bool isPointerOver)
        {
            if (_isPointerOver == isPointerOver)
            {
                return;
            }

            _isPointerOver = isPointerOver;
            OnPointerOverChanged(isPointerOver);
        }

        protected virtual void OnRowBound(FastTreeDataGridRow row)
        {
        }

        protected virtual void OnSelectionChanged(bool isSelected)
        {
        }

        protected virtual void OnPointerOverChanged(bool isPointerOver)
        {
        }

        private void OnPointerInput(WidgetPointerEvent e)
        {
            Host.HandleContainerPointer(this, e);
        }
    }
}
