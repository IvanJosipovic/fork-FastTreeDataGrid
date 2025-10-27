using System;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class GridLayoutWidget : PanelLayoutWidget
{
    private readonly GridLayoutAdapter _adapter = new();

    public int Columns
    {
        get => _adapter.Columns;
        set => _adapter.Columns = value;
    }

    public int Rows
    {
        get => _adapter.Rows;
        set => _adapter.Rows = value;
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;
}
