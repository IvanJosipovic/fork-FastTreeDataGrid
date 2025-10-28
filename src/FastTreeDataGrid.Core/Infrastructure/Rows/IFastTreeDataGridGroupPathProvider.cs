namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Provides access to the unique grouping path for a generated group row.
/// </summary>
public interface IFastTreeDataGridGroupPathProvider
{
    /// <summary>
    /// Gets the unique path that identifies the group.
    /// </summary>
    string GroupPath { get; }
}
