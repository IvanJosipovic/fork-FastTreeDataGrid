using System;
using Avalonia;
using Avalonia.Layout;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class StackLayoutWidget : SurfaceWidget
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;

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
        var totalSpacing = Spacing * Math.Max(0, Children.Count - 1);
        var contentWidth = Math.Max(0, inner.Width - totalSpacing);
        var remaining = contentWidth;
        var flexibleCount = 0;

        foreach (var child in Children)
        {
            var desired = child.DesiredWidth;
            if (double.IsNaN(desired) || desired <= 0)
            {
                flexibleCount++;
            }
        }

        var currentX = inner.X;
        var availableHeight = inner.Height;

        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var desiredWidth = child.DesiredWidth;
            double width;
            if (!double.IsNaN(desiredWidth) && desiredWidth > 0)
            {
                width = Math.Min(desiredWidth, remaining);
            }
            else
            {
                width = flexibleCount > 0 ? remaining / flexibleCount : 0;
                flexibleCount = Math.Max(0, flexibleCount - 1);
            }

            width = Math.Max(0, Math.Min(width, Math.Max(0, inner.Right - currentX)));
            remaining = Math.Max(0, remaining - width);

            var desiredHeight = child.DesiredHeight;
            var height = !double.IsNaN(desiredHeight) && desiredHeight > 0
                ? Math.Min(desiredHeight, availableHeight)
                : availableHeight;

            height = Math.Max(0, Math.Min(height, availableHeight));
            var offsetY = inner.Y + Math.Max(0, (availableHeight - height) / 2);
            var childRect = new Rect(currentX, offsetY, width, height);
            child.Arrange(childRect);

            currentX += width;
            if (index < Children.Count - 1)
            {
                currentX += Spacing;
            }
        }
    }

    private void ArrangeVertical(Rect inner)
    {
        var totalSpacing = Spacing * Math.Max(0, Children.Count - 1);
        var contentHeight = Math.Max(0, inner.Height - totalSpacing);
        var remaining = contentHeight;
        var flexibleCount = 0;

        foreach (var child in Children)
        {
            var desired = child.DesiredHeight;
            if (double.IsNaN(desired) || desired <= 0)
            {
                flexibleCount++;
            }
        }

        var currentY = inner.Y;
        var availableWidth = inner.Width;

        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var desiredHeight = child.DesiredHeight;
            double height;
            if (!double.IsNaN(desiredHeight) && desiredHeight > 0)
            {
                height = Math.Min(desiredHeight, remaining);
            }
            else
            {
                height = flexibleCount > 0 ? remaining / flexibleCount : 0;
                flexibleCount = Math.Max(0, flexibleCount - 1);
            }

            height = Math.Max(0, Math.Min(height, Math.Max(0, inner.Bottom - currentY)));
            remaining = Math.Max(0, remaining - height);

            var desiredWidth = child.DesiredWidth;
            var width = !double.IsNaN(desiredWidth) && desiredWidth > 0
                ? Math.Min(desiredWidth, availableWidth)
                : availableWidth;

            width = Math.Max(0, Math.Min(width, availableWidth));
            var offsetX = inner.X + Math.Max(0, (availableWidth - width) / 2);
            var childRect = new Rect(offsetX, currentY, width, height);
            child.Arrange(childRect);

            currentY += height;
            if (index < Children.Count - 1)
            {
                currentY += Spacing;
            }
        }
    }
}
