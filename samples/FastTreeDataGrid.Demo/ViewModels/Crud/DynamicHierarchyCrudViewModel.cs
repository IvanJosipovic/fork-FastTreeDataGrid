using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Demo.ViewModels;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class DynamicHierarchyCrudViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly FastTreeDataGridFlatSource<DynamicCrudNode> _source;
    private readonly AsyncCommand _addProjectCommand;
    private readonly AsyncCommand _addTaskCommand;
    private readonly AsyncCommand _saveCommand;
    private readonly AsyncCommand _deleteCommand;
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<DynamicCrudNode> _nodes = new();
    private readonly List<DynamicCrudNode> _tasksBuffer = new();
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private DynamicCrudNode? _selectedNode;
    private string _newProjectName = string.Empty;
    private string _newProjectOwner = "Team Lead";
    private string _newTaskName = string.Empty;
    private string _newTaskOwner = "Contributor";
    private double _newTaskProgress = 35.0;
    private string _editName = string.Empty;
    private string _editOwner = string.Empty;
    private double _editProgress;
    private string _status = "Ready";
    private string _selectionSummary = "Select a project to add tasks, or a task to edit progress.";
    private bool _disposed;
    private int _nextId = 1;

    public DynamicHierarchyCrudViewModel()
    {
        _source = new FastTreeDataGridFlatSource<DynamicCrudNode>(_nodes, node => node.Children);
        _addProjectCommand = new AsyncCommand(_ => AddProjectAsync(), _ => !string.IsNullOrWhiteSpace(NewProjectName));
        _addTaskCommand = new AsyncCommand(_ => AddTaskAsync(), _ => CanAddTask());
        _saveCommand = new AsyncCommand(_ => SaveAsync(), _ => CanSave());
        _deleteCommand = new AsyncCommand(_ => DeleteAsync(), _ => _selectedNode is not null);

        InitializeSampleData();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5),
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public AsyncCommand AddProjectCommand => _addProjectCommand;

    public AsyncCommand AddTaskCommand => _addTaskCommand;

    public AsyncCommand SaveCommand => _saveCommand;

    public AsyncCommand DeleteCommand => _deleteCommand;

    public IReadOnlyList<int> SelectedIndices
    {
        get => _selectedIndices;
        set
        {
            var normalized = value ?? Array.Empty<int>();
            if (SetProperty(ref _selectedIndices, normalized, nameof(SelectedIndices)))
            {
                UpdateSelection();
            }
        }
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set
        {
            if (SetProperty(ref _newProjectName, value ?? string.Empty, nameof(NewProjectName)))
            {
                _addProjectCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewProjectOwner
    {
        get => _newProjectOwner;
        set => SetProperty(ref _newProjectOwner, value ?? string.Empty, nameof(NewProjectOwner));
    }

    public string NewTaskName
    {
        get => _newTaskName;
        set
        {
            if (SetProperty(ref _newTaskName, value ?? string.Empty, nameof(NewTaskName)))
            {
                _addTaskCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewTaskOwner
    {
        get => _newTaskOwner;
        set
        {
            if (SetProperty(ref _newTaskOwner, value ?? string.Empty, nameof(NewTaskOwner)))
            {
                _addTaskCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double NewTaskProgress
    {
        get => _newTaskProgress;
        set
        {
            if (Math.Abs(_newTaskProgress - value) > 0.001)
            {
                _newTaskProgress = Math.Clamp(value, 0.0, 100.0);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewTaskProgress)));
            }
        }
    }

    public string EditName
    {
        get => _editName;
        set
        {
            if (SetProperty(ref _editName, value ?? string.Empty, nameof(EditName)))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditOwner
    {
        get => _editOwner;
        set
        {
            if (SetProperty(ref _editOwner, value ?? string.Empty, nameof(EditOwner)))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double EditProgress
    {
        get => _editProgress;
        set
        {
            if (Math.Abs(_editProgress - value) > 0.001)
            {
                _editProgress = Math.Clamp(value, 0.0, 100.0);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditProgress)));
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value, nameof(Status));
    }

    public string SelectionSummary
    {
        get => _selectionSummary;
        private set => SetProperty(ref _selectionSummary, value, nameof(SelectionSummary));
    }

    public bool IsProjectSelected => _selectedNode?.Kind == DynamicCrudNodeKind.Project;

    public bool IsTaskSelected => _selectedNode?.Kind == DynamicCrudNodeKind.Task;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void InitializeSampleData()
    {
        _nodes.Clear();
        var launch = DynamicCrudNode.CreateProject(
            NextId(),
            "Launch Dark Mode",
            "Amelia Carter",
            new[]
            {
                DynamicCrudNode.CreateTask(NextId(), 0, "Design audit", "Henry Singh", 60),
                DynamicCrudNode.CreateTask(NextId(), 0, "Theme tokens", "Priya Patel", 45),
                DynamicCrudNode.CreateTask(NextId(), 0, "QA regression", "Lucas Meyer", 20),
            });

        var migration = DynamicCrudNode.CreateProject(
            NextId(),
            "Migrate APIs",
            "Theo Morgan",
            new[]
            {
                DynamicCrudNode.CreateTask(NextId(), 0, "Inventory service", "Chen Wei", 75),
                DynamicCrudNode.CreateTask(NextId(), 0, "Billing hooks", "Lina Rossi", 55),
                DynamicCrudNode.CreateTask(NextId(), 0, "Monitoring rollout", "Noah Thompson", 35),
            });

        _nodes.Add(launch);
        _nodes.Add(migration);

        _source.Reset(_nodes, preserveExpansion: true);
        _source.ExpandAllGroups();
    }

    private int NextId() => _nextId++;

    private Task AddProjectAsync()
    {
        var name = NewProjectName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Status = "Project name cannot be empty.";
            return Task.CompletedTask;
        }

        var owner = string.IsNullOrWhiteSpace(NewProjectOwner) ? "Unassigned" : NewProjectOwner.Trim();
        var project = DynamicCrudNode.CreateProject(NextId(), name, owner, Array.Empty<DynamicCrudNode>());
        _nodes.Add(project);
        _source.Reset(_nodes, preserveExpansion: true);
        _source.ExpandAllGroups();
        NewProjectName = string.Empty;
        Status = $"Added project \"{name}\".";
        return Task.CompletedTask;
    }

    private Task AddTaskAsync()
    {
        if (!TryGetSelectedProject(out var project))
        {
            Status = "Select a project before adding tasks.";
            return Task.CompletedTask;
        }

        var name = NewTaskName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Status = "Task name cannot be empty.";
            return Task.CompletedTask;
        }

        var owner = string.IsNullOrWhiteSpace(NewTaskOwner) ? "Unassigned" : NewTaskOwner.Trim();
        var task = DynamicCrudNode.CreateTask(NextId(), project.Id, name, owner, NewTaskProgress);
        project.AddChild(task);
        _source.Reset(_nodes, preserveExpansion: true);
        _source.ExpandAllGroups();
        Status = $"Added task \"{name}\".";
        NewTaskName = string.Empty;
        return Task.CompletedTask;
    }

    private Task SaveAsync()
    {
        var node = _selectedNode;
        if (node is null)
        {
            return Task.CompletedTask;
        }

        var name = EditName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Status = "Name cannot be empty.";
            return Task.CompletedTask;
        }

        var owner = string.IsNullOrWhiteSpace(EditOwner) ? "Unassigned" : EditOwner.Trim();

        if (node.Kind == DynamicCrudNodeKind.Project)
        {
            node.Update(name, owner);
            Status = $"Updated project \"{name}\".";
        }
        else
        {
            node.Update(name, owner, EditProgress);
            Status = $"Updated task \"{name}\".";
        }

        _source.Reset(_nodes, preserveExpansion: true);
        _source.ExpandAllGroups();
        return Task.CompletedTask;
    }

    private Task DeleteAsync()
    {
        var node = _selectedNode;
        if (node is null)
        {
            return Task.CompletedTask;
        }

        if (node.Kind == DynamicCrudNodeKind.Project)
        {
            _nodes.Remove(node);
            Status = $"Deleted project \"{node.Name}\".";
        }
        else
        {
            node.Parent?.RemoveChild(node);
            Status = $"Deleted task \"{node.Name}\".";
        }

        SelectedIndices = Array.Empty<int>();
        _source.Reset(_nodes, preserveExpansion: true);
        _source.ExpandAllGroups();
        return Task.CompletedTask;
    }

    public Task CommitEditAsync(DynamicCrudNode node, CancellationToken cancellationToken)
    {
        if (node is null)
        {
            return Task.CompletedTask;
        }

        var name = string.IsNullOrWhiteSpace(node.Name) ? "Untitled" : node.Name.Trim();
        if (!string.Equals(node.Name, name, StringComparison.CurrentCulture))
        {
            node.Name = name;
        }

        var owner = string.IsNullOrWhiteSpace(node.Owner) ? "Unassigned" : node.Owner.Trim();
        if (!string.Equals(node.Owner, owner, StringComparison.CurrentCulture))
        {
            node.Owner = owner;
        }

        if (node.Kind == DynamicCrudNodeKind.Task)
        {
            var progress = Math.Clamp(node.Progress, 0.0, 100.0);
            if (Math.Abs(node.Progress - progress) > 0.001)
            {
                node.Progress = progress;
            }
        }

        Status = node.Kind == DynamicCrudNodeKind.Project
            ? $"Updated project \"{node.Name}\"."
            : $"Updated task \"{node.Name}\".";

        return Task.CompletedTask;
    }

    private void UpdateSelection()
    {
        DynamicCrudNode? selected = null;
        if (_selectedIndices.Count > 0)
        {
            var index = _selectedIndices[^1];
            if (_source.TryGetMaterializedRow(index, out var row) && row.Item is DynamicCrudNode node)
            {
                selected = node;
            }
        }

        _selectedNode = selected;
        if (selected is null)
        {
            SelectionSummary = "Select a project to add tasks, or a task to edit progress.";
            EditName = string.Empty;
            EditOwner = string.Empty;
            EditProgress = 0;
        }
        else
        {
            SelectionSummary = selected.Kind == DynamicCrudNodeKind.Project
                ? $"Project \"{selected.Name}\""
                : $"Task \"{selected.Name}\"";
            EditName = selected.Name;
            EditOwner = selected.Owner;
            EditProgress = selected.Kind == DynamicCrudNodeKind.Task ? selected.Progress : CalculateProjectAverage(selected);
        }

        RaiseSelectionPropertiesChanged();
        _addTaskCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
    }

    private void RaiseSelectionPropertiesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProjectSelected)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTaskSelected)));
    }

    private bool TryGetSelectedProject(out DynamicCrudNode project)
    {
        var node = _selectedNode;
        if (node?.Kind == DynamicCrudNodeKind.Project)
        {
            project = node;
            return true;
        }

        if (node?.Kind == DynamicCrudNodeKind.Task && node.Parent is { } parent)
        {
            project = parent;
            return true;
        }

        project = null!;
        return false;
    }

    private bool CanAddTask()
    {
        if (!TryGetSelectedProject(out _))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(NewTaskName) && !string.IsNullOrWhiteSpace(NewTaskOwner);
    }

    private bool CanSave()
    {
        if (_selectedNode is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            return false;
        }

        if (_selectedNode.Kind == DynamicCrudNodeKind.Task && (EditProgress < 0 || EditProgress > 100))
        {
            return false;
        }

        return true;
    }

    private double CalculateProjectAverage(DynamicCrudNode node)
    {
        if (node.Kind != DynamicCrudNodeKind.Project || node.Children.Count == 0)
        {
            return 0.0;
        }

        return node.Children.Average(child => child.Progress);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _tasksBuffer.Clear();
        foreach (var project in _nodes)
        {
            _tasksBuffer.AddRange(project.Children);
        }

        if (_tasksBuffer.Count == 0)
        {
            return;
        }

        var task = _tasksBuffer[_random.Next(_tasksBuffer.Count)];
        var delta = (_random.NextDouble() * 20.0) - 10.0;
        task.UpdateProgress(task.Progress + delta);
        Status = $"Auto-updated {task.Name} to {task.Progress:0}%";
    }

    private bool SetProperty<T>(ref T storage, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
