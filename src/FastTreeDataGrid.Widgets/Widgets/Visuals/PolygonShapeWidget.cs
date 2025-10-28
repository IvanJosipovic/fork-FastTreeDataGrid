using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class PolygonShapeWidget : ShapeWidget
{
    private Point[] _propertyPoints = Array.Empty<Point>();
    private Point[] _points = Array.Empty<Point>();

    public IReadOnlyList<Point> Points
    {
        get => _propertyPoints;
        set => _propertyPoints = ConvertPoints(value);
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        ResetShapeStyle();
        _points = _propertyPoints;

        var applied = TryApplyValue(provider is not null && Key is not null ? provider.GetValue(item, Key) : null);
        if (!applied)
        {
            TryApplyValue(item);
        }

        FinalizeShapeStyle();
    }

    protected override Geometry? CreateGeometry()
    {
        return ShapeGeometryCache.GetPolygonGeometry(_points);
    }

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case PolygonShapeWidgetValue polygon:
                ApplyShapeValue(polygon);
                if (polygon.Points is not null)
                {
                    _points = ConvertPoints(polygon.Points);
                }
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            case IReadOnlyList<Point> list:
                _points = ConvertPoints(list);
                return true;
            case IEnumerable<Point> enumerable:
                _points = enumerable.ToArray();
                return true;
            default:
                return false;
        }
    }

    private static Point[] ConvertPoints(IEnumerable<Point>? points)
    {
        if (points is null)
        {
            return Array.Empty<Point>();
        }

        if (points is Point[] array)
        {
            return array.Length == 0 ? Array.Empty<Point>() : array.ToArray();
        }

        if (points is IList<Point> list && list.Count == 0)
        {
            return Array.Empty<Point>();
        }

        return points.ToArray();
    }
}
