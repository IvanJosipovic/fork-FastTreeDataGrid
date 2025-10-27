using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridGroupingTests
{
    [Fact]
    public async Task FlatSourceProducesGroupAndSummaryRows()
    {
        var items = new[]
        {
            new TestItem("A", 1),
            new TestItem("A", 2),
            new TestItem("B", 3),
        };

        var source = new FastTreeDataGridFlatSource<TestItem>(items, _ => Array.Empty<TestItem>());

        var request = new FastTreeDataGridSortFilterRequest
        {
            GroupDescriptors = new[]
            {
                new FastTreeDataGridGroupDescriptor
                {
                    KeySelector = row => ((TestItem)row.Item!).GroupKey,
                },
            },
            AggregateDescriptors = new[]
            {
                new FastTreeDataGridAggregateDescriptor
                {
                    ColumnKey = "Value",
                    Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
                    Aggregator = rows => rows.Sum(r => ((TestItem)r.Item!).Value),
                    Formatter = value => value switch
                    {
                        long l => l.ToString("N0", CultureInfo.InvariantCulture),
                        int i => i.ToString("N0", CultureInfo.InvariantCulture),
                        _ => value?.ToString(),
                    },
                },
            },
        };

        await source.ApplySortFilterAsync(request, CancellationToken.None);
        SpinWait.SpinUntil(() => source.RowCount == 7, TimeSpan.FromSeconds(1));

        Assert.Equal(7, source.RowCount);

        var groupRowA = source.GetRow(0);
        Assert.True(groupRowA.IsGroup);

        var summaryRowA = source.GetRow(3);
        Assert.True(summaryRowA.IsSummary);
        Assert.Equal("3", GetSummaryValue(summaryRowA, "Value"));

        var groupRowB = source.GetRow(4);
        Assert.True(groupRowB.IsGroup);

        var summaryRowB = source.GetRow(6);
        Assert.True(summaryRowB.IsSummary);
        Assert.Equal("3", GetSummaryValue(summaryRowB, "Value"));
    }

    private static string? GetSummaryValue(FastTreeDataGridRow row, string key)
    {
        if (row.Item is IFastTreeDataGridValueProvider provider)
        {
            var value = provider.GetValue(row.Item, key);
            return value?.ToString();
        }

        return null;
    }

    private sealed class TestItem
    {
        public TestItem(string groupKey, int value)
        {
            GroupKey = groupKey;
            Value = value;
        }

        public string GroupKey { get; }

        public int Value { get; }
    }
}
