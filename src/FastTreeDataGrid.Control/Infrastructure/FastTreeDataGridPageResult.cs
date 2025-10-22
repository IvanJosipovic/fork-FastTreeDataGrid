using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridPageResult
{
    public static FastTreeDataGridPageResult Empty { get; } = new(Array.Empty<FastTreeDataGridRow>(), Array.Empty<int>(), completion: null, cancellation: null);

    public FastTreeDataGridPageResult(
        IReadOnlyList<FastTreeDataGridRow> rows,
        IReadOnlyList<int>? placeholderIndices,
        Task? completion,
        CancellationTokenSource? cancellation)
    {
        Rows = rows ?? Array.Empty<FastTreeDataGridRow>();
        PlaceholderIndices = placeholderIndices ?? Array.Empty<int>();
        Completion = completion;
        Cancellation = cancellation;
    }

    public IReadOnlyList<FastTreeDataGridRow> Rows { get; }

    public IReadOnlyList<int> PlaceholderIndices { get; }

    public Task? Completion { get; }

    public CancellationTokenSource? Cancellation { get; }

    public bool IsFullyMaterialized => PlaceholderIndices.Count == 0 && Completion is null;
}
