using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.DualVirtualizationDemo.ViewModels;

public sealed class MatrixVirtualizationSource : IFastTreeDataGridSource
{
    private readonly int _metricCount;
    private readonly TimeSpan _fetchDelay;
    private readonly ConcurrentDictionary<int, FastTreeDataGridRow> _rows = new();
    private readonly ConcurrentDictionary<int, Task> _inFlight = new();

    public MatrixVirtualizationSource(int rowCount, int metricCount, TimeSpan fetchDelay)
    {
        if (rowCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        if (metricCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metricCount));
        }

        RowCount = rowCount;
        _metricCount = metricCount;
        _fetchDelay = fetchDelay < TimeSpan.Zero ? TimeSpan.Zero : fetchDelay;
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount { get; }

    public int MetricCount => _metricCount;

    public bool SupportsPlaceholders => true;

    public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(RowCount);

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request.Count <= 0 || request.StartIndex >= RowCount)
        {
            return ValueTask.FromResult(FastTreeDataGridPageResult.Empty);
        }

        var endExclusive = Math.Min(request.StartIndex + request.Count, RowCount);
        var rows = new List<FastTreeDataGridRow>(endExclusive - request.StartIndex);
        var placeholderIndices = new List<int>();
        var pending = new List<Task>();

        for (var index = request.StartIndex; index < endExclusive; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_rows.TryGetValue(index, out var cached))
            {
                rows.Add(cached);
                continue;
            }

            rows.Add(CreatePlaceholderRow(index));
            placeholderIndices.Add(index - request.StartIndex);
            pending.Add(EnsureRowAsync(index));
        }

        Task? completion = null;
        if (pending.Count > 0)
        {
            completion = Task.WhenAll(pending);
        }

        var result = new FastTreeDataGridPageResult(rows, placeholderIndices, completion, cancellation: null);
        return ValueTask.FromResult(result);
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request.Count <= 0 || request.StartIndex >= RowCount)
        {
            return ValueTask.CompletedTask;
        }

        var endExclusive = Math.Min(request.StartIndex + request.Count, RowCount);
        for (var index = request.StartIndex; index < endExclusive; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = EnsureRowAsync(index);
        }

        return ValueTask.CompletedTask;
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        if (request.Kind != FastTreeDataGridInvalidationKind.Full)
        {
            return Task.CompletedTask;
        }

        _rows.Clear();
        _inFlight.Clear();
        ResetRequested?.Invoke(this, EventArgs.Empty);
        Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        if (_rows.TryGetValue(index, out row!))
        {
            return true;
        }

        row = CreatePlaceholderRow(index);
        return false;
    }

    public bool IsPlaceholder(int index) => !_rows.ContainsKey(index);

    public FastTreeDataGridRow GetRow(int index) =>
        _rows.TryGetValue(index, out var row) ? row : CreatePlaceholderRow(index);

    public void ToggleExpansion(int index)
    {
        _ = index;
    }

    private Task EnsureRowAsync(int index)
    {
        return _inFlight.GetOrAdd(index, static (idx, state) => ((MatrixVirtualizationSource)state).FetchRowAsync(idx), this);
    }

    private async Task FetchRowAsync(int index)
    {
        try
        {
            if (_fetchDelay > TimeSpan.Zero)
            {
                await Task.Delay(_fetchDelay).ConfigureAwait(false);
            }

            var descriptor = CreateDescriptor(index);
            var row = new FastTreeDataGridRow(
                new MatrixRowValueProvider(descriptor),
                level: 0,
                hasChildren: false,
                isExpanded: false,
                requestMeasureCallback: null);

            _rows[index] = row;
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));
        }
        finally
        {
            _inFlight.TryRemove(index, out _);
        }
    }

    private static FastTreeDataGridRow CreatePlaceholderRow(int index) =>
        new(new MatrixPlaceholderValueProvider(index), level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);

    private MatrixRowDescriptor CreateDescriptor(int index)
    {
        var values = new double[_metricCount];
        for (var metricIndex = 0; metricIndex < values.Length; metricIndex++)
        {
            values[metricIndex] = ComputeValue(index, metricIndex);
        }

        var average = values.Average();
        var label = "Row " + (index + 1).ToString("N0", CultureInfo.InvariantCulture);
        return new MatrixRowDescriptor(index, label, values, average);
    }

    private static double ComputeValue(int rowIndex, int metricIndex)
    {
        unchecked
        {
            var seed = (uint)(rowIndex * 1103515245 + metricIndex * 12345 + 0x9E3779B9);
            seed ^= seed >> 17;
            seed *= 2246822519u;
            seed ^= seed >> 15;
            var noise = seed / (double)uint.MaxValue;
            var trend = Math.Sqrt(rowIndex + 1) * 0.85 + metricIndex * 1.2;
            var oscillation = Math.Sin(rowIndex * 0.018 + metricIndex * 0.11) * 42;
            var value = trend + oscillation + noise * 65;
            return Math.Round(value, 2);
        }
    }

    private sealed class MatrixRowDescriptor
    {
        public MatrixRowDescriptor(int index, string label, double[] values, double average)
        {
            Index = index;
            Label = label;
            Values = values;
            Average = average;
        }

        public int Index { get; }

        public string Label { get; }

        public double[] Values { get; }

        public double Average { get; }
    }

    private sealed class MatrixRowValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly MatrixRowDescriptor _descriptor;

        public MatrixRowValueProvider(MatrixRowDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
        }

        public object? GetValue(object? row, string key)
        {
            if (key == MatrixVirtualizationColumns.KeyRowLabel)
            {
                return _descriptor.Label;
            }

            if (key == MatrixVirtualizationColumns.KeyAverage)
            {
                return Math.Round(_descriptor.Average, 2);
            }

            if (MatrixVirtualizationColumns.TryGetMetricIndex(key, out var metricIndex) &&
                metricIndex >= 0 &&
                metricIndex < _descriptor.Values.Length)
            {
                return _descriptor.Values[metricIndex];
            }

            return null;
        }
    }

    private sealed class MatrixPlaceholderValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly int _index;

        public MatrixPlaceholderValueProvider(int index)
        {
            _index = index;
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
        }

        public object? GetValue(object? row, string key)
        {
            if (key == MatrixVirtualizationColumns.KeyRowLabel)
            {
                return "Row " + (_index + 1).ToString("N0", CultureInfo.InvariantCulture);
            }

            return null;
        }
    }
}
