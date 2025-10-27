using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridInvalidationRequest
{
    public FastTreeDataGridInvalidationRequest(
        FastTreeDataGridInvalidationKind kind,
        int startIndex = 0,
        int count = 0)
    {
        if (kind == FastTreeDataGridInvalidationKind.Range && count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (kind == FastTreeDataGridInvalidationKind.Range && startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        Kind = kind;
        StartIndex = startIndex;
        Count = count;
    }

    public FastTreeDataGridInvalidationKind Kind { get; }

    public int StartIndex { get; }

    public int Count { get; }

    public bool HasRange => Kind == FastTreeDataGridInvalidationKind.Range && Count > 0;
}
