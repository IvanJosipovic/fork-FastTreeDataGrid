using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridSource
{
    event EventHandler? ResetRequested;

    int RowCount { get; }

    FastTreeDataGridRow GetRow(int index);

    void ToggleExpansion(int index);
}
