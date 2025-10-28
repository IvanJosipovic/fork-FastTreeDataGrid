using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.ExcelDemo.Models;

namespace FastTreeDataGrid.ExcelDemo.Services;

public sealed class PivotEngine
{
    private readonly IReadOnlyList<SalesRecord> _records;
    private readonly IReadOnlyList<MeasureOption> _baseMeasures;
    private readonly IReadOnlyList<FormulaDefinition> _formulas;

    public PivotEngine(
        IReadOnlyList<SalesRecord> records,
        IReadOnlyList<MeasureOption> baseMeasures,
        IReadOnlyList<FormulaDefinition> formulas)
    {
        _records = records ?? throw new ArgumentNullException(nameof(records));
        _baseMeasures = baseMeasures ?? throw new ArgumentNullException(nameof(baseMeasures));
        if (_baseMeasures.Count == 0)
        {
            throw new ArgumentException("At least one measure is required to build a pivot.", nameof(baseMeasures));
        }

        _formulas = formulas ?? throw new ArgumentNullException(nameof(formulas));
    }

    public PivotResult Build(DimensionOption rowDimension, DimensionOption columnDimension)
    {
        if (rowDimension is null)
        {
            throw new ArgumentNullException(nameof(rowDimension));
        }

        if (columnDimension is null)
        {
            throw new ArgumentNullException(nameof(columnDimension));
        }

        var measureCount = _baseMeasures.Count;
        var formulaCount = _formulas.Count;
        var rowLabels = new List<string>();
        var rowLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rowTotals = new List<double[]>();

        var columnLabels = new List<string>();
        var columnLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var columnTotals = new List<double[]>();

        var cells = new Dictionary<(int Row, int Column), double[]>();
        var grandTotals = new double[measureCount];

        foreach (var record in _records)
        {
            var rowKey = Normalize(rowDimension.Select(record));
            var columnKey = Normalize(columnDimension.Select(record));

            if (!rowLookup.TryGetValue(rowKey, out var rowIndex))
            {
                rowIndex = rowLabels.Count;
                rowLookup[rowKey] = rowIndex;
                rowLabels.Add(rowKey);
                rowTotals.Add(new double[measureCount]);
            }

            if (!columnLookup.TryGetValue(columnKey, out var columnIndex))
            {
                columnIndex = columnLabels.Count;
                columnLookup[columnKey] = columnIndex;
                columnLabels.Add(columnKey);
                columnTotals.Add(new double[measureCount]);
            }

            var cellKey = (rowIndex, columnIndex);
            if (!cells.TryGetValue(cellKey, out var measureValues))
            {
                measureValues = new double[measureCount];
                cells[cellKey] = measureValues;
            }

            for (var i = 0; i < measureCount; i++)
            {
                var value = _baseMeasures[i].ValueSelector(record);
                measureValues[i] += value;
                rowTotals[rowIndex][i] += value;
                columnTotals[columnIndex][i] += value;
                grandTotals[i] += value;
            }
        }

        var rowOrder = rowLabels
            .Select((label, index) => (label, index))
            .OrderBy(tuple => tuple.label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var columnOrder = columnLabels
            .Select((label, index) => (label, index))
            .OrderBy(tuple => tuple.label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sortedRows = new List<string>(rowOrder.Length);
        var sortedRowTotals = new double[rowOrder.Length][];
        var rowIndexMap = new Dictionary<int, int>();

        for (var i = 0; i < rowOrder.Length; i++)
        {
            sortedRows.Add(rowOrder[i].label);
            sortedRowTotals[i] = rowTotals[rowOrder[i].index];
            rowIndexMap[rowOrder[i].index] = i;
        }

        var sortedColumns = new List<string>(columnOrder.Length);
        var sortedColumnTotals = new double[columnOrder.Length][];
        var columnIndexMap = new Dictionary<int, int>();

        for (var i = 0; i < columnOrder.Length; i++)
        {
            sortedColumns.Add(columnOrder[i].label);
            sortedColumnTotals[i] = columnTotals[columnOrder[i].index];
            columnIndexMap[columnOrder[i].index] = i;
        }

        var sortedCells = new Dictionary<(int Row, int Column), double[]>(cells.Count);
        foreach (var (key, value) in cells)
        {
            var mappedRow = rowIndexMap[key.Row];
            var mappedColumn = columnIndexMap[key.Column];
            sortedCells[(mappedRow, mappedColumn)] = value;
        }

        return new PivotResult(
            sortedRows,
            sortedColumns,
            _baseMeasures,
            _formulas,
            sortedCells,
            formulaCount > 0 ? new Dictionary<(int Row, int Column), double?[]>() : null,
            sortedRowTotals,
            sortedColumnTotals,
            formulaCount > 0 ? new Dictionary<int, double?[]>() : null,
            formulaCount > 0 ? new Dictionary<int, double?[]>() : null,
            grandTotals,
            formulaCount > 0 ? Array.Empty<double?>() : null);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(Blank)";
        }

        return value;
    }
}
