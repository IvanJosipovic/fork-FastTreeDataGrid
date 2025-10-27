using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal readonly record struct ScrollViewerLayoutOptions(
    double HorizontalOffset,
    double VerticalOffset,
    double? ExtentWidth,
    double? ExtentHeight);

internal sealed class ScrollViewerLayoutAdapter : IPanelLayoutAdapter
{
    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        var child = children[0];
        var bounds = context.InnerBounds;
        var options = context.CustomData is ScrollViewerLayoutOptions o
            ? o
            : new ScrollViewerLayoutOptions(0, 0, null, null);

        var extentWidth = ResolveDimension(child.DesiredWidth, options.ExtentWidth, bounds.Width);
        var extentHeight = ResolveDimension(child.DesiredHeight, options.ExtentHeight, bounds.Height);

        var x = bounds.X - options.HorizontalOffset;
        var y = bounds.Y - options.VerticalOffset;

        child.Arrange(new Rect(x, y, extentWidth, extentHeight));

        for (var i = 1; i < children.Count; i++)
        {
            children[i].Arrange(new Rect(bounds.X, bounds.Y, 0, 0));
        }
    }

    private static double ResolveDimension(double desired, double? extentOverride, double viewport)
    {
        if (extentOverride.HasValue)
        {
            return extentOverride.Value;
        }

        if (!double.IsNaN(desired) && desired > 0)
        {
            return desired;
        }

        return viewport;
    }
}
