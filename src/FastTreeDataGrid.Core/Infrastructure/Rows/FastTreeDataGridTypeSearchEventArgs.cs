using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridTypeSearchEventArgs : EventArgs
{
    public FastTreeDataGridTypeSearchEventArgs(string query, int startIndex, int totalCount)
    {
        Query = query ?? string.Empty;
        StartIndex = Math.Max(-1, startIndex);
        TotalCount = Math.Max(0, totalCount);
    }

    public string Query { get; }

    public int StartIndex { get; }

    public int TotalCount { get; }

    public int TargetIndex { get; set; } = -1;

    public bool Handled { get; set; }
}
