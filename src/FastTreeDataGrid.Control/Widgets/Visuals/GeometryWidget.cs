using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class GeometryWidget : Widget
{
    private Geometry? _geometry;
    private IBrush? _fill;
    private Pen? _stroke;
    private Stretch _stretch = Stretch.Uniform;
    private double _padding = 4;

    public double Padding { get; set; } = 4;

    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public Geometry? Geometry
    {
        get => _geometry;
        set
        {
            if (ReferenceEquals(_geometry, value))
            {
                return;
            }

            _geometry = value;
            RefreshStyle();
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _geometry = null;
        _stroke = null;
        _fill = null;
        _padding = Padding;
        _stretch = Stretch;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case GeometryWidgetValue geometryValue:
                _geometry = geometryValue.Geometry;
                _stretch = geometryValue.Stretch;
                _fill = geometryValue.Fill ?? Foreground;
                _stroke = geometryValue.Stroke;
                _padding = geometryValue.Padding;
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

        var geometryBounds = _geometry.Bounds;
        if (geometryBounds.Width <= 0 || geometryBounds.Height <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(0, Bounds.Width - (_padding * 2));
        var availableHeight = Math.Max(0, Bounds.Height - (_padding * 2));
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        var (scaleX, scaleY) = CalculateScale(geometryBounds, availableWidth, availableHeight, _stretch);

        var scaledWidth = geometryBounds.Width * scaleX;
        var scaledHeight = geometryBounds.Height * scaleY;
        var offsetX = Bounds.X + _padding + (availableWidth - scaledWidth) / 2;
        var offsetY = Bounds.Y + _padding + (availableHeight - scaledHeight) / 2;

        using var rotation = PushRenderTransform(context);
        using var translation = context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY));
        using var scale = context.PushTransform(Matrix.CreateScale(scaleX, scaleY));
        using var origin = context.PushTransform(Matrix.CreateTranslation(-geometryBounds.X, -geometryBounds.Y));

        context.DrawGeometry(_fill ?? Foreground ?? Brushes.Gray, _stroke, _geometry);
    }

    public void SetGeometry(Geometry? geometry, Stretch? stretch = null, IBrush? fill = null, Pen? stroke = null, double? padding = null)
    {
        _geometry = geometry;
        if (stretch is not null)
        {
            Stretch = stretch.Value;
        }

        _stretch = Stretch;
        _fill = fill ?? Foreground;
        _stroke = stroke;

        if (padding is not null)
        {
            Padding = padding.Value;
        }
        _padding = Padding;
        RefreshStyle();
    }

    private static (double scaleX, double scaleY) CalculateScale(Rect geometryBounds, double availableWidth, double availableHeight, Stretch stretch)
    {
        var widthScale = geometryBounds.Width <= 0 ? 1 : availableWidth / geometryBounds.Width;
        var heightScale = geometryBounds.Height <= 0 ? 1 : availableHeight / geometryBounds.Height;

        switch (stretch)
        {
            case Stretch.None:
                return (1, 1);
            case Stretch.Fill:
                return (widthScale, heightScale);
            case Stretch.UniformToFill:
                var uniformFill = Math.Max(widthScale, heightScale);
                return (uniformFill, uniformFill);
            default:
                var uniform = Math.Min(widthScale, heightScale);
                if (!double.IsFinite(uniform) || uniform <= 0)
                {
                    uniform = 1;
                }
                return (uniform, uniform);
        }
    }

}
