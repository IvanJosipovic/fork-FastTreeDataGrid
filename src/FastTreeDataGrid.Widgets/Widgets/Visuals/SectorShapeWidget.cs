using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class SectorShapeWidget : ShapeWidget
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
        return ShapeGeometryCache.GetSectorGeometry(width, height, ResolvedStrokeThickness, _startAngle, _sweepAngle);
    }

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case SectorShapeWidgetValue sector:
                ApplyShapeValue(sector);
                _startAngle = sector.StartAngle;
                _sweepAngle = sector.SweepAngle;
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            default:
                return false;
        }
    }

}
