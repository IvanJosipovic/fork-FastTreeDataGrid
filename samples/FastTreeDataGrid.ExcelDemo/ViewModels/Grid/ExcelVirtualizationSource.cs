using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.ExcelDemo.Models;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ExcelDemo.ViewModels.Grid;

internal sealed class ExcelVirtualizationSource : IFastTreeDataGridSource
{
    private readonly PivotResult _result;
    private readonly IReadOnlyList<ExcelColumnDescriptor> _columns;
    private readonly Dictionary<string, ExcelColumnDescriptor> _columnLookup;

    public ExcelVirtualizationSource(PivotResult result, IReadOnlyList<ExcelColumnDescriptor> columns)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _columnLookup = new Dictionary<string, ExcelColumnDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in _columns)
        {
            _columnLookup[column.ValueKey] = column;
        }
    }

    public event EventHandler? ResetRequested;

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;

    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount => _result.RowCount;

    public bool SupportsPlaceholders => false;

    public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(RowCount);
    }

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<FastTreeDataGridRow>(request.Count);
        for (var i = 0; i < request.Count; i++)
        {
            var index = request.StartIndex + i;
            if (index >= RowCount)
            {
                break;
            }

            var provider = new ExcelPivotRowValueProvider(_result, index, _columnLookup);
            var row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
            rows.Add(row);
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));
        }

        return new ValueTask<FastTreeDataGridPageResult>(new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null));
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        if (index < 0 || index >= RowCount)
        {
            row = default!;
            return false;
        }

        var provider = new ExcelPivotRowValueProvider(_result, index, _columnLookup);
        row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
        return true;
    }

    public bool IsPlaceholder(int index) => false;

    public FastTreeDataGridRow GetRow(int index)
    {
        if (index < 0 || index >= RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var provider = new ExcelPivotRowValueProvider(_result, index, _columnLookup);
        return new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
    }

    public void ToggleExpansion(int index)
    {
        _ = index;
    }

    public void RequestReset() => ResetRequested?.Invoke(this, EventArgs.Empty);
}
