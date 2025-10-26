using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FastTreeDataGrid.Control.Models;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridHeaderPresenter : Canvas
{
    private readonly List<ContentControl> _cells = new();
    private readonly List<Thumb> _grips = new();
    private readonly List<Border> _separators = new();
    private readonly List<double> _columnOffsets = new();
    private IReadOnlyList<FastTreeDataGridColumn> _columns = Array.Empty<FastTreeDataGridColumn>();
    private readonly Dictionary<int, ColumnResizeState> _activeColumnResizes = new();
    private readonly IBrush _separatorBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210));
    private readonly IBrush _sortGlyphBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));
    private readonly Geometry _ascendingGeometry = StreamGeometry.Parse("M0,4 L3.5,0 7,4 Z");
    private readonly Geometry _descendingGeometry = StreamGeometry.Parse("M0,0 L7,0 3.5,4 Z");
    private const double GripWidth = 8;

    public double HeaderHeight { get; set; } = 32;

    public event Action<int, double>? ColumnResizeRequested;
    public event Action<int>? ColumnSortRequested;

    public void BindColumns(IReadOnlyList<FastTreeDataGridColumn> columns, IReadOnlyList<double> widths)
    {
        if (!ReferenceEquals(_columns, columns))
        {
            _columns = columns;
            _activeColumnResizes.Clear();
        }

        RemoveInvalidResizeStates(columns.Count);
        EnsureCellCount(columns.Count);
        EnsureGripCount(columns.Count);
        var offset = 0d;

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var cell = _cells[i];
            var grip = _grips[i];
            var width = widths[i];

            cell.Tag = i;
            cell.Width = width;
            cell.Height = HeaderHeight;
            cell.Content = CreateHeaderContent(column, i);
            cell.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            Canvas.SetLeft(cell, offset);
            Canvas.SetTop(cell, 0);
            offset += width;

            grip.Tag = i;
            grip.Width = GripWidth;
            grip.Height = HeaderHeight;
            grip.IsVisible = column.CanUserResize;
            grip.IsHitTestVisible = column.CanUserResize;
            Canvas.SetLeft(grip, offset - (GripWidth / 2));
            Canvas.SetTop(grip, 0);
        }

        Width = offset;
        Height = HeaderHeight;
        _columnOffsets.Clear();
        var cumulative = 0d;
        for (var i = 0; i < widths.Count; i++)
        {
            cumulative += widths[i];
            _columnOffsets.Add(cumulative);
        }
        InvalidateMeasure();
        UpdateSeparators();
    }

    public void UpdateWidths(IReadOnlyList<double> widths)
    {
        RemoveInvalidResizeStates(widths.Count);

        var offset = 0d;
        for (var i = 0; i < _cells.Count && i < widths.Count; i++)
        {
            var cell = _cells[i];
            var grip = i < _grips.Count ? _grips[i] : null;
            var width = widths[i];
            cell.Tag = i;
            cell.Width = width;
            Canvas.SetLeft(cell, offset);
            Canvas.SetTop(cell, 0);
            offset += width;

            if (grip is not null)
            {
                Canvas.SetLeft(grip, offset - (GripWidth / 2));
                Canvas.SetTop(grip, 0);
                grip.Height = HeaderHeight;
            }
        }

        Width = offset;
        Height = HeaderHeight;
        _columnOffsets.Clear();
        var cumulative = 0d;
        for (var i = 0; i < widths.Count; i++)
        {
            cumulative += widths[i];
            _columnOffsets.Add(cumulative);
        }
        InvalidateMeasure();
        UpdateSeparators();
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
            Children.Add(cell);
            _cells.Add(cell);
        }

        while (_cells.Count > count)
        {
            var last = _cells[^1];
            last.PointerPressed -= HeaderCellOnPointerPressed;
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
            grip.SetValue(Canvas.ZIndexProperty, 10);
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

    private void UpdateSeparators()
    {
        var height = HeaderHeight;
        for (var i = 0; i < _separators.Count; i++)
        {
            var separator = _separators[i];
            var offset = i < _columnOffsets.Count ? _columnOffsets[i] : 0;
            Canvas.SetLeft(separator, offset - 0.5);
            Canvas.SetTop(separator, 0);
            separator.Height = height;
            separator.IsHitTestVisible = false;
            separator.SetValue(Canvas.ZIndexProperty, 0);
        }
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

        var offset = 0d;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            var cellWidth = double.IsFinite(cell.Width) && cell.Width > 0 ? cell.Width : 0;
            cell.Height = HeaderHeight;
            Canvas.SetLeft(cell, offset);
            Canvas.SetTop(cell, 0);

            if (i < _grips.Count)
            {
                var grip = _grips[i];
                Canvas.SetLeft(grip, offset + cellWidth - (GripWidth / 2));
                Canvas.SetTop(grip, 0);
                grip.Height = HeaderHeight;
            }

            offset += cellWidth;
        }

        Width = offset;
        Height = HeaderHeight;

        _columnOffsets.Clear();
        var cumulative = 0d;
        for (var i = 0; i < _cells.Count; i++)
        {
            cumulative += double.IsFinite(_cells[i].Width) ? Math.Max(0, _cells[i].Width) : 0;
            _columnOffsets.Add(cumulative);
        }

        UpdateSeparators();
        InvalidateVisual();
    }

    private object? CreateHeaderContent(FastTreeDataGridColumn column, int columnIndex)
    {
        var header = column.Header ?? $"Column {columnIndex}";

        if (column.SortDirection == FastTreeDataGridSortDirection.None)
        {
            return header;
        }

        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var presenter = new ContentControl
        {
            Content = header,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var glyph = new Avalonia.Controls.Shapes.Path
        {
            Width = 8,
            Height = 6,
            Stretch = Stretch.Fill,
            Fill = _sortGlyphBrush,
            Data = column.SortDirection == FastTreeDataGridSortDirection.Ascending ? _ascendingGeometry : _descendingGeometry,
            IsHitTestVisible = false,
        };

        panel.Children.Add(presenter);
        panel.Children.Add(glyph);
        return panel;
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

        if (sender is ContentControl cell && cell.Tag is int index)
        {
            ColumnSortRequested?.Invoke(index);
            e.Handled = true;
        }
    }

    private struct ColumnResizeState
    {
        public ColumnResizeState(double originWidth)
        {
            OriginWidth = originWidth;
            LastAppliedWidth = originWidth;
            LastVisualWidth = originWidth;
        }

        public double OriginWidth { get; }
        public double LastAppliedWidth { get; set; }
        public double LastVisualWidth { get; set; }
    }
}
