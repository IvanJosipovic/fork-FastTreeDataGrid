using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class RectangleShapeWidget : ShapeWidget
{
    private double _radiusX;
    private double _radiusY;

    public double RadiusX { get; set; }

    public double RadiusY { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        ResetShapeStyle();
        _radiusX = RadiusX;
        _radiusY = RadiusY;

        var applied = TryApplyValue(provider is not null && Key is not null ? provider.GetValue(item, Key) : null);
        if (!applied)
        {
            TryApplyValue(item);
        }

        FinalizeShapeStyle();
    }

    protected override Geometry? CreateGeometry()
    {
        var width = Math.Max(0, Bounds.Width);
        var height = Math.Max(0, Bounds.Height);
        return ShapeGeometryCache.GetRectangleGeometry(width, height, ResolvedStrokeThickness, _radiusX, _radiusY);
    }

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case RectangleShapeWidgetValue rectangle:
                ApplyShapeValue(rectangle);
                if (rectangle.RadiusX is not null)
                {
                    _radiusX = rectangle.RadiusX.Value;
                }

                if (rectangle.RadiusY is not null)
                {
                    _radiusY = rectangle.RadiusY.Value;
                }
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            default:
                return false;
        }
    }
}
