using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridColumnMaterializedEventArgs : EventArgs
{
    public FastTreeDataGridColumnMaterializedEventArgs(int index, FastTreeDataGridColumnDescriptor column)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ColumnIndex = index;
        Column = column ?? throw new ArgumentNullException(nameof(column));
    }

    public int ColumnIndex { get; }

    public FastTreeDataGridColumnDescriptor Column { get; }
}
