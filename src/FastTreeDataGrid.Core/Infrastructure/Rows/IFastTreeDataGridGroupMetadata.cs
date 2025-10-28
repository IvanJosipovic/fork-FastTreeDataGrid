namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Provides additional metadata for grouped rows.
/// </summary>
public interface IFastTreeDataGridGroupMetadata : IFastTreeDataGridGroup
{
    /// <summary>
    /// Gets the number of items contained within the group.
    /// </summary>
    int ItemCount { get; }
}
