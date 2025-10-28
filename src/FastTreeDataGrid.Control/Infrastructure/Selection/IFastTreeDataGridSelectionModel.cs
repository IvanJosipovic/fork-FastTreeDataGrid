using System;
using System.Collections.Generic;
using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridSelectionModel
{
    FastTreeDataGridSelectionMode SelectionMode { get; set; }

    IReadOnlyList<int> SelectedIndices { get; }

    int PrimaryIndex { get; }

    int AnchorIndex { get; }

    event EventHandler<FastTreeDataGridSelectionChangedEventArgs>? SelectionChanged;

    void Attach(ControlsFastTreeDataGrid owner);

    void Detach();

    void Clear();

    void SelectSingle(int index);

    void SelectRange(int anchorIndex, int endIndex, bool keepExisting);

    void Toggle(int index);

    void SetAnchor(int index);

    void CoerceSelection(int rowCount);

    void SetSelection(IEnumerable<int> indices, int? primaryIndex = null, int? anchorIndex = null);
}
