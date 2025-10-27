using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

internal readonly record struct ViewboxLayoutOptions(Stretch Stretch, StretchDirection StretchDirection);

internal sealed class ViewboxLayoutAdapter : IPanelLayoutAdapter
{
    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        var child = children[0];
        var options = context.CustomData is ViewboxLayoutOptions opts
            ? opts
            : new ViewboxLayoutOptions(Stretch.Uniform, StretchDirection.Both);

        var inner = context.InnerBounds;
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            child.Arrange(new Rect(inner.X, inner.Y, 0, 0));
            return;
        }

        var desiredWidth = ResolveDimension(child.DesiredWidth, inner.Width);
        var desiredHeight = ResolveDimension(child.DesiredHeight, inner.Height);

        if (desiredWidth <= 0 || desiredHeight <= 0)
        {
            child.Arrange(new Rect(inner.X, inner.Y, 0, 0));
            return;
        }

        var scaleX = inner.Width / desiredWidth;
        var scaleY = inner.Height / desiredHeight;

        (scaleX, scaleY) = ApplyStretch(scaleX, scaleY, options.Stretch);
        scaleX = ApplyStretchDirection(scaleX, options.StretchDirection);
        scaleY = ApplyStretchDirection(scaleY, options.StretchDirection);

        var width = desiredWidth * scaleX;
        var height = desiredHeight * scaleY;

        var x = inner.X + Math.Max(0, (inner.Width - width) / 2);
        var y = inner.Y + Math.Max(0, (inner.Height - height) / 2);

        child.Arrange(new Rect(x, y, Math.Max(0, width), Math.Max(0, height)));

        for (var i = 1; i < children.Count; i++)
        {
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

    private static (double scaleX, double scaleY) ApplyStretch(double scaleX, double scaleY, Stretch stretch)
    {
        return stretch switch
        {
            Stretch.None => (1, 1),
            Stretch.Fill => (scaleX, scaleY),
            Stretch.Uniform =>
            (
                double.IsInfinity(scaleX) || double.IsNaN(scaleX) ? 1 : Math.Min(scaleX, scaleY),
                double.IsInfinity(scaleY) || double.IsNaN(scaleY) ? 1 : Math.Min(scaleX, scaleY)
            ),
            Stretch.UniformToFill =>
            (
                double.IsInfinity(scaleX) || double.IsNaN(scaleX) ? 1 : Math.Max(scaleX, scaleY),
                double.IsInfinity(scaleY) || double.IsNaN(scaleY) ? 1 : Math.Max(scaleX, scaleY)
            ),
            _ => (scaleX, scaleY)
        };
    }

    private static double ApplyStretchDirection(double scale, StretchDirection direction)
    {
        return direction switch
        {
            StretchDirection.DownOnly => Math.Min(scale, 1),
            StretchDirection.UpOnly => Math.Max(scale, 1),
            _ => scale,
        };
    }
}
