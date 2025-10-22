using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Models;
using Avalonia.Threading;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridFlatSource<T> : IFastTreeDataGridSource, IFastTreeDataGridSortFilterHandler
{
    private readonly Func<T, IEnumerable<T>> _childrenSelector;
    private readonly Func<T, string>? _keySelector;
    private readonly IEqualityComparer<string>? _keyComparer;
    private readonly List<TreeNode> _rootNodes = new();
    private readonly List<TreeNode> _flatNodes = new();
    private readonly List<TreeNode> _visibleNodes = new();
    private readonly Dictionary<string, TreeNode>? _nodeLookup;
    private Predicate<FastTreeDataGridRow>? _rowFilter;
    private bool _autoExpandFilteredMatches = true;
    private int _nextOriginalIndex;
    private readonly object _dataLock = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

    public FastTreeDataGridFlatSource(
        IEnumerable<T> rootItems,
        Func<T, IEnumerable<T>> childrenSelector,
        Func<T, string>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
    {
        _childrenSelector = childrenSelector ?? throw new ArgumentNullException(nameof(childrenSelector));
        _keySelector = keySelector;
        _keyComparer = keySelector is null ? null : keyComparer ?? StringComparer.Ordinal;
        if (_keySelector is not null)
        {
            _nodeLookup = new Dictionary<string, TreeNode>(_keyComparer);
        }

        lock (_dataLock)
        {
            BuildNodes(null, rootItems ?? Enumerable.Empty<T>(), level: 0);
            FlattenNodes();
            UpdateFilterStates();
            RebuildVisible();
        }
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount
    {
        get
        {
            lock (_dataLock)
            {
                return _visibleNodes.Count;
            }
        }
    }

    public bool SupportsPlaceholders => false;

    public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<int>(RowCount);
    }

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var startIndex = request.StartIndex;
        List<FastTreeDataGridRow> rows;
        lock (_dataLock)
        {
            if (_visibleNodes.Count == 0 || request.Count == 0 || request.StartIndex >= _visibleNodes.Count)
            {
                rows = new List<FastTreeDataGridRow>();
            }
            else
            {
                startIndex = Math.Max(0, request.StartIndex);
                var endExclusive = Math.Min(_visibleNodes.Count, startIndex + request.Count);
                var capacity = Math.Max(0, endExclusive - startIndex);
                rows = new List<FastTreeDataGridRow>(capacity);

                for (var i = startIndex; i < endExclusive; i++)
                {
                    rows.Add(_visibleNodes[i].Row);
                }
            }
        }

        if (rows.Count == 0)
        {
            return new ValueTask<FastTreeDataGridPageResult>(FastTreeDataGridPageResult.Empty);
        }

        NotifyRowsMaterialized(startIndex, rows);

        return new ValueTask<FastTreeDataGridPageResult>(
            new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null));
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        switch (request.Kind)
        {
            case FastTreeDataGridInvalidationKind.Full:
                RaiseResetRequested();
                break;
            case FastTreeDataGridInvalidationKind.Range:
            case FastTreeDataGridInvalidationKind.MetadataOnly:
                RaiseInvalidated(request);
                break;
        }

        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        lock (_dataLock)
        {
            if ((uint)index < (uint)_visibleNodes.Count)
            {
                row = _visibleNodes[index].Row;
                return true;
            }
        }

        row = default!;
        return false;
    }

    public bool IsPlaceholder(int index)
    {
        _ = index;
        return false;
    }

    public FastTreeDataGridRow GetRow(int index)
    {
        lock (_dataLock)
        {
            if ((uint)index >= (uint)_visibleNodes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _visibleNodes[index].Row;
        }
    }

    public void ToggleExpansion(int index)
    {
        var shouldNotify = false;

        lock (_dataLock)
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
            shouldNotify = true;
        }

        if (shouldNotify)
        {
            RaiseResetRequested();
        }
    }

    public void Sort(Comparison<FastTreeDataGridRow>? comparison) =>
        ScheduleWork(() => SortCore(comparison));

    public void SetFilter(Predicate<FastTreeDataGridRow>? filter, bool expandMatches = true) =>
        ScheduleWork(() => SetFilterCore(filter, expandMatches));

    public void Reset(IEnumerable<T> rootItems, bool preserveExpansion = true)
    {
        if (rootItems is null)
        {
            throw new ArgumentNullException(nameof(rootItems));
        }

        var snapshot = rootItems as IList<T> ?? rootItems.ToList();
        ScheduleWork(() => ResetCore(snapshot, preserveExpansion));
    }

    private void SortCore(Comparison<FastTreeDataGridRow>? comparison)
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
    }

    private void SetFilterCore(Predicate<FastTreeDataGridRow>? filter, bool expandMatches)
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
    }

    private void ResetCore(IEnumerable<T> rootItems, bool preserveExpansion)
    {
        Dictionary<string, NodeExpansionState>? expansionSnapshot = null;
        if (preserveExpansion && _keySelector is not null && _nodeLookup is not null)
        {
            expansionSnapshot = new Dictionary<string, NodeExpansionState>(_keyComparer);
            foreach (var (key, node) in _nodeLookup)
            {
                expansionSnapshot[key] = new NodeExpansionState(node.IsExpanded, node.StoredExpansionState);
            }
        }

        _rootNodes.Clear();
        _flatNodes.Clear();
        _visibleNodes.Clear();
        _nodeLookup?.Clear();
        _nextOriginalIndex = 0;

        BuildNodes(null, rootItems, level: 0);
        FlattenNodes();

        if (expansionSnapshot is not null)
        {
            RestoreExpansionSnapshot(expansionSnapshot);
        }

        UpdateFilterStates();
        RebuildVisible();
    }

    private void RestoreExpansionSnapshot(IReadOnlyDictionary<string, NodeExpansionState> snapshot)
    {
        if (_keySelector is null)
        {
            return;
        }

        foreach (var node in _flatNodes)
        {
            if (node.Key is null)
            {
                continue;
            }

            if (snapshot.TryGetValue(node.Key, out var state))
            {
                node.IsExpanded = state.IsExpanded;
                node.StoredExpansionState = state.StoredExpansionState;
                node.Row.IsExpanded = state.IsExpanded;
            }
        }
    }

    public Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.FilterDescriptors.Count == 0)
        {
            SetFilter(null);
        }
        else
        {
            SetFilter(row => EvaluateFilters(row, request.FilterDescriptors));
        }

        if (request.SortDescriptors.Count == 0)
        {
            Sort(null);
        }
        else
        {
            var descriptor = request.SortDescriptors[0];
            if (descriptor.RowComparison is not null)
            {
                var comparison = descriptor.Direction == FastTreeDataGridSortDirection.Descending
                    ? new Comparison<FastTreeDataGridRow>((a, b) => descriptor.RowComparison!(b, a))
                    : descriptor.RowComparison;
                Sort(comparison);
            }
        }

        return Task.CompletedTask;
    }

    private static bool EvaluateFilters(FastTreeDataGridRow row, IReadOnlyList<FastTreeDataGridFilterDescriptor> filters)
    {
        for (var i = 0; i < filters.Count; i++)
        {
            var descriptor = filters[i];
            if (descriptor.Predicate is not null && !descriptor.Predicate(row.Item))
            {
                return false;
            }
        }

        return true;
    }


    private void ScheduleWork(Action work)
    {
        _ = Task.Run(async () =>
        {
            Exception? error = null;
            await _operationSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (_dataLock)
                {
                    work();
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _operationSemaphore.Release();
            }

            if (error is not null)
            {
                Debug.WriteLine(error);
                return;
            }

            RaiseResetRequested();
        });
    }

    private void RaiseResetRequested()
    {
        RaiseInvalidated(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full));

        var handler = ResetRequested;
        if (handler is null)
        {
            return;
        }

        try
        {
            var dispatcher = Dispatcher.UIThread;
            if (dispatcher.CheckAccess())
            {
                handler.Invoke(this, EventArgs.Empty);
            }
            else
            {
                dispatcher.Post(() => handler.Invoke(this, EventArgs.Empty), DispatcherPriority.Background);
            }
        }
        catch (InvalidOperationException)
        {
            handler.Invoke(this, EventArgs.Empty);
        }
    }

    private void RaiseInvalidated(FastTreeDataGridInvalidationRequest request)
    {
        var handler = Invalidated;
        if (handler is null)
        {
            return;
        }

        var args = new FastTreeDataGridInvalidatedEventArgs(request);
        try
        {
            var dispatcher = Dispatcher.UIThread;
            if (dispatcher.CheckAccess())
            {
                handler.Invoke(this, args);
            }
            else
            {
                dispatcher.Post(() => handler.Invoke(this, args), DispatcherPriority.Background);
            }
        }
        catch (InvalidOperationException)
        {
            handler.Invoke(this, args);
        }
    }

    private void NotifyRowsMaterialized(int startIndex, IReadOnlyList<FastTreeDataGridRow> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var handler = RowMaterialized;
        if (handler is null)
        {
            return;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            handler.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(startIndex + i, rows[i]));
        }
    }

    private void BuildNodes(TreeNode? parent, IEnumerable<T> items, int level)
    {
        foreach (var item in items)
        {
            var key = _keySelector?.Invoke(item);
            var node = new TreeNode(item, parent, level, _nextOriginalIndex++, key, OnNodeRequestMeasure);
            if (key is not null && _nodeLookup is not null)
            {
                _nodeLookup[key] = node;
            }

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

    private void OnNodeRequestMeasure() => RaiseResetRequested();

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

    private readonly struct NodeExpansionState
    {
        public NodeExpansionState(bool isExpanded, bool? storedExpansionState)
        {
            IsExpanded = isExpanded;
            StoredExpansionState = storedExpansionState;
        }

        public bool IsExpanded { get; }

        public bool? StoredExpansionState { get; }
    }

    private sealed class TreeNode
    {
        public TreeNode(T item, TreeNode? parent, int level, int originalIndex, string? key, Action requestMeasure)
        {
            Item = item;
            Parent = parent;
            Level = level;
            OriginalIndex = originalIndex;
            Key = key;
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

        public string? Key { get; }

        public List<TreeNode> Children { get; } = new();

        public FastTreeDataGridRow Row { get; }

        public bool? StoredExpansionState { get; set; }
    }
}
