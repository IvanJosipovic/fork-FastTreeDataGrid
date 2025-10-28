using System.Collections.Generic;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Represents an immutable snapshot of grouping descriptors and state.
/// </summary>
public sealed class FastTreeDataGridGroupingSnapshot
{
    public FastTreeDataGridGroupingSnapshot(
        IReadOnlyList<FastTreeDataGridGroupDescriptor> descriptors,
        IReadOnlyList<FastTreeDataGridGroupState> groups)
    {
        Descriptors = descriptors;
        Groups = groups;
    }

    public IReadOnlyList<FastTreeDataGridGroupDescriptor> Descriptors { get; }

    public IReadOnlyList<FastTreeDataGridGroupState> Groups { get; }
}
