using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.FileSystem;

public sealed class FileSystemTreeSource : IFastTreeDataGridSource, IDisposable
{
    private readonly List<TreeNode> _rootNodes = new();
    private readonly List<TreeNode> _visibleNodes = new();
    private readonly Dispatcher _dispatcher = Dispatcher.UIThread;
    private bool _disposed;

    public FileSystemTreeSource()
    {
        BuildRoots();
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

        node.IsExpanded = !node.IsExpanded;
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Sort(Comparison<FastTreeDataGridRow> comparison)
    {
        // Sorting not implemented for file system source.
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
    }

    private void BeginLoad(TreeNode node)
    {
        node.IsExpanded = true;
        node.IsLoading = true;
        node.Children.Clear();
        node.Children.Add(CreateNode(FileSystemNode.CreateLoading("Loading..."), node.Level + 1));
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);

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
        node.IsExpanded = node.Children.Count > 0;
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
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

        return list
            .OrderBy(node => node.IsDirectory ? 0 : 1)
            .ThenBy(node => node.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private void RebuildVisible()
    {
        _visibleNodes.Clear();
        for (var i = 0; i < _rootNodes.Count; i++)
        {
            AddVisible(_rootNodes[i], 0);
        }
    }

    private void AddVisible(TreeNode node, int level)
    {
        node.Level = level;
        node.RecreateRow(level, node.IsExpanded, DetermineHasChildren(node));
        _visibleNodes.Add(node);

        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                AddVisible(child, level + 1);
            }
        }
    }

    private static bool DetermineHasChildren(TreeNode node) =>
        node.Item.IsDirectory && (!node.IsLoaded || node.IsLoading || node.Children.Count > 0);

    private TreeNode CreateNode(FileSystemNode item, int level = 0)
    {
        var node = new TreeNode(item, level, RequestRefresh);
        node.RecreateRow(level, isExpanded: false, DetermineHasChildren(node));
        return node;
    }

    private void RequestRefresh()
    {
        RebuildVisible();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class TreeNode
    {
        private readonly Action _requestRefresh;

        public TreeNode(FileSystemNode item, int level, Action requestRefresh)
        {
            Item = item;
            Level = level;
            _requestRefresh = requestRefresh;
            Row = new FastTreeDataGridRow(item, level, item.IsDirectory, isExpanded: false, requestRefresh);
        }

        public FileSystemNode Item { get; }
        public int Level { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsLoading { get; set; }
        public List<TreeNode> Children { get; } = new();
        public FastTreeDataGridRow Row { get; private set; }

        public void RecreateRow(int level, bool isExpanded, bool hasChildren)
        {
            Row = new FastTreeDataGridRow(Item, level, hasChildren, isExpanded, _requestRefresh);
        }
    }
}
