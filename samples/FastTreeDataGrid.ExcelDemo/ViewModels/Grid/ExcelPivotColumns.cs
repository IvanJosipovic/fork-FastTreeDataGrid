namespace FastTreeDataGrid.ExcelDemo.ViewModels.Grid;

internal static class ExcelPivotColumns
{
    public const string RowIndex = "row:index";
    public const string RowLabel = "row:label";
    public const string RowTotalPrefix = "row-total";
    public const string ColumnTotalPrefix = "column-total";
    public const string GrandTotalPrefix = "grand-total";
    public const string FormulaPrefix = "formula";

    public static string BuildMeasureCellKey(int columnIndex, string measureKey) => $"cell:{columnIndex}:{measureKey}";

    public static string BuildFormulaCellKey(int columnIndex, string formulaKey) => $"{FormulaPrefix}:{columnIndex}:{formulaKey}";

    public static string BuildRowTotalKey(string measureKey) => $"{RowTotalPrefix}:{measureKey}";

    public static string BuildFormulaRowTotalKey(string formulaKey) => $"{RowTotalPrefix}:{FormulaPrefix}:{formulaKey}";

    public static string BuildColumnTotalKey(int columnIndex, string measureKey) => $"{ColumnTotalPrefix}:{columnIndex}:{measureKey}";

    public static string BuildFormulaColumnTotalKey(int columnIndex, string formulaKey) => $"{ColumnTotalPrefix}:{columnIndex}:{FormulaPrefix}:{formulaKey}";

    public static string BuildGrandTotalKey(string measureKey) => $"{GrandTotalPrefix}:{measureKey}";

    public static string BuildFormulaGrandTotalKey(string formulaKey) => $"{GrandTotalPrefix}:{FormulaPrefix}:{formulaKey}";
}
