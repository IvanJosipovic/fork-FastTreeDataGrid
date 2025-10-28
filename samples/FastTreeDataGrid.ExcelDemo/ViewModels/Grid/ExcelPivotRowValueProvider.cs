using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.ExcelDemo.Models;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ExcelDemo.ViewModels.Grid;

internal sealed class ExcelPivotRowValueProvider : IFastTreeDataGridValueProvider
{
    private readonly PivotResult _result;
    private readonly int _rowIndex;
    private readonly Dictionary<string, ExcelColumnDescriptor> _columnLookup;
    private readonly Dictionary<string, object?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ExcelPivotRowValueProvider(
        PivotResult result,
        int rowIndex,
        Dictionary<string, ExcelColumnDescriptor> columnLookup)
    {
        _result = result;
        _rowIndex = rowIndex;
        _columnLookup = columnLookup;
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
        if (key is null)
        {
            return null;
        }

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!_columnLookup.TryGetValue(key, out var descriptor))
        {
            return null;
        }

        var value = descriptor.Role switch
        {
            ExcelColumnRole.RowIndex => FormatRowIndex(),
            ExcelColumnRole.RowHeader => _result.RowLabels[_rowIndex],
            ExcelColumnRole.MeasureCell => FormatMeasureCell(descriptor),
            ExcelColumnRole.MeasureRowTotal => FormatMeasureRowTotal(descriptor),
            ExcelColumnRole.MeasureColumnTotal => FormatMeasureColumnTotal(descriptor),
            ExcelColumnRole.MeasureGrandTotal => FormatMeasureGrandTotal(descriptor),
            ExcelColumnRole.FormulaCell => FormatFormulaCell(descriptor),
            ExcelColumnRole.FormulaRowTotal => FormatFormulaRowTotal(descriptor),
            ExcelColumnRole.FormulaColumnTotal => FormatFormulaColumnTotal(descriptor),
            ExcelColumnRole.FormulaGrandTotal => FormatFormulaGrandTotal(descriptor),
            _ => null,
        };

        _cache[key] = value;
        return value;
    }

    private string FormatRowIndex()
    {
        return (_rowIndex + 1).ToString("N0", CultureInfo.InvariantCulture);
    }

    private string? FormatMeasureCell(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.ColumnIndex is null || descriptor.MeasureIndex is null || descriptor.Measure is null)
        {
            return string.Empty;
        }

        if (!_result.TryGetCellValues(_rowIndex, descriptor.ColumnIndex.Value, out var values) || descriptor.MeasureIndex.Value >= values.Length)
        {
            return string.Empty;
        }

        var value = values[descriptor.MeasureIndex.Value];
        return descriptor.Measure.Format(value);
    }

    private string? FormatMeasureRowTotal(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.MeasureIndex is null || descriptor.Measure is null)
        {
            return string.Empty;
        }

        var totals = _result.GetRowTotals(_rowIndex);
        if (descriptor.MeasureIndex.Value >= totals.Length)
        {
            return string.Empty;
        }

        var value = totals[descriptor.MeasureIndex.Value];
        return descriptor.Measure.Format(value);
    }

    private string? FormatMeasureColumnTotal(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.ColumnIndex is null || descriptor.MeasureIndex is null || descriptor.Measure is null)
        {
            return string.Empty;
        }

        var totals = _result.GetColumnTotals(descriptor.ColumnIndex.Value);
        if (descriptor.MeasureIndex.Value >= totals.Length)
        {
            return string.Empty;
        }

        var value = totals[descriptor.MeasureIndex.Value];
        return descriptor.Measure.Format(value);
    }

    private string? FormatMeasureGrandTotal(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.MeasureIndex is null || descriptor.Measure is null)
        {
            return string.Empty;
        }

        var totals = _result.GrandTotals;
        if (descriptor.MeasureIndex.Value >= totals.Length)
        {
            return string.Empty;
        }

        var value = totals[descriptor.MeasureIndex.Value];
        return descriptor.Measure.Format(value);
    }

    private string? FormatFormulaCell(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.ColumnIndex is null || descriptor.Formula is null || descriptor.FormulaIndex is null)
        {
            return string.Empty;
        }

        if (!_result.TryGetFormulaCellValues(_rowIndex, descriptor.ColumnIndex.Value, out var values) || descriptor.FormulaIndex.Value >= values.Length)
        {
            return string.Empty;
        }

        var value = values[descriptor.FormulaIndex.Value];
        return descriptor.Formula.Format(value);
    }

    private string? FormatFormulaRowTotal(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.Formula is null || descriptor.FormulaIndex is null)
        {
            return string.Empty;
        }

        var totals = _result.GetFormulaRowTotals(_rowIndex);
        if (descriptor.FormulaIndex.Value >= totals.Length)
        {
            return string.Empty;
        }

        var value = totals[descriptor.FormulaIndex.Value];
        return descriptor.Formula.Format(value);
    }

    private string? FormatFormulaColumnTotal(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.ColumnIndex is null || descriptor.Formula is null || descriptor.FormulaIndex is null)
        {
            return string.Empty;
        }

        var totals = _result.GetFormulaColumnTotals(descriptor.ColumnIndex.Value);
        if (descriptor.FormulaIndex.Value >= totals.Length)
        {
            return string.Empty;
        }

        var value = totals[descriptor.FormulaIndex.Value];
        return descriptor.Formula.Format(value);
    }

    private string? FormatFormulaGrandTotal(ExcelColumnDescriptor descriptor)
    {
        if (descriptor.Formula is null || descriptor.FormulaIndex is null)
        {
            return string.Empty;
        }

        if (!_result.TryGetFormulaGrandTotals(out var totals) || descriptor.FormulaIndex.Value >= totals.Length)
        {
            return string.Empty;
        }

        var value = totals[descriptor.FormulaIndex.Value];
        return descriptor.Formula.Format(value);
    }
}
