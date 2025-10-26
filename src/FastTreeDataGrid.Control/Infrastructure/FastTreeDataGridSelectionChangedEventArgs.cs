using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridSelectionChangedEventArgs : EventArgs
{
    public FastTreeDataGridSelectionChangedEventArgs(
        IReadOnlyList<int> addedIndices,
        IReadOnlyList<int> removedIndices,
        IReadOnlyList<int> selectedIndices,
        int primaryIndex,
        int anchorIndex)
    {
        AddedIndices = addedIndices ?? Array.Empty<int>();
        RemovedIndices = removedIndices ?? Array.Empty<int>();
        SelectedIndices = selectedIndices ?? Array.Empty<int>();
        PrimaryIndex = primaryIndex;
        AnchorIndex = anchorIndex;
    }

    public IReadOnlyList<int> AddedIndices { get; }

    public IReadOnlyList<int> RemovedIndices { get; }

    public IReadOnlyList<int> SelectedIndices { get; }

    public int PrimaryIndex { get; }

    public int AnchorIndex { get; }
}
