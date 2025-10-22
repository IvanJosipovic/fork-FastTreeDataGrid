using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.FileSystem;

public sealed class FileSystemTreeSource : IFastTreeDataGridSource, IDisposable
{
    private readonly List<TreeNode> _rootNodes = new();
    private readonly List<TreeNode> _visibleNodes = new();
    private readonly Dispatcher _dispatcher = Dispatcher.UIThread;
    private readonly Comparison<TreeNode> _defaultOrder = (left, right) => left.InsertionIndex.CompareTo(right.InsertionIndex);

    private bool _disposed;
    private int _nextInsertionIndex;
    private Comparison<TreeNode>? _nodeComparison;
    private Func<FileSystemNode, bool>? _filterPredicate;
    private bool _filterActive;
    private bool _autoExpandFilteredMatches = true;

    public FileSystemTreeSource()
    {
        BuildRoots();
        UpdateFilterStates();
        RebuildVisible();
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount => _visibleNodes.Count;

    public bool SupportsPlaceholders => true;

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
        if ((uint)index < (uint)_visibleNodes.Count)
        {
            row = _visibleNodes[index].Row;
            return true;
        }

        row = default!;
        return false;
    }

    public bool IsPlaceholder(int index)
    {
        if ((uint)index >= (uint)_visibleNodes.Count)
        {
            return false;
        }

        var node = _visibleNodes[index];
        return node.IsLoading && !node.IsLoaded;
    }

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
        if (!node.Item.IsDirectory)
        {
            return;
        }

        if (!node.IsLoaded && !node.IsLoading)
        {
            BeginLoad(node);
            return;
        }

        if (node.IsLoading)
        {
            return;
        }

        if (_filterActive && node.IsLoaded && !node.HasVisibleChildren)
        {
            return;
        }

        node.IsExpanded = !node.IsExpanded;
        RebuildVisible();
        RaiseResetRequested();
    }

    public void Sort(Comparison<FastTreeDataGridRow> comparison)
    {
        if (comparison is null)
        {
            return;
        }

        _nodeComparison = WrapComparison(comparison);
        ApplySort();
    }

    public void ResetSort()
    {
        _nodeComparison = null;
        ApplySort();
    }

    internal void ApplyFilter(Func<FileSystemNode, bool>? predicate, bool expandMatches = true)
    {
        var hadFilter = _filterActive;

        _filterPredicate = predicate;
        _filterActive = predicate is not null;
        _autoExpandFilteredMatches = expandMatches;

        if (!hadFilter && _filterActive)
        {
            CaptureExpansionStates();
        }
        else if (hadFilter && !_filterActive)
        {
            RestoreExpansionStates();
        }

        UpdateFilterStates();
        RebuildVisible();
        RaiseResetRequested();
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void BuildRoots()
    {
        _rootNodes.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var rootNode = CreateNode(FileSystemNode.CreateDirectory(drive.Name, drive.RootDirectory.FullName, drive.RootDirectory.LastWriteTimeUtc));
            _rootNodes.Add(rootNode);
        }

        ApplySortToList(_rootNodes);
    }

    private void BeginLoad(TreeNode node)
    {
        node.IsExpanded = true;
        node.IsLoading = true;
        node.Children.Clear();
        node.Children.Add(CreateNode(FileSystemNode.CreateLoading("Loading..."), node.Level + 1));
        RebuildVisible();
        RaiseResetRequested();

        _ = LoadChildrenAsync(node);
    }

    private async Task LoadChildrenAsync(TreeNode node)
    {
        try
        {
            var items = await Task.Run(() => EnumerateChildren(node.Item.FullPath));
            if (_disposed)
            {
                return;
            }

            await _dispatcher.InvokeAsync(() => ApplyLoadedChildren(node, items));
        }
        catch
        {
            await _dispatcher.InvokeAsync(() => ApplyLoadedChildren(node, Array.Empty<FileSystemNode>()));
        }
    }

    private void ApplyLoadedChildren(TreeNode node, IReadOnlyList<FileSystemNode> children)
    {
        node.Children.Clear();
        foreach (var child in children)
        {
            node.Children.Add(CreateNode(child, node.Level + 1));
        }

        node.IsLoaded = true;
        node.IsLoading = false;

        ApplySortToSubtree(node);
        UpdateFilterStates();

        if (!_filterActive)
        {
            node.IsExpanded = node.Children.Count > 0;
        }
        else if (_autoExpandFilteredMatches)
        {
            node.IsExpanded = node.HasVisibleChildren || node.MatchesFilter;
        }

        RebuildVisible();
        RaiseResetRequested();
    }

    private IReadOnlyList<FileSystemNode> EnumerateChildren(string path)
    {
        var list = new List<FileSystemNode>();
        try
        {
            var directoryInfo = new DirectoryInfo(path);
            foreach (var dir in directoryInfo.EnumerateDirectories())
            {
                list.Add(FileSystemNode.CreateDirectory(dir.Name, dir.FullName, dir.LastWriteTimeUtc));
            }

            foreach (var file in directoryInfo.EnumerateFiles())
            {
                list.Add(FileSystemNode.CreateFile(file.Name, file.FullName, file.Length, file.LastWriteTimeUtc, file.Extension));
            }
        }
        catch
        {
            // Ignore directories we cannot access.
        }

        return list;
    }

    private void ApplySort()
    {
        ApplySortToList(_rootNodes);
        foreach (var root in _rootNodes)
        {
            ApplySortToSubtree(root);
        }

        RebuildVisible();
        RaiseResetRequested();
    }

    private void ApplySortToSubtree(TreeNode node)
    {
        if (node.Children.Count == 0)
        {
            return;
        }

        ApplySortToList(node.Children);
        foreach (var child in node.Children)
        {
            ApplySortToSubtree(child);
        }
    }

    private void ApplySortToList(List<TreeNode> nodes)
    {
        if (_nodeComparison is not null)
        {
            nodes.Sort(_nodeComparison);
        }
        else
        {
            nodes.Sort(_defaultOrder);
        }
    }

    private void UpdateFilterStates()
    {
        if (!_filterActive)
        {
            ForEachNode(node =>
            {
                node.MatchesFilter = true;
                node.HasVisibleChildren = DetermineHasChildren(node);
                node.IsFilterIncluded = true;
            });
            return;
        }

        foreach (var root in _rootNodes)
        {
            EvaluateFilter(root);
        }
    }

    private bool EvaluateFilter(TreeNode node)
    {
        var matches = node.Item.IsPlaceholder || _filterPredicate?.Invoke(node.Item) == true;
        var hasMatchingChild = false;

        foreach (var child in node.Children)
        {
            if (EvaluateFilter(child))
            {
                hasMatchingChild = true;
            }
        }

        node.MatchesFilter = matches;
        var hasPotentialChildren = node.Item.IsDirectory && (!node.IsLoaded || node.IsLoading);
        node.HasVisibleChildren = hasMatchingChild || hasPotentialChildren;
        node.IsFilterIncluded = matches || hasMatchingChild;

        if (_autoExpandFilteredMatches && node.Item.IsDirectory)
        {
            if (!node.StoredExpansionState.HasValue)
            {
                node.StoredExpansionState = node.IsExpanded;
            }

            node.IsExpanded = node.HasVisibleChildren || matches;
        }

        return node.IsFilterIncluded;
    }

    private void CaptureExpansionStates()
    {
        ForEachNode(node =>
        {
            if (node.Item.IsDirectory && !node.StoredExpansionState.HasValue)
            {
                node.StoredExpansionState = node.IsExpanded;
            }
        });
    }

    private void RestoreExpansionStates()
    {
        ForEachNode(node =>
        {
            if (node.StoredExpansionState.HasValue)
            {
                node.IsExpanded = node.StoredExpansionState.Value;
                node.StoredExpansionState = null;
            }
        });
    }

    private void RebuildVisible()
    {
        _visibleNodes.Clear();
        foreach (var root in _rootNodes)
        {
            AddVisible(root, 0);
        }
    }

    private void AddVisible(TreeNode node, int level)
    {
        if (_filterActive && !node.IsFilterIncluded)
        {
            return;
        }

        node.Level = level;
        var hasChildren = GetEffectiveHasChildren(node);
        node.RecreateRow(level, node.IsExpanded, hasChildren);
        _visibleNodes.Add(node);

        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                AddVisible(child, level + 1);
            }
        }
    }

    private static Comparison<TreeNode> WrapComparison(Comparison<FastTreeDataGridRow> comparison) =>
        (left, right) => comparison(left.Row, right.Row);

    private static bool DetermineHasChildren(TreeNode node) =>
        node.Item.IsDirectory && (!node.IsLoaded || node.IsLoading || node.Children.Count > 0);

    private bool GetEffectiveHasChildren(TreeNode node)
    {
        if (!_filterActive)
        {
            return DetermineHasChildren(node);
        }

        if (!node.Item.IsDirectory)
        {
            return false;
        }

        if (!node.IsLoaded || node.IsLoading)
        {
            return true;
        }

        return node.HasVisibleChildren;
    }

    private TreeNode CreateNode(FileSystemNode item, int level = 0)
    {
        var node = new TreeNode(item, level, RequestRefresh, _nextInsertionIndex++);
        node.RecreateRow(level, isExpanded: false, DetermineHasChildren(node));
        return node;
    }

    private void RequestRefresh()
    {
        UpdateFilterStates();
        RebuildVisible();
        RaiseResetRequested();
    }

    private void RaiseResetRequested()
    {
        RaiseInvalidated(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full));

        var handler = ResetRequested;
        if (handler is null)
        {
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            handler.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _dispatcher.Post(() => handler.Invoke(this, EventArgs.Empty), DispatcherPriority.Background);
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
        if (_dispatcher.CheckAccess())
        {
            handler.Invoke(this, args);
        }
        else
        {
            _dispatcher.Post(() => handler.Invoke(this, args), DispatcherPriority.Background);
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

        void InvokeHandlers()
        {
            for (var i = 0; i < rows.Count; i++)
            {
                handler.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(startIndex + i, rows[i]));
            }
        }

        if (_dispatcher.CheckAccess())
        {
            InvokeHandlers();
        }
        else
        {
            _dispatcher.Post(InvokeHandlers, DispatcherPriority.Background);
        }
    }

    private void ForEachNode(Action<TreeNode> action)
    {
        foreach (var root in _rootNodes)
        {
            ForEachNode(root, action);
        }
    }

    private void ForEachNode(TreeNode node, Action<TreeNode> action)
    {
        action(node);
        foreach (var child in node.Children)
        {
            ForEachNode(child, action);
        }
    }

    private sealed class TreeNode
    {
        private readonly Action _requestRefresh;

        public TreeNode(FileSystemNode item, int level, Action requestRefresh, int insertionIndex)
        {
            Item = item;
            Level = level;
            _requestRefresh = requestRefresh;
            InsertionIndex = insertionIndex;
            Row = new FastTreeDataGridRow(item, level, item.IsDirectory, isExpanded: false, requestRefresh);
        }

        public FileSystemNode Item { get; }
        public int Level { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsLoading { get; set; }
        public int InsertionIndex { get; }
        public bool MatchesFilter { get; set; } = true;
        public bool HasVisibleChildren { get; set; }
        public bool IsFilterIncluded { get; set; } = true;
        public bool? StoredExpansionState { get; set; }
        public List<TreeNode> Children { get; } = new();
        public FastTreeDataGridRow Row { get; private set; }

        public void RecreateRow(int level, bool isExpanded, bool hasChildren)
        {
            Row = new FastTreeDataGridRow(Item, level, hasChildren, isExpanded, _requestRefresh);
        }
    }
}
