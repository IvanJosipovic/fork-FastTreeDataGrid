using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Charts;

internal static class ChartSamplesFactory
{
    public static IReadOnlyList<ChartSampleNode> Create()
    {
        return new List<ChartSampleNode>
        {
            new ChartSampleNode(
                "Daily Price Trend",
                "Single series with area fill and baseline anchored at zero.",
                CreatePriceTrend()),
            new ChartSampleNode(
                "Sentiment Oscillation",
                "Clamped range and baseline highlighting neutral zone.",
                CreateOscillation()),
            new ChartSampleNode(
                "Comparison",
                "Multiple series comparing close price and moving average.",
                CreateMultiSeries()),
            new ChartSampleNode(
                "Sparkline",
                "Minimal sparkline without baseline using raw numeric feed.",
                CreateSparkline()),
        };
    }

    private static ChartWidgetValue CreatePriceTrend()
    {
        var data = GenerateSeries(40, i => Math.Sin(i * 0.18) * 1.4 + i * 0.08 + 2);
        var stroke = new ImmutableSolidColorBrush(Color.FromRgb(0x2B, 0x9B, 0x85));
        var fill = new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0x2B, 0x9B, 0x85));

        return new ChartWidgetValue(
            new[]
            {
                new ChartSeriesValue(
                    data,
                    Stroke: stroke,
                    Fill: fill,
                    StrokeThickness: 2,
                    FillToBaseline: true),
            },
            Baseline: 0,
            ShowBaseline: true,
            BaselineBrush: new ImmutableSolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),
            BaselineThickness: 1);
    }

    private static ChartWidgetValue CreateOscillation()
    {
        var data = GenerateSeries(64, i => Math.Sin(i * 0.24) * 0.6);
        var stroke = new ImmutableSolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0xD4));
        var fill = new ImmutableSolidColorBrush(Color.FromArgb(0x26, 0x1F, 0x7A, 0xD4));

        return new ChartWidgetValue(
            new[]
            {
                new ChartSeriesValue(
                    data,
                    Stroke: stroke,
                    Fill: fill,
                    StrokeThickness: 1.75,
                    FillToBaseline: true),
            },
            Minimum: -1,
            Maximum: 1,
            Baseline: 0,
            ShowBaseline: true,
            BaselineBrush: new ImmutableSolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
            BaselineThickness: 1);
    }

    private static ChartWidgetValue CreateMultiSeries()
    {
        var baseSeries = GenerateSeries(48, i => Math.Sin(i * 0.18) * 0.8 + i * 0.02);
        var movingAverage = CalculateMovingAverage(baseSeries, 5);

        return new ChartWidgetValue(
            new[]
            {
                new ChartSeriesValue(
                    baseSeries,
                    Stroke: new ImmutableSolidColorBrush(Color.FromRgb(0xD9, 0x47, 0x47)),
                    Fill: new ImmutableSolidColorBrush(Color.FromArgb(0x26, 0xD9, 0x47, 0x47)),
                    StrokeThickness: 1.8,
                    FillToBaseline: false),
                new ChartSeriesValue(
                    movingAverage,
                    Stroke: new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x1F)),
                    Fill: null,
                    StrokeThickness: 2.2,
                    FillToBaseline: false),
            },
            Baseline: 0,
            ShowBaseline: false);
    }

    private static ChartWidgetValue CreateSparkline()
    {
        var random = new Random(17);
        var data = new double[30];
        var value = 0.0;

        for (var i = 0; i < data.Length; i++)
        {
            value += (random.NextDouble() - 0.45) * 0.4;
            data[i] = value;
        }

        return new ChartWidgetValue(
            new[]
            {
                new ChartSeriesValue(
                    data,
                    Stroke: new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    Fill: null,
                    StrokeThickness: 1.2,
                    FillToBaseline: false),
            },
            ShowBaseline: false);
    }

    private static double[] GenerateSeries(int count, Func<int, double> selector)
    {
        return Enumerable.Range(0, count)
            .Select(selector)
            .ToArray();
    }

    private static double[] CalculateMovingAverage(IReadOnlyList<double> source, int window)
    {
        if (window <= 1 || source.Count == 0)
        {
            return source.ToArray();
        }

        var result = new double[source.Count];
        var queue = new Queue<double>(window);
        double sum = 0;

        for (var i = 0; i < source.Count; i++)
        {
            sum += source[i];
            queue.Enqueue(source[i]);

            if (queue.Count > window)
            {
                sum -= queue.Dequeue();
            }

            result[i] = sum / queue.Count;
        }

        return result;
    }
}
