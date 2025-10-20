using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FastTreeDataGrid.Control.Models;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridHeaderPresenter : Canvas
{
    private readonly List<ContentControl> _cells = new();
    private readonly List<Border> _separators = new();
    private readonly List<double> _columnOffsets = new();
    private readonly IBrush _separatorBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210));

    public double HeaderHeight { get; set; } = 32;

    public void BindColumns(IReadOnlyList<FastTreeDataGridColumn> columns, IReadOnlyList<double> widths)
    {
        EnsureCellCount(columns.Count);
        var offset = 0d;

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var cell = _cells[i];
            var width = widths[i];

            cell.Width = width;
            cell.Height = HeaderHeight;
            cell.Content = column.Header ?? $"Column {i}";
            cell.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            Canvas.SetLeft(cell, offset);
            Canvas.SetTop(cell, 0);
            offset += width;
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
        var offset = 0d;
        for (var i = 0; i < _cells.Count && i < widths.Count; i++)
        {
            var cell = _cells[i];
            var width = widths[i];
            cell.Width = width;
            Canvas.SetLeft(cell, offset);
            Canvas.SetTop(cell, 0);
            offset += width;
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
            };

            Children.Add(cell);
            _cells.Add(cell);
        }

        while (_cells.Count > count)
        {
            var last = _cells[^1];
            Children.Remove(last);
            _cells.RemoveAt(_cells.Count - 1);
        }

        EnsureSeparatorCount(Math.Max(0, count - 1));
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
            };

            Children.Add(separator);
            _separators.Add(separator);
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
        }
    }
}
