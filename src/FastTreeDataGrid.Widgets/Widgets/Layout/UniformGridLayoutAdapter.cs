using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal sealed class UniformGridLayoutAdapter : IPanelLayoutAdapter
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int FirstColumn { get; set; }

    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        var bounds = context.InnerBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var (rows, columns) = CalculateGrid(children.Count);
        if (rows <= 0 || columns <= 0)
        {
            return;
        }

        var spacing = Math.Max(0, context.Spacing);
        var totalSpacingX = spacing * Math.Max(0, columns - 1);
        var totalSpacingY = spacing * Math.Max(0, rows - 1);

        var cellWidth = Math.Max(0, (bounds.Width - totalSpacingX) / columns);
        var cellHeight = Math.Max(0, (bounds.Height - totalSpacingY) / rows);

        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var position = FirstColumn + index;
            var row = position / columns;
            var column = position % columns;

            if (row >= rows)
            {
                break;
            }

            var x = bounds.X + column * (cellWidth + spacing);
            var y = bounds.Y + row * (cellHeight + spacing);

            child.Arrange(new Rect(x, y, cellWidth, cellHeight));
        }
    }

    private (int rows, int columns) CalculateGrid(int childCount)
    {
        var rows = Rows;
        var columns = Columns;
        var firstColumn = Math.Max(0, FirstColumn);
        var adjustedCount = childCount + firstColumn;

        if (rows <= 0 && columns <= 0)
        {
            var size = Math.Ceiling(Math.Sqrt(adjustedCount));
            rows = (int)size;
            columns = (int)size;
        }
        else if (rows <= 0)
        {
            rows = (adjustedCount + columns - 1) / columns;
        }
        else if (columns <= 0)
        {
            columns = (adjustedCount + rows - 1) / rows;
        }

        rows = Math.Max(1, rows);
        columns = Math.Max(1, columns);

        return (rows, columns);
    }
}
