using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridHeaderPresenter : Canvas
{
    private const double GripWidth = 8;
    private const double DragActivationThreshold = 4;

    private readonly List<ContentControl> _cells = new();
    private readonly List<Thumb> _grips = new();
    private readonly List<Border> _separators = new();
    private readonly List<double> _visualColumnOffsets = new();
    private IReadOnlyList<FastTreeDataGridColumn> _columns = Array.Empty<FastTreeDataGridColumn>();
    private readonly Dictionary<int, ColumnResizeState> _activeColumnResizes = new();
    private readonly IBrush _separatorBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210));
    private readonly IBrush _sortGlyphBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));
    private readonly Geometry _ascendingGeometry = StreamGeometry.Parse("M0,4 L3.5,0 7,4 Z");
    private readonly Geometry _descendingGeometry = StreamGeometry.Parse("M0,0 L7,0 3.5,4 Z");

    private readonly Border _reorderIndicator = new()
    {
        Width = 3,
        Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
        IsHitTestVisible = false,
        CornerRadius = new CornerRadius(1.5),
        Opacity = 0,
    };

    private double _horizontalOffset;
    private double _viewportWidth;
    private int? _pressedColumnIndex;
    private Point _pressPoint;
    private bool _isReordering;
    private int _reorderInsertIndex;
    private bool _hasPointerMoved;
    private bool _suppressSort;

    public FastTreeDataGridHeaderPresenter()
    {
        Children.Add(_reorderIndicator);
        _reorderIndicator.SetValue(Canvas.ZIndexProperty, 50);
    }

    public double HeaderHeight { get; set; } = 32;

    public event Action<int, double>? ColumnResizeRequested;
    public event Action<int, FastTreeDataGridSortDirection?, KeyModifiers>? ColumnSortRequested;
    public event Action<int, int>? ColumnReorderRequested;
    public event Action<int, FastTreeDataGridPinnedPosition>? ColumnPinRequested;
    public event Action<int, bool>? ColumnAutoSizeRequested;
    public event Action<int>? ColumnMoveLeftRequested;
    public event Action<int>? ColumnMoveRightRequested;
    public event Action<int>? ColumnHideRequested;
    public event Action? ExpandAllRequested;
    public event Action? CollapseAllRequested;
    public event Action<bool>? AutoSizeAllRequested;
    public event Action<int, ContentControl>? ColumnFilterRequested;
    public event Action<int>? ColumnFilterCleared;

    public void BindColumns(
        IReadOnlyList<FastTreeDataGridColumn> columns,
        IReadOnlyList<double> widths,
        double horizontalOffset,
        double viewportWidth)
    {
        if (!ReferenceEquals(_columns, columns))
        {
            _columns = columns;
            _activeColumnResizes.Clear();
        }

        _horizontalOffset = horizontalOffset;
        _viewportWidth = viewportWidth;

        RemoveInvalidResizeStates(columns.Count);
        EnsureCellCount(columns.Count);
        EnsureGripCount(columns.Count);

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var cell = _cells[i];
            cell.Tag = i;
            cell.Width = widths[i];
            cell.Height = HeaderHeight;
            cell.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            cell.Content = CreateHeaderContent(column, i);
            AttachContextMenu(cell, column, i);

            if (i < _grips.Count)
            {
                var grip = _grips[i];
                grip.Tag = i;
                grip.Width = GripWidth;
                grip.Height = HeaderHeight;
                grip.IsVisible = column.CanUserResize;
                grip.IsHitTestVisible = column.CanUserResize;
            }
        }

        Width = Math.Max(widths.Sum(), viewportWidth);
        Height = HeaderHeight;

        UpdateLayout(widths);
    }

    public void UpdateWidths(IReadOnlyList<double> widths, double horizontalOffset, double viewportWidth)
    {
        _horizontalOffset = horizontalOffset;
        _viewportWidth = viewportWidth;
        RemoveInvalidResizeStates(widths.Count);

        for (var i = 0; i < _cells.Count && i < widths.Count; i++)
        {
            var cell = _cells[i];
            cell.Width = widths[i];

            if (i < _grips.Count)
            {
                var grip = _grips[i];
                grip.Width = GripWidth;
                grip.Height = HeaderHeight;
                grip.IsVisible = i < _columns.Count && _columns[i].CanUserResize;
                grip.IsHitTestVisible = grip.IsVisible;
            }
        }

        Width = Math.Max(widths.Sum(), viewportWidth);
        Height = HeaderHeight;

        UpdateLayout(widths);
    }

    private void UpdateLayout(IReadOnlyList<double> widths)
    {
        if (_columns.Count == 0 || widths.Count == 0)
        {
            return;
        }

        var leftIndices = new List<int>();
        var rightIndices = new List<int>();
        var bodyIndices = new List<int>();

        for (var i = 0; i < _columns.Count && i < widths.Count; i++)
        {
            var column = _columns[i];
            switch (column.PinnedPosition)
            {
                case FastTreeDataGridPinnedPosition.Left:
                    leftIndices.Add(i);
                    break;
                case FastTreeDataGridPinnedPosition.Right:
                    rightIndices.Add(i);
                    break;
                default:
                    bodyIndices.Add(i);
                    break;
            }
        }

        double SumWidths(IEnumerable<int> indices)
        {
            var total = 0d;
            foreach (var index in indices)
            {
                if (index < widths.Count && double.IsFinite(widths[index]))
                {
                    total += Math.Max(0, widths[index]);
                }
            }

            return total;
        }

        var leftWidth = SumWidths(leftIndices);
        var rightWidth = SumWidths(rightIndices);
        var bodyWidth = SumWidths(bodyIndices);
        var totalWidth = leftWidth + bodyWidth + rightWidth;

        Width = Math.Max(totalWidth, _viewportWidth);

        _visualColumnOffsets.Clear();
        _visualColumnOffsets.EnsureCapacity(_columns.Count);

        var visualPositions = new Dictionary<int, double>();

        var leftOffset = 0d;
        foreach (var index in leftIndices)
        {
            visualPositions[index] = leftOffset + _horizontalOffset;
            leftOffset += widths[index];
        }

        var bodyOffset = leftWidth;
        foreach (var index in bodyIndices)
        {
            visualPositions[index] = bodyOffset;
            bodyOffset += widths[index];
        }

        var viewportWidth = _viewportWidth > 0 ? _viewportWidth : totalWidth;
        var baseRight = Math.Max(viewportWidth, leftWidth + bodyWidth) - rightWidth;
        if (baseRight < leftWidth)
        {
            baseRight = leftWidth;
        }

        var rightOffset = 0d;
        foreach (var index in rightIndices)
        {
            visualPositions[index] = baseRight + rightOffset + _horizontalOffset;
            rightOffset += widths[index];
        }

        for (var i = 0; i < _cells.Count && i < widths.Count; i++)
        {
            var cell = _cells[i];
            var width = widths[i];
            var x = visualPositions.TryGetValue(i, out var position) ? position : 0;
            Canvas.SetLeft(cell, x);
            Canvas.SetTop(cell, 0);
            cell.Width = width;
            cell.Height = HeaderHeight;

            if (i < _grips.Count)
            {
                var grip = _grips[i];
                Canvas.SetLeft(grip, x + width - (GripWidth / 2));
                Canvas.SetTop(grip, 0);
                grip.Height = HeaderHeight;
            }

            var offset = x + width;
            if (_visualColumnOffsets.Count > i)
            {
                _visualColumnOffsets[i] = offset;
            }
            else
            {
                _visualColumnOffsets.Add(offset);
            }
        }

        UpdateSeparators();
        UpdateReorderIndicator(visible: _isReordering);
    }

    private void EnsureCellCount(int count)
    {
        while (_cells.Count < count)
        {
            var cell = new ContentControl
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
            };

            cell.PointerPressed += HeaderCellOnPointerPressed;
            cell.PointerMoved += HeaderCellOnPointerMoved;
            cell.PointerReleased += HeaderCellOnPointerReleased;
            cell.PointerCaptureLost += HeaderCellOnPointerCaptureLost;
            Children.Add(cell);
            _cells.Add(cell);
        }

        while (_cells.Count > count)
        {
            var last = _cells[^1];
            last.PointerPressed -= HeaderCellOnPointerPressed;
            last.PointerMoved -= HeaderCellOnPointerMoved;
            last.PointerReleased -= HeaderCellOnPointerReleased;
            last.PointerCaptureLost -= HeaderCellOnPointerCaptureLost;
            last.ContextMenu = null;
            Children.Remove(last);
            _cells.RemoveAt(_cells.Count - 1);
        }

        EnsureSeparatorCount(Math.Max(0, count - 1));
    }

    private void EnsureGripCount(int count)
    {
        while (_grips.Count < count)
        {
            var grip = new Thumb
            {
                Width = GripWidth,
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.SizeWestEast),
            };

            grip.DragDelta += GripOnDragDelta;
            grip.DragStarted += GripOnDragStarted;
            grip.DragCompleted += GripOnDragCompleted;
            Children.Add(grip);
            _grips.Add(grip);
            grip.SetValue(Canvas.ZIndexProperty, 20);
        }

        while (_grips.Count > count)
        {
            var last = _grips[^1];
            last.DragDelta -= GripOnDragDelta;
            last.DragStarted -= GripOnDragStarted;
            last.DragCompleted -= GripOnDragCompleted;
            Children.Remove(last);
            _grips.RemoveAt(_grips.Count - 1);
        }
    }

    private void EnsureSeparatorCount(int count)
    {
        while (_separators.Count < count)
        {
            var separator = new Border
            {
                Background = _separatorBrush,
                Width = 1,
                IsHitTestVisible = false,
            };

            Children.Add(separator);
            _separators.Add(separator);
            separator.SetValue(Canvas.ZIndexProperty, 0);
        }

        while (_separators.Count > count)
        {
            var last = _separators[^1];
            Children.Remove(last);
            _separators.RemoveAt(_separators.Count - 1);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var measured = base.MeasureOverride(availableSize);
        var width = double.IsFinite(Width) && Width > 0 ? Width : measured.Width;
        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            width = Math.Max(0, measured.Width);
            if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
            {
                width = 0;
            }
        }

        return new Size(width, HeaderHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        var width = double.IsFinite(Width) && Width > 0 ? Width : arranged.Width;
        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            width = Math.Max(0, arranged.Width);
            if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
            {
                width = 0;
            }
        }

        return new Size(width, HeaderHeight);
    }

    private void UpdateSeparators()
    {
        EnsureSeparatorCount(Math.Max(0, _cells.Count - 1));

        for (var i = 0; i < _separators.Count; i++)
        {
            var separator = _separators[i];
            var offset = i < _visualColumnOffsets.Count ? _visualColumnOffsets[i] : 0;
            Canvas.SetLeft(separator, offset - 0.5);
            Canvas.SetTop(separator, 0);
            separator.Height = HeaderHeight;
            separator.IsHitTestVisible = false;
            separator.Background = _separatorBrush;
            separator.SetValue(Canvas.ZIndexProperty, 0);
        }
    }

    private void AttachContextMenu(ContentControl cell, FastTreeDataGridColumn column, int columnIndex)
    {
        var menuItems = new List<object>();

        void AddMenuItem(string header, ColumnMenuAction action, bool isEnabled = true)
        {
            var item = new MenuItem
            {
                Header = header,
                IsEnabled = isEnabled,
                Tag = new ColumnMenuCommand(columnIndex, action, cell),
            };
            item.Click += OnMenuItemClick;
            menuItems.Add(item);
        }

        if (column.CanUserFilter)
        {
            AddMenuItem("Filter...", ColumnMenuAction.ShowFilter, column.CanUserFilter);
            AddMenuItem("Clear Filter", ColumnMenuAction.ClearFilter, column.CanUserFilter && column.IsFilterActive);
            menuItems.Add(new Separator());
        }

        AddMenuItem("Sort Ascending", ColumnMenuAction.SortAscending, column.CanUserSort);
        AddMenuItem("Sort Descending", ColumnMenuAction.SortDescending, column.CanUserSort);
        AddMenuItem("Clear Sort", ColumnMenuAction.ClearSort, column.SortDirection != FastTreeDataGridSortDirection.None || column.SortOrder > 0);
        menuItems.Add(new Separator());

        AddMenuItem("Expand All", ColumnMenuAction.ExpandAll);
        AddMenuItem("Collapse All", ColumnMenuAction.CollapseAll);
        menuItems.Add(new Separator());

        AddMenuItem("Auto-size Column", ColumnMenuAction.AutoSizeColumn, column.CanAutoSize);
        AddMenuItem("Auto-size Column (All Rows)", ColumnMenuAction.AutoSizeColumnAll, column.CanAutoSize);
        AddMenuItem("Auto-size All Columns", ColumnMenuAction.AutoSizeAllColumns, column.CanAutoSize);
        AddMenuItem("Auto-size All Columns (All Rows)", ColumnMenuAction.AutoSizeAllColumnsAll, column.CanAutoSize);
        menuItems.Add(new Separator());

        AddMenuItem("Pin Left", ColumnMenuAction.PinLeft, column.CanUserPin && column.PinnedPosition != FastTreeDataGridPinnedPosition.Left);
        AddMenuItem("Pin Right", ColumnMenuAction.PinRight, column.CanUserPin && column.PinnedPosition != FastTreeDataGridPinnedPosition.Right);
        AddMenuItem("Unpin", ColumnMenuAction.Unpin, column.CanUserPin && column.PinnedPosition != FastTreeDataGridPinnedPosition.None);
        menuItems.Add(new Separator());

        AddMenuItem("Move Left", ColumnMenuAction.MoveLeft, column.CanUserReorder);
        AddMenuItem("Move Right", ColumnMenuAction.MoveRight, column.CanUserReorder);
        AddMenuItem("Hide Column", ColumnMenuAction.Hide, true);

        var menu = new ContextMenu
        {
            ItemsSource = menuItems,
        };

        cell.ContextMenu = menu;
    }

    private void HeaderCellOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is Thumb)
        {
            return;
        }

        if (sender is not ContentControl cell || cell.Tag is not int index)
        {
            return;
        }

        _pressedColumnIndex = index;
        _pressPoint = e.GetPosition(this);
        _reorderInsertIndex = index;
        _isReordering = false;
        _hasPointerMoved = false;
        _suppressSort = false;

        e.Pointer.Capture(cell);
        e.Handled = true;
    }

    private void HeaderCellOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedColumnIndex is null || _columns.Count == 0)
        {
            return;
        }

        if (sender is not ContentControl cell || cell.Tag is not int index || index != _pressedColumnIndex)
        {
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _pressPoint;

        if (!_hasPointerMoved && (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1))
        {
            _hasPointerMoved = true;
            _suppressSort = true;
        }

        if (!_isReordering)
        {
            if (!_columns[index].CanUserReorder)
            {
                return;
            }

            if (Math.Abs(delta.X) < DragActivationThreshold)
            {
                return;
            }

            _isReordering = true;
            _suppressSort = true;
            UpdateReorderIndicator(visible: true);
        }

        var target = DetermineInsertionIndex(position.X, index);
        if (target != _reorderInsertIndex)
        {
            _reorderInsertIndex = target;
            UpdateReorderIndicator(visible: true);
        }
    }

    private void HeaderCellOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not ContentControl cell || cell.Tag is not int index)
        {
            return;
        }

        if (_pressedColumnIndex is null || index != _pressedColumnIndex)
        {
            return;
        }

        if (Equals(e.Pointer.Captured, cell))
        {
            e.Pointer.Capture(null);
        }
        var modifiers = e.KeyModifiers;

        if (_isReordering)
        {
            var fromIndex = _pressedColumnIndex.Value;
            var insertIndex = Math.Clamp(_reorderInsertIndex, 0, _columns.Count);

            if (insertIndex != fromIndex && insertIndex != fromIndex + 1)
            {
                ColumnReorderRequested?.Invoke(fromIndex, insertIndex);
            }
        }
        else if (!_hasPointerMoved && !_suppressSort)
        {
            ColumnSortRequested?.Invoke(index, null, modifiers);
        }

        ResetInteractionState();
        e.Handled = true;
    }

    private void HeaderCellOnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ResetInteractionState();
    }

    private void ResetInteractionState()
    {
        _pressedColumnIndex = null;
        _isReordering = false;
        _hasPointerMoved = false;
        _suppressSort = false;
        _reorderInsertIndex = -1;
        UpdateReorderIndicator(visible: false);
    }

    private void UpdateReorderIndicator(bool visible)
    {
        if (!visible || _pressedColumnIndex is null || _reorderInsertIndex < 0)
        {
            _reorderIndicator.Opacity = 0;
            return;
        }

        double x;
        if (_reorderInsertIndex >= _cells.Count)
        {
            var lastCell = _cells[^1];
            x = Canvas.GetLeft(lastCell) + lastCell.Width;
        }
        else
        {
            var targetCell = _cells[_reorderInsertIndex];
            x = Canvas.GetLeft(targetCell);
        }

        Canvas.SetLeft(_reorderIndicator, x - (_reorderIndicator.Width / 2));
        Canvas.SetTop(_reorderIndicator, 0);
        _reorderIndicator.Height = HeaderHeight;
        _reorderIndicator.Opacity = 0.9;
    }

    private int DetermineInsertionIndex(double pointerX, int columnIndex)
    {
        var groupIndices = GetGroupIndices(columnIndex);
        if (groupIndices.Count == 0)
        {
            return columnIndex;
        }

        var ordered = groupIndices.OrderBy(i => Canvas.GetLeft(_cells[i])).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var cell = _cells[ordered[i]];
            var left = Canvas.GetLeft(cell);
            var mid = left + (cell.Width / 2);
            if (pointerX < mid)
            {
                return ordered[i];
            }
        }

        var lastIndex = ordered[^1];
        return lastIndex + 1;
    }

    private List<int> GetGroupIndices(int columnIndex)
    {
        var indices = new List<int>();
        if (_columns.Count == 0 || columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return indices;
        }

        var pinned = _columns[columnIndex].PinnedPosition;
        for (var i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].PinnedPosition == pinned)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private void GripOnDragStarted(object? sender, VectorEventArgs e)
    {
        if (sender is Thumb thumb && thumb.Tag is int index)
        {
            var width = index >= 0 && index < _cells.Count ? _cells[index].Width : GetBoundColumnWidth(index);
            if (!double.IsFinite(width) || width <= 0)
            {
                width = GetBoundColumnWidth(index);
            }

            _activeColumnResizes[index] = new ColumnResizeState(width);
        }
    }

    private void GripOnDragDelta(object? sender, VectorEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not int index)
        {
            return;
        }

        if (_columns is null || index < 0 || index >= _columns.Count)
        {
            return;
        }

        if (!_activeColumnResizes.TryGetValue(index, out var state))
        {
            var width = index >= 0 && index < _cells.Count ? _cells[index].Width : GetBoundColumnWidth(index);
            if (!double.IsFinite(width) || width <= 0)
            {
                width = GetBoundColumnWidth(index);
            }

            state = new ColumnResizeState(width);
        }

        var column = _columns[index];
        var desiredWidth = state.LastVisualWidth + e.Vector.X;
        if (double.IsNaN(desiredWidth) || double.IsInfinity(desiredWidth))
        {
            return;
        }

        var minWidth = Math.Max(column.MinWidth, 16);
        var maxCandidate = column.MaxWidth;
        if (!double.IsFinite(maxCandidate) || maxCandidate <= 0)
        {
            maxCandidate = double.PositiveInfinity;
        }

        var maxWidth = double.IsPositiveInfinity(maxCandidate) ? double.PositiveInfinity : Math.Max(maxCandidate, minWidth);
        var clampedWidth = Math.Clamp(desiredWidth, minWidth, maxWidth);

        if (Math.Abs(clampedWidth - state.LastVisualWidth) < 0.01)
        {
            return;
        }

        ApplyColumnWidthVisual(index, clampedWidth);
        state.LastVisualWidth = clampedWidth;

        if (ColumnResizeRequested is { } resizeHandler)
        {
            var deltaForOwner = clampedWidth - state.LastAppliedWidth;
            if (Math.Abs(deltaForOwner) >= 0.05)
            {
                resizeHandler(index, deltaForOwner);

                var appliedWidth = GetBoundColumnWidth(index);
                if (!double.IsFinite(appliedWidth) || appliedWidth <= 0)
                {
                    appliedWidth = clampedWidth;
                }

                state.LastAppliedWidth = appliedWidth;
            }
        }

        _activeColumnResizes[index] = state;
    }

    private void GripOnDragCompleted(object? sender, VectorEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not int index)
        {
            return;
        }

        if (_activeColumnResizes.TryGetValue(index, out var state))
        {
            var visualWidth = index >= 0 && index < _cells.Count ? _cells[index].Width : state.LastVisualWidth;
            if (!double.IsFinite(visualWidth) || visualWidth <= 0)
            {
                visualWidth = GetBoundColumnWidth(index);
            }

            if (ColumnResizeRequested is { } resizeHandler)
            {
                var remainingDelta = visualWidth - state.LastAppliedWidth;
                if (Math.Abs(remainingDelta) >= 0.01)
                {
                    resizeHandler(index, remainingDelta);
                    var appliedWidth = GetBoundColumnWidth(index);
                    if (!double.IsFinite(appliedWidth) || appliedWidth <= 0)
                    {
                        appliedWidth = visualWidth;
                    }

                    state.LastAppliedWidth = appliedWidth;
                }
            }

            state.LastVisualWidth = visualWidth;
        }

        _activeColumnResizes.Remove(index);
    }

    private double GetBoundColumnWidth(int index)
    {
        if (_columns is not null && index >= 0 && index < _columns.Count)
        {
            var column = _columns[index];
            if (double.IsFinite(column.ActualWidth) && column.ActualWidth > 0)
            {
                return column.ActualWidth;
            }

            if (double.IsFinite(column.PixelWidth) && column.PixelWidth > 0)
            {
                return column.PixelWidth;
            }
        }

        if (index >= 0 && index < _cells.Count)
        {
            var width = _cells[index].Width;
            if (double.IsFinite(width) && width > 0)
            {
                return width;
            }
        }

        return 0;
    }

    private void RemoveInvalidResizeStates(int columnCount)
    {
        if (_activeColumnResizes.Count == 0)
        {
            return;
        }

        var keysToRemove = new List<int>();
        foreach (var key in _activeColumnResizes.Keys)
        {
            if (key >= columnCount)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _activeColumnResizes.Remove(key);
        }
    }

    private void ApplyColumnWidthVisual(int index, double width)
    {
        if (_cells.Count == 0 || index < 0 || index >= _cells.Count)
        {
            return;
        }

        var clampedWidth = double.IsFinite(width) && width > 0 ? width : 0;
        _cells[index].Width = clampedWidth;

        var widths = new double[_cells.Count];
        for (var i = 0; i < _cells.Count; i++)
        {
            widths[i] = double.IsFinite(_cells[i].Width) ? Math.Max(0, _cells[i].Width) : 0;
        }

        UpdateLayout(widths);
    }

    private object? CreateHeaderContent(FastTreeDataGridColumn column, int columnIndex)
    {
        var header = column.Header ?? $"Column {columnIndex}";

        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var textPresenter = new ContentControl
        {
            Content = header,
            VerticalAlignment = VerticalAlignment.Center,
        };

        panel.Children.Add(textPresenter);

        if (column.SortDirection != FastTreeDataGridSortDirection.None)
        {
        var glyph = new Avalonia.Controls.Shapes.Path
            {
                Width = 8,
                Height = 6,
                Stretch = Stretch.Fill,
                Fill = _sortGlyphBrush,
                Data = column.SortDirection == FastTreeDataGridSortDirection.Ascending ? _ascendingGeometry : _descendingGeometry,
                IsHitTestVisible = false,
            };

            panel.Children.Add(glyph);

            if (column.SortOrder > 0)
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(6, 0, 6, 0),
                    Margin = new Thickness(2, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = column.SortOrder.ToString(),
                        Foreground = Brushes.White,
                        FontSize = 11,
                    },
                    IsHitTestVisible = false,
                };

                panel.Children.Add(badge);
            }
        }

        return panel;
    }

    private void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || menuItem.Tag is not ColumnMenuCommand command)
        {
            return;
        }

        switch (command.Action)
        {
            case ColumnMenuAction.ShowFilter:
                if (command.Cell is not null)
                {
                    ColumnFilterRequested?.Invoke(command.ColumnIndex, command.Cell);
                }
                break;
            case ColumnMenuAction.ClearFilter:
                ColumnFilterCleared?.Invoke(command.ColumnIndex);
                break;
            case ColumnMenuAction.SortAscending:
                ColumnSortRequested?.Invoke(command.ColumnIndex, FastTreeDataGridSortDirection.Ascending, KeyModifiers.None);
                break;
            case ColumnMenuAction.SortDescending:
                ColumnSortRequested?.Invoke(command.ColumnIndex, FastTreeDataGridSortDirection.Descending, KeyModifiers.None);
                break;
            case ColumnMenuAction.ClearSort:
                ColumnSortRequested?.Invoke(command.ColumnIndex, FastTreeDataGridSortDirection.None, KeyModifiers.None);
                break;
            case ColumnMenuAction.AutoSizeColumn:
                ColumnAutoSizeRequested?.Invoke(command.ColumnIndex, false);
                break;
            case ColumnMenuAction.ExpandAll:
                ExpandAllRequested?.Invoke();
                break;
            case ColumnMenuAction.CollapseAll:
                CollapseAllRequested?.Invoke();
                break;
            case ColumnMenuAction.AutoSizeColumnAll:
                ColumnAutoSizeRequested?.Invoke(command.ColumnIndex, true);
                break;
            case ColumnMenuAction.AutoSizeAllColumns:
                AutoSizeAllRequested?.Invoke(false);
                break;
            case ColumnMenuAction.AutoSizeAllColumnsAll:
                AutoSizeAllRequested?.Invoke(true);
                break;
            case ColumnMenuAction.PinLeft:
                ColumnPinRequested?.Invoke(command.ColumnIndex, FastTreeDataGridPinnedPosition.Left);
                break;
            case ColumnMenuAction.PinRight:
                ColumnPinRequested?.Invoke(command.ColumnIndex, FastTreeDataGridPinnedPosition.Right);
                break;
            case ColumnMenuAction.Unpin:
                ColumnPinRequested?.Invoke(command.ColumnIndex, FastTreeDataGridPinnedPosition.None);
                break;
            case ColumnMenuAction.MoveLeft:
                ColumnMoveLeftRequested?.Invoke(command.ColumnIndex);
                break;
            case ColumnMenuAction.MoveRight:
                ColumnMoveRightRequested?.Invoke(command.ColumnIndex);
                break;
            case ColumnMenuAction.Hide:
                ColumnHideRequested?.Invoke(command.ColumnIndex);
                break;
        }
    }

    private enum ColumnMenuAction
    {
        ShowFilter,
        ClearFilter,
        SortAscending,
        SortDescending,
        ClearSort,
        AutoSizeColumn,
        AutoSizeColumnAll,
        AutoSizeAllColumns,
        AutoSizeAllColumnsAll,
        ExpandAll,
        CollapseAll,
        PinLeft,
        PinRight,
        Unpin,
        MoveLeft,
        MoveRight,
        Hide,
    }

    private readonly record struct ColumnMenuCommand(int ColumnIndex, ColumnMenuAction Action, ContentControl Cell);

    private sealed class ColumnResizeState
    {
        public ColumnResizeState(double width)
        {
            LastVisualWidth = width;
            LastAppliedWidth = width;
        }

        public double LastVisualWidth { get; set; }

        public double LastAppliedWidth { get; set; }
    }
}
