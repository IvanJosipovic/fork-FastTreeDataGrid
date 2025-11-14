using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using Xunit;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridGroupingInteropTests
{
    [AvaloniaFact]
    public void DescriptorFactoryCopiesColumnMetadata()
    {
        var column = new FastTreeDataGridColumn
        {
            ValueKey = "Value",
            Header = "Revenue",
        };

        column.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = "Value",
            Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
            Aggregator = rows => rows.Sum(r => (int)r.ValueProvider!.GetValue(r.Item, "Value")!),
            Formatter = value => value?.ToString(),
            Label = "Total",
        });

        var descriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(column);

        Assert.Equal(column.ValueKey, descriptor.ColumnKey);
        Assert.NotNull(descriptor.Adapter);
        Assert.Equal(FastTreeDataGridSortDirection.Ascending, descriptor.SortDirection);
        Assert.True(descriptor.IsExpanded);

        Assert.Single(descriptor.AggregateDescriptors);
        var aggregate = descriptor.AggregateDescriptors[0];
        Assert.NotSame(column.AggregateDescriptors[0], aggregate);
        Assert.Equal(column.AggregateDescriptors[0].ColumnKey, aggregate.ColumnKey);
        Assert.Equal(column.AggregateDescriptors[0].Aggregator, aggregate.Aggregator);
        Assert.Equal(column.AggregateDescriptors[0].Formatter, aggregate.Formatter);
        Assert.Equal(column.AggregateDescriptors[0].Label, aggregate.Label);

        Assert.Equal("Revenue", descriptor.Properties["ColumnHeader"]);
        Assert.Same(column, descriptor.Properties["ColumnReference"]);
        Assert.Equal(column.ValueKey, descriptor.Properties["ColumnKey"]);
    }

    [AvaloniaFact]
    public async Task GroupRowsExposeValuesForWidgetFactories()
    {
        var items = new[]
        {
            new WidgetValueItem("North", 10),
            new WidgetValueItem("North", 20),
            new WidgetValueItem("South", 5),
        };

        var source = new FastTreeDataGridFlatSource<WidgetValueItem>(items, _ => Array.Empty<WidgetValueItem>());

        var column = new FastTreeDataGridColumn
        {
            ValueKey = "Region",
            Header = "Region",
        };

        var descriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(column);
        descriptor.KeySelector = row => ((WidgetValueItem)row.Item!).Region;

        var request = new FastTreeDataGridGroupingRequest
        {
            GroupDescriptors = new[] { descriptor },
        };

        await source.ApplyGroupingAsync(request, CancellationToken.None);
        await source.WaitForPendingOperationsAsync();

        var groupRow = source.GetRow(0);
        Assert.True(groupRow.IsGroup);

        var provider = Assert.IsAssignableFrom<IFastTreeDataGridValueProvider>(groupRow.Item);
        var metadata = Assert.IsAssignableFrom<IFastTreeDataGridGroupMetadata>(groupRow.Item);

        Assert.Equal("North (2)", provider.GetValue(groupRow.Item, "Region"));
        Assert.Equal(2, metadata.ItemCount);
    }

    [AvaloniaFact]
    public void DescriptorAggregateDescriptorsRemainCallable()
    {
        var revenueColumn = new FastTreeDataGridColumn
        {
            ValueKey = "Revenue",
            Header = "Revenue",
        };

        revenueColumn.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = "Revenue",
            Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
            Aggregator = rows => rows.Sum(r => (int)r.ValueProvider!.GetValue(r.Item, "Revenue")!),
        });

        var descriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(revenueColumn);
        Assert.Single(descriptor.AggregateDescriptors);

        var aggregate = descriptor.AggregateDescriptors[0];
        Assert.NotNull(aggregate.Aggregator);

        var rows = new[]
        {
            new FastTreeDataGridRow(new WidgetValueItem("North", 10), level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null),
            new FastTreeDataGridRow(new WidgetValueItem("South", 5), level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null),
            new FastTreeDataGridRow(new WidgetValueItem("South", 15), level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null),
        };

        var result = aggregate.Aggregator!(rows);
        Assert.Equal(30, Convert.ToInt32(result, CultureInfo.InvariantCulture));
    }

    [AvaloniaFact]
    public async Task GroupingRespectsActiveFilters()
    {
        var items = new[]
        {
            new WidgetValueItem("North", 10),
            new WidgetValueItem("North", 2),
            new WidgetValueItem("South", 5),
            new WidgetValueItem("South", 15),
        };

        var source = new FastTreeDataGridFlatSource<WidgetValueItem>(items, _ => Array.Empty<WidgetValueItem>());

        var regionColumn = new FastTreeDataGridColumn
        {
            ValueKey = "Region",
            Header = "Region",
        };

        var descriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(regionColumn);
        descriptor.KeySelector = row => ((WidgetValueItem)row.Item!).Region;

        var request = new FastTreeDataGridSortFilterRequest
        {
            GroupDescriptors = new[] { descriptor },
            FilterDescriptors = new[]
            {
                new FastTreeDataGridFilterDescriptor
                {
                    ColumnKey = "Revenue",
                    Predicate = row => ((WidgetValueItem)row.Item!).Revenue >= 5,
                },
            },
        };

        await source.ApplySortFilterAsync(request, CancellationToken.None);
        await source.WaitForPendingOperationsAsync();

        var groupCounts = new Dictionary<string, int>();
        var dataRowCount = 0;

        for (var i = 0; i < source.RowCount; i++)
        {
            var row = source.GetRow(i);
            if (row.IsGroup)
            {
                var provider = Assert.IsAssignableFrom<IFastTreeDataGridValueProvider>(row.Item);
                var header = provider.GetValue(row.Item, "FastTreeDataGrid.Group.Header")?.ToString() ?? string.Empty;
                var metadata = Assert.IsAssignableFrom<IFastTreeDataGridGroupMetadata>(row.Item);
                groupCounts[header] = metadata.ItemCount;
            }
            else if (!row.IsSummary)
            {
                dataRowCount++;
            }
        }

        Assert.Equal(3, dataRowCount); // 10, 5, 15
        Assert.Equal(2, groupCounts.Count);

        var northKey = groupCounts.Keys.First(k => k.StartsWith("North", StringComparison.Ordinal));
        var southKey = groupCounts.Keys.First(k => k.StartsWith("South", StringComparison.Ordinal));

        Assert.Equal(1, groupCounts[northKey]);
        Assert.Equal(2, groupCounts[southKey]);
    }

    private sealed class WidgetValueItem : IFastTreeDataGridValueProvider
    {
        public WidgetValueItem(string region, int revenue)
        {
            Region = region;
            Revenue = revenue;
        }

        public string Region { get; }

        public int Revenue { get; }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }

        public object? GetValue(object? item, string key)
        {
            return key switch
            {
                "Region" => Region,
                "Revenue" => Revenue,
                _ => null,
            };
        }
    }
}
