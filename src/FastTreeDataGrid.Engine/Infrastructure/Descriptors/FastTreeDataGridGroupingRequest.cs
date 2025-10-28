using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Describes grouping-related operations requested by the grid.
/// </summary>
public sealed class FastTreeDataGridGroupingRequest
{
    public IReadOnlyList<FastTreeDataGridSortDescriptor> SortDescriptors { get; init; } = Array.Empty<FastTreeDataGridSortDescriptor>();

    public IReadOnlyList<FastTreeDataGridFilterDescriptor> FilterDescriptors { get; init; } = Array.Empty<FastTreeDataGridFilterDescriptor>();

    public IReadOnlyList<FastTreeDataGridGroupDescriptor> GroupDescriptors { get; init; } = Array.Empty<FastTreeDataGridGroupDescriptor>();

    public IReadOnlyList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors { get; init; } = Array.Empty<FastTreeDataGridAggregateDescriptor>();

    public static FastTreeDataGridGroupingRequest FromSortFilterRequest(FastTreeDataGridSortFilterRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return new FastTreeDataGridGroupingRequest
        {
            SortDescriptors = request.SortDescriptors ?? Array.Empty<FastTreeDataGridSortDescriptor>(),
            FilterDescriptors = request.FilterDescriptors ?? Array.Empty<FastTreeDataGridFilterDescriptor>(),
            GroupDescriptors = request.GroupDescriptors ?? Array.Empty<FastTreeDataGridGroupDescriptor>(),
            AggregateDescriptors = request.AggregateDescriptors ?? Array.Empty<FastTreeDataGridAggregateDescriptor>(),
        };
    }
}
