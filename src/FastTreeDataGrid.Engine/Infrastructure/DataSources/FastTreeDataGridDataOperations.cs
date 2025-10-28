using System;
using System.Collections.Generic;
using FastTreeDataGrid.Engine.Models;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Describes how rows should be grouped when data operations are applied.
/// </summary>
public sealed class FastTreeDataGridGroupDescriptor
{
    /// <summary>
    /// Gets or sets the column key whose values should be used for grouping.
    /// </summary>
    public string? ColumnKey { get; set; }

    /// <summary>
    /// Gets or sets a custom selector that returns the group key for a row.
    /// When <c>null</c>, the grid will attempt to resolve values through the column key.
    /// </summary>
    public Func<FastTreeDataGridRow, object?>? KeySelector { get; set; }

    /// <summary>
    /// Gets or sets an optional formatter used to build the group header text.
    /// When omitted, the key is converted to a string and formatted with the item count.
    /// </summary>
    public Func<FastTreeDataGridGroupHeaderContext, string>? HeaderFormatter { get; set; }

    /// <summary>
    /// Gets or sets the adapter that extracts group keys and labels.
    /// </summary>
    public IFastTreeDataGridGroupAdapter? Adapter { get; set; }

    /// <summary>
    /// Gets or sets the direction used to sort group keys.
    /// </summary>
    public FastTreeDataGridSortDirection SortDirection { get; set; } = FastTreeDataGridSortDirection.Ascending;

    /// <summary>
    /// Gets or sets the comparer used to order group keys.
    /// </summary>
    public IComparer<object?>? Comparer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether new groups are expanded by default.
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Gets the aggregate descriptors associated with this group level.
    /// </summary>
    public IList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors { get; } = new List<FastTreeDataGridAggregateDescriptor>();

    /// <summary>
    /// Gets a metadata bag for custom extensions.
    /// </summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
}

/// <summary>
/// Provides context when formatting group headers.
/// </summary>
public readonly struct FastTreeDataGridGroupHeaderContext
{
    public FastTreeDataGridGroupHeaderContext(object? key, int itemCount, int level)
    {
        Key = key;
        ItemCount = itemCount;
        Level = level;
    }

    public object? Key { get; }

    public int ItemCount { get; }

    public int Level { get; }
}

/// <summary>
/// Specifies where aggregate results should be rendered.
/// </summary>
public enum FastTreeDataGridAggregatePlacement
{
    None = 0,
    GroupFooter = 1,
    GridFooter = 2,
    GroupAndGrid = GroupFooter | GridFooter,
}

/// <summary>
/// Describes how an aggregate value should be calculated.
/// </summary>
public sealed class FastTreeDataGridAggregateDescriptor
{
    /// <summary>
    /// Gets or sets the column key where the aggregate result should appear.
    /// When <c>null</c>, the aggregate is rendered in the hierarchy column.
    /// </summary>
    public string? ColumnKey { get; set; }

    /// <summary>
    /// Gets or sets the placement of the aggregate.
    /// </summary>
    public FastTreeDataGridAggregatePlacement Placement { get; set; } = FastTreeDataGridAggregatePlacement.GroupFooter;

    /// <summary>
    /// Gets or sets the delegate that computes the aggregate for a set of rows.
    /// </summary>
    public Func<IEnumerable<FastTreeDataGridRow>, object?>? Aggregator { get; set; }

    /// <summary>
    /// Gets or sets the aggregate provider used to compute results.
    /// </summary>
    public IFastTreeDataGridAggregateProvider? Provider { get; set; }

    /// <summary>
    /// Gets or sets an optional formatter invoked with the aggregate result.
    /// When omitted, <see cref="object.ToString"/> with the current culture is used.
    /// </summary>
    public Func<object?, string?>? Formatter { get; set; }

    /// <summary>
    /// Gets or sets an optional label used when the aggregate targets the hierarchy column.
    /// </summary>
    public string? Label { get; set; }
}
