using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Simple thumb widget used by interactive slider or toggle controls.
/// </summary>
public sealed class ThumbWidget : Widget
{
    private Pen? _cachedBorderPen;

    public ImmutableSolidColorBrush? FillBrush { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double BorderThickness { get; set; } = 1;

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);

        var radiusX = rect.Width / 2;
        var radiusY = rect.Height / 2;
        context.DrawEllipse(FillBrush ?? Brushes.White, GetBorderPen(), rect.Center, radiusX, radiusY);
    }

    private Pen? GetBorderPen()
    {
        if (BorderBrush is null || BorderThickness <= 0)
        {
            _cachedBorderPen = null;
            return null;
        }

        if (_cachedBorderPen is null
            || !ReferenceEquals(_cachedBorderPen.Brush, BorderBrush)
            || Math.Abs(_cachedBorderPen.Thickness - BorderThickness) > double.Epsilon)
        {
            _cachedBorderPen = new Pen(BorderBrush, BorderThickness);
        }

        return _cachedBorderPen;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
    }
}
