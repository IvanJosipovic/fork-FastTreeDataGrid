using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class DockLayoutWidget : SurfaceWidget
{
    private readonly Dictionary<Widget, Dock> _dockMap = new();

    public Thickness Padding { get; set; } = new Thickness(0);

    public double Spacing { get; set; }

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

        var remaining = bounds.Deflate(Padding);
        if (remaining.Width <= 0 || remaining.Height <= 0 || Children.Count == 0)
        {
            return;
        }

        var children = Children;
        var lastFillIndex = LastChildFill && children.Count > 0 ? children.Count - 1 : -1;

        for (var index = 0; index < children.Count; index++)
        {
            if (remaining.Width <= 0 || remaining.Height <= 0)
            {
                break;
            }

            var child = children[index];
            var fill = index == lastFillIndex;

            if (fill)
            {
                child.Arrange(remaining);
                break;
            }

            var hasMore = index < children.Count - 1;
            ArrangeChild(child, GetDock(child), hasMore, ref remaining);
        }
    }

    private void ArrangeChild(Widget child, Dock dock, bool hasMoreChildren, ref Rect remaining)
    {
        var available = remaining;
        var spacing = Spacing > 0 ? Spacing : 0;

        switch (dock)
        {
            case Dock.Left:
            {
                var width = ResolvePrimary(child.DesiredWidth, available.Width);
                var rect = new Rect(available.X, available.Y, width, available.Height);
                child.Arrange(rect);

                var adjust = width;
                if (spacing > 0 && hasMoreChildren && available.Width - width > 0)
                {
                    adjust += spacing;
                }

                remaining = new Rect(available.X + adjust, available.Y, Math.Max(0, available.Width - adjust), available.Height);
                break;
            }

            case Dock.Right:
            {
                var width = ResolvePrimary(child.DesiredWidth, available.Width);
                var rect = new Rect(available.Right - width, available.Y, width, available.Height);
                child.Arrange(rect);

                var adjust = width;
                if (spacing > 0 && hasMoreChildren && available.Width - width > 0)
                {
                    adjust += spacing;
                }

                remaining = new Rect(available.X, available.Y, Math.Max(0, available.Width - adjust), available.Height);
                break;
            }

            case Dock.Top:
            {
                var height = ResolvePrimary(child.DesiredHeight, available.Height);
                var rect = new Rect(available.X, available.Y, available.Width, height);
                child.Arrange(rect);

                var adjust = height;
                if (spacing > 0 && hasMoreChildren && available.Height - height > 0)
                {
                    adjust += spacing;
                }

                remaining = new Rect(available.X, available.Y + adjust, available.Width, Math.Max(0, available.Height - adjust));
                break;
            }

            case Dock.Bottom:
            {
                var height = ResolvePrimary(child.DesiredHeight, available.Height);
                var rect = new Rect(available.X, available.Bottom - height, available.Width, height);
                child.Arrange(rect);

                var adjust = height;
                if (spacing > 0 && hasMoreChildren && available.Height - height > 0)
                {
                    adjust += spacing;
                }

                remaining = new Rect(available.X, available.Y, available.Width, Math.Max(0, available.Height - adjust));
                break;
            }
        }
    }

    private double ResolvePrimary(double requested, double available)
    {
        var value = double.IsNaN(requested) || requested <= 0 ? DefaultDockLength : requested;
        value = Math.Min(value, available);
        return Math.Max(0, value);
    }
}
