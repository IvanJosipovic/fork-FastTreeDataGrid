using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Provides column-specific grouping semantics for FastTreeDataGrid.
/// </summary>
public interface IFastTreeDataGridGroupAdapter
{
    /// <summary>
    /// Extracts a normalized group key for the given row.
    /// </summary>
    /// <param name="row">The row being grouped.</param>
    /// <returns>The group key used for comparisons.</returns>
    object? GetGroupKey(FastTreeDataGridRow row);

    /// <summary>
    /// Converts the group key into a display label for headers and chips.
    /// </summary>
    /// <param name="key">The key returned by <see cref="GetGroupKey"/>.</param>
    /// <param name="level">Grouping level (0-based).</param>
    /// <param name="itemCount">Number of rows within the group.</param>
    /// <returns>Display text for UI surfaces.</returns>
    string GetGroupLabel(object? key, int level, int itemCount);

    /// <summary>
    /// Provides the comparer used to sort group keys at this level.
    /// </summary>
    IComparer<object?>? Comparer { get; }

    /// <summary>
    /// Determines whether grouping with this adapter is currently allowed.
    /// </summary>
    /// <returns><c>true</c> when grouping is allowed; otherwise false.</returns>
    bool CanGroup() => true;
}
