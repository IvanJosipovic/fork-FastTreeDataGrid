using System;
using System.Collections.Generic;

using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridSortDescriptor
{
    public string? ColumnKey { get; set; }

    public FastTreeDataGridSortDirection Direction { get; set; }

    public Comparison<FastTreeDataGridRow>? RowComparison { get; set; }
}

public sealed class FastTreeDataGridFilterDescriptor
{
    public string? ColumnKey { get; set; }

    public Func<FastTreeDataGridRow, bool>? Predicate { get; set; }
}

public sealed class FastTreeDataGridSortFilterRequest
{
    public IReadOnlyList<FastTreeDataGridSortDescriptor> SortDescriptors { get; init; } = Array.Empty<FastTreeDataGridSortDescriptor>();

    public IReadOnlyList<FastTreeDataGridFilterDescriptor> FilterDescriptors { get; init; } = Array.Empty<FastTreeDataGridFilterDescriptor>();

    public IReadOnlyList<FastTreeDataGridGroupDescriptor> GroupDescriptors { get; init; } = Array.Empty<FastTreeDataGridGroupDescriptor>();

    public IReadOnlyList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors { get; init; } = Array.Empty<FastTreeDataGridAggregateDescriptor>();
}
