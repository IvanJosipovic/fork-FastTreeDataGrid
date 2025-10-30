using System;
using System.Diagnostics.CodeAnalysis;

namespace FastTreeDataGrid.DualVirtualizationDemo.ViewModels;

public static class MatrixVirtualizationColumns
{
    public const string KeyRowLabel = "RowLabel";
    public const string KeyAverage = "Average";

    private const string MetricPrefix = "Metric.";

    public static string GetMetricKey(int index) => $"{MetricPrefix}{index:D4}";

    public static bool TryGetMetricIndex(string? key, out int index)
    {
        index = 0;
        if (key is null)
        {
            return false;
        }

        if (!key.StartsWith(MetricPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var span = key.AsSpan(MetricPrefix.Length);
        return int.TryParse(span, out index);
    }
}
