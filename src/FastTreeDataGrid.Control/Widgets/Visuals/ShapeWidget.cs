using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class ShapeWidget : Widget
{
    private ImmutableSolidColorBrush? _resolvedFill;
    private Pen? _resolvedPen;
    private double _resolvedStrokeThickness;
    private Stretch _resolvedStretch = Stretch.Fill;

    private ImmutableSolidColorBrush? _effectiveFill;
    private ImmutableSolidColorBrush? _effectiveStroke;
    private double? _effectiveStrokeThickness;
    private IReadOnlyList<double>? _effectiveStrokeDashArray;
    private double _effectiveStrokeDashOffset;
    private PenLineCap _effectiveStrokeLineCap;
    private PenLineJoin _effectiveStrokeLineJoin;
    private double _effectiveStrokeMiterLimit;
    private Stretch _effectiveStretch = Stretch.Fill;

    static ShapeWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(ShapeWidget),
                WidgetVisualState.Normal,
                (widget, theme) =>
                {
                    if (widget is not ShapeWidget shape)
                    {
                        return;
                    }

                    var palette = theme.Palette.Shape;
                    widget.Foreground ??= palette.Stroke;
                    shape.Fill ??= palette.Fill;
                    shape.Stroke ??= palette.Stroke;
                    shape.StrokeThickness ??= palette.StrokeThickness;

                    if (shape.StrokeDashArray is null && palette.StrokeDashArray is not null)
                    {
                        shape.StrokeDashArray = palette.StrokeDashArray;
                    }

                    if (Math.Abs(shape.StrokeDashOffset) <= double.Epsilon && palette.StrokeDashOffset > 0)
                    {
                        shape.StrokeDashOffset = palette.StrokeDashOffset;
                    }

                    if (shape.StrokeLineCap == PenLineCap.Flat)
                    {
                        shape.StrokeLineCap = palette.StrokeLineCap;
                    }

                    if (shape.StrokeLineJoin == PenLineJoin.Miter)
                    {
                        shape.StrokeLineJoin = palette.StrokeLineJoin;
                    }

                    if (Math.Abs(shape.StrokeMiterLimit - 10) <= double.Epsilon)
                    {
                        shape.StrokeMiterLimit = palette.StrokeMiterLimit;
                    }
                }));
    }

    protected ShapeWidget()
    {
        _effectiveStrokeLineCap = StrokeLineCap;
        _effectiveStrokeLineJoin = StrokeLineJoin;
        _effectiveStrokeMiterLimit = StrokeMiterLimit;
        _effectiveStretch = Stretch;
    }

    public ImmutableSolidColorBrush? Fill { get; set; }

    public ImmutableSolidColorBrush? Stroke { get; set; }

    public double? StrokeThickness { get; set; }

    public IReadOnlyList<double>? StrokeDashArray { get; set; }

    public double StrokeDashOffset { get; set; }

    public PenLineCap StrokeLineCap { get; set; } = PenLineCap.Flat;

    public PenLineJoin StrokeLineJoin { get; set; } = PenLineJoin.Miter;

    public double StrokeMiterLimit { get; set; } = 10;

    public Stretch Stretch { get; set; } = Stretch.Fill;

    protected ImmutableSolidColorBrush? ResolvedFill => _resolvedFill;

    protected Pen? ResolvedPen => _resolvedPen;

    protected double ResolvedStrokeThickness => _resolvedStrokeThickness;

    protected Stretch ResolvedStretch => _resolvedStretch;

    protected void ResetShapeStyle()
    {
        _effectiveFill = Fill;
        _effectiveStroke = Stroke;
        _effectiveStrokeThickness = StrokeThickness;
        _effectiveStrokeDashArray = StrokeDashArray;
        _effectiveStrokeDashOffset = StrokeDashOffset;
        _effectiveStrokeLineCap = StrokeLineCap;
        _effectiveStrokeLineJoin = StrokeLineJoin;
        _effectiveStrokeMiterLimit = StrokeMiterLimit;
        _effectiveStretch = Stretch;
    }

    protected void ApplyShapeValue(ShapeWidgetValue value)
    {
        if (value.Fill is not null)
        {
            _effectiveFill = value.Fill;
        }

        if (value.Stroke is not null)
        {
            _effectiveStroke = value.Stroke;
        }

        if (value.StrokeThickness is not null)
        {
            _effectiveStrokeThickness = value.StrokeThickness;
        }

        if (value.StrokeDashArray is not null)
        {
            _effectiveStrokeDashArray = value.StrokeDashArray;
        }

        if (value.StrokeDashOffset is not null)
        {
            _effectiveStrokeDashOffset = value.StrokeDashOffset.Value;
        }

        if (value.StrokeLineCap is not null)
        {
            _effectiveStrokeLineCap = value.StrokeLineCap.Value;
        }

        if (value.StrokeLineJoin is not null)
        {
            _effectiveStrokeLineJoin = value.StrokeLineJoin.Value;
        }

        if (value.StrokeMiterLimit is not null)
        {
            _effectiveStrokeMiterLimit = value.StrokeMiterLimit.Value;
        }

        if (value.Stretch is not null)
        {
            _effectiveStretch = value.Stretch.Value;
        }
    }

    protected void FinalizeShapeStyle()
    {
        var palette = WidgetFluentPalette.Current.Shape;

        _resolvedFill = _effectiveFill ?? palette.Fill;
        var stroke = _effectiveStroke ?? Foreground ?? palette.Stroke;
        var thickness = _effectiveStrokeThickness ?? palette.StrokeThickness;
        thickness = double.IsFinite(thickness) ? Math.Max(0, thickness) : palette.StrokeThickness;
        _resolvedStrokeThickness = thickness;
        _resolvedStretch = _effectiveStretch;

        if (stroke is not null && thickness > 0)
        {
            DashStyle? dashStyle = null;
            if (_effectiveStrokeDashArray is { Count: > 0 })
            {
                dashStyle = new DashStyle(_effectiveStrokeDashArray, _effectiveStrokeDashOffset);
            }

            _resolvedPen = new Pen(stroke, thickness, dashStyle, _effectiveStrokeLineCap, _effectiveStrokeLineJoin, Math.Max(0.1, _effectiveStrokeMiterLimit));
        }
        else
        {
            _resolvedPen = null;
        }
    }

    protected bool TryApplyShapeValue(object? value)
    {
        if (value is ShapeWidgetValue shapeValue)
        {
            ApplyShapeValue(shapeValue);
            return true;
        }

        return false;
    }

    protected object? ResolveValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (value is not null)
            {
                return value;
            }
        }

        return item;
    }

    public override void Draw(DrawingContext context)
    {
        var geometry = CreateGeometry();
        if (geometry is null)
        {
            return;
        }

        var fill = ResolvedFill;
        var pen = ResolvedPen;
        if (fill is null && pen is null)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);
        using var translation = context.PushTransform(Matrix.CreateTranslation(Bounds.X, Bounds.Y));

        var geometryBounds = geometry.Bounds;
        if (geometryBounds.Width <= 0 && geometryBounds.Height <= 0)
        {
            context.DrawGeometry(fill, pen, geometry);
            return;
        }

        var (_, transform) = CalculateSizeAndTransform(Bounds.Size, geometryBounds, ResolvedStretch);
        if (transform != Matrix.Identity)
        {
            using var stretch = context.PushTransform(transform);
            context.DrawGeometry(fill, pen, geometry);
            return;
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    protected abstract Geometry? CreateGeometry();

    protected static (Size size, Matrix transform) CalculateSizeAndTransform(Size availableSize, Rect shapeBounds, Stretch stretch)
    {
        Size shapeSize = new Size(shapeBounds.Right, shapeBounds.Bottom);
        Matrix translate = Matrix.Identity;
        double desiredX = availableSize.Width;
        double desiredY = availableSize.Height;
        double sx = 0.0;
        double sy = 0.0;

        if (stretch != Stretch.None)
        {
            shapeSize = shapeBounds.Size;
            translate = Matrix.CreateTranslation(-(Vector)shapeBounds.Position);
        }

        if (double.IsInfinity(availableSize.Width))
        {
            desiredX = shapeSize.Width;
        }

        if (double.IsInfinity(availableSize.Height))
        {
            desiredY = shapeSize.Height;
        }

        if (shapeBounds.Width > 0)
        {
            sx = desiredX / shapeSize.Width;
        }

        if (shapeBounds.Height > 0)
        {
            sy = desiredY / shapeSize.Height;
        }

        if (double.IsInfinity(availableSize.Width))
        {
            sx = sy;
        }

        if (double.IsInfinity(availableSize.Height))
        {
            sy = sx;
        }

        switch (stretch)
        {
            case Stretch.Uniform:
                sx = sy = Math.Min(sx, sy);
                break;
            case Stretch.UniformToFill:
                sx = sy = Math.Max(sx, sy);
                break;
            case Stretch.Fill:
                if (double.IsInfinity(availableSize.Width))
                {
                    sx = 1.0;
                }

                if (double.IsInfinity(availableSize.Height))
                {
                    sy = 1.0;
                }

                break;
            default:
                sx = sy = 1;
                break;
        }

        var transform = translate * Matrix.CreateScale(sx, sy);
        var size = new Size(shapeSize.Width * sx, shapeSize.Height * sy);
        return (size, transform);
    }
}
