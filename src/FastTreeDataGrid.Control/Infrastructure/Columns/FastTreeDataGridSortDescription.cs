using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Infrastructure;

public readonly struct FastTreeDataGridSortDescription
{
    public FastTreeDataGridSortDescription(FastTreeDataGridColumn column, FastTreeDataGridSortDirection direction, int order)
    {
        Column = column;
        Direction = direction;
        Order = order;
    }

    public FastTreeDataGridColumn Column { get; }

    public FastTreeDataGridSortDirection Direction { get; }

    public int Order { get; }
}
