using System;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Helper responsible for producing grouping descriptors from column metadata.
/// </summary>
internal static class FastTreeDataGridGroupingDescriptorFactory
{
    public static FastTreeDataGridGroupDescriptor CreateFromColumn(FastTreeDataGridColumn column)
    {
        if (column is null)
        {
            throw new ArgumentNullException(nameof(column));
        }

        var adapter = column.GroupAdapter ?? new FastTreeDataGridValueGroupAdapter(column.ValueKey, column.GroupAdapter?.Comparer);

        var descriptor = new FastTreeDataGridGroupDescriptor
        {
            ColumnKey = column.ValueKey,
            Adapter = adapter,
            SortDirection = FastTreeDataGridSortDirection.Ascending,
            Comparer = adapter.Comparer,
            IsExpanded = true,
        };

        foreach (var aggregate in column.AggregateDescriptors)
        {
            descriptor.AggregateDescriptors.Add(CloneAggregateDescriptor(aggregate));
        }

        descriptor.Properties["ColumnHeader"] = column.Header?.ToString();
        descriptor.Properties["ColumnReference"] = column;
        descriptor.Properties["ColumnKey"] = column.ValueKey ?? string.Empty;

        return descriptor;
    }

    private static FastTreeDataGridAggregateDescriptor CloneAggregateDescriptor(FastTreeDataGridAggregateDescriptor source)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = source.ColumnKey,
            Placement = source.Placement,
            Aggregator = source.Aggregator,
            Formatter = source.Formatter,
            Label = source.Label,
        };
    }
}
