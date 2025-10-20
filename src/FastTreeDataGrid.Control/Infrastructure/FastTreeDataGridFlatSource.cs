using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridFlatSource<T> : IFastTreeDataGridSource
{
    private readonly Func<T, IEnumerable<T>> _childrenSelector;
    private readonly List<TreeNode> _flatNodes = new();
    private readonly List<TreeNode> _visibleNodes = new();

    public FastTreeDataGridFlatSource(IEnumerable<T> rootItems, Func<T, IEnumerable<T>> childrenSelector)
    {
        _childrenSelector = childrenSelector ?? throw new ArgumentNullException(nameof(childrenSelector));
        BuildNodes(null, rootItems ?? Enumerable.Empty<T>(), level: 0);
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
        if (!node.HasChildren)
        {
            return;
        }

        node.IsExpanded = !node.IsExpanded;
        node.Row.IsExpanded = node.IsExpanded;

        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Sort(Comparison<FastTreeDataGridRow> comparison)
    {
        if (comparison is null)
        {
            return;
        }

        _flatNodes.Sort((x, y) => comparison(x.Row, y.Row));
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildNodes(TreeNode? parent, IEnumerable<T> items, int level)
    {
        foreach (var item in items)
        {
            var node = new TreeNode(item, parent, level, OnNodeRequestMeasure);
            _flatNodes.Add(node);
            parent?.Children.Add(node);

            var children = (_childrenSelector(item) ?? Enumerable.Empty<T>()).ToList();
            if (children.Count > 0)
            {
                node.Row.HasChildren = true;
                node.HasChildren = true;
                BuildNodes(node, children, level + 1);
            }
        }
    }

    private void RebuildVisible()
    {
        _visibleNodes.Clear();

        foreach (var node in _flatNodes)
        {
            if (!IsVisible(node))
            {
                continue;
            }

            node.Row.Level = node.Level;
            node.Row.IsExpanded = node.IsExpanded;
            _visibleNodes.Add(node);
        }
    }

    private bool IsVisible(TreeNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (!current.IsExpanded)
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

    private sealed class TreeNode
    {
        public TreeNode(T item, TreeNode? parent, int level, Action requestMeasure)
        {
            Item = item;
            Parent = parent;
            Level = level;
            Row = new FastTreeDataGridRow(item, level, hasChildren: false, isExpanded: false, requestMeasure);
            Children = new List<TreeNode>();
        }

        public T Item { get; }

        public TreeNode? Parent { get; }

        public int Level { get; }

        public bool HasChildren { get; set; }

        public bool IsExpanded { get; set; }

        public List<TreeNode> Children { get; }

        public FastTreeDataGridRow Row { get; }
    }
}
