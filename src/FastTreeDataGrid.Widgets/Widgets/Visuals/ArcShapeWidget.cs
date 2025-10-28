using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ArcShapeWidget : ShapeWidget
{
    private double _startAngle;
    private double _sweepAngle;

    public double StartAngle { get; set; }

    public double SweepAngle { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        ResetShapeStyle();
        _startAngle = StartAngle;
        _sweepAngle = SweepAngle;

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
        if (width <= 0 && height <= 0)
        {
            return null;
        }

        return ShapeGeometryCache.GetArcGeometry(width, height, ResolvedStrokeThickness, _startAngle, _sweepAngle);
    }

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case ArcShapeWidgetValue arc:
                ApplyShapeValue(arc);
                _startAngle = arc.StartAngle;
                _sweepAngle = arc.SweepAngle;
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            default:
                return false;
        }
    }

}
