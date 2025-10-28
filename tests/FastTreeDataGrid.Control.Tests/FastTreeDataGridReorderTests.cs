using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Infrastructure;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridReorderTests
{
    [Fact]
    public async Task ReorderRootNodesUpdatesVisibleOrder()
    {
        var roots = new[]
        {
            new TreeNodeModel("Alpha"),
            new TreeNodeModel("Beta"),
            new TreeNodeModel("Gamma"),
        };

        var source = new FastTreeDataGridFlatSource<TreeNodeModel>(roots, node => node.Children);
        var handler = (IFastTreeDataGridRowReorderHandler)source;

        var request = new FastTreeDataGridRowReorderRequest(new[] { 0 }, insertIndex: 2);

        Assert.True(handler.CanReorder(request));

        var result = await handler.ReorderAsync(request, CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal(new[] { 2 }, result.NewIndices);

        var order = Enumerable.Range(0, source.RowCount)
            .Select(i => ((TreeNodeModel)source.GetRow(i).Item!).Name)
            .ToArray();

        Assert.Equal(new[] { "Beta", "Gamma", "Alpha" }, order);
    }

    [Fact]
    public async Task CannotReorderAcrossDifferentParents()
    {
        var parent1 = new TreeNodeModel("Parent1", new[] { new TreeNodeModel("Child1") });
        var parent2 = new TreeNodeModel("Parent2");
        var roots = new[] { parent1, parent2 };

        var source = new FastTreeDataGridFlatSource<TreeNodeModel>(roots, node => node.Children);
        source.ToggleExpansion(0); // ensure child is visible

        Assert.Equal(3, source.RowCount);

        var handler = (IFastTreeDataGridRowReorderHandler)source;
        var request = new FastTreeDataGridRowReorderRequest(new[] { 1 }, insertIndex: 2);

        Assert.False(handler.CanReorder(request));

        var result = await handler.ReorderAsync(request, CancellationToken.None);
        Assert.False(result.Success);
    }

    private sealed class TreeNodeModel
    {
        public TreeNodeModel(string name)
        {
            Name = name;
            Children = new List<TreeNodeModel>();
        }

        public TreeNodeModel(string name, IEnumerable<TreeNodeModel> children)
        {
            Name = name;
            Children = new List<TreeNodeModel>(children);
        }

        public string Name { get; }

        public List<TreeNodeModel> Children { get; }
    }
}
