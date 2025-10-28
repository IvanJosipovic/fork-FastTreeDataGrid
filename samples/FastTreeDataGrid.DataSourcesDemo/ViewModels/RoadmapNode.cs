using System;
using System.Collections.Generic;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.DataSourcesDemo.ViewModels;

public sealed class RoadmapNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyTitle = "Roadmap.Title";
    public const string KeyOwner = "Roadmap.Owner";
    public const string KeyStatus = "Roadmap.Status";
    public const string KeyEffort = "Roadmap.Effort";

    private static int s_counter;
    private readonly List<RoadmapNode> _children = new();

    public RoadmapNode(string title, string owner, string status, int effort, bool isLocked = false)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Owner = owner;
        Status = status;
        Effort = effort;
        IsLocked = isLocked;
        Id = $"roadmap:{++s_counter:D4}";
    }

    public string Id { get; }

    public string Title { get; }

    public string Owner { get; }

    public string Status { get; }

    public int Effort { get; }

    public bool IsLocked { get; }

    public IReadOnlyList<RoadmapNode> Children => _children;

    public bool IsGroup => _children.Count > 0;

    public RoadmapNode AddChildren(params RoadmapNode[] children)
    {
        if (children is null)
        {
            return this;
        }

        foreach (var child in children)
        {
            if (child is null)
            {
                continue;
            }

            _children.Add(child);
        }

        return this;
    }

    public object? GetValue(object? item, string key) => key switch
    {
        KeyTitle => IsLocked ? "\uD83D\uDD12 " + Title : Title,
        KeyOwner => string.IsNullOrWhiteSpace(Owner) ? "—" : Owner,
        KeyStatus => string.IsNullOrWhiteSpace(Status) ? "—" : Status,
        KeyEffort => Effort > 0 ? $"{Effort} pts" : "—",
        _ => null
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }
}
