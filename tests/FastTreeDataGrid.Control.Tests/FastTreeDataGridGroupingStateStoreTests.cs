using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridGroupingStateStoreTests
{
    [Fact]
    public void SetDescriptorsStoresDescriptorsAndRaisesChange()
    {
        var store = new FastTreeDataGridGroupingStateStore();
        var events = new List<FastTreeDataGridGroupingStateChangedEventArgs>();
        store.GroupingStateChanged += (_, e) => events.Add(e);

        var descriptors = new[]
        {
            new FastTreeDataGridGroupDescriptor { ColumnKey = "Region" },
            new FastTreeDataGridGroupDescriptor { ColumnKey = "Product" },
        };

        store.SetDescriptors(descriptors);

        Assert.Collection(
            store.Descriptors,
            d => Assert.Same(descriptors[0], d),
            d => Assert.Same(descriptors[1], d));

        var change = Assert.Single(events);
        Assert.Equal(FastTreeDataGridGroupingChangeKind.DescriptorsChanged, change.Kind);
        Assert.Null(change.Path);
    }

    [Fact]
    public void UpdateGroupPersistsStateAndNotifies()
    {
        var store = new FastTreeDataGridGroupingStateStore();
        var events = new List<FastTreeDataGridGroupingStateChangedEventArgs>();
        store.GroupingStateChanged += (_, e) => events.Add(e);

        var descriptor = new FastTreeDataGridGroupDescriptor { ColumnKey = "Region" };
        var state = new FastTreeDataGridGroupState(descriptor, "|0:Region", 0, "North", true);

        store.UpdateGroup(state.Path, state);

        var retrieved = store.GetState(state.Path);
        Assert.Same(state, retrieved);

        var change = Assert.Single(events);
        Assert.Equal(FastTreeDataGridGroupingChangeKind.GroupStateChanged, change.Kind);
        Assert.Equal(state.Path, change.Path);
    }

    [Fact]
    public void SetExpandedCreatesOrUpdatesState()
    {
        var store = new FastTreeDataGridGroupingStateStore();

        store.SetExpanded("|0:Region", false);
        Assert.False(store.GetState("|0:Region").IsExpanded);

        store.SetExpanded("|0:Region", true);
        Assert.True(store.GetState("|0:Region").IsExpanded);
    }

    [Fact]
    public void SnapshotReturnsDescriptorsAndGroupStates()
    {
        var store = new FastTreeDataGridGroupingStateStore();

        var descriptor = new FastTreeDataGridGroupDescriptor { ColumnKey = "Region" };
        store.SetDescriptors(new[] { descriptor });
        store.UpdateGroup("|0:Region", new FastTreeDataGridGroupState(descriptor, "|0:Region", 0, "North", true));

        var snapshot = store.CreateSnapshot();

        Assert.Single(snapshot.Descriptors);
        Assert.Same(descriptor, snapshot.Descriptors[0]);

        var group = Assert.Single(snapshot.Groups);
        Assert.Equal("|0:Region", group.Path);
        Assert.Equal("North", group.Key);
        Assert.True(group.IsExpanded);
    }

    [Fact]
    public void ClearRemovesGroupsAndRaisesReset()
    {
        var store = new FastTreeDataGridGroupingStateStore();
        var events = new List<FastTreeDataGridGroupingStateChangedEventArgs>();
        store.GroupingStateChanged += (_, e) => events.Add(e);

        store.SetExpanded("|0:Region", true);
        store.Clear();

        var change = Assert.Single(events, e => e.Kind == FastTreeDataGridGroupingChangeKind.Reset);
        Assert.Equal(FastTreeDataGridGroupingChangeKind.Reset, change.Kind);

        var snapshot = store.CreateSnapshot();
        Assert.Empty(snapshot.Groups);
    }
}
