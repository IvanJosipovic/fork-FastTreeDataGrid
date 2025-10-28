using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Infrastructure;
using Xunit;

namespace FastTreeDataGrid.Engine.Tests;

public class FastTreeDataGridFlatSourceTests
{
    [Fact]
    public void FlatteningProducesExpectedOrderWhenExpanded()
    {
        var root = new TreeItem("A", new TreeItem("A1"), new TreeItem("A2"));
        var root2 = new TreeItem("B");

        var source = new FastTreeDataGridFlatSource<TreeItem>(
            new[] { root, root2 },
            item => item.Children,
            keySelector: item => item.Id);

        source.ExpandAllGroups();

        Assert.Equal(4, source.RowCount);

        var ids = Enumerable.Range(0, source.RowCount)
            .Select(index => (TreeItem)source.GetRow(index).Item!)
            .Select(item => item.Id)
            .ToArray();

        Assert.Equal(new[] { "A", "A1", "A2", "B" }, ids);

        source.ToggleExpansion(0);
        Assert.Equal(2, source.RowCount);
    }

    [Fact]
    public async Task GroupingAddsGroupAndSummaryRows()
    {
        var items = new[]
        {
            new FlatItem("A", "East", 10),
            new FlatItem("B", "West", 20),
            new FlatItem("C", "East", 30),
        };

        var source = new FastTreeDataGridFlatSource<FlatItem>(
            items,
            _ => Array.Empty<FlatItem>(),
            keySelector: item => item.Id);

        var groupDescriptor = new FastTreeDataGridGroupDescriptor
        {
            KeySelector = row => ((FlatItem)row.Item!).Region,
            IsExpanded = true,
        };
        groupDescriptor.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = "Value",
            Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
            Aggregator = rows => rows.Sum(r => ((FlatItem)r.Item!).Value),
        });

        var gridAggregate = new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = "Value",
            Placement = FastTreeDataGridAggregatePlacement.GridFooter,
            Aggregator = rows => rows.Sum(r => ((FlatItem)r.Item!).Value),
        };

        var request = new FastTreeDataGridGroupingRequest
        {
            GroupDescriptors = new[] { groupDescriptor },
            AggregateDescriptors = new[] { gridAggregate },
        };

        await source.ApplyGroupingAsync(request, CancellationToken.None);
        await source.WaitForPendingOperationsAsync();

        var rows = Enumerable.Range(0, source.RowCount)
            .Select(source.GetRow)
            .ToList();

        var groupRows = rows.Where(row => row.Item is IFastTreeDataGridGroupMetadata).ToList();
        Assert.Equal(2, groupRows.Count);
        Assert.Contains(groupRows, row => GetGroupKey(row)?.ToString() == "East");
        Assert.Contains(groupRows, row => GetGroupKey(row)?.ToString() == "West");

        var groupSummaries = rows.Where(row => row.IsSummary && row.Level == 1).ToList();
        Assert.Equal(2, groupSummaries.Count);
        Assert.Contains(groupSummaries, row => Math.Abs(ParseSummary(row) - 40) < double.Epsilon);
        Assert.Contains(groupSummaries, row => Math.Abs(ParseSummary(row) - 20) < double.Epsilon);

        var gridFooter = rows.Last();
        Assert.True(gridFooter.IsSummary);
        Assert.Equal(0, gridFooter.Level);
        Assert.True(Math.Abs(ParseSummary(gridFooter) - 60) < double.Epsilon);
    }

    private static double ParseSummary(FastTreeDataGridRow row)
    {
        var text = row.ValueProvider?.GetValue(row.Item, "Value")?.ToString() ?? string.Empty;
        return double.Parse(text, CultureInfo.CurrentCulture);
    }

    private static object? GetGroupKey(FastTreeDataGridRow row) =>
        row.Item is null
            ? null
            : row.Item.GetType().GetProperty("Key")?.GetValue(row.Item);

    private sealed class TreeItem
    {
        public TreeItem(string id, params TreeItem[] children)
        {
            Id = id;
            Children.AddRange(children);
        }

        public string Id { get; }

        public List<TreeItem> Children { get; } = new();
    }

    private sealed class FlatItem
    {
        public FlatItem(string id, string region, double value)
        {
            Id = id;
            Region = region;
            Value = value;
        }

        public string Id { get; }

        public string Region { get; }

        public double Value { get; }
    }
}
