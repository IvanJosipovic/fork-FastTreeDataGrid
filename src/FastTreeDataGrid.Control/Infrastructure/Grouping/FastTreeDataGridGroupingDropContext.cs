using System.Collections.Generic;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Represents a drop operation within the grouping band.
/// </summary>
public sealed class FastTreeDataGridGroupingDropContext
{
    public FastTreeDataGridGroupingDropContext(
        int targetIndex,
        IReadOnlyList<FastTreeDataGridGroupDescriptor> currentDescriptors,
        FastTreeDataGridGroupDescriptor pendingDescriptor,
        bool isReorder)
    {
        TargetIndex = targetIndex;
        CurrentDescriptors = currentDescriptors;
        PendingDescriptor = pendingDescriptor;
        IsReorder = isReorder;
    }

    public int TargetIndex { get; }

    public IReadOnlyList<FastTreeDataGridGroupDescriptor> CurrentDescriptors { get; }

    public FastTreeDataGridGroupDescriptor PendingDescriptor { get; }

    public bool IsReorder { get; }
}
