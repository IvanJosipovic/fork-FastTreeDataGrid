using System;
using System.Collections.Generic;
using Avalonia.Input;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Controls;

public sealed class FastTreeDataGridSortEventArgs : EventArgs
{
    public FastTreeDataGridSortEventArgs(
        FastTreeDataGridColumn column,
        int columnIndex,
        FastTreeDataGridSortDirection direction,
        KeyModifiers modifiers,
        IReadOnlyList<FastTreeDataGridSortDescription> descriptions)
    {
        Column = column ?? throw new ArgumentNullException(nameof(column));
        ColumnIndex = columnIndex;
        Direction = direction;
        Modifiers = modifiers;
        Descriptions = descriptions ?? Array.Empty<FastTreeDataGridSortDescription>();
    }

    public FastTreeDataGridColumn Column { get; }

    public int ColumnIndex { get; }

    public FastTreeDataGridSortDirection Direction { get; }

    public KeyModifiers Modifiers { get; }

    public IReadOnlyList<FastTreeDataGridSortDescription> Descriptions { get; }
}
