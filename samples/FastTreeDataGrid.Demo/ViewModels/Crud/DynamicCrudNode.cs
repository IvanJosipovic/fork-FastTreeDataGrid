using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class DynamicCrudNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyName = "DynamicCrud.Name";
    public const string KeyType = "DynamicCrud.Type";
    public const string KeyOwner = "DynamicCrud.Owner";
    public const string KeyProgress = "DynamicCrud.Progress";

    private readonly List<DynamicCrudNode> _children = new();
    private string _name;
    private string _owner;
    private double _progress;

    private DynamicCrudNode(int id, DynamicCrudNodeKind kind, string name, string owner, double progress)
    {
        Id = id;
        Kind = kind;
        _name = name;
        _owner = owner;
        _progress = progress;
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public int Id { get; }

    public DynamicCrudNodeKind Kind { get; }

    public int? ParentId { get; private set; }

    public DynamicCrudNode? Parent { get; private set; }

    public IReadOnlyList<DynamicCrudNode> Children => _children;

    public bool IsGroup => Kind == DynamicCrudNodeKind.Project;

    public string Name
    {
        get => _name;
        private set
        {
            if (!string.Equals(_name, value, StringComparison.CurrentCulture))
            {
                _name = value;
                NotifyValueChanged(KeyName);
            }
        }
    }

    public string Owner
    {
        get => _owner;
        private set
        {
            if (!string.Equals(_owner, value, StringComparison.CurrentCulture))
            {
                _owner = value;
                NotifyValueChanged(KeyOwner);
            }
        }
    }

    public double Progress
    {
        get => _progress;
        private set
        {
            var clamped = Math.Clamp(value, 0.0, 100.0);
            if (Math.Abs(_progress - clamped) > 0.001)
            {
                _progress = clamped;
                NotifyValueChanged(KeyProgress);
                Parent?.NotifyValueChanged(KeyProgress);
            }
        }
    }

    public static DynamicCrudNode CreateProject(int id, string name, string owner, IEnumerable<DynamicCrudNode> tasks)
    {
        var project = new DynamicCrudNode(id, DynamicCrudNodeKind.Project, name, owner, 0.0);
        foreach (var task in tasks ?? Enumerable.Empty<DynamicCrudNode>())
        {
            project.AddChild(task);
        }

        return project;
    }

    public static DynamicCrudNode CreateTask(int id, int projectId, string name, string owner, double progress)
    {
        var task = new DynamicCrudNode(id, DynamicCrudNodeKind.Task, name, owner, progress)
        {
            ParentId = projectId,
        };
        return task;
    }

    public void AddChild(DynamicCrudNode child)
    {
        if (child is null)
        {
            return;
        }

        child.Parent = this;
        child.ParentId = Id;
        _children.Add(child);
        NotifyValueChanged(KeyProgress);
    }

    public bool RemoveChild(DynamicCrudNode child)
    {
        if (child is null)
        {
            return false;
        }

        var removed = _children.Remove(child);
        if (removed)
        {
            child.Parent = null;
            child.ParentId = null;
            NotifyValueChanged(KeyProgress);
        }

        return removed;
    }

    public void Update(string name, string owner, double? progress = null)
    {
        Name = name;
        Owner = owner;
        if (progress is { } value && Kind == DynamicCrudNodeKind.Task)
        {
            Progress = value;
        }
    }

    public void UpdateProgress(double value)
    {
        if (Kind == DynamicCrudNodeKind.Task)
        {
            Progress = value;
        }
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyName => Name,
            KeyType => Kind == DynamicCrudNodeKind.Project ? "Project" : "Task",
            KeyOwner => Owner,
            KeyProgress => Kind == DynamicCrudNodeKind.Project
                ? $"{CalculateAverageProgress():0}%"
                : $"{Progress:0}%",
            _ => string.Empty,
        };
    }

    private double CalculateAverageProgress()
    {
        if (_children.Count == 0)
        {
            return 0.0;
        }

        return _children.Average(child => child.Progress);
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }
}

public enum DynamicCrudNodeKind
{
    Project,
    Task,
}
