using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public readonly record struct FastTreeDataGridViewportRequest(int StartIndex, int Count, int PrefetchRadius)
{
    public bool Equals(FastTreeDataGridViewportRequest other) =>
        StartIndex == other.StartIndex && Count == other.Count && PrefetchRadius == other.PrefetchRadius;

    public override int GetHashCode() => HashCode.Combine(StartIndex, Count, PrefetchRadius);
}
