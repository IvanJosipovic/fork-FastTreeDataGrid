using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using Xunit;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridGroupingLayoutTests
{
    [Fact]
    public void LayoutDefaultsToVersionOne()
    {
        var layout = new FastTreeDataGridGroupingLayout();
        Assert.Equal(1, layout.Version);
    }

    [Fact]
    public void SerializeAndDeserialize_RetainsVersion()
    {
        var layout = new FastTreeDataGridGroupingLayout
        {
            Version = 1,
        };
        layout.Groups.Add(new FastTreeDataGridGroupingLayoutDescriptor
        {
            ColumnKey = "Region",
            SortDirection = FastTreeDataGridSortDirection.Descending,
        });
        layout.ExpansionStates.Add(new FastTreeDataGridGroupingExpansionState
        {
            Path = "|0:Region",
            IsExpanded = false,
        });

        var json = FastTreeDataGridGroupingLayoutSerializer.Serialize(layout);
        var restored = FastTreeDataGridGroupingLayoutSerializer.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(1, restored!.Version);
        Assert.NotNull(restored);
        Assert.Single(restored!.Groups);
        Assert.Single(restored.ExpansionStates);
    }

    [Fact]
    public void DeserializeWithoutVersion_DefaultsToOne()
    {
        var json = "{\"groups\":[],\"expansionStates\":[]}";
        var layout = FastTreeDataGridGroupingLayoutSerializer.Deserialize(json);
        Assert.NotNull(layout);
        Assert.Equal(1, layout!.Version);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesGroupOrder()
    {
        var layout = new FastTreeDataGridGroupingLayout
        {
            Version = 1,
        };

        layout.Groups.Add(new FastTreeDataGridGroupingLayoutDescriptor { ColumnKey = "Region" });
        layout.Groups.Add(new FastTreeDataGridGroupingLayoutDescriptor { ColumnKey = "Product" });
        layout.Groups.Add(new FastTreeDataGridGroupingLayoutDescriptor { ColumnKey = "Year" });

        var json = FastTreeDataGridGroupingLayoutSerializer.Serialize(layout);
        var restored = FastTreeDataGridGroupingLayoutSerializer.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Collection(
            restored!.Groups,
            g => Assert.Equal("Region", g.ColumnKey),
            g => Assert.Equal("Product", g.ColumnKey),
            g => Assert.Equal("Year", g.ColumnKey));
    }

    [AvaloniaFact]
    public void ToggleExpansion_UpdatesExpansionStateInLayout()
    {
        var grid = new GridControl();
        grid.GroupDescriptors.Add(new FastTreeDataGridGroupDescriptor { ColumnKey = "GroupKey" });

        var groupRowItem = new StubGroupRow("|0:Group", "Group Header", itemCount: 3);
        var row = new FastTreeDataGridRow(groupRowItem, level: 0, hasChildren: true, isExpanded: true, requestMeasureCallback: () => { });
        var source = new StubGroupingSource(row);

        SetItemsSource(grid, source);

        var layoutBefore = grid.GetGroupingLayout();
        Assert.Empty(layoutBefore.ExpansionStates);

        var toggle = typeof(GridControl).GetMethod("ToggleExpansionAt", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(toggle);
        toggle!.Invoke(grid, new object[] { 0, false });

        var layoutAfter = grid.GetGroupingLayout();
        var state = Assert.Single(layoutAfter.ExpansionStates);
        Assert.Equal("|0:Group", state.Path);
        Assert.False(state.IsExpanded);
    }

    [AvaloniaFact]
    public void ApplyGroupingLayout_ForwardsExpansionStatesToController()
    {
        var grid = new GridControl();
        grid.Columns.Add(new FastTreeDataGridColumn { ValueKey = "GroupKey", Header = "Group" });

        var groupRowItem = new StubGroupRow("|0:Group", "Group Header", itemCount: 2);
        var row = new FastTreeDataGridRow(groupRowItem, level: 0, hasChildren: true, isExpanded: true, requestMeasureCallback: () => { });
        var source = new StubGroupingSource(row);

        grid.ItemsSource = source;

        var layout = new FastTreeDataGridGroupingLayout();
        layout.Groups.Add(new FastTreeDataGridGroupingLayoutDescriptor
        {
            ColumnKey = "GroupKey",
            IsExpanded = false,
        });
        layout.ExpansionStates.Add(new FastTreeDataGridGroupingExpansionState
        {
            Path = "|0:Group",
            IsExpanded = false,
        });

        grid.ApplyGroupingLayout(layout);

        Assert.True(source.ApplyGroupStateCalled);
        Assert.False(source.LastDefaultExpanded);
        var state = Assert.Single(source.LastExpansionStates);
        Assert.Equal("|0:Group", state.Path);
        Assert.False(state.IsExpanded);
    }

    private static void SetItemsSource(GridControl grid, IFastTreeDataGridSource source)
    {
        var field = typeof(GridControl).GetField("_itemsSource", BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(grid, source);
    }

    private sealed class StubGroupRow : IFastTreeDataGridValueProvider, IFastTreeDataGridGroupMetadata, IFastTreeDataGridGroupPathProvider
    {
        public StubGroupRow(string groupPath, string header, int itemCount)
        {
            GroupPath = groupPath;
            Header = header;
            ItemCount = itemCount;
        }

        public string GroupPath { get; }

        public string Header { get; }

        public int ItemCount { get; }

        public bool IsGroup => true;

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }

        public object? GetValue(object? item, string key)
        {
            return key switch
            {
                "GroupKey" => Header,
                "FastTreeDataGrid.Group.Header" => Header,
                _ => string.Empty,
            };
        }
    }

    private sealed class StubGroupingSource : IFastTreeDataGridSource, IFastTreeDataGridGroupingController
    {
        public StubGroupingSource(FastTreeDataGridRow row)
        {
            Row = row;
        }

        public FastTreeDataGridRow Row { get; }

        public bool ApplyGroupStateCalled { get; private set; }

        public bool LastDefaultExpanded { get; private set; }

        public List<FastTreeDataGridGroupingExpansionState> LastExpansionStates { get; } = new();

        public event EventHandler? ResetRequested
        {
            add { }
            remove { }
        }

        public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated
        {
            add { }
            remove { }
        }

        public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized
        {
            add { }
            remove { }
        }

        public int RowCount => 1;

        public bool SupportsPlaceholders => false;

        public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) => new(RowCount);

        public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) => new(FastTreeDataGridPageResult.Empty);

        public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

        public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
        {
            row = Row;
            return index == 0;
        }

        public bool IsPlaceholder(int index) => false;

        public FastTreeDataGridRow GetRow(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Row;
        }

        public void ToggleExpansion(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var newState = !Row.IsExpanded;
            var property = typeof(FastTreeDataGridRow).GetProperty("IsExpanded", BindingFlags.Instance | BindingFlags.Public);
            var setter = property?.GetSetMethod(nonPublic: true);
            setter?.Invoke(Row, new object[] { newState });
        }

        public void ApplyGroupExpansionLayout(IEnumerable<FastTreeDataGridGroupingExpansionState> states, bool defaultExpanded)
        {
            ApplyGroupStateCalled = true;
            LastDefaultExpanded = defaultExpanded;
            LastExpansionStates.Clear();

            if (states is not null)
            {
                foreach (var state in states)
                {
                    if (state is null)
                    {
                        continue;
                    }

                    LastExpansionStates.Add(new FastTreeDataGridGroupingExpansionState
                    {
                        Path = state.Path,
                        IsExpanded = state.IsExpanded,
                    });
                }
            }
        }

        public void ExpandAllGroups()
        {
        }

        public void CollapseAllGroups()
        {
        }
    }
}
