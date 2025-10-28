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

    [Fact]
    public async Task DescriptorLevelAggregatesAreCalculated()
    {
        var items = new[]
        {
            new TestItem("A", 1),
            new TestItem("A", 2),
            new TestItem("B", 3),
        };

        var source = new FastTreeDataGridFlatSource<TestItem>(items, _ => Array.Empty<TestItem>());

        var descriptor = new FastTreeDataGridGroupDescriptor
        {
            KeySelector = row => ((TestItem)row.Item!).GroupKey,
        };

        descriptor.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = "Value",
            Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
            Aggregator = rows => rows.Count(),
        });

        var request = new FastTreeDataGridSortFilterRequest
        {
            GroupDescriptors = new[] { descriptor },
        };

        await source.ApplySortFilterAsync(request, CancellationToken.None);
        SpinWait.SpinUntil(() => source.RowCount == 7, TimeSpan.FromSeconds(1));

        var summaryRow = source.GetRow(3);
        Assert.True(summaryRow.IsSummary);
        Assert.Equal("2", GetSummaryValue(summaryRow, "Value"));
    }

    [Fact]
    public async Task AggregatesAreCachedAcrossGroupToggle()
    {
        var items = new[]
        {
            new TestItem("North", 10),
            new TestItem("North", 20),
            new TestItem("South", 5),
            new TestItem("South", 15),
        };

        var source = new FastTreeDataGridFlatSource<TestItem>(items, _ => Array.Empty<TestItem>());

        var aggregateCallCount = 0;
        var request = new FastTreeDataGridGroupingRequest
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
                    Aggregator = rows =>
                    {
                        aggregateCallCount++;
                        return rows.Sum(r => ((TestItem)r.Item!).Value);
                    },
                },
            },
        };

        await source.ApplyGroupingAsync(request, CancellationToken.None);
        await source.WaitForPendingOperationsAsync();

        Assert.Equal(2, aggregateCallCount); // one call per group

        var firstGroupIndex = FindFirstGroupRowIndex(source);
        Assert.InRange(firstGroupIndex, 0, source.RowCount - 1);

        source.ToggleExpansion(firstGroupIndex); // collapse
        source.ToggleExpansion(firstGroupIndex); // expand again (should reuse cached aggregates)

        Assert.Equal(2, aggregateCallCount);
    }

    [Fact]
    public async Task AggregateProviderResultsAreProjectedIntoSummaryRows()
    {
        var items = new[]
        {
            new TestItem("North", 10),
            new TestItem("North", 20),
            new TestItem("North", 15),
        };

        var source = new FastTreeDataGridFlatSource<TestItem>(items, _ => Array.Empty<TestItem>());

        var provider = new CountingAggregateProvider();
        var descriptor = new FastTreeDataGridGroupDescriptor
        {
            KeySelector = row => ((TestItem)row.Item!).GroupKey,
        };

        descriptor.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
        {
            Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
            Provider = provider,
        });

        var request = new FastTreeDataGridGroupingRequest
        {
            GroupDescriptors = new[] { descriptor },
        };

        await source.ApplyGroupingAsync(request, CancellationToken.None);
        await source.WaitForPendingOperationsAsync();

        Assert.Equal(1, provider.InvocationCount);

        var summaryRowIndex = Enumerable.Range(0, source.RowCount)
            .Select(i => new { Index = i, Row = source.GetRow(i) })
            .First(entry => entry.Row.IsSummary)
            .Index;

        var summaryRow = source.GetRow(summaryRowIndex);
        var valueProvider = Assert.IsAssignableFrom<IFastTreeDataGridValueProvider>(summaryRow.Item);
        Assert.Equal("45 units", valueProvider.GetValue(summaryRow.Item, string.Empty));
    }

    private static int FindFirstGroupRowIndex(FastTreeDataGridFlatSource<TestItem> source)
    {
        for (var i = 0; i < source.RowCount; i++)
        {
            var row = source.GetRow(i);
            if (row.IsGroup)
            {
                return i;
            }
        }

        return -1;
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

    private sealed class CountingAggregateProvider : IFastTreeDataGridAggregateProvider
    {
        public int InvocationCount { get; private set; }

        public FastTreeDataGridAggregateResult Calculate(FastTreeDataGridGroupContext context)
        {
            InvocationCount++;
            var total = context.Rows.Sum(r => ((TestItem)r.Item!).Value);
            return new FastTreeDataGridAggregateResult(null, total, $"{total} units");
        }
    }

    [Fact]
    public async Task DescriptorDefaultExpansionHonored()
    {
        var items = new[]
        {
            new TestItem("North", 10),
            new TestItem("North", 5),
            new TestItem("South", 3),
        };

        var source = new FastTreeDataGridFlatSource<TestItem>(items, _ => Array.Empty<TestItem>());

        var descriptor = new FastTreeDataGridGroupDescriptor
        {
            KeySelector = row => ((TestItem)row.Item!).GroupKey,
            IsExpanded = false,
        };

        var request = new FastTreeDataGridGroupingRequest
        {
            GroupDescriptors = new[] { descriptor },
        };

        await source.ApplyGroupingAsync(request, CancellationToken.None);
        await source.WaitForPendingOperationsAsync();

        var groupRow = source.GetRow(0);
        Assert.True(groupRow.IsGroup);
        Assert.False(groupRow.IsExpanded);
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
