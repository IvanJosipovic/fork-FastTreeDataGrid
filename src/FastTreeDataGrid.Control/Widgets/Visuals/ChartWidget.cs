using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ChartWidget : Widget
{
    private readonly List<SeriesData> _series = new();
    private double? _minimumOverride;
    private double? _maximumOverride;
    private double? _baselineOverride;
    private bool _showBaseline;
    private ImmutableSolidColorBrush? _baselineBrush;
    private double _baselineThickness = 1;
    private ImmutableSolidColorBrush? _defaultStroke;
    private ImmutableSolidColorBrush? _defaultFill;

    public double StrokeThickness { get; set; } = 1.5;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _series.Clear();
        _minimumOverride = null;
        _maximumOverride = null;
        _baselineOverride = null;
        _showBaseline = false;
        _baselineBrush = null;
        _baselineThickness = 1;

        var palette = WidgetFluentPalette.Current.Chart;
        _defaultStroke = Foreground ?? palette.Line;
        _defaultFill = palette.Fill;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);

        switch (value)
        {
            case null:
                break;

            case ChartWidgetValue chartValue:
                ApplyChartValue(chartValue);
                break;

            case ChartSeriesValue seriesValue:
                AddSeries(seriesValue.Points, seriesValue.Stroke, seriesValue.Fill, seriesValue.StrokeThickness, seriesValue.FillToBaseline);
                break;

            case IEnumerable<double> numericSeries when value is not string:
                AddSeries(numericSeries, null, null, StrokeThickness, fillToBaseline: false);
                break;

            case double numeric:
                AddSeries(new[] { numeric }, null, null, StrokeThickness, fillToBaseline: false);
                break;

            default:
            {
                if (value is IEnumerable<object> mixedSeries)
                {
                    AddSeries(mixedSeries.OfType<double>(), null, null, StrokeThickness, fillToBaseline: false);
                }

                break;
            }
        }

        _baselineBrush ??= palette.Baseline;
    }

    public override void Draw(DrawingContext context)
    {
        if (_series.Count == 0)
        {
            return;
        }

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);

        var palette = WidgetFluentPalette.Current.Chart;
        var (min, max) = ResolveRange();
        if (!double.IsFinite(min) || !double.IsFinite(max))
        {
            return;
        }

        if (Math.Abs(max - min) < double.Epsilon)
        {
            var adjustment = Math.Max(1, Math.Abs(max));
            min -= adjustment * 0.5;
            max += adjustment * 0.5;
        }

        var range = max - min;
        if (range <= 0)
        {
            range = 1;
        }

        var baseline = ResolveBaseline(min, max);
        var baselineY = MapValue(baseline, min, range, rect);

        foreach (var series in _series)
        {
            DrawSeries(context, rect, min, max, range, baselineY, series, palette);
        }

        if (_showBaseline)
        {
            var baselinePen = new Pen(_baselineBrush ?? palette.Baseline, Math.Max(0.5, _baselineThickness))
            {
                LineCap = PenLineCap.Square,
            };
            context.DrawLine(baselinePen, new Point(rect.X, baselineY), new Point(rect.Right, baselineY));
        }
    }

    private void ApplyChartValue(ChartWidgetValue value)
    {
        _minimumOverride = value.Minimum;
        _maximumOverride = value.Maximum;
        _baselineOverride = value.Baseline;
        _showBaseline = value.ShowBaseline;

        if (value.BaselineBrush is not null)
        {
            _baselineBrush = value.BaselineBrush;
        }

        if (value.BaselineThickness > 0)
        {
            _baselineThickness = value.BaselineThickness;
        }

        if (value.Series is null || value.Series.Count == 0)
        {
            return;
        }

        foreach (var series in value.Series)
        {
            AddSeries(series.Points, series.Stroke, series.Fill, series.StrokeThickness, series.FillToBaseline);
        }
    }

    private void AddSeries(IEnumerable<double> values, ImmutableSolidColorBrush? stroke, ImmutableSolidColorBrush? fill, double thickness, bool fillToBaseline)
    {
        var buffer = new List<double>();

        foreach (var sample in values)
        {
            if (double.IsFinite(sample))
            {
                buffer.Add(sample);
            }
        }

        if (buffer.Count == 0)
        {
            return;
        }

        var points = buffer.ToArray();
        var seriesThickness = double.IsFinite(thickness) && thickness > 0 ? thickness : StrokeThickness;

        _series.Add(new SeriesData(points, stroke, fill, seriesThickness, fillToBaseline));
    }

    private (double Min, double Max) ResolveRange()
    {
        double min;
        double max;

        if (_minimumOverride.HasValue)
        {
            min = _minimumOverride.Value;
        }
        else
        {
            min = double.PositiveInfinity;
            foreach (var series in _series)
            {
                if (series.Minimum < min)
                {
                    min = series.Minimum;
                }
            }
        }

        if (_maximumOverride.HasValue)
        {
            max = _maximumOverride.Value;
        }
        else
        {
            max = double.NegativeInfinity;
            foreach (var series in _series)
            {
                if (series.Maximum > max)
                {
                    max = series.Maximum;
                }
            }
        }

        return (min, max);
    }

    private double ResolveBaseline(double min, double max)
    {
        if (!_showBaseline)
        {
            return min;
        }

        var baseline = _baselineOverride ?? 0;
        if (!double.IsFinite(baseline))
        {
            baseline = 0;
        }

        if (baseline < min)
        {
            baseline = min;
        }
        else if (baseline > max)
        {
            baseline = max;
        }

        return baseline;
    }

    private void DrawSeries(
        DrawingContext context,
        Rect rect,
        double min,
        double max,
        double range,
        double baselineY,
        SeriesData series,
        WidgetFluentPalette.ChartPalette palette)
    {
        var stroke = series.Stroke ?? _defaultStroke ?? palette.Line;
        var fill = series.Fill ?? (series.FillToBaseline ? _defaultFill : null);
        var thickness = series.StrokeThickness > 0 ? series.StrokeThickness : StrokeThickness;

        if (series.Points.Length == 1)
        {
            var y = MapValue(series.Points[0], min, range, rect);
            var x = rect.X + rect.Width / 2;
            var singlePointPen = new Pen(stroke, thickness)
            {
                LineCap = PenLineCap.Round,
            };
            context.DrawLine(singlePointPen, new Point(x, y), new Point(x, y));
            return;
        }

        var step = series.Points.Length > 1
            ? rect.Width / Math.Max(1, series.Points.Length - 1)
            : 0;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var firstPoint = new Point(rect.X, MapValue(series.Points[0], min, range, rect));
            var hasFill = fill is not null;
            ctx.BeginFigure(firstPoint, hasFill);

            for (var i = 1; i < series.Points.Length; i++)
            {
                var point = new Point(rect.X + step * i, MapValue(series.Points[i], min, range, rect));
                ctx.LineTo(point);
            }

            if (hasFill)
            {
                var fillBaseline = series.FillToBaseline ? baselineY : MapValue(min, min, range, rect);
                var lastX = rect.X + step * (series.Points.Length - 1);
                ctx.LineTo(new Point(lastX, fillBaseline));
                ctx.LineTo(new Point(rect.X, fillBaseline));
            }

            ctx.EndFigure(hasFill);
        }

        var pen = new Pen(stroke, thickness)
        {
            LineJoin = PenLineJoin.Round,
            LineCap = PenLineCap.Round,
        };

        context.DrawGeometry(fill, pen, geometry);
    }

    private static double MapValue(double value, double min, double range, Rect rect)
    {
        if (!double.IsFinite(value))
        {
            value = min;
        }

        if (range <= 0)
        {
            range = 1;
        }

        value = Math.Clamp(value, min, min + range);
        var normalized = (value - min) / range;
        return rect.Bottom - normalized * rect.Height;
    }

    private readonly struct SeriesData
    {
        public SeriesData(
            double[] points,
            ImmutableSolidColorBrush? stroke,
            ImmutableSolidColorBrush? fill,
            double strokeThickness,
            bool fillToBaseline)
        {
            Points = points;
            Stroke = stroke;
            Fill = fill;
            StrokeThickness = strokeThickness;
            FillToBaseline = fillToBaseline;

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            foreach (var point in points)
            {
                if (point < min)
                {
                    min = point;
                }

                if (point > max)
                {
                    max = point;
                }
            }

            Minimum = min;
            Maximum = max;
        }

        public double[] Points { get; }
        public ImmutableSolidColorBrush? Stroke { get; }
        public ImmutableSolidColorBrush? Fill { get; }
        public double StrokeThickness { get; }
        public bool FillToBaseline { get; }
        public double Minimum { get; }
        public double Maximum { get; }
    }
}
