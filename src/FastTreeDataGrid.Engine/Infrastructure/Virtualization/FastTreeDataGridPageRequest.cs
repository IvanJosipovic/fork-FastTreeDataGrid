using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridPageRequest
{
    public FastTreeDataGridPageRequest(int startIndex, int count, int prefetchRadius = 0, FastTreeDataGridPagePriority priority = FastTreeDataGridPagePriority.Normal)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (prefetchRadius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(prefetchRadius));
        }

        StartIndex = startIndex;
        Count = count;
        PrefetchRadius = prefetchRadius;
        Priority = priority;
    }

    public int StartIndex { get; }

    public int Count { get; }

    public int PrefetchRadius { get; }

    public FastTreeDataGridPagePriority Priority { get; }
}
