using System;
using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.GroupingDemo.ViewModels;

namespace FastTreeDataGrid.GroupingDemo.Adapters;

internal sealed class RevenueBucketGroupAdapter : IFastTreeDataGridGroupAdapter
{
    private static readonly IReadOnlyList<(decimal Threshold, string Label)> Buckets = new[]
    {
        (100_000m, "<$100K"),
        (250_000m, "$100K - $249K"),
        (decimal.MaxValue, "$250K+"),
    };

    public object? GetGroupKey(FastTreeDataGridRow row)
    {
        if (row.Item is not SalesRecord record)
        {
            return "Unknown";
        }

        foreach (var bucket in Buckets)
        {
            if (record.Revenue < bucket.Threshold)
            {
                return bucket.Label;
            }
        }

        return Buckets[^1].Label;
    }

    public string GetGroupLabel(object? key, int level, int itemCount)
    {
        var label = key?.ToString() ?? "Unknown";
        return $"{label} ({itemCount:N0} items)";
    }

    public IComparer<object?>? Comparer => Comparer<object?>.Create(static (left, right) => string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)));
}
