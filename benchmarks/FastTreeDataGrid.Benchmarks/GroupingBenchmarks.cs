using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Benchmarks;

[MemoryDiagnoser]
public class GroupingBenchmarks
{
    private FastTreeDataGridFlatSource<GroupingRecord>? _source;
    private List<GroupingRecord> _records = null!;
    private Random _random = null!;

    [Params(10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _random = new Random(123);
        _records = GroupingRecord.Generate(RowCount, _random);
        _source = new FastTreeDataGridFlatSource<GroupingRecord>(_records, _ => Array.Empty<GroupingRecord>());
    }

    [Benchmark]
    public void ApplyRegionCategoryGrouping()
    {
        if (_source is null)
        {
            throw new InvalidOperationException();
        }

        var request = BuildGroupingRequest(GroupingRecord.RegionKey, GroupingRecord.CategoryKey);
        _source.ApplyGroupingAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        _source.WaitForPendingOperationsAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public void ApplyRegionCategoryProductGrouping()
    {
        if (_source is null)
        {
            throw new InvalidOperationException();
        }

        var request = BuildGroupingRequest(GroupingRecord.RegionKey, GroupingRecord.CategoryKey, GroupingRecord.ProductKey);
        _source.ApplyGroupingAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        _source.WaitForPendingOperationsAsync().GetAwaiter().GetResult();
    }

    private static FastTreeDataGridGroupingRequest BuildGroupingRequest(params string[] keys)
    {
        var descriptors = keys.Select(key => new FastTreeDataGridGroupDescriptor
        {
            ColumnKey = key,
            Adapter = new FastTreeDataGridValueGroupAdapter(key),
            SortDirection = FastTreeDataGridSortDirection.Ascending,
            Comparer = null,
            IsExpanded = true,
        }).ToArray();

        var aggregates = new[]
        {
            GroupingRecord.CreateRevenueAggregate(),
        };

        return new FastTreeDataGridGroupingRequest
        {
            SortDescriptors = Array.Empty<FastTreeDataGridSortDescriptor>(),
            FilterDescriptors = Array.Empty<FastTreeDataGridFilterDescriptor>(),
            GroupDescriptors = descriptors,
            AggregateDescriptors = aggregates,
        };
    }
}

public sealed class GroupingRecord : IFastTreeDataGridValueProvider
{
    public const string RegionKey = nameof(Region);
    public const string CategoryKey = nameof(Category);
    public const string ProductKey = nameof(Product);
    public const string QuarterKey = nameof(Quarter);
    public const string RevenueKey = nameof(Revenue);

    public GroupingRecord(string region, string category, string product, string quarter, decimal revenue)
    {
        Region = region;
        Category = category;
        Product = product;
        Quarter = quarter;
        Revenue = revenue;
    }

    public string Region { get; }
    public string Category { get; }
    public string Product { get; }
    public string Quarter { get; }
    public decimal Revenue { get; }

    public static List<GroupingRecord> Generate(int count, Random random)
    {
        var regions = new[] { "North America", "Europe", "Asia-Pacific", "South America" };
        var categories = new[] { "Electronics", "Home", "Apparel", "Sports" };
        var products = new[] { "A1", "B2", "C3", "D4", "E5" };
        var quarters = new[] { "Q1", "Q2", "Q3", "Q4" };

        var records = new List<GroupingRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var revenue = Math.Round((decimal)random.NextDouble() * 10_000m + 500m, 2);
            records.Add(new GroupingRecord(
                regions[random.Next(regions.Length)],
                categories[random.Next(categories.Length)],
                products[random.Next(products.Length)],
                quarters[random.Next(quarters.Length)],
                revenue));
        }

        return records;
    }

    public static FastTreeDataGridAggregateDescriptor CreateRevenueAggregate()
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = RevenueKey,
            Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
            Aggregator = rows =>
            {
                decimal sum = 0;
                foreach (var row in rows)
                {
                    if (row.Item is GroupingRecord record)
                    {
                        sum += record.Revenue;
                    }
                }

                return sum;
            },
            Formatter = value => value is decimal dec
                ? dec.ToString("C0", CultureInfo.InvariantCulture)
                : value?.ToString(),
        };
    }

    public object? GetValue(object? item, string key) =>
        key switch
        {
            RegionKey => Region,
            CategoryKey => Category,
            ProductKey => Product,
            QuarterKey => Quarter,
            RevenueKey => Revenue,
            _ => null,
        };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }
}
