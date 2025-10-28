namespace FastTreeDataGrid.Engine.Infrastructure;

public interface IFastTreeDataGridVariableRowHeightProvider
{
    double GetRowHeight(FastTreeDataGridRow row, int index, double defaultRowHeight);
}
