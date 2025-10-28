using Avalonia.Media;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.ExcelDemo.Models;

namespace FastTreeDataGrid.ExcelDemo.ViewModels.Grid;

internal enum ExcelColumnRole
{
    RowIndex,
    RowHeader,
    MeasureCell,
    MeasureRowTotal,
    MeasureColumnTotal,
    MeasureGrandTotal,
    FormulaCell,
    FormulaRowTotal,
    FormulaColumnTotal,
    FormulaGrandTotal,
}

internal sealed class ExcelColumnDescriptor
{
    public string Header { get; init; } = string.Empty;

    public string ValueKey { get; init; } = string.Empty;

    public ColumnSizingMode SizingMode { get; init; } = ColumnSizingMode.Pixel;

    public double PixelWidth { get; init; } = 120d;

    public double MinWidth { get; init; } = 80d;

    public double StarValue { get; init; } = 1d;

    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    public bool IsNumeric { get; init; }

    public ExcelColumnRole Role { get; init; }

    public int? ColumnIndex { get; init; }

    public MeasureOption? Measure { get; init; }

    public int? MeasureIndex { get; init; }

    public FormulaDefinition? Formula { get; init; }

    public int? FormulaIndex { get; init; }

    public bool IsFormula => Formula is not null;
}
