namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Describes the severity of a validation finding.
/// </summary>
public enum FastTreeDataGridValidationLevel
{
    None = 0,
    Information = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>
/// Represents validation metadata for an individual cell.
/// </summary>
public readonly struct FastTreeDataGridCellValidationState
{
    public FastTreeDataGridCellValidationState(FastTreeDataGridValidationLevel level, string? message)
    {
        Level = level;
        Message = message;
    }

    public FastTreeDataGridValidationLevel Level { get; }

    public string? Message { get; }

    public bool HasError => Level == FastTreeDataGridValidationLevel.Error;

    public bool HasWarning => Level == FastTreeDataGridValidationLevel.Warning;

    public static FastTreeDataGridCellValidationState None { get; } =
        new(FastTreeDataGridValidationLevel.None, null);
}

/// <summary>
/// Captures aggregated validation state for a row.
/// </summary>
public readonly struct FastTreeDataGridRowValidationState
{
    public FastTreeDataGridRowValidationState(int errorCount, int warningCount, string? message)
    {
        ErrorCount = errorCount;
        WarningCount = warningCount;
        Message = message;
    }

    public int ErrorCount { get; }

    public int WarningCount { get; }

    public string? Message { get; }

    public bool HasErrors => ErrorCount > 0;

    public bool HasWarnings => WarningCount > 0;

    public static FastTreeDataGridRowValidationState None { get; } =
        new(0, 0, null);
}
