using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Simple border widget that draws background and border while hosting a single child.
/// </summary>
public class BorderWidget : SurfaceWidget
{
    private Widget? _child;
    private IImmutableBrush? _background;
    private IImmutableBrush? _borderBrush;

    public Thickness Padding { get; set; } = new Thickness(0);

    public Thickness BorderThickness { get; set; } = new Thickness(0);

    public IBrush? Background
    {
        get => _background;
        set => _background = value?.ToImmutable();
    }

    public IBrush? BorderBrush
    {
        get => _borderBrush;
        set => _borderBrush = value?.ToImmutable();
    }

    [Content]
    public Widget? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child is not null)
            {
                Children.Remove(_child);
            }

            _child = value;

            if (_child is not null && !Children.Contains(_child))
            {
                Children.Add(_child);
            }
        }
    }

    protected override Size MeasureCore(Size available)
    {
        var border = BorderThickness;
        var padding = Padding;

        var innerWidth = Math.Max(0, available.Width - border.Left - border.Right - padding.Left - padding.Right);
        var innerHeight = Math.Max(0, available.Height - border.Top - border.Bottom - padding.Top - padding.Bottom);

        Size childSize = default;
        if (_child is not null)
        {
            childSize = _child.Measure(new Size(innerWidth, innerHeight));
        }

        var width = childSize.Width + border.Left + border.Right + padding.Left + padding.Right;
        var height = childSize.Height + border.Top + border.Bottom + padding.Top + padding.Bottom;

        return new Size(width, height);
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (_child is null)
        {
            return;
        }

        var content = Deflate(bounds, BorderThickness);
        content = Deflate(content, Padding);
        content = Normalize(content);

        _child.Arrange(content);
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);

        DrawBorder(context);

        _child?.Draw(context);
    }

    public override double GetAutoWidth(double availableHeight)
    {
        var border = BorderThickness;
        var padding = Padding;
        var childAvailableHeight = Math.Max(0, availableHeight - border.Top - border.Bottom - padding.Top - padding.Bottom);

        var childWidth = _child?.GetAutoWidth(childAvailableHeight) ?? 0;
        if ((double.IsNaN(childWidth) || childWidth <= 0) && _child is not null)
        {
            childWidth = _child.DesiredWidth > 0 ? _child.DesiredWidth : 0;
        }

        return childWidth + border.Left + border.Right + padding.Left + padding.Right;
    }

    public override double GetAutoHeight(double availableWidth)
    {
        var border = BorderThickness;
        var padding = Padding;
        var childAvailableWidth = Math.Max(0, availableWidth - border.Left - border.Right - padding.Left - padding.Right);

        var childHeight = _child?.GetAutoHeight(childAvailableWidth) ?? 0;
        if ((double.IsNaN(childHeight) || childHeight <= 0) && _child is not null)
        {
            childHeight = _child.DesiredHeight > 0 ? _child.DesiredHeight : 0;
        }

        return childHeight + border.Top + border.Bottom + padding.Top + padding.Bottom;
    }

    private void DrawBorder(DrawingContext context)
    {
        var brush = _background;
        var pen = CreateBorderPen();

        if (brush is null && pen is null)
        {
            return;
        }

        var radius = GetUniformCornerRadius(CornerRadius);
        context.DrawRectangle(brush, pen, Bounds, radius, radius);
    }

    private Pen? CreateBorderPen()
    {
        if (_borderBrush is null)
        {
            return null;
        }

        var thickness = GetAverage(BorderThickness);
        if (thickness <= 0)
        {
            return null;
        }

        return new Pen(_borderBrush, thickness);
    }

    private static double GetAverage(Thickness thickness)
    {
        return (Math.Max(0, thickness.Left)
               + Math.Max(0, thickness.Top)
               + Math.Max(0, thickness.Right)
               + Math.Max(0, thickness.Bottom)) / 4;
    }

    private static Rect Deflate(Rect rect, Thickness thickness)
    {
        if (thickness == default)
        {
            return rect;
        }

        var left = Math.Max(0, thickness.Left);
        var top = Math.Max(0, thickness.Top);
        var right = Math.Max(0, thickness.Right);
        var bottom = Math.Max(0, thickness.Bottom);

        var width = Math.Max(0, rect.Width - left - right);
        var height = Math.Max(0, rect.Height - top - bottom);

        return new Rect(rect.X + left, rect.Y + top, width, height);
    }

    private static Rect Normalize(Rect rect)
    {
        if (rect.Width >= 0 && rect.Height >= 0)
        {
            return rect;
        }

        var width = Math.Max(0, rect.Width);
        var height = Math.Max(0, rect.Height);
        return new Rect(rect.X, rect.Y, width, height);
    }

    private static double GetUniformCornerRadius(CornerRadius cornerRadius)
    {
        if (cornerRadius == default)
        {
            return 0;
        }

        if (Math.Abs(cornerRadius.TopLeft - cornerRadius.TopRight) < double.Epsilon
            && Math.Abs(cornerRadius.TopLeft - cornerRadius.BottomLeft) < double.Epsilon
            && Math.Abs(cornerRadius.TopLeft - cornerRadius.BottomRight) < double.Epsilon)
        {
            return cornerRadius.TopLeft;
        }

        return (cornerRadius.TopLeft + cornerRadius.TopRight + cornerRadius.BottomLeft + cornerRadius.BottomRight) / 4;
    }

}
