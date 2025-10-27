using System;
using System.Collections.Generic;

using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridVariableRowLayout : IFastTreeDataGridRowLayout
{
    private readonly IFastTreeDataGridVariableRowHeightProvider _heightProvider;
    private ControlsFastTreeDataGrid? _owner;
    private IFastTreeDataGridSource? _source;
    private readonly List<double> _heights = new();
    private readonly List<double> _cumulativeHeights = new();
    private double _lastDefaultHeight = 28d;

    public FastTreeDataGridVariableRowLayout()
        : this(new FastTreeDataGridDefaultVariableRowHeightProvider())
    {
    }

    public FastTreeDataGridVariableRowLayout(IFastTreeDataGridVariableRowHeightProvider heightProvider)
    {
        _heightProvider = heightProvider ?? throw new ArgumentNullException(nameof(heightProvider));
    }

    public void Attach(ControlsFastTreeDataGrid owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Detach()
    {
        _owner = null;
        _source = null;
        Reset();
    }

    public void Bind(IFastTreeDataGridSource? source)
    {
        _source = source;
        Reset();
    }

    public void Reset()
    {
        _heights.Clear();
        _cumulativeHeights.Clear();
    }

    public RowLayoutViewport GetVisibleRange(double verticalOffset, double viewportHeight, double defaultRowHeight, int totalRows, int buffer)
    {
        if (_source is null || totalRows == 0)
        {
            return RowLayoutViewport.Empty;
        }

        _lastDefaultHeight = Math.Max(1d, defaultRowHeight);

        var firstIndex = FindRowIndexAtOffset(verticalOffset, totalRows);

        if (firstIndex >= totalRows)
        {
            firstIndex = Math.Max(0, totalRows - 1);
        }

        EnsureRow(firstIndex);
        var firstTop = firstIndex == 0 ? 0 : _cumulativeHeights[firstIndex - 1];
        var targetBottom = verticalOffset + viewportHeight;
        var lastIndexExclusive = Math.Max(firstIndex, firstIndex);
        var currentBottom = firstTop;

        while (lastIndexExclusive < totalRows && currentBottom < targetBottom)
        {
            EnsureRow(lastIndexExclusive);
            currentBottom = _cumulativeHeights[lastIndexExclusive];
            lastIndexExclusive++;
        }

        lastIndexExclusive = Math.Min(totalRows, lastIndexExclusive + buffer);

        return new RowLayoutViewport(firstIndex, lastIndexExclusive, firstTop);
    }

    public double GetRowHeight(int rowIndex, FastTreeDataGridRow row, double defaultRowHeight)
    {
        if (_source is null)
        {
            return Math.Max(1d, defaultRowHeight);
        }

        _lastDefaultHeight = Math.Max(1d, defaultRowHeight);

        if (rowIndex < 0)
        {
            return _lastDefaultHeight;
        }

        EnsureRow(rowIndex - 1);

        if (_heights.Count > rowIndex)
        {
            return _heights[rowIndex];
        }

        var height = Math.Max(1d, _heightProvider.GetRowHeight(row, rowIndex, _lastDefaultHeight));
        AppendHeight(height);
        return height;
    }

    public double GetRowTop(int rowIndex)
    {
        if (rowIndex <= 0)
        {
            return 0;
        }

        EnsureRow(rowIndex - 1);
        return _cumulativeHeights.Count > rowIndex - 1 ? _cumulativeHeights[rowIndex - 1] : 0;
    }

    public double GetTotalHeight(double viewportHeight, double defaultRowHeight, int totalRows)
    {
        if (_source is null || totalRows <= 0)
        {
            return Math.Max(0, viewportHeight);
        }

        _lastDefaultHeight = Math.Max(1d, defaultRowHeight);

        EnsureRow(totalRows - 1);

        var total = _cumulativeHeights.Count > 0 ? _cumulativeHeights[^1] : 0;
        return Math.Max(total, viewportHeight);
    }

    public void InvalidateRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _heights.Count)
        {
            return;
        }

        var truncated = rowIndex;
        if (truncated < _heights.Count)
        {
            _heights.RemoveRange(truncated, _heights.Count - truncated);
        }

        if (truncated < _cumulativeHeights.Count)
        {
            _cumulativeHeights.RemoveRange(truncated, _cumulativeHeights.Count - truncated);
        }
    }

    private void EnsureRow(int rowIndex)
    {
        if (_source is null || rowIndex < 0)
        {
            return;
        }

        var target = Math.Min(rowIndex, Math.Max(0, _source.RowCount - 1));
        for (var i = _heights.Count; i <= target; i++)
        {
            if (i >= _source.RowCount)
            {
                break;
            }

            var row = _source.GetRow(i);
            var height = Math.Max(1d, _heightProvider.GetRowHeight(row, i, _lastDefaultHeight));
            AppendHeight(height);
        }
    }

    private void AppendHeight(double height)
    {
        var lastTotal = _cumulativeHeights.Count > 0 ? _cumulativeHeights[^1] : 0;
        _heights.Add(height);
        _cumulativeHeights.Add(lastTotal + height);
    }

    private int FindRowIndexAtOffset(double offset, int totalRows)
    {
        if (offset <= 0 || totalRows == 0)
        {
            return 0;
        }

        EnsureOffset(offset, totalRows);

        if (_cumulativeHeights.Count == 0)
        {
            return 0;
        }

        var index = _cumulativeHeights.BinarySearch(offset);
        if (index < 0)
        {
            index = ~index;
        }

        if (index >= totalRows)
        {
            index = totalRows - 1;
        }

        return Math.Max(0, index);
    }

    private void EnsureOffset(double offset, int totalRows)
    {
        if (_source is null || totalRows <= 0)
        {
            return;
        }

        while (_cumulativeHeights.Count < totalRows)
        {
            if (_cumulativeHeights.Count > 0 && _cumulativeHeights[^1] >= offset)
            {
                break;
            }

            var nextIndex = _heights.Count;
            if (nextIndex >= totalRows)
            {
                break;
            }

            var row = _source.GetRow(nextIndex);
            var height = Math.Max(1d, _heightProvider.GetRowHeight(row, nextIndex, _lastDefaultHeight));
            AppendHeight(height);
        }
    }
}
