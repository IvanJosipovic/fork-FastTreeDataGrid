using System;
using FastTreeDataGrid.Engine.Infrastructure;

using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridUniformRowLayout : IFastTreeDataGridRowLayout
{
    private double _currentRowHeight = 28d;

    public void Attach(ControlsFastTreeDataGrid owner)
    {
        _ = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Detach()
    {
    }

    public void Bind(IFastTreeDataGridSource? source)
    {
        _ = source;
    }

    public void Reset()
    {
        // No state to reset for uniform layout.
    }

    public RowLayoutViewport GetVisibleRange(double verticalOffset, double viewportHeight, double defaultRowHeight, int totalRows, int buffer)
    {
        if (totalRows == 0)
        {
            return RowLayoutViewport.Empty;
        }

        _currentRowHeight = Math.Max(1d, defaultRowHeight);

        var firstIndex = Math.Clamp((int)Math.Floor(verticalOffset / _currentRowHeight), 0, Math.Max(0, totalRows - 1));
        var visibleCount = (int)Math.Ceiling(viewportHeight / _currentRowHeight) + buffer;
        var lastIndexExclusive = Math.Min(totalRows, firstIndex + visibleCount);
        var top = firstIndex * _currentRowHeight;

        return new RowLayoutViewport(firstIndex, lastIndexExclusive, top);
    }

    public double GetRowHeight(int rowIndex, FastTreeDataGridRow row, double defaultRowHeight)
    {
        _ = rowIndex;
        _ = row;
        _currentRowHeight = Math.Max(1d, defaultRowHeight);
        return _currentRowHeight;
    }

    public double GetRowTop(int rowIndex)
    {
        return rowIndex <= 0 ? 0 : rowIndex * _currentRowHeight;
    }

    public double GetTotalHeight(double viewportHeight, double defaultRowHeight, int totalRows)
    {
        var rowHeight = Math.Max(1d, defaultRowHeight);
        var total = totalRows * rowHeight;
        return Math.Max(total, viewportHeight);
    }

    public void InvalidateRow(int rowIndex)
    {
        _ = rowIndex;
    }
}
