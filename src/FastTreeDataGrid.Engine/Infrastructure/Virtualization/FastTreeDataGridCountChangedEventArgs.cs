using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridCountChangedEventArgs : EventArgs
{
    public FastTreeDataGridCountChangedEventArgs(int newCount)
    {
        if (newCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCount));
        }

        NewCount = newCount;
    }

    public int NewCount { get; }
}
