using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;

namespace FastTreeDataGrid.Control.Widgets;

internal sealed class StackLayoutAdapter : IPanelLayoutAdapter
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        var inner = context.InnerBounds;
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            ArrangeHorizontal(children, context, inner);
        }
        else
        {
            ArrangeVertical(children, context, inner);
        }
    }

    private static double ResolveFlexible(double remaining, ref int flexibleCount)
    {
        if (flexibleCount <= 0)
        {
            return 0;
        }

        var value = remaining / flexibleCount;
        flexibleCount = Math.Max(0, flexibleCount - 1);
        return value;
    }

    private void ArrangeHorizontal(IList<Widget> children, in PanelLayoutContext context, Rect inner)
    {
        var spacing = Math.Max(0, context.Spacing);
        var totalSpacing = spacing * Math.Max(0, children.Count - 1);
        var contentWidth = Math.Max(0, inner.Width - totalSpacing);
        var remaining = contentWidth;
        var flexibleCount = CountFlexible(children, vertical: false, availableCross: inner.Height);

        var currentX = inner.X;
        var availableHeight = inner.Height;

        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var desiredWidth = child.DesiredWidth;
            double width;

            if (!double.IsNaN(desiredWidth) && desiredWidth > 0)
            {
                width = Math.Min(desiredWidth, remaining);
            }
            else
            {
                var auto = child.GetAutoWidth(availableHeight);
                if (!double.IsNaN(auto) && auto > 0)
                {
                    width = Math.Min(auto, remaining);
                }
                else
                {
                    width = ResolveFlexible(remaining, ref flexibleCount);
                }
            }

            width = Math.Max(0, Math.Min(width, Math.Max(0, inner.Right - currentX)));
            remaining = Math.Max(0, remaining - width);

            var desiredHeight = child.DesiredHeight;
            double height;

            if (!double.IsNaN(desiredHeight) && desiredHeight > 0)
            {
                height = Math.Min(desiredHeight, availableHeight);
            }
            else
            {
                var auto = child.GetAutoHeight(double.IsNaN(width) || width <= 0 ? availableHeight : width);
                height = !double.IsNaN(auto) && auto > 0
                    ? Math.Min(auto, availableHeight)
                    : availableHeight;
            }

            height = Math.Max(0, Math.Min(height, availableHeight));
            var offsetY = inner.Y + Math.Max(0, (availableHeight - height) / 2);
            child.Arrange(new Rect(currentX, offsetY, width, height));

            currentX += width;
            if (index < children.Count - 1)
            {
                currentX += spacing;
            }
        }
    }

    private void ArrangeVertical(IList<Widget> children, in PanelLayoutContext context, Rect inner)
    {
        var spacing = Math.Max(0, context.Spacing);
        var totalSpacing = spacing * Math.Max(0, children.Count - 1);
        var contentHeight = Math.Max(0, inner.Height - totalSpacing);
        var remaining = contentHeight;
        var flexibleCount = CountFlexible(children, vertical: true, availableCross: inner.Width);

        var currentY = inner.Y;
        var availableWidth = inner.Width;

        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var desiredHeight = child.DesiredHeight;
            double height;

            if (!double.IsNaN(desiredHeight) && desiredHeight > 0)
            {
                height = Math.Min(desiredHeight, remaining);
            }
            else
            {
                var auto = child.GetAutoHeight(availableWidth);
                if (!double.IsNaN(auto) && auto > 0)
                {
                    height = Math.Min(auto, remaining);
                }
                else
                {
                    height = ResolveFlexible(remaining, ref flexibleCount);
                }
            }

            height = Math.Max(0, Math.Min(height, Math.Max(0, inner.Bottom - currentY)));
            remaining = Math.Max(0, remaining - height);

            var desiredWidth = child.DesiredWidth;
            double width;

            if (!double.IsNaN(desiredWidth) && desiredWidth > 0)
            {
                width = Math.Min(desiredWidth, availableWidth);
            }
            else
            {
                var auto = child.GetAutoWidth(height);
                width = !double.IsNaN(auto) && auto > 0
                    ? Math.Min(auto, availableWidth)
                    : availableWidth;
            }

            width = Math.Max(0, Math.Min(width, availableWidth));
            var offsetX = inner.X + Math.Max(0, (availableWidth - width) / 2);
            child.Arrange(new Rect(offsetX, currentY, width, height));

            currentY += height;
            if (index < children.Count - 1)
            {
                currentY += spacing;
            }
        }
    }

    private static int CountFlexible(IList<Widget> children, bool vertical, double availableCross)
    {
        var count = 0;
        foreach (var child in children)
        {
            var desired = vertical ? child.DesiredHeight : child.DesiredWidth;
            var auto = vertical
                ? child.GetAutoHeight(availableCross)
                : child.GetAutoWidth(availableCross);

            if ((double.IsNaN(desired) || desired <= 0)
                && (double.IsNaN(auto) || auto <= 0))
            {
                count++;
            }
        }

        return count;
    }
}
