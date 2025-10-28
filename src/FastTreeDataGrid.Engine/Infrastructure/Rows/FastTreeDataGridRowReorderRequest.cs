using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridRowReorderRequest
{
    public FastTreeDataGridRowReorderRequest(IReadOnlyList<int> sourceIndices, int insertIndex)
    {
        SourceIndices = sourceIndices ?? throw new ArgumentNullException(nameof(sourceIndices));
        if (sourceIndices.Count == 0)
        {
            throw new ArgumentException("At least one index must be provided.", nameof(sourceIndices));
        }

        if (insertIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(insertIndex));
        }

        InsertIndex = insertIndex;
    }

    /// <summary>
    /// Zero-based row indices being moved. Values should be ordered ascending relative to the pre-move layout.
    /// </summary>
    public IReadOnlyList<int> SourceIndices { get; }

    /// <summary>
    /// Target index where the moved block should be inserted once the original rows are removed.
    /// </summary>
    public int InsertIndex { get; }

    /// <summary>
    /// Optional custom payload for reorder handlers.
    /// </summary>
    public object? Context { get; init; }

    /// <summary>
    /// Optional data source reference the request was generated for.
    /// </summary>
    public object? Source { get; init; }
}
