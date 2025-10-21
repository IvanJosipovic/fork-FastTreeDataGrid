using System;
using Avalonia;
using Avalonia.Layout;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class WrapLayoutWidget : SurfaceWidget
{
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    public Thickness Padding { get; set; } = new Thickness(0);

    public double Spacing { get; set; } = 4;

    public double DefaultItemWidth { get; set; } = 32;

    public double DefaultItemHeight { get; set; } = 32;

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var inner = bounds.Deflate(Padding);
        if (inner.Width <= 0 || inner.Height <= 0 || Children.Count == 0)
        {
            return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            ArrangeHorizontal(inner);
        }
        else
        {
            ArrangeVertical(inner);
        }
    }

    private void ArrangeHorizontal(Rect inner)
    {
        var x = inner.X;
        var y = inner.Y;
        var lineHeight = 0d;
        var maxX = inner.Right;

        foreach (var child in Children)
        {
            var width = ResolveSize(child.DesiredWidth, DefaultItemWidth, inner.Width);
            var height = ResolveSize(child.DesiredHeight, DefaultItemHeight, inner.Height);

            if (x > inner.X && x + width > maxX)
            {
                x = inner.X;
                y += lineHeight + Spacing;
                lineHeight = 0;
            }

            var rect = new Rect(x, y, width, height);
            child.Arrange(rect);

            lineHeight = Math.Max(lineHeight, height);
            x += width + Spacing;
        }
    }

    private void ArrangeVertical(Rect inner)
    {
        var x = inner.X;
        var y = inner.Y;
        var lineWidth = 0d;
        var maxY = inner.Bottom;

        foreach (var child in Children)
        {
            var width = ResolveSize(child.DesiredWidth, DefaultItemWidth, inner.Width);
            var height = ResolveSize(child.DesiredHeight, DefaultItemHeight, inner.Height);

            if (y > inner.Y && y + height > maxY)
            {
                y = inner.Y;
                x += lineWidth + Spacing;
                lineWidth = 0;
            }

            var rect = new Rect(x, y, width, height);
            child.Arrange(rect);

            lineWidth = Math.Max(lineWidth, width);
            y += height + Spacing;
        }
    }

    private static double ResolveSize(double requested, double fallback, double maxAvailable)
    {
        var value = double.IsNaN(requested) || requested <= 0 ? fallback : requested;
        value = Math.Min(value, maxAvailable);
        return Math.Max(0, value);
    }
}
