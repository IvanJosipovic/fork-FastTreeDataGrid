using System;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class PathShapeWidget : ShapeWidget
{
    private Geometry? _geometry;

    public Geometry? Data { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        ResetShapeStyle();
        _geometry = Data;

        var applied = TryApplyValue(provider is not null && Key is not null ? provider.GetValue(item, Key) : null);
        if (!applied)
        {
            TryApplyValue(item);
        }

        FinalizeShapeStyle();
    }

    protected override Geometry? CreateGeometry() => _geometry;

    private bool TryApplyValue(object? value)
    {
        switch (value)
        {
            case PathShapeWidgetValue path:
                ApplyShapeValue(path);
                if (path.Data is not null)
                {
                    _geometry = path.Data;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(path.DataString))
                {
                    _geometry = ShapeGeometryCache.GetPathGeometry(path.DataString);
                    return _geometry is not null;
                }

                _geometry = null;
                return true;
            case ShapeWidgetValue shape:
                ApplyShapeValue(shape);
                return true;
            case Geometry geometry:
                _geometry = geometry;
                return true;
            case string data when !string.IsNullOrWhiteSpace(data):
                _geometry = ShapeGeometryCache.GetPathGeometry(data);
                return _geometry is not null;
            default:
                return false;
        }
    }
}
