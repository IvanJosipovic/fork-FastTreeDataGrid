using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal sealed class RelativePanelLayoutAdapter : IPanelLayoutAdapter
{
    private readonly Dictionary<Widget, RelativePlacement> _placements;

    public RelativePanelLayoutAdapter(Dictionary<Widget, RelativePlacement> placements)
    {
        _placements = placements;
    }

    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        var bounds = context.InnerBounds;
        var positioned = new Dictionary<Widget, Rect>();

        foreach (var child in children)
        {
            var placement = _placements.TryGetValue(child, out var p) ? p : default;
            var rect = MeasureChild(child, bounds);

            if (placement.Left.HasValue)
            {
                rect = rect.WithX(bounds.X + placement.Left.Value);
            }
            else if (placement.Right.HasValue)
            {
                rect = rect.WithX(bounds.Right - rect.Width - placement.Right.Value);
            }

            if (placement.Top.HasValue)
            {
                rect = rect.WithY(bounds.Y + placement.Top.Value);
            }
            else if (placement.Bottom.HasValue)
            {
                rect = rect.WithY(bounds.Bottom - rect.Height - placement.Bottom.Value);
            }

            if (placement.AlignLeftWith is { } leftTarget && Positioned(leftTarget, positioned, out var leftRect))
            {
                rect = rect.WithX(leftRect.X);
            }
            else if (placement.AlignLeftWithPanel)
            {
                rect = rect.WithX(bounds.X);
            }

            if (placement.AlignTopWith is { } topTarget && Positioned(topTarget, positioned, out var topRect))
            {
                rect = rect.WithY(topRect.Y);
            }
            else if (placement.AlignTopWithPanel)
            {
                rect = rect.WithY(bounds.Y);
            }

            if (placement.AlignRightWith is { } rightTarget && Positioned(rightTarget, positioned, out var rightRect))
            {
                rect = rect.WithX(rightRect.Right - rect.Width);
            }
            else if (placement.AlignRightWithPanel)
            {
                rect = rect.WithX(bounds.Right - rect.Width);
            }

            if (placement.AlignBottomWith is { } bottomTarget && Positioned(bottomTarget, positioned, out var bottomRect))
            {
                rect = rect.WithY(bottomRect.Bottom - rect.Height);
            }
            else if (placement.AlignBottomWithPanel)
            {
                rect = rect.WithY(bounds.Bottom - rect.Height);
            }

            if (placement.LeftOf is { } leftOf && Positioned(leftOf, positioned, out var leftOfRect))
            {
                rect = rect.WithX(leftOfRect.X - rect.Width - placement.Margin);
            }

            if (placement.RightOf is { } rightOf && Positioned(rightOf, positioned, out var rightOfRect))
            {
                rect = rect.WithX(rightOfRect.Right + placement.Margin);
            }

            if (placement.Above is { } above && Positioned(above, positioned, out var aboveRect))
            {
                rect = rect.WithY(aboveRect.Y - rect.Height - placement.Margin);
            }

            if (placement.Below is { } below && Positioned(below, positioned, out var belowRect))
            {
                rect = rect.WithY(belowRect.Bottom + placement.Margin);
            }

            rect = Constrain(rect, bounds);
            child.Arrange(rect);
            positioned[child] = rect;
        }
    }

    private static bool Positioned(Widget target, Dictionary<Widget, Rect> positioned, out Rect rect)
    {
        return positioned.TryGetValue(target, out rect);
    }

    private static Rect MeasureChild(Widget child, Rect bounds)
    {
        var width = child.DesiredWidth;
        if (double.IsNaN(width) || width <= 0)
        {
            width = Math.Min(bounds.Width, Math.Max(0, bounds.Width));
        }

        var height = child.DesiredHeight;
        if (double.IsNaN(height) || height <= 0)
        {
            height = Math.Min(bounds.Height, Math.Max(0, bounds.Height));
        }

        return new Rect(bounds.X, bounds.Y, Math.Max(0, width), Math.Max(0, height));
    }

    private static Rect Constrain(Rect rect, Rect bounds)
    {
        var x = Math.Max(bounds.X, rect.X);
        var y = Math.Max(bounds.Y, rect.Y);
        var width = Math.Min(rect.Width, bounds.Right - x);
        var height = Math.Min(rect.Height, bounds.Bottom - y);
        return new Rect(x, y, Math.Max(0, width), Math.Max(0, height));
    }
}
