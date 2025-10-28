using System.Collections.Generic;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Provides context to grouping and aggregation providers.
/// </summary>
public sealed class FastTreeDataGridGroupContext
{
    public FastTreeDataGridGroupContext(
        string path,
        int level,
        object? key,
        IReadOnlyList<FastTreeDataGridRow> rows,
        FastTreeDataGridGroupDescriptor descriptor)
    {
        Path = path;
        Level = level;
        Key = key;
        Rows = rows;
        Descriptor = descriptor;
    }

    /// <summary>
    /// Gets the unique path identifying the group.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the depth of the group.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the key value associated with the group.
    /// </summary>
    public object? Key { get; }

    /// <summary>
    /// Gets the rows contained within the group.
    /// </summary>
    public IReadOnlyList<FastTreeDataGridRow> Rows { get; }

    /// <summary>
    /// Gets the descriptor that produced the group.
    /// </summary>
    public FastTreeDataGridGroupDescriptor Descriptor { get; }
}
