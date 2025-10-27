using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal sealed class GridLayoutAdapter : IPanelLayoutAdapter
{
    public int Columns { get; set; } = 1;
    public int Rows { get; set; }

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

        var columnCount = Math.Max(1, Columns);
        var rowCount = Rows > 0 ? Rows : (int)Math.Ceiling(children.Count / (double)columnCount);
        rowCount = Math.Max(1, rowCount);

        var spacing = Math.Max(0, context.Spacing);
        var totalSpacingX = spacing * Math.Max(0, columnCount - 1);
        var totalSpacingY = spacing * Math.Max(0, rowCount - 1);

        var cellWidth = Math.Max(0, (bounds.Width - totalSpacingX) / columnCount);
        var cellHeight = Math.Max(0, (bounds.Height - totalSpacingY) / rowCount);

        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var row = index / columnCount;
            var column = index % columnCount;

            var x = bounds.X + column * (cellWidth + spacing);
            var y = bounds.Y + row * (cellHeight + spacing);

            var width = ResolveSize(child.DesiredWidth, cellWidth);
            var height = ResolveSize(child.DesiredHeight, cellHeight);

            child.Arrange(new Rect(x, y, width, height));
        }
    }

    private static double ResolveSize(double desired, double available)
    {
        if (double.IsNaN(desired) || desired <= 0)
        {
            return available;
        }

        return Math.Min(desired, available);
    }
}
