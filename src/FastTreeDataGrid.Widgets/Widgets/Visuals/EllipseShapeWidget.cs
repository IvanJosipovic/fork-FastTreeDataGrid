using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class EllipseShapeWidget : ShapeWidget
{
    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        ResetShapeStyle();

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
        return ShapeGeometryCache.GetEllipseGeometry(width, height, ResolvedStrokeThickness);
    }

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case EllipseShapeWidgetValue ellipse:
                ApplyShapeValue(ellipse);
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            default:
                return false;
        }
    }
}
