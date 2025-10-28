using System.Collections.Generic;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridCellSelectionModel : IFastTreeDataGridSelectionModel
{
    IReadOnlyList<FastTreeDataGridCellIndex> SelectedCells { get; }

    FastTreeDataGridCellIndex PrimaryCell { get; }

    FastTreeDataGridCellIndex AnchorCell { get; }

    void SelectCell(FastTreeDataGridCellIndex cell);

    void SelectCellRange(FastTreeDataGridCellIndex anchor, FastTreeDataGridCellIndex end, bool keepExisting);

    void ToggleCell(FastTreeDataGridCellIndex cell);

    void SetCellAnchor(FastTreeDataGridCellIndex cell);

    void SetCellSelection(IEnumerable<FastTreeDataGridCellIndex> cells, FastTreeDataGridCellIndex? primaryCell = null, FastTreeDataGridCellIndex? anchorCell = null);
}
