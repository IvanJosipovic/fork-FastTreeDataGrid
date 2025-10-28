using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Provides policy hooks for grouping gestures.
/// </summary>
public interface IFastTreeDataGridGroupingBehavior
{
    bool CanBeginDrag(FastTreeDataGridColumn column) => column.CanUserGroup;

    bool TryHandleDrop(FastTreeDataGridColumn column, FastTreeDataGridGroupingDropContext context);
}
