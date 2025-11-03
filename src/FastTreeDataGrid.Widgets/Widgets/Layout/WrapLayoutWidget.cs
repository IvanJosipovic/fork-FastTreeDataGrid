using System;
using Avalonia;
using Avalonia.Layout;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class WrapLayoutWidget : SurfaceWidget
{
    static WrapLayoutWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(WrapLayoutWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not WrapLayoutWidget wrap)
                {
                    return;
                }

                var layout = theme.Palette.Layout;
                if (wrap.Padding == default)
                {
                    wrap.Padding = layout.ContentPadding;
                }

                if (wrap.Spacing <= 0)
                {
                    wrap.Spacing = layout.DefaultSpacing;
                }
            }));
    }

    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    public Thickness Padding { get; set; } = new Thickness(0);

    public double Spacing { get; set; } = 0;

    public double DefaultItemWidth { get; set; } = 32;

    public double DefaultItemHeight { get; set; } = 32;

    protected override Size MeasureCore(Size available)
    {
        var padding = Padding;
        var inner = new Rect(padding.Left, padding.Top,
            Math.Max(0, available.Width - padding.Left - padding.Right),
            Math.Max(0, available.Height - padding.Top - padding.Bottom));

        if (Children.Count == 0 || inner.Width <= 0)
        {
            return new Size(padding.Left + padding.Right, padding.Top + padding.Bottom);
        }

        double lineExtent = 0;
        double lineThickness = 0;
        double totalExtent = 0;
        double maxThickness = 0;

        foreach (var child in Children)
        {
            var childSize = child.Measure(new Size(inner.Width, inner.Height));
            var width = childSize.Width;
            var height = childSize.Height;

            if (width <= 0)
            {
                width = DefaultItemWidth;
            }

            if (height <= 0)
            {
                height = DefaultItemHeight;
            }

            if (Orientation == Orientation.Horizontal)
            {
                if (lineExtent > 0 && lineExtent + width > inner.Width)
                {
                    totalExtent = Math.Max(totalExtent, Math.Max(0, lineExtent - Spacing));
                    maxThickness += lineThickness + Spacing;
                    lineExtent = 0;
                    lineThickness = 0;
                }

                lineExtent += width + Spacing;
                lineThickness = Math.Max(lineThickness, height);
            }
            else
            {
                if (lineExtent > 0 && lineExtent + height > inner.Height)
                {
                    totalExtent = Math.Max(totalExtent, Math.Max(0, lineExtent - Spacing));
                    maxThickness += lineThickness + Spacing;
                    lineExtent = 0;
                    lineThickness = 0;
                }

                lineExtent += height + Spacing;
                lineThickness = Math.Max(lineThickness, width);
            }
        }

        if (lineExtent > 0)
        {
            if (Orientation == Orientation.Horizontal)
            {
                totalExtent = Math.Max(totalExtent, Math.Max(0, lineExtent - Spacing));
                maxThickness += lineThickness;
            }
            else
            {
                totalExtent = Math.Max(totalExtent, Math.Max(0, lineExtent - Spacing));
                maxThickness += lineThickness;
            }
        }

        if (Orientation == Orientation.Horizontal)
        {
            return new Size(
                padding.Left + padding.Right + Math.Min(totalExtent, inner.Width),
                padding.Top + padding.Bottom + Math.Min(maxThickness, Math.Max(0, available.Height - padding.Top - padding.Bottom)));
        }

        return new Size(
            padding.Left + padding.Right + Math.Min(maxThickness, Math.Max(0, available.Width - padding.Left - padding.Right)),
            padding.Top + padding.Bottom + Math.Min(totalExtent, inner.Height));
    }

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
            var width = ResolveWidth(child, inner.Width, inner.Height, DefaultItemWidth);
            var height = ResolveHeight(child, width, inner.Height, DefaultItemHeight);

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
            var width = ResolveWidth(child, inner.Width, inner.Height, DefaultItemWidth);
            var height = ResolveHeight(child, width, inner.Height, DefaultItemHeight);

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

    private static double ResolveWidth(Widget child, double maxWidth, double availableHeight, double fallback)
    {
        var requested = child.DesiredWidth;

        if (!double.IsNaN(requested) && requested > 0)
        {
            return Math.Max(0, Math.Min(requested, maxWidth));
        }

        var fallbackValue = fallback > 0 ? fallback : maxWidth;

        var auto = child.GetAutoWidth(availableHeight);
        if (!double.IsNaN(auto) && auto > 0)
        {
            return Math.Max(0, Math.Min(auto, maxWidth));
        }

        return Math.Max(0, Math.Min(fallbackValue, maxWidth));
    }

    private static double ResolveHeight(Widget child, double width, double maxHeight, double fallback)
    {
        var requested = child.DesiredHeight;

        if (!double.IsNaN(requested) && requested > 0)
        {
            return Math.Max(0, Math.Min(requested, maxHeight));
        }

        var auto = child.GetAutoHeight(width);
        if (!double.IsNaN(auto) && auto > 0)
        {
            return Math.Max(0, Math.Min(auto, maxHeight));
        }

        var fallbackValue = fallback > 0 ? fallback : maxHeight;
        return Math.Max(0, Math.Min(fallbackValue, maxHeight));
    }
}
