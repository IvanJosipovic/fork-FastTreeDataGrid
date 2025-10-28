using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridSelectionChangedEventArgs : EventArgs
{
    public FastTreeDataGridSelectionChangedEventArgs(
        IReadOnlyList<int> addedIndices,
        IReadOnlyList<int> removedIndices,
        IReadOnlyList<int> selectedIndices,
        int primaryIndex,
        int anchorIndex,
        IReadOnlyList<FastTreeDataGridCellIndex>? addedCells = null,
        IReadOnlyList<FastTreeDataGridCellIndex>? removedCells = null,
        IReadOnlyList<FastTreeDataGridCellIndex>? selectedCells = null,
        FastTreeDataGridCellIndex? primaryCell = null,
        FastTreeDataGridCellIndex? anchorCell = null)
    {
        AddedIndices = addedIndices ?? Array.Empty<int>();
        RemovedIndices = removedIndices ?? Array.Empty<int>();
        SelectedIndices = selectedIndices ?? Array.Empty<int>();
        PrimaryIndex = primaryIndex;
        AnchorIndex = anchorIndex;
        AddedCells = addedCells ?? Array.Empty<FastTreeDataGridCellIndex>();
        RemovedCells = removedCells ?? Array.Empty<FastTreeDataGridCellIndex>();
        SelectedCells = selectedCells ?? Array.Empty<FastTreeDataGridCellIndex>();
        PrimaryCell = primaryCell ?? FastTreeDataGridCellIndex.Invalid;
        AnchorCell = anchorCell ?? FastTreeDataGridCellIndex.Invalid;
    }

    public IReadOnlyList<int> AddedIndices { get; }

    public IReadOnlyList<int> RemovedIndices { get; }

    public IReadOnlyList<int> SelectedIndices { get; }

    public int PrimaryIndex { get; }

    public int AnchorIndex { get; }

    public IReadOnlyList<FastTreeDataGridCellIndex> AddedCells { get; }

    public IReadOnlyList<FastTreeDataGridCellIndex> RemovedCells { get; }

    public IReadOnlyList<FastTreeDataGridCellIndex> SelectedCells { get; }

    public FastTreeDataGridCellIndex PrimaryCell { get; }

    public FastTreeDataGridCellIndex AnchorCell { get; }
}
