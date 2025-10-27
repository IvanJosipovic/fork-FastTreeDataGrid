using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridDefaultVariableRowHeightProvider : IFastTreeDataGridVariableRowHeightProvider
{
    public double GetRowHeight(FastTreeDataGridRow row, int index, double defaultRowHeight)
    {
        _ = index;

        if (row.Item is IFastTreeDataGridRowHeightAware aware)
        {
            return Math.Max(1d, aware.GetRowHeight(defaultRowHeight));
        }

        if (row.ValueProvider is IFastTreeDataGridRowHeightAware providerAware)
        {
            return Math.Max(1d, providerAware.GetRowHeight(defaultRowHeight));
        }

        return Math.Max(1d, defaultRowHeight);
    }
}
