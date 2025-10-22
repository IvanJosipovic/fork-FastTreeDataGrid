using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridRowMaterializedEventArgs : EventArgs
{
    public FastTreeDataGridRowMaterializedEventArgs(int index, FastTreeDataGridRow row)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        Row = row ?? throw new ArgumentNullException(nameof(row));
    }

    public int Index { get; }

    public FastTreeDataGridRow Row { get; }
}
