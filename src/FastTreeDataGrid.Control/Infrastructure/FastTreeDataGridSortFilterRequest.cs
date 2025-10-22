using System;
using System.Collections.Generic;

using FastTreeDataGrid.Control.Models;

using FastTreeDataGrid.Control.Infrastructure;

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

    public Func<object?, bool>? Predicate { get; set; }
}

public sealed class FastTreeDataGridSortFilterRequest
{
    public IReadOnlyList<FastTreeDataGridSortDescriptor> SortDescriptors { get; init; } = Array.Empty<FastTreeDataGridSortDescriptor>();

    public IReadOnlyList<FastTreeDataGridFilterDescriptor> FilterDescriptors { get; init; } = Array.Empty<FastTreeDataGridFilterDescriptor>();
}
