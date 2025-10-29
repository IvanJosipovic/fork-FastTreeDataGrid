using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public readonly record struct FastTreeDataGridColumnViewportRequest(int StartIndex, int Count, int PrefetchRadius)
{
    public bool Equals(FastTreeDataGridColumnViewportRequest other) =>
        StartIndex == other.StartIndex && Count == other.Count && PrefetchRadius == other.PrefetchRadius;

    public override int GetHashCode() => HashCode.Combine(StartIndex, Count, PrefetchRadius);
}
