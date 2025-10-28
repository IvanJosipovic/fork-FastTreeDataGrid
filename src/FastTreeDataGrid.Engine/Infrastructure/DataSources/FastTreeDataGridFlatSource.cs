using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Models;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridFlatSource<T> : IFastTreeDataGridSource, IFastTreeDataGridSortFilterHandler, IFastTreeDataGridGroupingController, IFastTreeDataGridGroupingHandler, IFastTreeDataGridRowReorderHandler
{
    private readonly Func<T, IEnumerable<T>> _childrenSelector;
    private readonly Func<T, string>? _keySelector;
    private readonly IEqualityComparer<string>? _keyComparer;
    private readonly List<TreeNode> _rootNodes = new();
    private readonly List<TreeNode> _flatNodes = new();
    private readonly List<VisibleEntry> _visibleEntries = new();
    private readonly Dictionary<string, TreeNode>? _nodeLookup;
    private Predicate<FastTreeDataGridRow>? _rowFilter;
    private bool _autoExpandFilteredMatches = true;
    private int _nextOriginalIndex;
    private readonly object _dataLock = new();
    private readonly object _workLock = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    private IReadOnlyList<FastTreeDataGridGroupDescriptor> _groupDescriptors = Array.Empty<FastTreeDataGridGroupDescriptor>();
    private IReadOnlyList<FastTreeDataGridAggregateDescriptor> _aggregateDescriptors = Array.Empty<FastTreeDataGridAggregateDescriptor>();
    private readonly Dictionary<string, bool> _groupExpansionStates = new(StringComparer.Ordinal);
    private bool _defaultGroupExpansionState = true;
    private readonly List<FastTreeDataGridRow> _aggregationBuffer = new();
    private readonly Dictionary<AggregateCacheKey, VisibleEntry> _aggregateCache = new();
    private int _pendingOperations;
    private CancellationTokenSource? _activeCancellableWork;

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
                return _visibleEntries.Count;
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
            if (_visibleEntries.Count == 0 || request.Count == 0 || request.StartIndex >= _visibleEntries.Count)
            {
                rows = new List<FastTreeDataGridRow>();
            }
            else
            {
                startIndex = Math.Max(0, request.StartIndex);
                var endExclusive = Math.Min(_visibleEntries.Count, startIndex + request.Count);
                var capacity = Math.Max(0, endExclusive - startIndex);
                rows = new List<FastTreeDataGridRow>(capacity);

                for (var i = startIndex; i < endExclusive; i++)
                {
                    rows.Add(_visibleEntries[i].Row);
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
        FastTreeDataGridVirtualizationDiagnostics.Log(
            "PrefetchAsync",
            $"Prefetch request startIndex={request.StartIndex}, count={request.Count}, radius={request.PrefetchRadius}");
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
            if ((uint)index < (uint)_visibleEntries.Count)
            {
                row = _visibleEntries[index].Row;
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
            if ((uint)index >= (uint)_visibleEntries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _visibleEntries[index].Row;
        }
    }

    public void ToggleExpansion(int index)
    {
        var shouldNotify = false;

        lock (_dataLock)
        {
            if ((uint)index >= (uint)_visibleEntries.Count)
            {
                return;
            }

            var entry = _visibleEntries[index];
            if (entry.IsGroup && entry.GroupPath is { } groupPath)
            {
                var newState = !entry.IsExpanded;
                entry.IsExpanded = newState;
                entry.Row.IsExpanded = newState;
                _groupExpansionStates[groupPath] = newState;
                RebuildVisible();
                shouldNotify = true;
            }
            else if (entry.Node is { } node)
            {
                if (!entry.HasChildren || (_rowFilter is not null && !node.HasVisibleChildren))
                {
                    return;
                }

                node.IsExpanded = !node.IsExpanded;
                node.Row.IsExpanded = node.IsExpanded;

                RebuildVisible();
                shouldNotify = true;
            }
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

    public void ExpandAllGroups()
    {
        var shouldNotify = false;
        lock (_dataLock)
        {
            if (_groupDescriptors.Count > 0)
            {
                _defaultGroupExpansionState = true;
                _groupExpansionStates.Clear();
                RebuildVisible();
                shouldNotify = true;
            }
            else
            {
                shouldNotify = SetHierarchyExpansionState(expanded: true);
            }
        }

        if (shouldNotify)
        {
            RaiseResetRequested();
        }
    }

    public void CollapseAllGroups()
    {
        var shouldNotify = false;
        lock (_dataLock)
        {
            if (_groupDescriptors.Count > 0)
            {
                _defaultGroupExpansionState = false;
                _groupExpansionStates.Clear();
                RebuildVisible();
                shouldNotify = true;
            }
            else
            {
                shouldNotify = SetHierarchyExpansionState(expanded: false);
            }
        }

        if (shouldNotify)
        {
            RaiseResetRequested();
        }
    }

    public void ApplyGroupExpansionLayout(IEnumerable<FastTreeDataGridGroupingExpansionState> states, bool defaultExpanded)
    {
        if (states is null)
        {
            throw new ArgumentNullException(nameof(states));
        }

        lock (_dataLock)
        {
            _groupExpansionStates.Clear();
            foreach (var state in states)
            {
                if (state is null || string.IsNullOrEmpty(state.Path))
                {
                    continue;
                }

                _groupExpansionStates[state.Path] = state.IsExpanded;
            }

            _defaultGroupExpansionState = defaultExpanded;
            RebuildVisible();
        }

        RaiseResetRequested();
    }

    bool IFastTreeDataGridRowReorderHandler.CanReorder(FastTreeDataGridRowReorderRequest request)
    {
        return TryProcessReorderRequest(request, applyChanges: false, out _);
    }

    Task<FastTreeDataGridRowReorderResult> IFastTreeDataGridRowReorderHandler.ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryProcessReorderRequest(request, applyChanges: true, out var newIndices))
        {
            return Task.FromResult(FastTreeDataGridRowReorderResult.Cancelled);
        }

        RaiseResetRequested();
        return Task.FromResult(FastTreeDataGridRowReorderResult.Successful(newIndices));
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
        ClearAggregateCache();

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
        ClearAggregateCache();

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
        _visibleEntries.Clear();
        _nodeLookup?.Clear();
        _nextOriginalIndex = 0;
        _groupExpansionStates.Clear();
        _defaultGroupExpansionState = true;

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
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        ScheduleCancellableOperation(() => ApplySortFilterCore(request), cancellationToken);
        return Task.CompletedTask;
    }

    public Task ApplyGroupingAsync(FastTreeDataGridGroupingRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var sortFilterRequest = new FastTreeDataGridSortFilterRequest
        {
            SortDescriptors = request.SortDescriptors,
            FilterDescriptors = request.FilterDescriptors,
            GroupDescriptors = request.GroupDescriptors,
            AggregateDescriptors = request.AggregateDescriptors,
        };

        ScheduleCancellableOperation(() => ApplySortFilterCore(sortFilterRequest), cancellationToken);
        return Task.CompletedTask;
    }

    public async ValueTask WaitForPendingOperationsAsync(CancellationToken cancellationToken = default)
    {
        while (Volatile.Read(ref _pendingOperations) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool SetHierarchyExpansionState(bool expanded)
    {
        var changed = false;

        foreach (var node in _flatNodes)
        {
            if (!node.HasChildren)
            {
                continue;
            }

            if (_rowFilter is not null && !node.HasVisibleChildren)
            {
                continue;
            }

            if (node.IsExpanded == expanded)
            {
                continue;
            }

            node.IsExpanded = expanded;
            node.Row.IsExpanded = expanded;
            node.StoredExpansionState = expanded;
            changed = true;
        }

        if (changed)
        {
            RebuildVisible();
        }

        return changed;
    }

    private void ApplySortFilterCore(FastTreeDataGridSortFilterRequest request)
    {
        ClearAggregateCache();

        if (request.GroupDescriptors.Count == 0)
        {
            _groupExpansionStates.Clear();
            _defaultGroupExpansionState = true;
        }

        _groupDescriptors = request.GroupDescriptors.Count == 0
            ? Array.Empty<FastTreeDataGridGroupDescriptor>()
            : request.GroupDescriptors.ToArray();

        _aggregateDescriptors = request.AggregateDescriptors.Count == 0
            ? Array.Empty<FastTreeDataGridAggregateDescriptor>()
            : request.AggregateDescriptors.ToArray();

        Predicate<FastTreeDataGridRow>? filter = request.FilterDescriptors.Count == 0
            ? null
            : row => EvaluateFilters(row, request.FilterDescriptors);

        SetFilterCore(filter, _autoExpandFilteredMatches);

        Comparison<FastTreeDataGridRow>? comparison = null;
        if (request.SortDescriptors.Count > 0)
        {
            var descriptor = request.SortDescriptors[0];
            if (descriptor.RowComparison is not null)
            {
                comparison = descriptor.Direction == FastTreeDataGridSortDirection.Descending
                    ? new Comparison<FastTreeDataGridRow>((a, b) => descriptor.RowComparison!(b, a))
                    : descriptor.RowComparison;
            }
        }

        SortCore(comparison);
    }

    private static bool EvaluateFilters(FastTreeDataGridRow row, IReadOnlyList<FastTreeDataGridFilterDescriptor> filters)
    {
        for (var i = 0; i < filters.Count; i++)
        {
            var descriptor = filters[i];
            if (descriptor.Predicate is not null && !descriptor.Predicate(row))
            {
                return false;
            }
        }

        return true;
    }


    private void ScheduleCancellableOperation(Action work, CancellationToken cancellationToken)
    {
        CancellationTokenSource linkedCts;
        lock (_workLock)
        {
            _activeCancellableWork?.Cancel();
            _activeCancellableWork?.Dispose();
            _activeCancellableWork = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts = _activeCancellableWork;
        }

        ScheduleWork(work, linkedCts.Token, linkedCts);
    }

    private void ScheduleWork(Action work, CancellationToken cancellationToken = default, CancellationTokenSource? ownerCts = null)
    {
        Interlocked.Increment(ref _pendingOperations);

        _ = Task.Run(async () =>
        {
            var token = cancellationToken;
            var completed = false;
            Exception? error = null;
            try
            {
                await _operationSemaphore.WaitAsync(token).ConfigureAwait(false);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                lock (_dataLock)
                {
                    if (!token.IsCancellationRequested)
                    {
                        work();
                        completed = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow cancellation.
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _operationSemaphore.Release();
                Interlocked.Decrement(ref _pendingOperations);
                if (ownerCts is not null)
                {
                    lock (_workLock)
                    {
                        if (ReferenceEquals(_activeCancellableWork, ownerCts))
                        {
                            _activeCancellableWork = null;
                        }
                    }

                    ownerCts.Dispose();
                }
            }

            if (error is not null)
            {
                Debug.WriteLine(error);
                return;
            }

            if (completed)
            {
                RaiseResetRequested();
            }
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
            var dispatcher = FastTreeDataGridDispatcherProvider.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                handler.Invoke(this, EventArgs.Empty);
            }
            else
            {
                dispatcher.Post(() => handler.Invoke(this, EventArgs.Empty), FastTreeDataGridDispatchPriority.Background);
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
            var dispatcher = FastTreeDataGridDispatcherProvider.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                handler.Invoke(this, args);
            }
            else
            {
                dispatcher.Post(() => handler.Invoke(this, args), FastTreeDataGridDispatchPriority.Background);
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

    private bool TryProcessReorderRequest(FastTreeDataGridRowReorderRequest request, bool applyChanges, out IReadOnlyList<int> newIndices)
    {
        newIndices = Array.Empty<int>();
        if (request is null)
        {
            return false;
        }

        lock (_dataLock)
        {
            if (_visibleEntries.Count == 0 || request.SourceIndices.Count == 0)
            {
                return false;
            }

            var allowGroupReorder = request.Context is IFastTreeDataGridRowReorderSettings settings && settings.AllowGroupReorder;

            var sortedIndices = request.SourceIndices
                .Where(index => index >= 0)
                .Distinct()
                .OrderBy(index => index)
                .ToList();

            if (sortedIndices.Count == 0)
            {
                return false;
            }

            if (request.InsertIndex < 0 || request.InsertIndex > _visibleEntries.Count)
            {
                return false;
            }

            var nodes = new List<TreeNode>(sortedIndices.Count);
            TreeNode? parent = null;

            foreach (var index in sortedIndices)
            {
                if (index >= _visibleEntries.Count)
                {
                    return false;
                }

                var entry = _visibleEntries[index];
                if (!IsReorderableEntry(entry, allowGroupReorder))
                {
                    return false;
                }

                var node = entry.Node!;
                if (parent is null)
                {
                    parent = node.Parent;
                }
                else if (!ReferenceEquals(node.Parent, parent))
                {
                    return false;
                }

                nodes.Add(node);
            }

            var workingEntries = new List<VisibleEntry>(_visibleEntries);
            RemoveEntriesForNodes(sortedIndices, workingEntries);

            if (request.InsertIndex < 0 || request.InsertIndex > workingEntries.Count)
            {
                return false;
            }

            if (parent is not null)
            {
                var parentIndex = workingEntries.FindIndex(e => ReferenceEquals(e.Node, parent));
                if (parentIndex < 0)
                {
                    return false;
                }

                var parentLevel = parent.Row.Level;
                var minInsert = parentIndex + 1;
                var maxInsert = minInsert;

                while (maxInsert < workingEntries.Count && workingEntries[maxInsert].Level > parentLevel)
                {
                    maxInsert++;
                }

                if (request.InsertIndex < minInsert || request.InsertIndex > maxInsert)
                {
                    return false;
                }
            }

            var siblings = parent is null
                ? new List<TreeNode>(_rootNodes)
                : new List<TreeNode>(parent.Children);

            foreach (var node in nodes)
            {
                siblings.Remove(node);
            }

            var childInsertIndex = siblings.Count;
            if (workingEntries.Count == 0)
            {
                childInsertIndex = siblings.Count;
            }
            else if (request.InsertIndex < workingEntries.Count)
            {
                var targetEntry = workingEntries[request.InsertIndex];
                if (!IsReorderableEntry(targetEntry, allowGroupReorder) || !ReferenceEquals(targetEntry.Node!.Parent, parent))
                {
                    return false;
                }

                childInsertIndex = siblings.IndexOf(targetEntry.Node!);
                if (childInsertIndex < 0)
                {
                    childInsertIndex = siblings.Count;
                }
            }
            else
            {
                childInsertIndex = siblings.Count;
            }

            if (!applyChanges)
            {
                return true;
            }

            ApplyReorder(parent, nodes, childInsertIndex);
            FlattenNodes();
            UpdateFilterStates();
            RebuildVisible();

            var nodeSet = new HashSet<TreeNode>(nodes);
            var indices = new List<int>(nodes.Count);
            for (var i = 0; i < _visibleEntries.Count && nodeSet.Count > 0; i++)
            {
                var entry = _visibleEntries[i];
                if (entry.Node is not null && nodeSet.Remove(entry.Node))
                {
                    indices.Add(i);
                }
            }

            newIndices = indices;
            return true;
        }
    }

    private static bool IsReorderableEntry(VisibleEntry entry, bool allowGroupReorder) =>
        entry.Node is not null && !entry.IsSummary && (allowGroupReorder || !entry.IsGroup);

    private static void RemoveEntriesForNodes(IReadOnlyList<int> indices, List<VisibleEntry> workingEntries)
    {
        for (var i = indices.Count - 1; i >= 0; i--)
        {
            var index = indices[i];
            if (index < 0 || index >= workingEntries.Count)
            {
                continue;
            }

            var level = workingEntries[index].Level;
            workingEntries.RemoveAt(index);
            while (index < workingEntries.Count && workingEntries[index].Level > level)
            {
                workingEntries.RemoveAt(index);
            }
        }
    }

    private void ApplyReorder(TreeNode? parent, IReadOnlyList<TreeNode> nodes, int insertIndex)
    {
        var targetList = parent is null ? _rootNodes : parent.Children;

        foreach (var node in nodes)
        {
            targetList.Remove(node);
        }

        var boundedInsertIndex = Math.Clamp(insertIndex, 0, targetList.Count);
        for (var i = 0; i < nodes.Count; i++)
        {
            targetList.Insert(Math.Min(boundedInsertIndex + i, targetList.Count), nodes[i]);
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
        _visibleEntries.Clear();

        if (_groupDescriptors.Count == 0)
        {
            RebuildVisibleWithoutGrouping();
            return;
        }

        RebuildVisibleWithGrouping();
    }

    private void RebuildVisibleWithoutGrouping()
    {
        foreach (var node in _flatNodes)
        {
            if (!node.IsFilterIncluded || !IsVisible(node))
            {
                continue;
            }

            node.Row.Level = node.Level;
            node.Row.HasChildren = node.HasChildren && (_rowFilter is null || node.HasVisibleChildren);
            node.Row.IsExpanded = node.IsExpanded;

            _visibleEntries.Add(VisibleEntry.ForNode(node));
        }

        var summaryEntry = CreateSummaryEntry(
            _visibleEntries.Where(static e => !e.IsGroup && !e.IsSummary).Select(static e => e.Row),
            level: 0,
            groupPath: null,
            groupKey: null,
            FastTreeDataGridAggregatePlacement.GridFooter);

        if (summaryEntry is not null)
        {
            _visibleEntries.Add(summaryEntry);
        }
    }

    private void RebuildVisibleWithGrouping()
    {
        var groupsRoot = new GroupView("root", -1, null, null, null);

        foreach (var node in _flatNodes)
        {
            if (!node.IsFilterIncluded || !IsVisible(node))
            {
                continue;
            }

            AddNodeToGroup(groupsRoot, node, 0);
        }

        if (groupsRoot.Children.Count == 0 && groupsRoot.Items.Count == 0)
        {
            return;
        }

        FlattenGroupView(groupsRoot);

        var footer = CreateSummaryEntry(
            _visibleEntries.Where(static e => e.Node is not null).Select(static e => e.Row),
            level: 0,
            groupPath: null,
            groupKey: null,
            FastTreeDataGridAggregatePlacement.GridFooter);

        if (footer is not null)
        {
            _visibleEntries.Add(footer);
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

    private void AddNodeToGroup(GroupView parent, TreeNode node, int descriptorIndex)
    {
        if (descriptorIndex >= _groupDescriptors.Count)
        {
            parent.Items.Add(node);
            return;
        }

        var descriptor = _groupDescriptors[descriptorIndex];
        var key = GetGroupKey(node.Row, descriptor);
        var path = BuildGroupPath(parent.Path, descriptorIndex, key);

        var child = parent.Children.FirstOrDefault(c => c.Path == path);
        if (child is null)
        {
            child = new GroupView(path, descriptorIndex, descriptor, key, parent);
            if (_groupExpansionStates.TryGetValue(path, out var expanded))
            {
                child.IsExpanded = expanded;
            }
            else if (descriptor is not null)
            {
                child.IsExpanded = descriptor.IsExpanded;
            }
            else
            {
                child.IsExpanded = _defaultGroupExpansionState;
            }

            InsertGroupView(parent.Children, child, descriptor);
        }

        if (!child.IsExpanded)
        {
            child.Items.Add(node);
            return;
        }

        AddNodeToGroup(child, node, descriptorIndex + 1);
    }

    private static void InsertGroupView(List<GroupView> children, GroupView view, FastTreeDataGridGroupDescriptor descriptor)
    {
        var comparer = descriptor.Comparer;
        var direction = descriptor.SortDirection == FastTreeDataGridSortDirection.Descending ? -1 : 1;

        if (children.Count == 0)
        {
            children.Add(view);
            return;
        }

        for (var i = 0; i < children.Count; i++)
        {
            var existing = children[i];
            var compare = CompareGroupKeys(comparer, view.Key, existing.Key) * direction;
            if (compare < 0)
            {
                children.Insert(i, view);
                return;
            }
        }

        children.Add(view);
    }

    private static string BuildGroupPath(string parentPath, int descriptorIndex, object? key) =>
        $"{parentPath}|{descriptorIndex}:{NormalizeKey(key)}";

    private static string NormalizeKey(object? key) =>
        key switch
        {
            null => "<null>",
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => key?.ToString() ?? "<null>"
        };

    private object? GetGroupKey(FastTreeDataGridRow row, FastTreeDataGridGroupDescriptor descriptor)
    {
        if (descriptor.Adapter is not null)
        {
            return descriptor.Adapter.GetGroupKey(row);
        }

        if (descriptor.KeySelector is not null)
        {
            return descriptor.KeySelector(row);
        }

        if (!string.IsNullOrEmpty(descriptor.ColumnKey) && row.ValueProvider is { } provider)
        {
            return provider.GetValue(row.Item, descriptor.ColumnKey);
        }

        return row.Item;
    }

    private void FlattenGroupView(GroupView root)
    {
        foreach (var child in root.Children)
        {
            FlattenGroup(child);
        }

        if (root.Level < 0 && root.Items.Count > 0)
        {
            foreach (var node in root.Items)
            {
                node.Row.Level = node.Level;
                node.Row.HasChildren = node.HasChildren && (_rowFilter is null || node.HasVisibleChildren);
                node.Row.IsExpanded = node.IsExpanded;
                _visibleEntries.Add(VisibleEntry.ForNode(node));
            }
        }
    }

    private void FlattenGroup(GroupView view)
    {
        var itemCount = CountGroupItems(view);
        var headerText = BuildGroupHeader(view, itemCount);
        var provider = new FastTreeDataGridGeneratedGroupRow(
            view.Descriptor?.ColumnKey,
            headerText,
            view.Key,
            itemCount,
            view.Level,
            view.Path);

        var hasChildren = view.Children.Count > 0 || view.Items.Count > 0;
        var row = new FastTreeDataGridRow(provider, view.Level, hasChildren, view.IsExpanded, OnNodeRequestMeasure)
        {
            HasChildren = hasChildren
        };
        row.Level = view.Level;
        row.IsExpanded = view.IsExpanded;

        _visibleEntries.Add(VisibleEntry.ForGroup(row, view.Path, view.IsExpanded, hasChildren, view.Level));

        if (!view.IsExpanded)
        {
            return;
        }

        foreach (var child in view.Children)
        {
            FlattenGroup(child);
        }

        foreach (var node in view.Items)
        {
            var level = view.Level + 1 + Math.Max(0, node.OriginalLevel);
            node.Row.Level = level;
            node.Row.HasChildren = node.HasChildren && (_rowFilter is null || node.HasVisibleChildren);
            node.Row.IsExpanded = node.IsExpanded;
            _visibleEntries.Add(VisibleEntry.ForNode(node));
        }

        var summaryEntry = CreateSummaryEntryForGroup(view);
        if (summaryEntry is not null)
        {
            _visibleEntries.Add(summaryEntry);
        }
    }

    private VisibleEntry? CreateSummaryEntryForGroup(GroupView view)
    {
        var hasGlobalAggregates = _aggregateDescriptors.Count > 0 && _aggregateDescriptors.Any(d => (d.Placement & FastTreeDataGridAggregatePlacement.GroupFooter) != 0);
        var hasLocalAggregates = view.Descriptor is not null && view.Descriptor.AggregateDescriptors.Any(d => (d.Placement & FastTreeDataGridAggregatePlacement.GroupFooter) != 0);

        if (!hasGlobalAggregates && !hasLocalAggregates)
        {
            return null;
        }

        _aggregationBuffer.Clear();
        CollectGroupRows(view, _aggregationBuffer);
        if (_aggregationBuffer.Count == 0)
        {
            return null;
        }

        var entry = CreateSummaryEntry(_aggregationBuffer, view.Level + 1, view.Path, view.Key, FastTreeDataGridAggregatePlacement.GroupFooter, view.Descriptor);
        _aggregationBuffer.Clear();
        return entry;
    }

    private List<FastTreeDataGridAggregateDescriptor>? CollectAggregateDescriptors(FastTreeDataGridGroupDescriptor? descriptor, FastTreeDataGridAggregatePlacement placement)
    {
        List<FastTreeDataGridAggregateDescriptor>? descriptors = null;

        foreach (var aggregate in _aggregateDescriptors)
        {
            if ((aggregate.Placement & placement) == 0)
            {
                continue;
            }

            descriptors ??= new List<FastTreeDataGridAggregateDescriptor>();
            descriptors.Add(aggregate);
        }

        if (descriptor is not null && descriptor.AggregateDescriptors.Count > 0)
        {
            foreach (var aggregate in descriptor.AggregateDescriptors)
            {
                if ((aggregate.Placement & placement) == 0)
                {
                    continue;
                }

                descriptors ??= new List<FastTreeDataGridAggregateDescriptor>();
                descriptors.Add(aggregate);
            }
        }

        return descriptors;
    }

    private bool TryGetCachedAggregate(FastTreeDataGridGroupDescriptor? descriptor, string? path, FastTreeDataGridAggregatePlacement placement, out VisibleEntry entry)
    {
        var key = new AggregateCacheKey(descriptor, path ?? string.Empty, placement);
        if (_aggregateCache.TryGetValue(key, out var cached))
        {
            FastTreeDataGridVirtualizationDiagnostics.CacheHits.Add(1);
            entry = cached;
            return true;
        }

        FastTreeDataGridVirtualizationDiagnostics.CacheMisses.Add(1);
        entry = null!;
        return false;
    }

    private void CacheAggregateEntry(FastTreeDataGridGroupDescriptor? descriptor, string? path, FastTreeDataGridAggregatePlacement placement, VisibleEntry entry)
    {
        var key = new AggregateCacheKey(descriptor, path ?? string.Empty, placement);
        _aggregateCache[key] = entry;
    }

    private void ClearAggregateCache() => _aggregateCache.Clear();

    private VisibleEntry? CreateSummaryEntry(IEnumerable<FastTreeDataGridRow> sourceRows, int level, string? groupPath, object? groupKey, FastTreeDataGridAggregatePlacement placement, FastTreeDataGridGroupDescriptor? descriptor = null)
    {
        if (TryGetCachedAggregate(descriptor, groupPath, placement, out var cachedEntry))
        {
            return cachedEntry;
        }

        var descriptors = CollectAggregateDescriptors(descriptor, placement);

        if (descriptors is null || descriptors.Count == 0)
        {
            return null;
        }

        var rows = sourceRows as IReadOnlyList<FastTreeDataGridRow> ?? sourceRows.ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        FastTreeDataGridAggregateDescriptor? labelDescriptor = descriptors.FirstOrDefault(d => string.IsNullOrEmpty(d.ColumnKey) && d.Label is not null);
        var labelKey = labelDescriptor?.ColumnKey ?? string.Empty;
        var labelText = labelDescriptor?.Label ?? (placement == FastTreeDataGridAggregatePlacement.GroupFooter ? "Group Total" : "Total");

        var summaryProvider = new FastTreeDataGridGeneratedSummaryRow(level, labelKey, labelText);

        foreach (var aggregateDescriptor in descriptors)
        {
            if (aggregateDescriptor.Provider is not null)
            {
                var contextDescriptor = descriptor ?? new FastTreeDataGridGroupDescriptor();
                var context = new FastTreeDataGridGroupContext(groupPath ?? string.Empty, level, groupKey, rows, contextDescriptor);
                var result = aggregateDescriptor.Provider.Calculate(context);

                if (result is not null)
                {
                    var targetColumnKey = result.ColumnKey ?? aggregateDescriptor.ColumnKey;
                    summaryProvider.SetValue(targetColumnKey, result.Value, _ => result.FormattedText);
                }

                continue;
            }

            if (aggregateDescriptor.Aggregator is null)
            {
                if (aggregateDescriptor.ColumnKey is null && aggregateDescriptor.Label is not null)
                {
                    summaryProvider.SetValue(aggregateDescriptor.ColumnKey, aggregateDescriptor.Label, aggregateDescriptor.Formatter);
                }

                continue;
            }

            var aggregateValue = aggregateDescriptor.Aggregator(rows);
            summaryProvider.SetValue(aggregateDescriptor.ColumnKey, aggregateValue, aggregateDescriptor.Formatter);
        }

        var summaryRow = new FastTreeDataGridRow(summaryProvider, level, hasChildren: false, isExpanded: false, OnNodeRequestMeasure)
        {
            HasChildren = false
        };
        summaryRow.Level = level;

        var entry = VisibleEntry.ForSummary(summaryRow, groupPath, level);
        CacheAggregateEntry(descriptor, groupPath, placement, entry);
        return entry;
    }

    private static void CollectGroupRows(GroupView view, List<FastTreeDataGridRow> buffer)
    {
        foreach (var node in view.Items)
        {
            buffer.Add(node.Row);
        }

        foreach (var child in view.Children)
        {
            CollectGroupRows(child, buffer);
        }
    }

    private static int CountGroupItems(GroupView view)
    {
        var count = view.Items.Count;
        foreach (var child in view.Children)
        {
            count += CountGroupItems(child);
        }

        return count;
    }

    private static string BuildGroupHeader(GroupView view, int itemCount)
    {
        if (view.Descriptor?.Adapter is not null)
        {
            try
            {
                return view.Descriptor.Adapter.GetGroupLabel(view.Key, view.Level, itemCount);
            }
            catch
            {
                // Fallback to default formatting below.
            }
        }

        if (view.Descriptor?.HeaderFormatter is { } formatter)
        {
            return formatter(new FastTreeDataGridGroupHeaderContext(view.Key, itemCount, view.Level));
        }

        var keyText = view.Key switch
        {
            null => "None",
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
            _ => view.Key?.ToString() ?? string.Empty
        };

        return string.Format(CultureInfo.CurrentCulture, "{0} ({1:N0})", keyText, itemCount);
    }

    private static int CompareGroupKeys(IComparer<object?>? comparer, object? left, object? right)
    {
        if (comparer is not null)
        {
            return comparer.Compare(left, right);
        }

        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        if (right is IComparable comparableRight)
        {
            return -comparableRight.CompareTo(left);
        }

        var leftText = left.ToString() ?? string.Empty;
        var rightText = right.ToString() ?? string.Empty;
        return string.Compare(leftText, rightText, StringComparison.CurrentCulture);
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

    private readonly struct AggregateCacheKey : IEquatable<AggregateCacheKey>
    {
        private readonly FastTreeDataGridGroupDescriptor? _descriptor;
        private readonly string _path;
        private readonly FastTreeDataGridAggregatePlacement _placement;

        public AggregateCacheKey(FastTreeDataGridGroupDescriptor? descriptor, string path, FastTreeDataGridAggregatePlacement placement)
        {
            _descriptor = descriptor;
            _path = path;
            _placement = placement;
        }

        public bool Equals(AggregateCacheKey other) => ReferenceEquals(_descriptor, other._descriptor) && string.Equals(_path, other._path, StringComparison.Ordinal) && _placement == other._placement;

        public override bool Equals(object? obj) => obj is AggregateCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_descriptor);
            hash.Add(_path, StringComparer.Ordinal);
            hash.Add((int)_placement);
            return hash.ToHashCode();
        }
    }

    private sealed class GroupView
    {
        public GroupView(string path, int level, FastTreeDataGridGroupDescriptor? descriptor, object? key, GroupView? parent)
        {
            Path = path;
            Level = level;
            Descriptor = descriptor;
            Key = key;
            Parent = parent;
        }

        public string Path { get; }

        public int Level { get; }

        public FastTreeDataGridGroupDescriptor? Descriptor { get; }

        public object? Key { get; }

        public GroupView? Parent { get; }

        public List<GroupView> Children { get; } = new();

        public List<TreeNode> Items { get; } = new();

        public bool IsExpanded { get; set; } = true;
    }

    private sealed class VisibleEntry
    {
        private VisibleEntry(FastTreeDataGridRow row, TreeNode? node, string? groupPath, bool isGroup, bool isSummary)
        {
            Row = row;
            Node = node;
            GroupPath = groupPath;
            IsGroup = isGroup;
            IsSummary = isSummary;
        }

        public static VisibleEntry ForNode(TreeNode node)
        {
            var entry = new VisibleEntry(node.Row, node, null, isGroup: false, isSummary: false)
            {
                HasChildren = node.HasChildren,
                IsExpanded = node.IsExpanded,
            };
            entry.Level = node.Row.Level;
        return entry;
    }


        public static VisibleEntry ForGroup(FastTreeDataGridRow row, string groupPath, bool isExpanded, bool hasChildren, int level)
        {
            var entry = new VisibleEntry(row, null, groupPath, isGroup: true, isSummary: false)
            {
                HasChildren = hasChildren,
                IsExpanded = isExpanded,
            };
            entry.Level = level;
            return entry;
        }

        public static VisibleEntry ForSummary(FastTreeDataGridRow row, string? groupPath, int level)
        {
            var entry = new VisibleEntry(row, null, groupPath, isGroup: false, isSummary: true)
            {
                HasChildren = false,
                IsExpanded = false,
            };
            entry.Level = level;
            return entry;
        }

        public FastTreeDataGridRow Row { get; }

        public TreeNode? Node { get; }

        public string? GroupPath { get; }

        public bool IsGroup { get; }

        public bool IsSummary { get; }

        public bool HasChildren { get; set; }

        public bool IsExpanded { get; set; }

        public int Level
        {
            get => Row.Level;
            set => Row.Level = value;
        }
    }

    private sealed class TreeNode
    {
        public TreeNode(T item, TreeNode? parent, int level, int originalIndex, string? key, Action requestMeasure)
        {
            Item = item;
            Parent = parent;
            Level = level;
            OriginalLevel = level;
            OriginalIndex = originalIndex;
            Key = key;
            Row = new FastTreeDataGridRow(item, level, hasChildren: false, isExpanded: false, requestMeasure);
        }

        public T Item { get; }

        public TreeNode? Parent { get; }

        public int Level { get; set; }

        public int OriginalLevel { get; }

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
