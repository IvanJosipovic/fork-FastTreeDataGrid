using System;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class GridLayoutWidget : SurfaceWidget
{
    public int Columns { get; set; } = 1;

    public int Rows { get; set; }

    public Thickness Padding { get; set; } = new Thickness(0);

    public double Spacing { get; set; } = 4;

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var inner = bounds.Deflate(Padding);
        if (inner.Width <= 0 || inner.Height <= 0 || Children.Count == 0)
        {
            return;
        }

        var columnCount = Math.Max(1, Columns);
        var rowCount = Rows > 0 ? Rows : (int)Math.Ceiling(Children.Count / (double)columnCount);
        rowCount = Math.Max(1, rowCount);

        var totalSpacingX = Spacing * Math.Max(0, columnCount - 1);
        var totalSpacingY = Spacing * Math.Max(0, rowCount - 1);

        var cellWidth = Math.Max(0, (inner.Width - totalSpacingX) / columnCount);
        var cellHeight = Math.Max(0, (inner.Height - totalSpacingY) / rowCount);

        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var row = index / columnCount;
            var column = index % columnCount;

            var x = inner.X + column * (cellWidth + Spacing);
            var y = inner.Y + row * (cellHeight + Spacing);

            var width = ResolveSize(child.DesiredWidth, cellWidth);
            var height = ResolveSize(child.DesiredHeight, cellHeight);

            var rect = new Rect(x, y, width, height);
            child.Arrange(rect);
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
