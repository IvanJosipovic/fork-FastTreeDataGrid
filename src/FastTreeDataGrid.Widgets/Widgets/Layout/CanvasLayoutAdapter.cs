using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal readonly struct CanvasPlacement
{
    public readonly double? Left;
    public readonly double? Top;
    public readonly double? Right;
    public readonly double? Bottom;
    public readonly double? Width;
    public readonly double? Height;

    public CanvasPlacement(double? left, double? top, double? right, double? bottom, double? width, double? height)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    public CanvasPlacement With(
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        bool clearLeft = false,
        bool clearTop = false,
        bool clearRight = false,
        bool clearBottom = false,
        bool clearWidth = false,
        bool clearHeight = false)
    {
        return new CanvasPlacement(
            clearLeft ? null : (left ?? Left),
            clearTop ? null : (top ?? Top),
            clearRight ? null : (right ?? Right),
            clearBottom ? null : (bottom ?? Bottom),
            clearWidth ? null : (width ?? Width),
            clearHeight ? null : (height ?? Height));
    }
}

internal sealed class CanvasLayoutAdapter : IPanelLayoutAdapter
{
    private readonly IReadOnlyDictionary<Widget, CanvasPlacement> _placements;

    public CanvasLayoutAdapter(IReadOnlyDictionary<Widget, CanvasPlacement> placements)
    {
        _placements = placements;
    }

    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        var surface = context.InnerBounds;

        foreach (var child in children)
        {
            var placement = _placements.TryGetValue(child, out var stored) ? stored : default;
            ArrangeChild(child, placement, surface);
        }
    }

    private static void ArrangeChild(Widget child, CanvasPlacement placement, Rect bounds)
    {
        var width = ResolveDimension(placement.Width, child.DesiredWidth);
        var height = ResolveDimension(placement.Height, child.DesiredHeight);

        var x = bounds.X;
        var y = bounds.Y;

        if (placement.Left.HasValue)
        {
            x = bounds.X + placement.Left.Value;
        }
        else if (placement.Right.HasValue)
        {
            x = bounds.Right - width - placement.Right.Value;
        }

        if (placement.Top.HasValue)
        {
            y = bounds.Y + placement.Top.Value;
        }
        else if (placement.Bottom.HasValue)
        {
            y = bounds.Bottom - height - placement.Bottom.Value;
        }

        var rect = new Rect(x, y, width, height);
        child.Arrange(rect);
    }

    private static double ResolveDimension(double? explicitValue, double desired)
    {
        if (explicitValue.HasValue)
        {
            return Math.Max(0, explicitValue.Value);
        }

        if (!double.IsNaN(desired) && desired > 0)
        {
            return desired;
        }

        return 0;
    }
}
