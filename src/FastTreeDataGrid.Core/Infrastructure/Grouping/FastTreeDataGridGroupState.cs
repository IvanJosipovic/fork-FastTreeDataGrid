using System.Collections.Generic;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Represents the runtime state for a grouping level.
/// </summary>
public sealed class FastTreeDataGridGroupState
{
    public FastTreeDataGridGroupState(
        FastTreeDataGridGroupDescriptor descriptor,
        string path,
        int level,
        object? key,
        bool isExpanded)
    {
        Descriptor = descriptor;
        Path = path;
        Level = level;
        Key = key;
        IsExpanded = isExpanded;
    }

    /// <summary>
    /// Gets the descriptor this state is associated with.
    /// </summary>
    public FastTreeDataGridGroupDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the unique path identifying this group.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the depth of the group (0-based).
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the key for the group.
    /// </summary>
    public object? Key { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the group is expanded.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Gets a metadata bag for custom extensions.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();
}
