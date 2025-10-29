using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FastTreeDataGrid.ExcelLikeDemo.Models;

public sealed class WorkbookModel
{
    public WorkbookModel(string name, IReadOnlyList<WorksheetModel> worksheets)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        var collection = worksheets?.ToList() ?? new List<WorksheetModel>();
        Worksheets = new ReadOnlyCollection<WorksheetModel>(collection);
    }

    public string Name { get; }

    public IReadOnlyList<WorksheetModel> Worksheets { get; }
}

public sealed class WorksheetModel
{
    public WorksheetModel(string name, IReadOnlyList<WorksheetRowModel> rows, IReadOnlyList<string> columnHeaders)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        var rowCollection = rows?.ToList() ?? new List<WorksheetRowModel>();
        var headers = columnHeaders?.ToList() ?? new List<string>();
        Rows = new ReadOnlyCollection<WorksheetRowModel>(rowCollection);
        ColumnHeaders = new ReadOnlyCollection<string>(headers);
    }

    public string Name { get; }

    public IReadOnlyList<WorksheetRowModel> Rows { get; }

    public IReadOnlyList<string> ColumnHeaders { get; }

    public int RowCount => Rows.Count;

    public int ColumnCount => ColumnHeaders.Count;
}

public sealed class WorksheetRowModel
{
    private readonly Dictionary<int, WorksheetCellModel> _cells;

    public WorksheetRowModel(int index, Dictionary<int, WorksheetCellModel> cells, double? explicitHeight = null)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        _cells = cells ?? throw new ArgumentNullException(nameof(cells));
        ExplicitHeight = explicitHeight;
    }

    public int Index { get; }

    public double? ExplicitHeight { get; }

    public IReadOnlyDictionary<int, WorksheetCellModel> Cells => _cells;

    public WorksheetCellModel GetOrAddCell(int columnIndex)
    {
        if (!_cells.TryGetValue(columnIndex, out var cell))
        {
            cell = new WorksheetCellModel(Index, columnIndex, address: ExcelAddressHelper.BuildAddress(columnIndex, Index));
            _cells[columnIndex] = cell;
        }

        return cell;
    }

    public WorksheetCellModel? TryGetCell(int columnIndex)
    {
        _cells.TryGetValue(columnIndex, out var cell);
        return cell;
    }
}

public sealed class WorksheetCellModel : INotifyPropertyChanged
{
    private string? _displayText;
    private string? _formulaText;
    private string? _rawText;

    public WorksheetCellModel(int rowIndex, int columnIndex, string address)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int RowIndex { get; }

    public int ColumnIndex { get; }

    public string Address { get; }

    public string? DisplayText
    {
        get => _displayText;
        private set
        {
            if (_displayText == value)
            {
                return;
            }

            _displayText = value;
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(DisplayOrEmpty));
            OnPropertyChanged(nameof(EditText));
        }
    }

    public string? FormulaText
    {
        get => _formulaText;
        private set
        {
            if (_formulaText == value)
            {
                return;
            }

            _formulaText = value;
            OnPropertyChanged(nameof(FormulaText));
            OnPropertyChanged(nameof(EditText));
        }
    }

    public string? RawText
    {
        get => _rawText;
        private set
        {
            if (_rawText == value)
            {
                return;
            }

            _rawText = value;
            OnPropertyChanged(nameof(RawText));
            OnPropertyChanged(nameof(EditText));
        }
    }

    public string DisplayOrEmpty => DisplayText ?? string.Empty;

    public string EditText
    {
        get => FormulaText ?? RawText ?? DisplayText ?? string.Empty;
        set => ApplyEditorText(value);
    }

    public void SetFromExcelValue(string? display, string? raw, string? formula)
    {
        _displayText = display;
        _rawText = raw ?? display;
        _formulaText = formula;

        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(DisplayOrEmpty));
        OnPropertyChanged(nameof(FormulaText));
        OnPropertyChanged(nameof(RawText));
        OnPropertyChanged(nameof(EditText));
    }

    public void ApplyEditorText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _displayText = null;
            _formulaText = null;
            _rawText = null;
        }
        else if (text.StartsWith('='))
        {
            _formulaText = text;
            _rawText = null;
            _displayText = text;
        }
        else
        {
            _displayText = text;
            _rawText = text;
            _formulaText = null;
        }

        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(DisplayOrEmpty));
        OnPropertyChanged(nameof(FormulaText));
        OnPropertyChanged(nameof(RawText));
        OnPropertyChanged(nameof(EditText));
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal static class ExcelAddressHelper
{
    public static string BuildAddress(int columnIndex, int rowIndex)
    {
        var column = ToColumnName(columnIndex);
        return $"{column}{rowIndex + 1}";
    }

    public static string ToColumnName(int columnIndex)
    {
        columnIndex = Math.Max(columnIndex, 0);
        var dividend = columnIndex + 1;
        var chars = new Stack<char>();

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            chars.Push((char)('A' + modulo));
            dividend = (dividend - modulo - 1) / 26;
        }

        return new string(chars.ToArray());
    }

    public static int FromColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return 0;
        }

        var value = 0;
        var letters = columnName.Trim().ToUpperInvariant();
        for (var i = 0; i < letters.Length; i++)
        {
            var c = letters[i];
            if (c < 'A' || c > 'Z')
            {
                continue;
            }

            value = value * 26 + (c - 'A' + 1);
        }

        return Math.Max(0, value - 1);
    }

    public static int GetColumnIndex(string cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var length = 0;
        while (length < cellReference.Length && char.IsLetter(cellReference[length]))
        {
            length++;
        }

        var columnPart = cellReference[..length];
        return FromColumnName(columnPart);
    }
}
