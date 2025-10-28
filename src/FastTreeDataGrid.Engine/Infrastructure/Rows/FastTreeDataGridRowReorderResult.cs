using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridRowReorderResult
{
    private static readonly IReadOnlyList<int> s_empty = new List<int>(0);

    public FastTreeDataGridRowReorderResult(bool success, IReadOnlyList<int>? newIndices = null)
    {
        Success = success;
        NewIndices = newIndices ?? s_empty;
    }

    public bool Success { get; }

    /// <summary>
    /// New zero-based indices of the moved rows after the operation completes. Not all handlers supply this information.
    /// </summary>
    public IReadOnlyList<int> NewIndices { get; }

    public static FastTreeDataGridRowReorderResult Cancelled { get; } = new(false);

    public static FastTreeDataGridRowReorderResult Successful(IReadOnlyList<int>? newIndices = null) =>
        new(true, newIndices);
}
