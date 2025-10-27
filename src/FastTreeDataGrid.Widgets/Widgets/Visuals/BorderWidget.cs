using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Simple border widget that draws background and border while hosting a single child.
/// </summary>
public class BorderWidget : SurfaceWidget
{
    private Widget? _child;

    public Thickness Padding { get; set; } = new Thickness(0);

    public Thickness BorderThickness { get; set; } = new Thickness(0);

    public ImmutableSolidColorBrush? Background { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

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

    private void DrawBorder(DrawingContext context)
    {
        var brush = Background;
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
        if (BorderBrush is null)
        {
            return null;
        }

        var thickness = GetAverage(BorderThickness);
        if (thickness <= 0)
        {
            return null;
        }

        return new Pen(BorderBrush, thickness);
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
