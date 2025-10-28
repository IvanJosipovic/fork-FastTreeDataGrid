using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.Engine.Infrastructure;
using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

public class FastTreeDataGridCellSelectionModel : IFastTreeDataGridCellSelectionModel
{
    private readonly HashSet<FastTreeDataGridCellIndex> _selectedCells = new();
    private IReadOnlyList<FastTreeDataGridCellIndex> _cachedCells = Array.Empty<FastTreeDataGridCellIndex>();
    private IReadOnlyList<int> _cachedRows = Array.Empty<int>();
    private ControlsFastTreeDataGrid? _owner;
    private FastTreeDataGridSelectionMode _selectionMode = FastTreeDataGridSelectionMode.Extended;
    private FastTreeDataGridCellIndex _primaryCell = FastTreeDataGridCellIndex.Invalid;
    private FastTreeDataGridCellIndex _anchorCell = FastTreeDataGridCellIndex.Invalid;
    private int _primaryIndex = -1;
    private int _anchorIndex = -1;

    public event EventHandler<FastTreeDataGridSelectionChangedEventArgs>? SelectionChanged;

    public FastTreeDataGridSelectionUnit SelectionUnit => FastTreeDataGridSelectionUnit.Cell;

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
            if (_selectionMode == FastTreeDataGridSelectionMode.Single && _selectedCells.Count > 1)
            {
                if (_primaryCell.IsValid)
                {
                    SelectCell(_primaryCell);
                }
                else if (_selectedCells.Count > 0)
                {
                    SelectCell(_selectedCells.First());
                }
                else
                {
                    Clear();
                }
            }
            else if (_selectionMode == FastTreeDataGridSelectionMode.None)
            {
                Clear();
            }
        }
    }

    public IReadOnlyList<int> SelectedIndices => _cachedRows;

    public IReadOnlyList<FastTreeDataGridCellIndex> SelectedCells => _cachedCells;

    public int PrimaryIndex => _primaryIndex;

    public int AnchorIndex => _anchorIndex;

    public FastTreeDataGridCellIndex PrimaryCell => _primaryCell;

    public FastTreeDataGridCellIndex AnchorCell => _anchorCell;

    public void Attach(ControlsFastTreeDataGrid owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Detach()
    {
        _owner = null;
        Clear();
    }

    public void Clear()
    {
        if (_selectedCells.Count == 0 && !_primaryCell.IsValid && !_anchorCell.IsValid)
        {
            return;
        }

        var removedCells = _selectedCells.Count > 0 ? _selectedCells.ToArray() : Array.Empty<FastTreeDataGridCellIndex>();
        var removedRows = _cachedRows.Count > 0 ? _cachedRows.ToArray() : Array.Empty<int>();

        _selectedCells.Clear();
        _cachedCells = Array.Empty<FastTreeDataGridCellIndex>();
        _cachedRows = Array.Empty<int>();
        _primaryCell = FastTreeDataGridCellIndex.Invalid;
        _anchorCell = FastTreeDataGridCellIndex.Invalid;
        _primaryIndex = -1;
        _anchorIndex = -1;

        RaiseSelectionChanged(
            Array.Empty<int>(),
            removedRows,
            _cachedRows,
            _primaryIndex,
            _anchorIndex,
            Array.Empty<FastTreeDataGridCellIndex>(),
            removedCells,
            _cachedCells,
            _primaryCell,
            _anchorCell);
    }

    public void SelectSingle(int index)
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

        var column = ResolvePreferredColumn();
        SelectCell(new FastTreeDataGridCellIndex(index, column));
    }

    public void SelectRange(int anchorIndex, int endIndex, bool keepExisting)
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

        var column = ResolvePreferredColumn();
        var anchorCell = new FastTreeDataGridCellIndex(anchorIndex, column);
        var endCell = new FastTreeDataGridCellIndex(endIndex, column);
        SelectCellRange(anchorCell, endCell, keepExisting);
    }

    public void Toggle(int index)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        var column = ResolvePreferredColumn();
        ToggleCell(new FastTreeDataGridCellIndex(index, column));
    }

    public void SetAnchor(int index)
    {
        var column = ResolvePreferredColumn();
        SetCellAnchor(new FastTreeDataGridCellIndex(index, column));
    }

    public void CoerceSelection(int rowCount)
    {
        if (rowCount < 0)
        {
            rowCount = 0;
        }

        if (_selectedCells.Count == 0)
        {
            return;
        }

        var removed = _selectedCells.RemoveWhere(cell => cell.RowIndex < 0 || cell.RowIndex >= rowCount || cell.ColumnIndex < 0);
        if (removed > 0)
        {
            UpdateCaches();
            RaiseSelectionChanged(
                Array.Empty<int>(),
                Array.Empty<int>(),
                _cachedRows,
                _primaryIndex,
                _anchorIndex,
                Array.Empty<FastTreeDataGridCellIndex>(),
                Array.Empty<FastTreeDataGridCellIndex>(),
                _cachedCells,
                _primaryCell,
                _anchorCell);
        }
    }

    public void SetSelection(IEnumerable<int> indices, int? primaryIndex = null, int? anchorIndex = null)
    {
        if (indices is null)
        {
            Clear();
            return;
        }

        var column = ResolvePreferredColumn();
        var cells = indices.Select(i => new FastTreeDataGridCellIndex(i, column));
        FastTreeDataGridCellIndex? primaryCell = primaryIndex.HasValue ? new FastTreeDataGridCellIndex(primaryIndex.Value, column) : null;
        FastTreeDataGridCellIndex? anchorCell = anchorIndex.HasValue ? new FastTreeDataGridCellIndex(anchorIndex.Value, column) : null;
        SetCellSelection(cells, primaryCell, anchorCell);
    }

    public void SelectCell(FastTreeDataGridCellIndex cell)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        if (!cell.IsValid)
        {
            Clear();
            return;
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            var newSelection = new HashSet<FastTreeDataGridCellIndex> { cell };
            UpdateSelection(newSelection, cell, cell);
            return;
        }

        var selection = new HashSet<FastTreeDataGridCellIndex>(_selectedCells) { cell };
        UpdateSelection(selection, cell, cell);
    }

    public void SelectCellRange(FastTreeDataGridCellIndex anchor, FastTreeDataGridCellIndex end, bool keepExisting)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        if (!anchor.IsValid)
        {
            SelectCell(end);
            return;
        }

        if (!end.IsValid)
        {
            end = anchor;
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            SelectCell(end);
            return;
        }

        var selection = keepExisting ? new HashSet<FastTreeDataGridCellIndex>(_selectedCells) : new HashSet<FastTreeDataGridCellIndex>();
        foreach (var cell in EnumerateRectangle(anchor, end))
        {
            selection.Add(cell);
        }

        UpdateSelection(selection, end, anchor);
    }

    public void ToggleCell(FastTreeDataGridCellIndex cell)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        if (!cell.IsValid)
        {
            return;
        }

        var selection = new HashSet<FastTreeDataGridCellIndex>(_selectedCells);
        if (selection.Contains(cell))
        {
            selection.Remove(cell);
        }
        else
        {
            if (_selectionMode == FastTreeDataGridSelectionMode.Single)
            {
                selection.Clear();
            }

            selection.Add(cell);
        }

        var primary = selection.Contains(cell) ? cell : GetFirstCell(selection);
        var anchor = _anchorCell.IsValid ? _anchorCell : primary;
        UpdateSelection(selection, primary, anchor);
    }

    public void SetCellAnchor(FastTreeDataGridCellIndex cell)
    {
        if (!cell.IsValid)
        {
            _anchorCell = FastTreeDataGridCellIndex.Invalid;
            _anchorIndex = -1;
            return;
        }

        _anchorCell = cell;
        _anchorIndex = cell.RowIndex;
    }

    public void SetCellSelection(IEnumerable<FastTreeDataGridCellIndex> cells, FastTreeDataGridCellIndex? primaryCell = null, FastTreeDataGridCellIndex? anchorCell = null)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            Clear();
            return;
        }

        var newSelection = new HashSet<FastTreeDataGridCellIndex>();
        foreach (var cell in cells ?? Array.Empty<FastTreeDataGridCellIndex>())
        {
            if (cell.IsValid)
            {
                newSelection.Add(cell);
                if (_selectionMode == FastTreeDataGridSelectionMode.Single && newSelection.Count > 1)
                {
                    break;
                }
            }
        }

        FastTreeDataGridCellIndex resolvedPrimary = FastTreeDataGridCellIndex.Invalid;
        if (primaryCell.HasValue && primaryCell.Value.IsValid && newSelection.Contains(primaryCell.Value))
        {
            resolvedPrimary = primaryCell.Value;
        }
        else if (newSelection.Count > 0)
        {
            resolvedPrimary = newSelection.First();
        }

        FastTreeDataGridCellIndex resolvedAnchor = FastTreeDataGridCellIndex.Invalid;
        if (anchorCell.HasValue && anchorCell.Value.IsValid && newSelection.Contains(anchorCell.Value))
        {
            resolvedAnchor = anchorCell.Value;
        }
        else
        {
            resolvedAnchor = resolvedPrimary;
        }

        UpdateSelection(newSelection, resolvedPrimary, resolvedAnchor);
    }

    private void UpdateSelection(HashSet<FastTreeDataGridCellIndex> newSelection, FastTreeDataGridCellIndex primary, FastTreeDataGridCellIndex anchor)
    {
        if (_selectionMode == FastTreeDataGridSelectionMode.Single && newSelection.Count > 1)
        {
            var first = primary.IsValid ? primary : newSelection.First();
            newSelection.Clear();
            newSelection.Add(first);
            primary = first;
            anchor = first;
        }

        var previousCells = _selectedCells.ToArray();
        var previousRows = new HashSet<int>(_cachedRows);

        var addedCells = newSelection.Except(_selectedCells).ToArray();
        var removedCells = _selectedCells.Except(newSelection).ToArray();

        var cellsChanged = addedCells.Length > 0 || removedCells.Length > 0;
        var primaryChanged = !_primaryCell.Equals(primary) || !_anchorCell.Equals(anchor);

        if (!cellsChanged && !primaryChanged)
        {
            return;
        }

        _selectedCells.Clear();
        foreach (var cell in newSelection)
        {
            _selectedCells.Add(cell);
        }

        _primaryCell = primary.IsValid && _selectedCells.Contains(primary)
            ? primary
            : (_selectedCells.Count > 0 ? _selectedCells.First() : FastTreeDataGridCellIndex.Invalid);

        _anchorCell = anchor.IsValid && _selectedCells.Contains(anchor)
            ? anchor
            : _primaryCell;

        _primaryIndex = _primaryCell.IsValid ? _primaryCell.RowIndex : -1;
        _anchorIndex = _anchorCell.IsValid ? _anchorCell.RowIndex : -1;

        UpdateCaches();

        var newRows = new HashSet<int>(_cachedRows);
        var addedRows = newRows.Except(previousRows).OrderBy(i => i).ToArray();
        var removedRows = previousRows.Except(newRows).OrderBy(i => i).ToArray();

        SelectionChanged?.Invoke(this, new FastTreeDataGridSelectionChangedEventArgs(
            addedRows,
            removedRows,
            _cachedRows,
            _primaryIndex,
            _anchorIndex,
            addedCells,
            removedCells,
            _cachedCells,
            _primaryCell,
            _anchorCell));
    }

    private void UpdateCaches()
    {
        if (_selectedCells.Count == 0)
        {
            _cachedCells = Array.Empty<FastTreeDataGridCellIndex>();
            _cachedRows = Array.Empty<int>();
            return;
        }

        _cachedCells = _selectedCells
            .OrderBy(static c => c.RowIndex)
            .ThenBy(static c => c.ColumnIndex)
            .ToArray();

        _cachedRows = _cachedCells
            .Select(static c => c.RowIndex)
            .Distinct()
            .OrderBy(static i => i)
            .ToArray();
    }

    private IEnumerable<FastTreeDataGridCellIndex> EnumerateRectangle(FastTreeDataGridCellIndex start, FastTreeDataGridCellIndex end)
    {
        var rowStart = Math.Min(start.RowIndex, end.RowIndex);
        var rowEnd = Math.Max(start.RowIndex, end.RowIndex);
        var colStart = Math.Min(start.ColumnIndex, end.ColumnIndex);
        var colEnd = Math.Max(start.ColumnIndex, end.ColumnIndex);

        for (var row = rowStart; row <= rowEnd; row++)
        {
            for (var col = colStart; col <= colEnd; col++)
            {
                yield return new FastTreeDataGridCellIndex(row, col);
            }
        }
    }

    private int ResolvePreferredColumn()
    {
        if (_primaryCell.IsValid)
        {
            return _primaryCell.ColumnIndex;
        }

        if (_anchorCell.IsValid)
        {
            return _anchorCell.ColumnIndex;
        }

        return 0;
    }

    private static FastTreeDataGridCellIndex GetFirstCell(HashSet<FastTreeDataGridCellIndex> cells)
    {
        if (cells is null || cells.Count == 0)
        {
            return FastTreeDataGridCellIndex.Invalid;
        }

        foreach (var cell in cells)
        {
            return cell;
        }

        return FastTreeDataGridCellIndex.Invalid;
    }

    private void RaiseSelectionChanged(
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
