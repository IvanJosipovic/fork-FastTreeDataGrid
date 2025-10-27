using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridFuncVariableRowHeightProvider : IFastTreeDataGridVariableRowHeightProvider
{
    private readonly Func<FastTreeDataGridRow, int, double, double> _selector;

    public FastTreeDataGridFuncVariableRowHeightProvider(Func<FastTreeDataGridRow, int, double, double> selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public double GetRowHeight(FastTreeDataGridRow row, int index, double defaultRowHeight)
    {
        var value = _selector(row, index, defaultRowHeight);
        return Math.Max(1d, double.IsFinite(value) ? value : defaultRowHeight);
    }
}
