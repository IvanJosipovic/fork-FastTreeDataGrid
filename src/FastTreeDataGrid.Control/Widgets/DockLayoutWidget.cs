using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class DockLayoutWidget : SurfaceWidget
{
    private readonly Dictionary<Widget, Dock> _dockMap = new();

    public Thickness Padding { get; set; } = new Thickness(0);

    public double Spacing { get; set; } = 4;

    public double DefaultDockLength { get; set; } = 80;

    public bool LastChildFill { get; set; } = true;

    public void SetDock(Widget child, Dock dock)
    {
        _dockMap[child] = dock;
    }

    public Dock GetDock(Widget child)
    {
        return _dockMap.TryGetValue(child, out var dock) ? dock : Dock.Left;
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var inner = bounds.Deflate(Padding);
        if (inner.Width <= 0 || inner.Height <= 0 || Children.Count == 0)
        {
            return;
        }

        var left = inner.X;
        var top = inner.Y;
        var right = inner.Right;
        var bottom = inner.Bottom;

        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var dock = GetDock(child);
            var isLast = LastChildFill && index == Children.Count - 1;

            var availableWidth = Math.Max(0, right - left);
            var availableHeight = Math.Max(0, bottom - top);

            double width;
            double height;

            if (isLast)
            {
                width = availableWidth;
                height = availableHeight;
                var finalRect = new Rect(left, top, width, height);
                child.Arrange(finalRect);
                continue;
            }

            switch (dock)
            {
                case Dock.Left:
                    width = ResolvePrimaryLength(child.DesiredWidth, availableWidth);
                    height = ResolveCrossLength(child.DesiredHeight, availableHeight);
                    child.Arrange(new Rect(left, top, width, height));
                    left += width;
                    if (index < Children.Count - 1)
                    {
                        left += Spacing;
                    }
                    break;

                case Dock.Right:
                    width = ResolvePrimaryLength(child.DesiredWidth, availableWidth);
                    height = ResolveCrossLength(child.DesiredHeight, availableHeight);
                    child.Arrange(new Rect(right - width, top, width, height));
                    right -= width;
                    if (index < Children.Count - 1)
                    {
                        right -= Spacing;
                    }
                    break;

                case Dock.Top:
                    height = ResolvePrimaryLength(child.DesiredHeight, availableHeight);
                    width = ResolveCrossLength(child.DesiredWidth, availableWidth);
                    child.Arrange(new Rect(left, top, width, height));
                    top += height;
                    if (index < Children.Count - 1)
                    {
                        top += Spacing;
                    }
                    break;

                case Dock.Bottom:
                    height = ResolvePrimaryLength(child.DesiredHeight, availableHeight);
                    width = ResolveCrossLength(child.DesiredWidth, availableWidth);
                    child.Arrange(new Rect(left, bottom - height, width, height));
                    bottom -= height;
                    if (index < Children.Count - 1)
                    {
                        bottom -= Spacing;
                    }
                    break;
            }
        }
    }

    private double ResolvePrimaryLength(double requested, double available)
    {
        var value = double.IsNaN(requested) || requested <= 0 ? DefaultDockLength : requested;
        value = Math.Min(value, available);
        return Math.Max(0, value);
    }

    private static double ResolveCrossLength(double requested, double available)
    {
        if (double.IsNaN(requested) || requested <= 0)
        {
            return available;
        }

        return Math.Max(0, Math.Min(requested, available));
    }
}
