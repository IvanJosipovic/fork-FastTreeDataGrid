using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal readonly record struct LayoutTransformOptions(double ScaleX, double ScaleY, double AngleDegrees);

internal sealed class LayoutTransformLayoutAdapter : IPanelLayoutAdapter
{
    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        var child = children[0];
        var options = context.CustomData is LayoutTransformOptions opts
            ? opts
            : new LayoutTransformOptions(1, 1, 0);

        var inner = context.InnerBounds;
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            child.Arrange(new Rect(inner.X, inner.Y, 0, 0));
            child.Rotation = 0;
            return;
        }

        var width = ResolveDimension(child.DesiredWidth, inner.Width) * Math.Max(0, options.ScaleX);
        var height = ResolveDimension(child.DesiredHeight, inner.Height) * Math.Max(0, options.ScaleY);

        var x = inner.X + Math.Max(0, (inner.Width - width) / 2);
        var y = inner.Y + Math.Max(0, (inner.Height - height) / 2);

        child.Rotation = options.AngleDegrees;
        child.Arrange(new Rect(x, y, Math.Max(0, width), Math.Max(0, height)));

        for (var i = 1; i < children.Count; i++)
        {
            children[i].Rotation = 0;
            children[i].Arrange(new Rect(inner.X, inner.Y, 0, 0));
        }
    }

    private static double ResolveDimension(double desired, double available)
    {
        if (!double.IsNaN(desired) && desired > 0)
        {
            return desired;
        }

        return available > 0 ? available : 0;
    }
}
