using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class DynamicCrudNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup, IEditableObject, INotifyPropertyChanged
{
    public const string KeyName = "DynamicCrud.Name";
    public const string KeyType = "DynamicCrud.Type";
    public const string KeyOwner = "DynamicCrud.Owner";
    public const string KeyProgress = "DynamicCrud.Progress";

    private readonly List<DynamicCrudNode> _children = new();
    private string _name;
    private string _owner;
    private double _progress;
    private EditSnapshot? _snapshot;

    private DynamicCrudNode(int id, DynamicCrudNodeKind kind, string name, string owner, double progress)
    {
        Id = id;
        Kind = kind;
        _name = name;
        _owner = owner;
        _progress = progress;
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }

    public DynamicCrudNodeKind Kind { get; }

    public int? ParentId { get; private set; }

    public DynamicCrudNode? Parent { get; private set; }

    public IReadOnlyList<DynamicCrudNode> Children => _children;

    public bool IsGroup => Kind == DynamicCrudNodeKind.Project;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value ?? string.Empty, KeyName, nameof(Name));
    }

    public string Owner
    {
        get => _owner;
        set => SetProperty(ref _owner, value ?? string.Empty, KeyOwner, nameof(Owner));
    }

    public double Progress
    {
        get => _progress;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 100.0);
            if (SetProperty(ref _progress, clamped, KeyProgress, nameof(Progress)))
            {
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

    public void BeginEdit()
    {
        _snapshot ??= new EditSnapshot(_name, _owner, _progress);
    }

    public void EndEdit()
    {
        _snapshot = null;
    }

    public void CancelEdit()
    {
        if (_snapshot is not { } snapshot)
        {
            return;
        }

        SetProperty(ref _name, snapshot.Name, KeyName, nameof(Name));
        SetProperty(ref _owner, snapshot.Owner, KeyOwner, nameof(Owner));
        SetProperty(ref _progress, snapshot.Progress, KeyProgress, nameof(Progress));
        Parent?.NotifyValueChanged(KeyProgress);
        _snapshot = null;
    }

    private bool SetProperty<T>(ref T storage, T value, string key, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        NotifyValueChanged(key);
        return true;
    }

    private readonly struct EditSnapshot
    {
        public EditSnapshot(string name, string owner, double progress)
        {
            Name = name;
            Owner = owner;
            Progress = progress;
        }

        public string Name { get; }

        public string Owner { get; }

        public double Progress { get; }
    }
}

public enum DynamicCrudNodeKind
{
    Project,
    Task,
}
