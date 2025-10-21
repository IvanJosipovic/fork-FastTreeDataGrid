using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridFlatSource<T> : IFastTreeDataGridSource
{
    private readonly Func<T, IEnumerable<T>> _childrenSelector;
    private readonly List<TreeNode> _rootNodes = new();
    private readonly List<TreeNode> _flatNodes = new();
    private readonly List<TreeNode> _visibleNodes = new();
    private Predicate<FastTreeDataGridRow>? _rowFilter;
    private bool _autoExpandFilteredMatches = true;
    private int _nextOriginalIndex;

    public FastTreeDataGridFlatSource(IEnumerable<T> rootItems, Func<T, IEnumerable<T>> childrenSelector)
    {
        _childrenSelector = childrenSelector ?? throw new ArgumentNullException(nameof(childrenSelector));
        BuildNodes(null, rootItems ?? Enumerable.Empty<T>(), level: 0);
        FlattenNodes();
        UpdateFilterStates();
        RebuildVisible();
    }

    public event EventHandler? ResetRequested;

    public int RowCount => _visibleNodes.Count;

    public FastTreeDataGridRow GetRow(int index)
    {
        if ((uint)index >= (uint)_visibleNodes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _visibleNodes[index].Row;
    }

    public void ToggleExpansion(int index)
    {
        if ((uint)index >= (uint)_visibleNodes.Count)
        {
            return;
        }

        var node = _visibleNodes[index];
        if (!node.HasChildren || (_rowFilter is not null && !node.HasVisibleChildren))
        {
            return;
        }

        node.IsExpanded = !node.IsExpanded;
        node.Row.IsExpanded = node.IsExpanded;

        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Sort(Comparison<FastTreeDataGridRow>? comparison)
    {
        if (comparison is null)
        {
            ResetSortRecursive(_rootNodes);
        }
        else
        {
            SortRecursive(_rootNodes, comparison);
        }

        FlattenNodes();
        UpdateFilterStates();
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetFilter(Predicate<FastTreeDataGridRow>? filter, bool expandMatches = true)
    {
        var hadFilter = _rowFilter is not null;
        _rowFilter = filter;
        _autoExpandFilteredMatches = expandMatches;

        if (!hadFilter && filter is not null)
        {
            CaptureExpansionStates();
        }
        else if (hadFilter && filter is null)
        {
            RestoreExpansionStates();
        }

        UpdateFilterStates();
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildNodes(TreeNode? parent, IEnumerable<T> items, int level)
    {
        foreach (var item in items)
        {
            var node = new TreeNode(item, parent, level, _nextOriginalIndex++, OnNodeRequestMeasure);
            if (parent is null)
            {
                _rootNodes.Add(node);
            }
            else
            {
                parent.Children.Add(node);
                parent.HasChildren = true;
                parent.HasVisibleChildren = true;
                parent.Row.HasChildren = true;
            }

            var children = (_childrenSelector(item) ?? Enumerable.Empty<T>()).ToList();
            if (children.Count > 0)
            {
                node.HasChildren = true;
                node.HasVisibleChildren = true;
                node.Row.HasChildren = true;
                BuildNodes(node, children, level + 1);
            }
        }
    }

    private void FlattenNodes()
    {
        _flatNodes.Clear();
        foreach (var root in _rootNodes)
        {
            root.Level = 0;
            FlattenNode(root);
        }
    }

    private void FlattenNode(TreeNode node)
    {
        _flatNodes.Add(node);
        foreach (var child in node.Children)
        {
            child.Level = node.Level + 1;
            FlattenNode(child);
        }
    }

    private void UpdateFilterStates()
    {
        if (_rowFilter is null)
        {
            foreach (var node in _flatNodes)
            {
                node.IsFilterIncluded = true;
                node.MatchesFilter = true;
                node.HasVisibleChildren = node.HasChildren;
                node.Row.HasChildren = node.HasChildren;
                node.Row.IsExpanded = node.IsExpanded;
            }

            return;
        }

        foreach (var root in _rootNodes)
        {
            EvaluateFilter(root);
        }
    }

    private bool EvaluateFilter(TreeNode node)
    {
        var matches = _rowFilter?.Invoke(node.Row) == true;
        var hasMatchingChild = false;

        foreach (var child in node.Children)
        {
            if (EvaluateFilter(child))
            {
                hasMatchingChild = true;
            }
        }

        node.MatchesFilter = matches;
        node.HasVisibleChildren = hasMatchingChild;
        node.IsFilterIncluded = matches || hasMatchingChild;

        node.Row.HasChildren = node.HasChildren && (hasMatchingChild || _rowFilter is null);

        if (_autoExpandFilteredMatches && node.HasChildren)
        {
            if (!node.StoredExpansionState.HasValue)
            {
                node.StoredExpansionState = node.IsExpanded;
            }

            node.IsExpanded = hasMatchingChild;
            node.Row.IsExpanded = node.IsExpanded;
        }
        else
        {
            node.Row.IsExpanded = node.IsExpanded;
        }

        return node.IsFilterIncluded;
    }

    private void RebuildVisible()
    {
        _visibleNodes.Clear();

        foreach (var node in _flatNodes)
        {
            if (!node.IsFilterIncluded || !IsVisible(node))
            {
                continue;
            }

            node.Row.Level = node.Level;
            node.Row.HasChildren = node.HasChildren && (_rowFilter is null || node.HasVisibleChildren);
            node.Row.IsExpanded = node.IsExpanded;
            _visibleNodes.Add(node);
        }
    }

    private bool IsVisible(TreeNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (!current.IsExpanded || !current.IsFilterIncluded)
            {
                return false;
            }

            current = current.Parent;
        }

        return true;
    }

    private void OnNodeRequestMeasure()
    {
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SortRecursive(List<TreeNode> nodes, Comparison<FastTreeDataGridRow> comparison)
    {
        if (nodes.Count > 1)
        {
            nodes.Sort((x, y) => comparison(x.Row, y.Row));
        }

        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortRecursive(node.Children, comparison);
            }
        }
    }

    private void ResetSortRecursive(List<TreeNode> nodes)
    {
        if (nodes.Count > 1)
        {
            nodes.Sort((x, y) => x.OriginalIndex.CompareTo(y.OriginalIndex));
        }

        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                ResetSortRecursive(node.Children);
            }
        }
    }

    private void CaptureExpansionStates()
    {
        foreach (var node in _flatNodes)
        {
            if (node.HasChildren && !node.StoredExpansionState.HasValue)
            {
                node.StoredExpansionState = node.IsExpanded;
            }
        }
    }

    private void RestoreExpansionStates()
    {
        foreach (var node in _flatNodes)
        {
            if (node.StoredExpansionState.HasValue)
            {
                node.IsExpanded = node.StoredExpansionState.Value;
                node.Row.IsExpanded = node.IsExpanded;
                node.StoredExpansionState = null;
            }

            node.Row.HasChildren = node.HasChildren;
            node.HasVisibleChildren = node.HasChildren;
            node.IsFilterIncluded = true;
        }
    }

    private sealed class TreeNode
    {
        public TreeNode(T item, TreeNode? parent, int level, int originalIndex, Action requestMeasure)
        {
            Item = item;
            Parent = parent;
            Level = level;
            OriginalIndex = originalIndex;
            Row = new FastTreeDataGridRow(item, level, hasChildren: false, isExpanded: false, requestMeasure);
        }

        public T Item { get; }

        public TreeNode? Parent { get; }

        public int Level { get; set; }

        public bool HasChildren { get; set; }

        public bool HasVisibleChildren { get; set; }

        public bool IsExpanded { get; set; }

        public bool IsFilterIncluded { get; set; } = true;

        public bool MatchesFilter { get; set; }

        public int OriginalIndex { get; }

        public List<TreeNode> Children { get; } = new();

        public FastTreeDataGridRow Row { get; }

        public bool? StoredExpansionState { get; set; }
    }
}
