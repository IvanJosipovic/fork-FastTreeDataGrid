using System.Collections.Generic;
using Avalonia;
using Avalonia.Headless.XUnit;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Engine.Infrastructure;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridPresenterPatchTests
{
    [AvaloniaFact]
    public void ApplyColumnPatch_UpdatesPlaceholderCellInPlace()
    {
        var presenter = new FastTreeDataGridPresenter();
        var column = new FastTreeDataGridColumn { ValueKey = "Value" };
        var row = new FastTreeDataGridRow(item: null, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);

        var rowInfo = new FastTreeDataGridPresenter.RowRenderInfo(
            row,
            rowIndex: 0,
            top: 0,
            height: 20,
            isSelected: false,
            hasChildren: false,
            isExpanded: false,
            toggleRect: default,
            isGroup: false,
            isSummary: false,
            isPlaceholder: false);

        var placeholderCell = new FastTreeDataGridPresenter.CellRenderInfo(
            columnIndex: 0,
            column,
            bounds: new Rect(0, 0, 100, 20),
            contentBounds: new Rect(0, 0, 100, 20),
            widget: null,
            formattedText: null,
            textOrigin: default,
            control: null,
            FastTreeDataGridCellValidationState.None,
            isSelected: false,
            isPlaceholder: true);

        rowInfo.Cells.Add(placeholderCell);

        presenter.UpdateContent(new List<FastTreeDataGridPresenter.RowRenderInfo> { rowInfo }, totalWidth: 100, totalHeight: 20, columnOffsets: new List<double> { 100 });

        var initialRow = presenter.VisibleRows[0];
        var initialCell = initialRow.Cells[0];
        Assert.True(initialCell.IsPlaceholder);

        presenter.ApplyColumnPatch(
            new[] { 0 },
            (existingRow, columnIndex) =>
            {
                var cell = existingRow.TryGetCell(columnIndex);
                Assert.NotNull(cell);
                presenter.ReleaseCellResources(cell!);
                cell!.Update(cell.Bounds, cell.ContentBounds, widget: null, formattedText: null, cell.TextOrigin, control: null, FastTreeDataGridCellValidationState.None, isSelected: false, isPlaceholder: false);
            },
            totalWidth: 100,
            totalHeight: 20,
            columnOffsets: new List<double> { 100 });

        Assert.Same(initialRow, presenter.VisibleRows[0]);
        Assert.False(presenter.VisibleRows[0].Cells[0].IsPlaceholder);
    }
}
