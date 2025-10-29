using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Engine.Infrastructure;
using Xunit;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridInlineColumnSourceTests
{
    [AvaloniaFact]
    public async Task ColumnPropertyChangeRaisesRangeInvalidation()
    {
        var grid = new GridControl();
        var columnA = new FastTreeDataGridColumn { ValueKey = "A", Header = "A", PixelWidth = 100 };
        var columnB = new FastTreeDataGridColumn { ValueKey = "B", Header = "B", PixelWidth = 120 };
        grid.Columns.Add(columnA);
        grid.Columns.Add(columnB);

        var source = GetColumnSource(grid);
        var materialized = new List<int>();
        var invalidations = new List<FastTreeDataGridInvalidationRequest>();
        var resetCount = 0;

        source.ColumnMaterialized += (_, e) => materialized.Add(e.ColumnIndex);
        source.Invalidated += (_, e) => invalidations.Add(e.Request);
        source.ResetRequested += (_, _) => resetCount++;

        await MaterializeAsync(source, 0, source.ColumnCount);
        Assert.NotEmpty(materialized);
        materialized.Clear();

        columnA.PixelWidth = 150;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        Assert.False(source.IsPlaceholder(0));
        Assert.True(source.TryGetMaterializedColumn(0, out var beforeDescriptor));
        var beforeWidth = Assert.IsType<double>(beforeDescriptor.Properties["pixelWidth"]);
        Assert.Equal(100d, beforeWidth);
        var page = await source.GetPageAsync(new FastTreeDataGridPageRequest(0, 1), CancellationToken.None);
        Assert.DoesNotContain(0, page.PlaceholderIndices);
        if (page.Completion is { } completion)
        {
            await completion;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        }

        Assert.True(source.TryGetMaterializedColumn(0, out var afterDescriptor));
        Assert.False(source.IsPlaceholder(0));
        Assert.Contains(0, materialized);
        Assert.Contains(invalidations, request =>
            request.Kind == FastTreeDataGridInvalidationKind.Range &&
            request.StartIndex == 0 &&
            request.Count == 1);
        Assert.Equal(0, resetCount);
        var afterWidth = Assert.IsType<double>(afterDescriptor.Properties["pixelWidth"]);
        Assert.Equal(150d, afterWidth);
    }

    [AvaloniaFact]
    public async Task AddingColumnTriggersFullReset()
    {
        var grid = new GridControl();
        var columnA = new FastTreeDataGridColumn { ValueKey = "A", Header = "A", PixelWidth = 100 };
        var columnB = new FastTreeDataGridColumn { ValueKey = "B", Header = "B", PixelWidth = 120 };
        grid.Columns.Add(columnA);
        grid.Columns.Add(columnB);

        var source = GetColumnSource(grid);
        await MaterializeAsync(source, 0, source.ColumnCount);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        var materialized = new List<int>();
        var invalidations = new List<FastTreeDataGridInvalidationRequest>();
        var resetCount = 0;

        source.ColumnMaterialized += (_, e) => materialized.Add(e.ColumnIndex);
        source.Invalidated += (_, e) => invalidations.Add(e.Request);
        source.ResetRequested += (_, _) => resetCount++;

        var columnC = new FastTreeDataGridColumn { ValueKey = "C", Header = "C", PixelWidth = 80 };
        grid.Columns.Add(columnC);

        Assert.Equal(1, resetCount);
        Assert.Contains(invalidations, request => request.Kind == FastTreeDataGridInvalidationKind.Full);
        Assert.True(source.IsPlaceholder(2));
        Assert.False(source.TryGetMaterializedColumn(2, out _));

        await MaterializeAsync(source, 0, source.ColumnCount);

        Assert.Contains(2, materialized);
        Assert.True(source.TryGetMaterializedColumn(2, out _));
    }

    private static FastTreeDataGridInlineColumnSource GetColumnSource(GridControl grid)
    {
        var field = typeof(GridControl).GetField("_columnSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var value = field.GetValue(grid);
        return Assert.IsType<FastTreeDataGridInlineColumnSource>(value);
    }

    private static async Task MaterializeAsync(FastTreeDataGridInlineColumnSource source, int startIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var page = await source.GetPageAsync(new FastTreeDataGridPageRequest(startIndex, count), CancellationToken.None);
        if (page.Completion is { } completion)
        {
            await completion;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
