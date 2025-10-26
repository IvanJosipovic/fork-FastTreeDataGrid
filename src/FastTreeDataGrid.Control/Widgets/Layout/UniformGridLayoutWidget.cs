namespace FastTreeDataGrid.Control.Widgets;

public sealed class UniformGridLayoutWidget : PanelLayoutWidget
{
    private readonly UniformGridLayoutAdapter _adapter = new();

    public int Rows
    {
        get => _adapter.Rows;
        set => _adapter.Rows = value;
    }

    public int Columns
    {
        get => _adapter.Columns;
        set => _adapter.Columns = value;
    }

    public int FirstColumn
    {
        get => _adapter.FirstColumn;
        set => _adapter.FirstColumn = value;
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override bool SupportsSpacing => false;
}
