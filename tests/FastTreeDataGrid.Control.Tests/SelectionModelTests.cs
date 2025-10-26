using System.Linq;
using FastTreeDataGrid.Control.Infrastructure;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public class SelectionModelTests
{
    [Fact]
    public void SelectSingle_SetsPrimaryAndAnchor()
    {
        var model = new FastTreeDataGridSelectionModel();

        model.SelectSingle(3);

        Assert.Equal(3, model.PrimaryIndex);
        Assert.Equal(3, model.AnchorIndex);
        Assert.Equal(new[] { 3 }, model.SelectedIndices);
    }

    [Fact]
    public void SelectRange_ExtendedModeCreatesContiguousSelection()
    {
        var model = new FastTreeDataGridSelectionModel
        {
            SelectionMode = FastTreeDataGridSelectionMode.Extended,
        };

        model.SelectSingle(2);
        model.SelectRange(2, 5, keepExisting: false);

        Assert.Equal(5, model.PrimaryIndex);
        Assert.Equal(2, model.AnchorIndex);
        Assert.True(model.SelectedIndices.SequenceEqual(new[] { 2, 3, 4, 5 }));
    }

    [Fact]
    public void Toggle_RemovesIndexAndUpdatesPrimary()
    {
        var model = new FastTreeDataGridSelectionModel
        {
            SelectionMode = FastTreeDataGridSelectionMode.Extended,
        };

        model.SelectRange(1, 3, keepExisting: false);
        model.Toggle(2);

        Assert.Equal(3, model.PrimaryIndex);
        Assert.True(model.SelectedIndices.SequenceEqual(new[] { 1, 3 }));
    }

    [Fact]
    public void CoerceSelection_RemovesOutOfRangeIndices()
    {
        var model = new FastTreeDataGridSelectionModel
        {
            SelectionMode = FastTreeDataGridSelectionMode.Extended,
        };

        model.SelectRange(0, 4, keepExisting: false);
        model.CoerceSelection(2);

        Assert.Equal(new[] { 0, 1 }, model.SelectedIndices);
        Assert.Equal(1, model.PrimaryIndex);
        Assert.Equal(0, model.AnchorIndex);
    }

    [Fact]
    public void SetSelection_ReplacesSelectionAndSorts()
    {
        var model = new FastTreeDataGridSelectionModel
        {
            SelectionMode = FastTreeDataGridSelectionMode.Extended,
        };

        model.SetSelection(new[] { 4, 2, 4, -1, 3 });

        Assert.True(model.SelectedIndices.SequenceEqual(new[] { 2, 3, 4 }));
        Assert.Equal(4, model.PrimaryIndex);
        Assert.Equal(2, model.AnchorIndex);
    }

    [Fact]
    public void SetSelection_HonorsSingleMode()
    {
        var model = new FastTreeDataGridSelectionModel
        {
            SelectionMode = FastTreeDataGridSelectionMode.Single,
        };

        model.SetSelection(new[] { 5, 1, 2 });

        Assert.True(model.SelectedIndices.SequenceEqual(new[] { 5 }));
        Assert.Equal(5, model.PrimaryIndex);
        Assert.Equal(5, model.AnchorIndex);
    }
}
