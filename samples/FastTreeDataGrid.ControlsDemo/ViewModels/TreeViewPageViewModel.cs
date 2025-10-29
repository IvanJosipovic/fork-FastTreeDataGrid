using System;
using System.Collections.Generic;
using System.ComponentModel;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class TreeViewPageViewModel : IDisposable
{
    private readonly FastTreeDataGridFlatSource<TreeNode> _source;

    public TreeViewPageViewModel()
    {
        var roots = CreateTree();
        _source = new FastTreeDataGridFlatSource<TreeNode>(roots, node => node.Children);

        if (_source.RowCount > 0)
        {
            _source.ToggleExpansion(0);
        }
    }

    public IFastTreeDataGridSource Source => _source;

    public void Dispose()
    {
    }

    private static IReadOnlyList<TreeNode> CreateTree()
    {
        var project = new TreeNode("FastTreeDataGrid", "Solution", 0, new[]
        {
            new TreeNode("samples", "Folder", 0, new[]
            {
                new TreeNode("ControlsDemo", "Project", 0.65, Array.Empty<TreeNode>()),
                new TreeNode("WidgetsDemo", "Project", 0.9, Array.Empty<TreeNode>()),
                new TreeNode("VirtualizationDemo", "Project", 0.75, Array.Empty<TreeNode>()),
            }),
            new TreeNode("src", "Folder", 0, new[]
            {
                new TreeNode("FastTreeDataGrid.Control", "Project", 0.82, Array.Empty<TreeNode>()),
                new TreeNode("FastTreeDataGrid.Engine", "Project", 0.88, Array.Empty<TreeNode>()),
                new TreeNode("FastTreeDataGrid.Widgets", "Project", 0.54, Array.Empty<TreeNode>()),
            }),
            new TreeNode("tests", "Folder", 0, new[]
            {
                new TreeNode("FastTreeDataGrid.Control.Tests", "Project", 0.64, Array.Empty<TreeNode>()),
                new TreeNode("FastTreeDataGrid.Engine.Tests", "Project", 0.58, Array.Empty<TreeNode>()),
            }),
        });

        var docs = new TreeNode("docs", "Folder", 0, new[]
        {
            new TreeNode("virtualization", "Folder", 0, new[]
            {
                new TreeNode("providers.md", "Markdown", 1, Array.Empty<TreeNode>()),
                new TreeNode("guides.md", "Markdown", 0.4, Array.Empty<TreeNode>()),
            }),
            new TreeNode("architecture.md", "Markdown", 0.7, Array.Empty<TreeNode>()),
        });

        return new[] { project, docs };
    }
}

public sealed class TreeNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup, INotifyPropertyChanged
{
    public const string KeyName = nameof(Name);
    public const string KeyKind = nameof(Kind);
    public const string KeyProgress = nameof(Progress);

    private double _progress;

    public TreeNode(string name, string kind, double progress, IReadOnlyList<TreeNode> children)
    {
        Name = name;
        Kind = kind;
        _progress = progress;
        Children = children ?? Array.Empty<TreeNode>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public string Name { get; }

    public string Kind { get; }

    public IReadOnlyList<TreeNode> Children { get; }

    public bool IsGroup => Children.Count > 0;

    public double Progress
    {
        get => _progress;
        set
        {
            if (Math.Abs(_progress - value) < 0.0001)
            {
                return;
            }

            _progress = Math.Clamp(value, 0, 1);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
            ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, nameof(Progress)));
        }
    }

    public object? GetValue(object? item, string key) =>
        key switch
        {
            KeyName => Name,
            KeyKind => Kind,
            KeyProgress => Progress,
            _ => null,
        };
}
