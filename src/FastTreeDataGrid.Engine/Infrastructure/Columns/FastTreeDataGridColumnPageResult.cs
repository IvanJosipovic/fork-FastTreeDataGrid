using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridColumnPageResult
{
    public static FastTreeDataGridColumnPageResult Empty { get; } = new(Array.Empty<FastTreeDataGridColumnDescriptor>(), Array.Empty<int>(), completion: null, cancellation: null);

    public FastTreeDataGridColumnPageResult(
        IReadOnlyList<FastTreeDataGridColumnDescriptor> columns,
        IReadOnlyList<int>? placeholderIndices,
        Task? completion,
        CancellationTokenSource? cancellation)
    {
        Columns = columns ?? Array.Empty<FastTreeDataGridColumnDescriptor>();
        PlaceholderIndices = placeholderIndices ?? Array.Empty<int>();
        Completion = completion;
        Cancellation = cancellation;
    }

    public IReadOnlyList<FastTreeDataGridColumnDescriptor> Columns { get; }

    public IReadOnlyList<int> PlaceholderIndices { get; }

    public Task? Completion { get; }

    public CancellationTokenSource? Cancellation { get; }

    public bool IsFullyMaterialized => PlaceholderIndices.Count == 0 && Completion is null;
}
