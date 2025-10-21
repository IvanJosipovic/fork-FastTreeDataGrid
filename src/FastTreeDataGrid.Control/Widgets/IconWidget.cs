using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class IconWidget : Widget
{
    private Geometry? _geometry;
    private ImmutableSolidColorBrush? _fill;
    private Pen? _stroke;
    private double _padding = 4;

    public double Padding { get; set; } = 4;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _geometry = null;
        _stroke = null;
        _fill = null;
        _padding = Padding;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case IconWidgetValue iconValue:
                _geometry = iconValue.Geometry;
                _fill = iconValue.Fill ?? Foreground;
                _stroke = iconValue.Stroke;
                _padding = iconValue.Padding;
                break;
            case Geometry geometry:
                _geometry = geometry;
                _fill = Foreground;
                break;
            case string path when !string.IsNullOrWhiteSpace(path):
                try
                {
                    _geometry = StreamGeometry.Parse(path);
                }
                catch
                {
                    _geometry = null;
                }
                _fill = Foreground;
                break;
        }
    }

    public override void Draw(DrawingContext context)
    {
        if (_geometry is null)
        {
            return;
        }

        using var clip = PushClip(context);

        var availableWidth = Math.Max(0, Bounds.Width - (_padding * 2));
        var availableHeight = Math.Max(0, Bounds.Height - (_padding * 2));
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        var geometryBounds = _geometry.Bounds;
        if (geometryBounds.Width <= 0 || geometryBounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(availableWidth / geometryBounds.Width, availableHeight / geometryBounds.Height);
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1;
        }

        var scaledWidth = geometryBounds.Width * scale;
        var scaledHeight = geometryBounds.Height * scale;
        var offsetX = Bounds.X + _padding + (availableWidth - scaledWidth) / 2;
        var offsetY = Bounds.Y + _padding + (availableHeight - scaledHeight) / 2;

        using var rotation = context.PushTransform(CreateRotationMatrix());
        using var translation = context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY));
        using var scaleTransform = context.PushTransform(Matrix.CreateScale(scale, scale));
        using var origin = context.PushTransform(Matrix.CreateTranslation(-geometryBounds.X, -geometryBounds.Y));

        context.DrawGeometry(_fill ?? Foreground ?? Brushes.Gray, _stroke, _geometry);
    }

    public void SetIcon(Geometry geometry, ImmutableSolidColorBrush? fill = null, Pen? stroke = null, double? padding = null)
    {
        _geometry = geometry;
        _fill = fill ?? Foreground;
        _stroke = stroke;
        if (padding is not null)
        {
            Padding = padding.Value;
        }
        _padding = Padding;
        RefreshStyle();
    }

    private Matrix CreateRotationMatrix()
    {
        if (Math.Abs(Rotation) <= double.Epsilon)
        {
            return Matrix.Identity;
        }

        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        return Matrix.CreateTranslation(-centerX, -centerY)
               * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
               * Matrix.CreateTranslation(centerX, centerY);
    }
}
