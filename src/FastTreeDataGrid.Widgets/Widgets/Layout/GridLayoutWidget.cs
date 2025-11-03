using System;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class GridLayoutWidget : PanelLayoutWidget
{
    private readonly GridLayoutAdapter _adapter = new();

    public int Columns
    {
        get => _adapter.Columns;
        set => _adapter.Columns = value;
    }

    public int Rows
    {
        get => _adapter.Rows;
        set => _adapter.Rows = value;
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override Size MeasureCore(Size available)
    {
        var padding = Padding;
        var spacing = Math.Max(0, Spacing);
        var innerWidth = Math.Max(0, available.Width - padding.Left - padding.Right);
        var innerHeight = Math.Max(0, available.Height - padding.Top - padding.Bottom);

        var columnCount = Math.Max(1, Columns);
        var rowCount = Rows > 0 ? Rows : (int)Math.Ceiling(Children.Count / (double)columnCount);
        rowCount = Math.Max(1, rowCount);

        var totalSpacingX = spacing * Math.Max(0, columnCount - 1);
        var totalSpacingY = spacing * Math.Max(0, rowCount - 1);

        var cellAvailableWidth = double.IsPositiveInfinity(innerWidth)
            ? double.PositiveInfinity
            : Math.Max(0, (innerWidth - totalSpacingX) / columnCount);
        var cellAvailableHeight = double.IsPositiveInfinity(innerHeight)
            ? double.PositiveInfinity
            : Math.Max(0, (innerHeight - totalSpacingY) / rowCount);

        double maxCellWidth = 0;
        double maxCellHeight = 0;

        foreach (var child in Children)
        {
            var childSize = child.Measure(new Size(cellAvailableWidth, cellAvailableHeight));
            maxCellWidth = Math.Max(maxCellWidth, childSize.Width);
            maxCellHeight = Math.Max(maxCellHeight, childSize.Height);
        }

        if (maxCellWidth <= 0)
        {
            maxCellWidth = double.IsPositiveInfinity(cellAvailableWidth) ? 0 : cellAvailableWidth;
        }

        if (maxCellHeight <= 0)
        {
            maxCellHeight = double.IsPositiveInfinity(cellAvailableHeight) ? 0 : cellAvailableHeight;
        }

        var width = padding.Left + padding.Right + columnCount * maxCellWidth + totalSpacingX;
        var height = padding.Top + padding.Bottom + rowCount * maxCellHeight + totalSpacingY;

        return new Size(width, height);
    }
}
