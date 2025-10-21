using System;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Controls;

public sealed class FastTreeDataGridSortEventArgs : EventArgs
{
    public FastTreeDataGridSortEventArgs(FastTreeDataGridColumn column, int columnIndex, FastTreeDataGridSortDirection direction)
    {
        Column = column ?? throw new ArgumentNullException(nameof(column));
        ColumnIndex = columnIndex;
        Direction = direction;
    }

    public FastTreeDataGridColumn Column { get; }

    public int ColumnIndex { get; }

    public FastTreeDataGridSortDirection Direction { get; }
}
