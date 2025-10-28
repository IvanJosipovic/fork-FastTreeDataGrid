using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Lightweight track widget that can render a background and optional indicator fill.
/// </summary>
public sealed class TrackWidget : Widget
{
    private double _indicatorValue;
    private Pen? _cachedBorderPen;

    public ImmutableSolidColorBrush? BackgroundBrush { get; set; }

    public ImmutableSolidColorBrush? IndicatorBrush { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double BorderThickness { get; set; }

    public double IndicatorValue
    {
        get => _indicatorValue;
        set => _indicatorValue = Math.Clamp(value, 0, 1);
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);

        var radius = Math.Max(0, CornerRadius.TopLeft);
        context.DrawRectangle(BackgroundBrush ?? Brushes.Transparent, GetBorderPen(), rect, radius, radius);

        if (IndicatorBrush is null)
        {
            return;
        }

        var width = rect.Width * _indicatorValue;
        if (width <= 0)
        {
            return;
        }

        var indicatorRect = new Rect(rect.X, rect.Y, width, rect.Height);
        context.DrawRectangle(IndicatorBrush, null, indicatorRect, radius, radius);
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
