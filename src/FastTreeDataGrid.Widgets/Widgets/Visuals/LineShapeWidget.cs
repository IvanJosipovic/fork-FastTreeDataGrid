using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class LineShapeWidget : ShapeWidget
{
    private Point _startPoint;
    private Point _endPoint;

    public Point StartPoint { get; set; }

    public Point EndPoint { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        ResetShapeStyle();
        _startPoint = StartPoint;
        _endPoint = EndPoint;

        var applied = TryApplyValue(provider is not null && Key is not null ? provider.GetValue(item, Key) : null);
        if (!applied)
        {
            TryApplyValue(item);
        }

        FinalizeShapeStyle();
    }

    protected override Geometry? CreateGeometry() => ShapeGeometryCache.GetLineGeometry(_startPoint, _endPoint);

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case LineShapeWidgetValue line:
                ApplyShapeValue(line);
                _startPoint = line.StartPoint;
                _endPoint = line.EndPoint;
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            case LineGeometry geometry:
                _startPoint = geometry.StartPoint;
                _endPoint = geometry.EndPoint;
                return true;
            case (Point start, Point end):
                _startPoint = start;
                _endPoint = end;
                return true;
            default:
                return false;
        }
    }
}
