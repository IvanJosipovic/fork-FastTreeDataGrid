using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class IconElementWidget : Widget
{
    private Geometry? _geometry;
    private IImage? _image;
    private ImmutableSolidColorBrush? _fill;
    private ImmutableSolidColorBrush? _background;
    private Pen? _stroke;
    private double _padding = 4;
    private Stretch _stretch = Stretch.Uniform;
    private StretchDirection _stretchDirection = StretchDirection.Both;

    public double Padding { get; set; } = 4;

    public Stretch Stretch
    {
        get => _stretch;
        set => _stretch = value;
    }

    public StretchDirection StretchDirection
    {
        get => _stretchDirection;
        set => _stretchDirection = value;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _geometry = null;
        _image = null;
        _fill = Foreground;
        _stroke = null;
        _background = null;
        _padding = Padding;
        _stretch = Stretch;
        _stretchDirection = StretchDirection;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value))
            {
                return;
            }
        }

        ApplyValue(item);
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);
        using var transform = PushRenderTransform(context);

        if (_background is not null)
        {
            context.FillRectangle(_background, Bounds);
        }

        if (_image is not null)
        {
            var sourceSize = ImageWidget.TryGetImageSize(_image);
            var destRect = ImageWidget.CalculateDestRect(Bounds, sourceSize, _padding, _stretch, _stretchDirection);
            if (destRect.Width > 0 && destRect.Height > 0)
            {
                context.DrawImage(_image, new Rect(sourceSize), destRect);
            }
            return;
        }

        if (_geometry is null)
        {
            return;
        }

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

        using var translation = context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY));
        using var scaleTransform = context.PushTransform(Matrix.CreateScale(scale, scale));
        using var origin = context.PushTransform(Matrix.CreateTranslation(-geometryBounds.X, -geometryBounds.Y));

        context.DrawGeometry(_fill ?? Foreground ?? Brushes.Gray, _stroke, _geometry);
    }

    public void SetIcon(Geometry geometry, ImmutableSolidColorBrush? fill = null, Pen? stroke = null, double? padding = null)
    {
        _geometry = geometry;
        _image = null;
        _fill = fill ?? Foreground;
        _stroke = stroke;
        if (padding is not null)
        {
            Padding = padding.Value;
        }
        _padding = Padding;
        RefreshStyle();
    }

    public void SetImage(IImage? image)
    {
        _image = image;
        RefreshStyle();
    }

    protected virtual bool ApplyValue(object? value)
    {
        switch (value)
        {
            case IconElementWidgetValue iconValue:
                SetIconValue(iconValue);
                return true;
            case PathIconWidgetValue pathValue:
                SetIconValue(new IconElementWidgetValue(PathData: pathValue.Data, Foreground: pathValue.Foreground, Stroke: pathValue.Stroke, Padding: pathValue.Padding));
                return true;
            case Geometry geometry:
                _geometry = geometry;
                _image = null;
                _fill = Foreground;
                return true;
            case string path when !string.IsNullOrWhiteSpace(path):
                try
                {
                    _geometry = StreamGeometry.Parse(path);
                    _image = null;
                }
                catch
                {
                    _geometry = null;
                }
                _fill = Foreground;
                return true;
            case IImage image:
                _image = image;
                _geometry = null;
                return true;
            default:
                return false;
        }
    }

    private void SetIconValue(IconElementWidgetValue value)
    {
        _background = value.Background;
        _stroke = value.Stroke;
        _padding = value.Padding;
        _stretch = value.Stretch;
        _stretchDirection = value.StretchDirection;
        _fill = value.Foreground ?? Foreground;

        if (!string.IsNullOrWhiteSpace(value.PathData))
        {
            try
            {
                _geometry = StreamGeometry.Parse(value.PathData);
                _image = null;
            }
            catch
            {
                _geometry = null;
            }
            return;
        }

        if (value.Image is not null)
        {
            _image = value.Image;
            _geometry = value.Geometry;
            return;
        }

        _geometry = value.Geometry;
        _image = null;
    }

    protected void ClearIcon()
    {
        _geometry = null;
        _image = null;
    }
}

public sealed class PathIconWidget : IconElementWidget
{
    private string? _data;

    public string? Data
    {
        get => _data;
        set
        {
            _data = value;
            if (!string.IsNullOrWhiteSpace(_data))
            {
                try
                {
                    ApplyPathIcon(StreamGeometry.Parse(_data));
                }
                catch
                {
                    ClearIcon();
                }
            }
            else
            {
                ClearIcon();
            }
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        if (provider is null || Key is null)
        {
            if (item is PathIconWidgetValue pathValue)
            {
                TryApply(pathValue);
            }
            return;
        }

        var value = provider.GetValue(item, Key);
        if (value is PathIconWidgetValue path)
        {
            TryApply(path);
        }
    }

    private void TryApply(PathIconWidgetValue value)
    {
        try
        {
            var geometry = StreamGeometry.Parse(value.Data);
            ApplyPathIcon(geometry, value.Foreground, value.Stroke, value.Padding);
        }
        catch
        {
            ClearIcon();
        }
    }

    private void ApplyPathIcon(Geometry? geometry, ImmutableSolidColorBrush? fill = null, Pen? stroke = null, double? padding = null)
    {
        if (geometry is null)
        {
            ClearIcon();
            return;
        }

        base.SetIcon(geometry, fill, stroke, padding);
    }
}
