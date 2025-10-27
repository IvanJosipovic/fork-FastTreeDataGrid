using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Infrastructure;

#pragma warning disable CS0067

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Virtualization;

public sealed class RandomVirtualizationSource : IFastTreeDataGridSource
{
    private readonly int _rowCount;

    public RandomVirtualizationSource(int rowCount)
    {
        _rowCount = rowCount;
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount => _rowCount;
    public bool SupportsPlaceholders => false;

    public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(_rowCount);

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        var rows = new List<FastTreeDataGridRow>(request.Count);
        for (var i = 0; i < request.Count; i++)
        {
            var index = request.StartIndex + i;
            var row = CreateRow(index);
            rows.Add(row);
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));
        }

        return new ValueTask<FastTreeDataGridPageResult>(
            new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null));
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        row = CreateRow(index);
        return true;
    }

    public bool IsPlaceholder(int index) => false;

    public FastTreeDataGridRow GetRow(int index) => CreateRow(index);

    public void ToggleExpansion(int index)
    {
        _ = index;
    }

    private static FastTreeDataGridRow CreateRow(int index)
    {
        return new FastTreeDataGridRow(new RandomRow(index), level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
    }

    private sealed class RandomRow : IFastTreeDataGridValueProvider
    {
        private readonly int _index;

        public RandomRow(int index)
        {
            _index = index;
        }

        public void Dispose()
        {
        }

        public object? GetValue(object? row, string key)
        {
            return key switch
            {
                RandomVirtualizationColumns.KeyIndex => _index,
                RandomVirtualizationColumns.KeyValue => ComputeValue(_index),
                RandomVirtualizationColumns.KeyHash => ComputeHash(_index),
                _ => null,
            };
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }

        private static int ComputeValue(int index)
        {
            unchecked
            {
                return (int)((index * 2654435761L) & 0x7FFFFFFF);
            }
        }

        private static string ComputeHash(int index)
        {
            var value = ComputeValue(index);
            return string.Create(8, value, static (span, state) =>
            {
                var alphabet = "0123456789ABCDEF";
                for (var i = 0; i < span.Length; i++)
                {
                    var nibble = (state >> ((span.Length - 1 - i) * 4)) & 0xF;
                    span[i] = alphabet[nibble];
                }
            });
        }
    }
}

public static class RandomVirtualizationColumns
{
    public const string KeyIndex = "Index";
    public const string KeyValue = "Value";
    public const string KeyHash = "Hash";
}
