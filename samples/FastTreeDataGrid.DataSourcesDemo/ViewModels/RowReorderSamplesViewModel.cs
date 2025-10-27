using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.DataSourcesDemo.ViewModels;

public sealed class RowReorderSamplesViewModel : INotifyPropertyChanged
{
    private string _statusMessage;

    public RowReorderSamplesViewModel()
    {
        ReorderSettings = new FastTreeDataGridRowReorderSettings
        {
            IsEnabled = true,
            ActivationThreshold = 4,
            ShowDragPreview = true,
            DragPreviewBrush = new ImmutableSolidColorBrush(Color.FromArgb(48, 49, 130, 206)),
            DragPreviewOpacity = 0.85,
            DragPreviewCornerRadius = 6,
            ShowDropIndicator = true,
            DropIndicatorBrush = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            DropIndicatorThickness = 3,
            UseSelection = true,
        };

        var roadmap = BuildRoadmap();
        Roadmap = new FastTreeDataGridFlatSource<RoadmapNode>(
            roadmap,
            node => node.Children,
            node => node.Id,
            StringComparer.Ordinal);

        _statusMessage = "Drag backlog items to reprioritize. Milestones marked \uD83D\uDD12 remain pinned.";
    }

    public FastTreeDataGridFlatSource<RoadmapNode> Roadmap { get; }

    public FastTreeDataGridRowReorderSettings ReorderSettings { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool ContainsLocked(IReadOnlyList<int> indices)
    {
        if (indices is null || indices.Count == 0)
        {
            return false;
        }

        foreach (var index in indices)
        {
            if (TryGetNode(index) is { IsLocked: true })
            {
                return true;
            }
        }

        return false;
    }

    public void NotifyCancelled()
    {
        StatusMessage = "Those milestones are locked. Try moving feature rows or adjust your selection.";
    }

    public void NotifyCommitted(IReadOnlyList<int> newIndices)
    {
        if (newIndices is null || newIndices.Count == 0)
        {
            StatusMessage = "Drag backlog items to reprioritize. Milestones marked \uD83D\uDD12 remain pinned.";
            return;
        }

        var titles = new List<string>();
        foreach (var index in newIndices)
        {
            if (TryGetNode(index) is { } node)
            {
                titles.Add(node.Title);
            }
        }

        if (titles.Count == 0)
        {
            StatusMessage = "Backlog updated.";
            return;
        }

        StatusMessage = $"Moved {string.Join(", ", titles)} to slot {newIndices[0] + 1}.";
    }

    private static IReadOnlyList<RoadmapNode> BuildRoadmap()
    {
        var discovery = new RoadmapNode("Discovery & Research", "Evelyn Price", "In Review", 5, isLocked: true);
        discovery.AddChildren(
            new RoadmapNode("Usage analytics audit", "Max Benton", "In Progress", 3),
            new RoadmapNode("Persona refresh interviews", "Priya Desai", "In Review", 5),
            new RoadmapNode("Competitor deep dive", "Kai Jensen", "Planned", 2));

        var foundations = new RoadmapNode("Platform Foundations", "Mia Torres", "In Progress", 0);
        foundations.AddChildren(
            new RoadmapNode("Authentication hardening", "Mia Torres", "In Progress", 8),
            new RoadmapNode("Global feature flags", "Linh Nguyen", "In Progress", 5),
            new RoadmapNode("Telemetry pipeline v2", "Noah Clarke", "Planned", 8));

        var growth = new RoadmapNode("Growth Experiments", "Avery Chen", "Planned", 0);
        growth.AddChildren(
            new RoadmapNode("Self-serve onboarding 2.0", "Avery Chen", "Planned", 6),
            new RoadmapNode("Activation playbook revamp", "Rosa Martínez", "Planned", 4),
            new RoadmapNode("Referral loop prototype", "Samir Patel", "Backlog", 3));

        var polish = new RoadmapNode("Finish Line Polish", "Taylor Brooks", "Backlog", 0, isLocked: true);
        polish.AddChildren(
            new RoadmapNode("Accessibility sweep", "Jordan Lee", "Backlog", 4),
            new RoadmapNode("Performance regression fixes", "Mia Torres", "Backlog", 5),
            new RoadmapNode("Release notes automation", "Linh Nguyen", "Backlog", 2));

        return new[]
        {
            discovery,
            foundations,
            growth,
            polish,
        };
    }

    private RoadmapNode? TryGetNode(int index)
    {
        if (index < 0 || index >= Roadmap.RowCount)
        {
            return null;
        }

        if (Roadmap.TryGetMaterializedRow(index, out var row))
        {
            return row.Item as RoadmapNode;
        }

        try
        {
            return Roadmap.GetRow(index).Item as RoadmapNode;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
