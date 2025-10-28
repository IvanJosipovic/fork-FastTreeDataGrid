using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FastTreeDataGrid.ExcelDemo.Models;

public sealed class PivotResult
{
    private readonly Dictionary<(int Row, int Column), double[]> _cells;
    private readonly ConcurrentDictionary<(int Row, int Column), double?[]> _formulaCells;
    private readonly ReadOnlyCollection<string> _rowLabels;
    private readonly ReadOnlyCollection<string> _columnLabels;
    private readonly ReadOnlyCollection<MeasureOption> _measures;
    private readonly ReadOnlyCollection<FormulaDefinition> _formulas;
    private readonly double[][] _rowTotals;
    private readonly double[][] _columnTotals;
    private readonly ConcurrentDictionary<int, double?[]> _rowFormulaTotals;
    private readonly ConcurrentDictionary<int, double?[]> _columnFormulaTotals;
    private readonly double[] _grandTotals;
    private double?[]? _formulaGrandTotals;
    private readonly Dictionary<string, int> _measureIndexLookup;
    private readonly Dictionary<string, int> _formulaIndexLookup;

    public PivotResult(
        IReadOnlyList<string> rowLabels,
        IReadOnlyList<string> columnLabels,
        IReadOnlyList<MeasureOption> measures,
        IReadOnlyList<FormulaDefinition> formulas,
        Dictionary<(int Row, int Column), double[]> cells,
        IDictionary<(int Row, int Column), double?[]>? formulaCells,
        double[][] rowTotals,
        double[][] columnTotals,
        IDictionary<int, double?[]>? rowFormulaTotals,
        IDictionary<int, double?[]>? columnFormulaTotals,
        double[] grandTotals,
        double?[]? formulaGrandTotals)
    {
        if (rowLabels is null)
        {
            throw new ArgumentNullException(nameof(rowLabels));
        }

        if (columnLabels is null)
        {
            throw new ArgumentNullException(nameof(columnLabels));
        }

        if (measures is null)
        {
            throw new ArgumentNullException(nameof(measures));
        }

        formulas ??= Array.Empty<FormulaDefinition>();

        _rowLabels = new ReadOnlyCollection<string>(new List<string>(rowLabels));
        _columnLabels = new ReadOnlyCollection<string>(new List<string>(columnLabels));
        _measures = new ReadOnlyCollection<MeasureOption>(new List<MeasureOption>(measures));
        _formulas = new ReadOnlyCollection<FormulaDefinition>(new List<FormulaDefinition>(formulas));
        _cells = cells ?? throw new ArgumentNullException(nameof(cells));
        _formulaCells = formulaCells is null
            ? new ConcurrentDictionary<(int Row, int Column), double?[]>()
            : new ConcurrentDictionary<(int Row, int Column), double?[]>(formulaCells);
        _rowTotals = rowTotals ?? throw new ArgumentNullException(nameof(rowTotals));
        _columnTotals = columnTotals ?? throw new ArgumentNullException(nameof(columnTotals));
        _rowFormulaTotals = rowFormulaTotals is null
            ? new ConcurrentDictionary<int, double?[]>()
            : new ConcurrentDictionary<int, double?[]>(rowFormulaTotals);
        _columnFormulaTotals = columnFormulaTotals is null
            ? new ConcurrentDictionary<int, double?[]>()
            : new ConcurrentDictionary<int, double?[]>(columnFormulaTotals);
        _grandTotals = grandTotals ?? throw new ArgumentNullException(nameof(grandTotals));
        _formulaGrandTotals = formulaGrandTotals;

        if (_rowTotals.Length != _rowLabels.Count)
        {
            throw new ArgumentException("Row totals length must match row labels count.", nameof(rowTotals));
        }

        if (_columnTotals.Length != _columnLabels.Count)
        {
            throw new ArgumentException("Column totals length must match column labels count.", nameof(columnTotals));
        }

        if (_grandTotals.Length != _measures.Count)
        {
            throw new ArgumentException("Grand totals length must match measures count.", nameof(grandTotals));
        }

        if (_formulaGrandTotals is not null && _formulaGrandTotals.Length != 0 && _formulaGrandTotals.Length != _formulas.Count)
        {
            throw new ArgumentException("Formula grand totals length must match formulas count.", nameof(formulaGrandTotals));
        }

        _measureIndexLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _measures.Count; i++)
        {
            _measureIndexLookup[_measures[i].Key] = i;
        }

        _formulaIndexLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _formulas.Count; i++)
        {
            _formulaIndexLookup[_formulas[i].Key] = i;
        }
    }

    public IReadOnlyList<string> RowLabels => _rowLabels;

    public IReadOnlyList<string> ColumnLabels => _columnLabels;

    public IReadOnlyList<MeasureOption> Measures => _measures;

    public IReadOnlyList<FormulaDefinition> Formulas => _formulas;

    public int RowCount => _rowLabels.Count;

    public int ColumnCount => _columnLabels.Count;

    public double[] GrandTotals => _grandTotals;

    public IReadOnlyList<double?> FormulaGrandTotals => _formulaGrandTotals ?? Array.Empty<double?>();

    public int FormulaCount => _formulas.Count;

    public double[] GetRowTotals(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _rowTotals.Length)
        {
            return Array.Empty<double>();
        }

        return _rowTotals[rowIndex];
    }

    public double[] GetColumnTotals(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _columnTotals.Length)
        {
            return Array.Empty<double>();
        }

        return _columnTotals[columnIndex];
    }

    public bool TryGetCellValues(int rowIndex, int columnIndex, out double[] values)
    {
        if (_cells.TryGetValue((rowIndex, columnIndex), out values!))
        {
            return true;
        }

        values = Array.Empty<double>();
        return false;
    }

    public bool TryGetFormulaCellValues(int rowIndex, int columnIndex, out double?[] values)
    {
        if (_formulas.Count == 0)
        {
            values = Array.Empty<double?>();
            return false;
        }

        if (_formulaCells.TryGetValue((rowIndex, columnIndex), out values!))
        {
            return true;
        }

        values = Array.Empty<double?>();
        return false;
    }

    public void SetFormulaCellValues(int rowIndex, int columnIndex, double?[] values)
    {
        if (values is null || values.Length == 0)
        {
            _formulaCells.TryRemove((rowIndex, columnIndex), out _);
        }
        else
        {
            _formulaCells[(rowIndex, columnIndex)] = values;
        }
    }

    public bool TryGetMeasureIndex(string key, out int index)
    {
        if (key is null)
        {
            index = -1;
            return false;
        }

        return _measureIndexLookup.TryGetValue(key, out index);
    }

    public bool TryGetFormulaIndex(string key, out int index)
    {
        if (key is null)
        {
            index = -1;
            return false;
        }

        return _formulaIndexLookup.TryGetValue(key, out index);
    }

    public double?[] GetFormulaRowTotals(int rowIndex)
    {
        if (_rowFormulaTotals.TryGetValue(rowIndex, out var values))
        {
            return values;
        }

        return Array.Empty<double?>();
    }

    public double?[] GetFormulaColumnTotals(int columnIndex)
    {
        if (_columnFormulaTotals.TryGetValue(columnIndex, out var values))
        {
            return values;
        }

        return Array.Empty<double?>();
    }

    public void SetFormulaRowTotals(int rowIndex, double?[] values)
    {
        if (values is null || values.Length == 0)
        {
            _rowFormulaTotals.TryRemove(rowIndex, out _);
        }
        else
        {
            _rowFormulaTotals[rowIndex] = values;
        }
    }

    public void SetFormulaColumnTotals(int columnIndex, double?[] values)
    {
        if (values is null || values.Length == 0)
        {
            _columnFormulaTotals.TryRemove(columnIndex, out _);
        }
        else
        {
            _columnFormulaTotals[columnIndex] = values;
        }
    }

    public bool TryGetFormulaGrandTotals(out double?[] values)
    {
        if (_formulaGrandTotals is { } totals && totals.Length > 0)
        {
            values = totals;
            return true;
        }

        values = Array.Empty<double?>();
        return false;
    }

    public void SetFormulaGrandTotals(double?[] values)
    {
        _formulaGrandTotals = values is null || values.Length == 0 ? Array.Empty<double?>() : values;
    }
}
