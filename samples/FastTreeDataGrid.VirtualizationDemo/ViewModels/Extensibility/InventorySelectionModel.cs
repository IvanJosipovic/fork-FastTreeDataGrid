using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Extensibility;

/// <summary>
/// Selection model that promotes category-aware selection semantics. Selecting a single row
/// selects the entire category, and toggling a row toggles the whole category block.
/// Demonstrates how the default selection implementation can be extended without touching control internals.
/// </summary>
public sealed class InventorySelectionModel : FastTreeDataGridSelectionModel
{
    private readonly InventoryDataService _service;

    public InventorySelectionModel(InventoryDataService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public override void SelectSingle(int index)
    {
        if (!TrySelectCategory(index, clearExisting: true))
        {
            base.SelectSingle(index);
        }
    }

    public override void SelectRange(int anchorIndex, int endIndex, bool keepExisting)
    {
        if (!TrySelectCategory(endIndex, clearExisting: !keepExisting))
        {
            base.SelectRange(anchorIndex, endIndex, keepExisting);
        }
    }

    public override void Toggle(int index)
    {
        if (!TryToggleCategory(index))
        {
            base.Toggle(index);
        }
    }

    private bool TrySelectCategory(int index, bool clearExisting)
    {
        if (!_service.TryGetRecord(index, out var record))
        {
            return false;
        }

        var members = _service.GetIndicesForCategory(record.Category);
        if (members.Count == 0)
        {
            return false;
        }

        var selection = clearExisting ? new SortedSet<int>() : new SortedSet<int>(SelectedIndices);
        foreach (var member in members)
        {
            selection.Add(member);
        }

        var anchor = members[0];
        SetSelection(selection, primaryIndex: index, anchorIndex: anchor);
        return true;
    }

    private bool TryToggleCategory(int index)
    {
        if (!_service.TryGetRecord(index, out var record))
        {
            return false;
        }

        var members = _service.GetIndicesForCategory(record.Category);
        if (members.Count == 0)
        {
            return false;
        }

        var current = new SortedSet<int>(SelectedIndices);
        var allSelected = members.All(current.Contains);

        if (allSelected)
        {
            foreach (var member in members)
            {
                current.Remove(member);
            }

            var newPrimary = current.Contains(PrimaryIndex) ? PrimaryIndex : (current.Count > 0 ? current.Max : -1);
            var newAnchor = current.Contains(AnchorIndex) ? AnchorIndex : newPrimary;
            SetSelection(current, newPrimary, newAnchor);
        }
        else
        {
            foreach (var member in members)
            {
                current.Add(member);
            }

            SetSelection(current, index, members[0]);
        }

        return true;
    }
}
