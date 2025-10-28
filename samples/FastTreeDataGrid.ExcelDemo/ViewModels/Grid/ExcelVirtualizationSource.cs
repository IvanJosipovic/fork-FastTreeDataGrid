using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.ExcelDemo.Models;
using FastTreeDataGrid.ExcelDemo.Services;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ExcelDemo.ViewModels.Grid;

internal sealed class ExcelVirtualizationSource : IFastTreeDataGridSource
{
    private readonly PivotResult _result;
    private readonly IReadOnlyList<ExcelColumnDescriptor> _columns;
    private readonly Dictionary<string, ExcelColumnDescriptor> _columnLookup;
    private readonly PowerFxFormulaEvaluator _formulaEvaluator;
    private CancellationTokenSource _processingCts = new();
    private readonly ConcurrentDictionary<(int Row, int Column), Task> _cellFormulaTasks = new();
    private readonly ConcurrentDictionary<int, Task> _rowFormulaTasks = new();
    private readonly ConcurrentDictionary<int, Task> _columnFormulaTasks = new();
    private Task? _grandTotalsTask;
    private readonly List<ExcelColumnDescriptor> _formulaCellDescriptors;
    private readonly List<ExcelColumnDescriptor> _formulaRowTotalDescriptors;
    private readonly List<ExcelColumnDescriptor> _formulaColumnTotalDescriptors;
    private readonly bool _hasFormulas;

    public ExcelVirtualizationSource(PivotResult result, IReadOnlyList<ExcelColumnDescriptor> columns, PowerFxFormulaEvaluator formulaEvaluator)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _formulaEvaluator = formulaEvaluator ?? throw new ArgumentNullException(nameof(formulaEvaluator));
        _columnLookup = new Dictionary<string, ExcelColumnDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in _columns)
        {
            _columnLookup[column.ValueKey] = column;
        }

        _hasFormulas = _result.FormulaCount > 0;
        _formulaCellDescriptors = _columns
            .Where(c => c.Role == ExcelColumnRole.FormulaCell && c.ColumnIndex.HasValue && c.FormulaIndex.HasValue)
            .ToList();
        _formulaRowTotalDescriptors = _columns
            .Where(c => c.Role == ExcelColumnRole.FormulaRowTotal && c.FormulaIndex.HasValue)
            .ToList();
        _formulaColumnTotalDescriptors = _columns
            .Where(c => c.Role == ExcelColumnRole.FormulaColumnTotal && c.ColumnIndex.HasValue && c.FormulaIndex.HasValue)
            .ToList();
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
        var end = Math.Min(request.StartIndex + request.Count, RowCount);
        for (var index = request.StartIndex; index < end; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var provider = new ExcelPivotRowValueProvider(_result, index, _columnLookup);
            var row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
            rows.Add(row);
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));

            ScheduleFormulaWork(index);
        }

        if (_hasFormulas)
        {
            ScheduleGrandTotals();
        }

        return new ValueTask<FastTreeDataGridPageResult>(new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null));
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (!_hasFormulas)
        {
            return ValueTask.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var end = Math.Min(request.StartIndex + request.Count, RowCount);
        for (var index = request.StartIndex; index < end; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScheduleFormulaWork(index);
        }

        ScheduleGrandTotals();
        return ValueTask.CompletedTask;
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        if (request.Kind == FastTreeDataGridInvalidationKind.Full)
        {
            ResetProcessingState();
        }

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

    public void RequestReset()
    {
        ResetProcessingState();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetProcessingState()
    {
        _processingCts.Cancel();
        try
        {
            Task.WaitAll(_cellFormulaTasks.Values.ToArray(), TimeSpan.FromMilliseconds(50));
        }
        catch
        {
            // ignore background cancellation exceptions
        }

        _processingCts.Dispose();
        _processingCts = new CancellationTokenSource();
        _cellFormulaTasks.Clear();
        _rowFormulaTasks.Clear();
        _columnFormulaTasks.Clear();
        _grandTotalsTask = null;
    }

    private void ScheduleFormulaWork(int rowIndex)
    {
        if (!_hasFormulas || rowIndex < 0 || rowIndex >= RowCount)
        {
            return;
        }

        foreach (var descriptor in _formulaCellDescriptors)
        {
            if (descriptor.ColumnIndex is int columnIndex)
            {
                ScheduleCellFormula(rowIndex, columnIndex);
            }
        }

        if (_formulaRowTotalDescriptors.Count > 0)
        {
            ScheduleRowTotals(rowIndex);
        }

        foreach (var descriptor in _formulaColumnTotalDescriptors)
        {
            if (descriptor.ColumnIndex is int columnIndex)
            {
                ScheduleColumnTotals(columnIndex);
            }
        }
    }

    private void ScheduleCellFormula(int rowIndex, int columnIndex)
    {
        if (_result.TryGetFormulaCellValues(rowIndex, columnIndex, out _))
        {
            return;
        }

        var key = (rowIndex, columnIndex);
        _cellFormulaTasks.GetOrAdd(key, _ => Task.Run(() =>
        {
            try
            {
                if (_processingCts.IsCancellationRequested)
                {
                    return;
                }

                if (!_result.TryGetCellValues(rowIndex, columnIndex, out var measures) || measures.Length == 0 || _result.FormulaCount == 0)
                {
                    _result.SetFormulaCellValues(rowIndex, columnIndex, Array.Empty<double?>());
                    return;
                }

                var buffer = new double?[_result.FormulaCount];
                _formulaEvaluator.EvaluateAll(measures, buffer);
                _result.SetFormulaCellValues(rowIndex, columnIndex, buffer);
                RaiseRowInvalidated(rowIndex);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _cellFormulaTasks.TryRemove(key, out var _);
            }
        }, _processingCts.Token));
    }

    private void ScheduleRowTotals(int rowIndex)
    {
        if (_rowFormulaTasks.ContainsKey(rowIndex))
        {
            return;
        }

        if (_result.GetFormulaRowTotals(rowIndex).Length > 0)
        {
            return;
        }

        _rowFormulaTasks[rowIndex] = Task.Run(() =>
        {
            try
            {
                if (_processingCts.IsCancellationRequested || _result.FormulaCount == 0)
                {
                    return;
                }

                var measures = _result.GetRowTotals(rowIndex);
                if (measures.Length == 0)
                {
                    _result.SetFormulaRowTotals(rowIndex, Array.Empty<double?>());
                    return;
                }

                var buffer = new double?[_result.FormulaCount];
                _formulaEvaluator.EvaluateAll(measures, buffer);
                _result.SetFormulaRowTotals(rowIndex, buffer);
                RaiseRowInvalidated(rowIndex);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _rowFormulaTasks.TryRemove(rowIndex, out var _);
            }
        }, _processingCts.Token);
    }

    private void ScheduleColumnTotals(int columnIndex)
    {
        if (_columnFormulaTasks.ContainsKey(columnIndex))
        {
            return;
        }

        if (_result.GetFormulaColumnTotals(columnIndex).Length > 0)
        {
            return;
        }

        _columnFormulaTasks[columnIndex] = Task.Run(() =>
        {
            try
            {
                if (_processingCts.IsCancellationRequested || _result.FormulaCount == 0)
                {
                    return;
                }

                var measures = _result.GetColumnTotals(columnIndex);
                if (measures.Length == 0)
                {
                    _result.SetFormulaColumnTotals(columnIndex, Array.Empty<double?>());
                    return;
                }

                var buffer = new double?[_result.FormulaCount];
                _formulaEvaluator.EvaluateAll(measures, buffer);
                _result.SetFormulaColumnTotals(columnIndex, buffer);
                RaiseFullInvalidation();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _columnFormulaTasks.TryRemove(columnIndex, out var _);
            }
        }, _processingCts.Token);
    }

    private void ScheduleGrandTotals()
    {
        if (_grandTotalsTask is not null || !_hasFormulas)
        {
            return;
        }

        if (_result.TryGetFormulaGrandTotals(out var totals) && totals.Length > 0)
        {
            return;
        }

        _grandTotalsTask = Task.Run(() =>
        {
            try
            {
                if (_processingCts.IsCancellationRequested || _result.FormulaCount == 0)
                {
                    return;
                }

                var measures = _result.GrandTotals;
                var buffer = new double?[_result.FormulaCount];
                _formulaEvaluator.EvaluateAll(measures, buffer);
                _result.SetFormulaGrandTotals(buffer);
                RaiseFullInvalidation();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }, _processingCts.Token);
    }

    private void RaiseRowInvalidated(int rowIndex)
    {
        try
        {
            Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, rowIndex, 1)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void RaiseFullInvalidation()
    {
        try
        {
            Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
