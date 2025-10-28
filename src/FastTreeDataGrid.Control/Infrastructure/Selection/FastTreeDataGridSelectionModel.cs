using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.Engine.Infrastructure;
using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

public class FastTreeDataGridSelectionModel : IFastTreeDataGridSelectionModel
{
    private readonly SortedSet<int> _selected = new();
    private IReadOnlyList<int> _cachedSelection = Array.Empty<int>();
    private ControlsFastTreeDataGrid? _owner;
    private FastTreeDataGridSelectionMode _selectionMode = FastTreeDataGridSelectionMode.Extended;
    private int _primaryIndex = -1;
    private int _anchorIndex = -1;

    public event EventHandler<FastTreeDataGridSelectionChangedEventArgs>? SelectionChanged;

    public virtual FastTreeDataGridSelectionUnit SelectionUnit => FastTreeDataGridSelectionUnit.Row;

    public FastTreeDataGridSelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            if (_selectionMode == value)
            {
                return;
            }

            _selectionMode = value;
            if (_selectionMode == FastTreeDataGridSelectionMode.Single && _selected.Count > 1)
            {
                var retained = _primaryIndex >= 0 ? _primaryIndex : _selected.Min;
                SelectSingle(retained);
            }
            else if (_selectionMode == FastTreeDataGridSelectionMode.None)
            {
                Clear();
            }
        }
    }

    public IReadOnlyList<int> SelectedIndices => _cachedSelection;

    public int PrimaryIndex => _primaryIndex;

    public int AnchorIndex => _anchorIndex;

    public virtual void Attach(ControlsFastTreeDataGrid owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public virtual void Detach()
    {
        _owner = null;
        Clear();
    }

    public virtual void Clear()
    {
        if (_selected.Count == 0 && _primaryIndex < 0 && _anchorIndex < 0)
        {
            return;
        }

        var removed = _selected.Count > 0 ? _selected.ToArray() : Array.Empty<int>();
        _selected.Clear();
        _cachedSelection = Array.Empty<int>();
        _primaryIndex = -1;
        _anchorIndex = -1;

        RaiseSelectionChanged(
            Array.Empty<int>(),
            removed,
            _cachedSelection,
            _primaryIndex,
            _anchorIndex,
            Array.Empty<FastTreeDataGridCellIndex>(),
            Array.Empty<FastTreeDataGridCellIndex>(),
            Array.Empty<FastTreeDataGridCellIndex>(),
            FastTreeDataGridCellIndex.Invalid,
            FastTreeDataGridCellIndex.Invalid);
    }

    public virtual void SelectSingle(int index)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            Clear();
            return;
        }

        if (index < 0)
        {
            Clear();
            return;
        }

        if (_selected.Count == 1 && _selected.Contains(index) && _primaryIndex == index && _anchorIndex == index)
        {
            return;
        }

        var newSelection = new SortedSet<int> { index };
        UpdateSelection(newSelection, index, index);
    }

    public virtual void SelectRange(int anchorIndex, int endIndex, bool keepExisting)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            SelectSingle(endIndex);
            return;
        }

        if (anchorIndex < 0)
        {
            SelectSingle(endIndex);
            return;
        }

        var start = Math.Min(anchorIndex, endIndex);
        var end = Math.Max(anchorIndex, endIndex);
        var newSelection = keepExisting ? new SortedSet<int>(_selected) : new SortedSet<int>();

        for (var i = start; i <= end; i++)
        {
            newSelection.Add(i);
        }

        UpdateSelection(newSelection, endIndex, anchorIndex);
    }

    public virtual void Toggle(int index)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            if (_selected.Contains(index))
            {
                Clear();
            }
            else
            {
                SelectSingle(index);
            }

            return;
        }

        var newSelection = new SortedSet<int>(_selected);
        if (newSelection.Remove(index))
        {
            var newPrimary = newSelection.Contains(_primaryIndex)
                ? _primaryIndex
                : (newSelection.Count > 0 ? newSelection.Max : -1);
            var newAnchor = newSelection.Contains(_anchorIndex)
                ? _anchorIndex
                : newPrimary;

            UpdateSelection(newSelection, newPrimary, newAnchor);
        }
        else
        {
            newSelection.Add(index);
            UpdateSelection(newSelection, index, index);
        }
    }

    public virtual void SetAnchor(int index)
    {
        _anchorIndex = index;
    }

    public virtual void SetSelection(IEnumerable<int> indices, int? primaryIndex = null, int? anchorIndex = null)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            Clear();
            return;
        }

        if (indices is null)
        {
            Clear();
            return;
        }

        var newSelection = new SortedSet<int>();
        foreach (var index in indices)
        {
            if (index < 0)
            {
                continue;
            }

            newSelection.Add(index);

            if (_selectionMode == FastTreeDataGridSelectionMode.Single)
            {
                break;
            }
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            if (newSelection.Count == 0)
            {
                Clear();
                return;
            }

            var retained = primaryIndex.HasValue && newSelection.Contains(primaryIndex.Value)
                ? primaryIndex.Value
                : (newSelection.Contains(_primaryIndex) ? _primaryIndex : newSelection.Max);

            newSelection.Clear();
            newSelection.Add(retained);
            primaryIndex = retained;
            anchorIndex = retained;
        }

        if (newSelection.Count == 0)
        {
            Clear();
            return;
        }

        var resolvedPrimary = primaryIndex.HasValue && newSelection.Contains(primaryIndex.Value)
            ? primaryIndex.Value
            : (newSelection.Contains(_primaryIndex) ? _primaryIndex : (int?)null);
        if (!resolvedPrimary.HasValue)
        {
            resolvedPrimary = newSelection.Max;
        }

        var resolvedAnchor = anchorIndex.HasValue && newSelection.Contains(anchorIndex.Value)
            ? anchorIndex.Value
            : (newSelection.Contains(_anchorIndex) ? _anchorIndex : (int?)null);
        if (!resolvedAnchor.HasValue)
        {
            resolvedAnchor = newSelection.Min;
        }

        UpdateSelection(newSelection, resolvedPrimary.Value, resolvedAnchor.Value);
    }

    public virtual void CoerceSelection(int rowCount)
    {
        if (rowCount < 0)
        {
            rowCount = 0;
        }

        if (_selected.Count == 0)
        {
            if (rowCount == 0)
            {
                Clear();
            }

            return;
        }

        var newSelection = new SortedSet<int>();
        foreach (var index in _selected)
        {
            if (index >= 0 && index < rowCount)
            {
                newSelection.Add(index);
            }
        }

        if (newSelection.Count == _selected.Count)
        {
            return;
        }

        var newPrimary = newSelection.Contains(_primaryIndex)
            ? _primaryIndex
            : (newSelection.Count > 0 ? newSelection.Max : -1);
        var newAnchor = newSelection.Contains(_anchorIndex)
            ? _anchorIndex
            : newPrimary;

        UpdateSelection(newSelection, newPrimary, newAnchor);
    }

    protected virtual void UpdateSelection(SortedSet<int> newSelection, int newPrimaryIndex, int? newAnchorIndex)
    {
        var added = new List<int>();
        var removed = new List<int>();

        foreach (var index in newSelection)
        {
            if (!_selected.Contains(index))
            {
                added.Add(index);
            }
        }

        foreach (var index in _selected)
        {
            if (!newSelection.Contains(index))
            {
                removed.Add(index);
            }
        }

        var anchor = newAnchorIndex ?? _anchorIndex;
        var primary = newPrimaryIndex;

        if (added.Count == 0 && removed.Count == 0 && primary == _primaryIndex && anchor == _anchorIndex)
        {
            return;
        }

        _selected.Clear();
        foreach (var index in newSelection)
        {
            _selected.Add(index);
        }

        if (primary >= 0 && !_selected.Contains(primary))
        {
            _selected.Add(primary);
            if (!added.Contains(primary))
            {
                added.Add(primary);
            }
        }

        if (_selected.Count == 0)
        {
            _cachedSelection = Array.Empty<int>();
            _primaryIndex = -1;
            _anchorIndex = -1;
        }
        else
        {
            _cachedSelection = _selected.ToList();
            _primaryIndex = primary >= 0 && _selected.Contains(primary) ? primary : _selected.Max;

            if (anchor >= 0 && _selected.Contains(anchor))
            {
                _anchorIndex = anchor;
            }
            else
            {
                _anchorIndex = _primaryIndex;
            }
        }

        var addedArray = added.Count == 0 ? Array.Empty<int>() : added.ToArray();
        var removedArray = removed.Count == 0 ? Array.Empty<int>() : removed.ToArray();
        RaiseSelectionChanged(addedArray, removedArray, _cachedSelection, _primaryIndex, _anchorIndex, Array.Empty<FastTreeDataGridCellIndex>(), Array.Empty<FastTreeDataGridCellIndex>(), Array.Empty<FastTreeDataGridCellIndex>(), FastTreeDataGridCellIndex.Invalid, FastTreeDataGridCellIndex.Invalid);
    }

    protected void RaiseSelectionChanged(
        IReadOnlyList<int> addedIndices,
        IReadOnlyList<int> removedIndices,
        IReadOnlyList<int> selectedIndices,
        int primaryIndex,
        int anchorIndex,
        IReadOnlyList<FastTreeDataGridCellIndex> addedCells,
        IReadOnlyList<FastTreeDataGridCellIndex> removedCells,
        IReadOnlyList<FastTreeDataGridCellIndex> selectedCells,
        FastTreeDataGridCellIndex primaryCell,
        FastTreeDataGridCellIndex anchorCell)
    {
        SelectionChanged?.Invoke(this, new FastTreeDataGridSelectionChangedEventArgs(
            addedIndices,
            removedIndices,
            selectedIndices,
            primaryIndex,
            anchorIndex,
            addedCells,
            removedCells,
            selectedCells,
            primaryCell,
            anchorCell));
    }
}
