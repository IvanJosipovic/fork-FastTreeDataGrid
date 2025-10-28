using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Benchmarks;

[MemoryDiagnoser]
public class VirtualizationBenchmarks
{
    private FastTreeDataGridFlatSource<BenchmarkRow>? _flatSource;
    private FastTreeDataGridSourceVirtualizationProvider? _flatProvider;
    private FastTreeDataGridVirtualizationSettings _settings = null!;
    private Random _random = null!;

    [Params(10_000, 100_000)]
    public int RowCount { get; set; }

    [Params(128, 512)]
    public int PageSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _random = new Random(42);
        var data = BenchmarkDataGenerator.CreateFlatHierarchy(RowCount, _random);
        _flatSource = new FastTreeDataGridFlatSource<BenchmarkRow>(data, _ => Array.Empty<BenchmarkRow>());
        _flatProvider = new FastTreeDataGridSourceVirtualizationProvider(_flatSource);
        _settings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = PageSize,
            PrefetchRadius = 2,
            MaxPages = 32,
            MaxConcurrentLoads = 8,
        };
    }

    [Benchmark]
    public async Task SequentialPagingFlatSource()
    {
        if (_flatProvider is null)
        {
            throw new InvalidOperationException();
        }

        for (var start = 0; start < RowCount; start += PageSize)
        {
            var request = new FastTreeDataGridPageRequest(start, Math.Min(PageSize, RowCount - start));
            await _flatProvider.GetPageAsync(request, CancellationToken.None).ConfigureAwait(false);
        }
    }

    [Benchmark]
    public async Task RandomAccessPagingFlatSource()
    {
        if (_flatProvider is null)
        {
            throw new InvalidOperationException();
        }

        for (var i = 0; i < 200; i++)
        {
            var start = _random.Next(0, Math.Max(1, RowCount - PageSize));
            var request = new FastTreeDataGridPageRequest(start, PageSize);
            await _flatProvider.GetPageAsync(request, CancellationToken.None).ConfigureAwait(false);
        }
    }
}

public static class BenchmarkDataGenerator
{
    public static BenchmarkRow[] CreateFlatHierarchy(int count, Random random)
    {
        var data = new BenchmarkRow[count];
        for (var i = 0; i < count; i++)
        {
            data[i] = new BenchmarkRow($"Row {i}", random.Next());
        }

        return data;
    }
}

public sealed class BenchmarkRow : IFastTreeDataGridValueProvider
{
    public const string ValueKey = "Name";

    public BenchmarkRow(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public int Value { get; }

    public void Dispose()
    {
    }

    public object? GetValue(object? row, string key) => key == ValueKey ? Name : null;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }
}
