using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using Xunit;

namespace FastTreeDataGrid.Control.Tests.Performance;

public sealed class ColumnVirtualizationPerformanceTests
{
    [AvaloniaFact]
    public async Task PlaceholderMaterializationRaisesTargetedInvalidation()
    {
        var grid = new FastTreeDataGrid.Control.Controls.FastTreeDataGrid();
        for (var i = 0; i < 200; i++)
        {
            grid.Columns.Add(new FastTreeDataGridColumn
            {
                ValueKey = $"Col{i}",
                Header = $"Column {i}",
                PixelWidth = 120,
                MinWidth = 80,
                MaxWidth = 200,
                SizingMode = ColumnSizingMode.Pixel,
            });
        }

        var columnSource = GetColumnSource(grid);
        var invalidations = new List<FastTreeDataGridInvalidationRequest>();
        columnSource.Invalidated += (_, args) => invalidations.Add(args.Request);

        var page = await columnSource.GetPageAsync(new FastTreeDataGridPageRequest(0, 32), CancellationToken.None);
        Assert.NotNull(page.Completion);
        Assert.NotEmpty(page.PlaceholderIndices);

        if (page.Completion is not null)
        {
            await page.Completion;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        }

        var rangeInvalidations = invalidations.Where(r => r.Kind == FastTreeDataGridInvalidationKind.Range).ToList();
        Assert.NotEmpty(rangeInvalidations);
        Assert.DoesNotContain(invalidations, r => r.Kind == FastTreeDataGridInvalidationKind.Full);
        Assert.All(rangeInvalidations, r => Assert.True(r.HasRange && r.Count > 0));
    }

    [AvaloniaFact]
    public void HorizontalScrollPatchesColumnsAndUpdatesMetrics()
    {
        using var listener = new MetricCollector();

        const int columnCount = 240;
        const int rowCount = 12;
        var grid = CreateConfiguredGrid(columnCount, rowCount);
        var presenter = GetPrivateField<FastTreeDataGridPresenter>(grid, "_presenter");
        Assert.NotNull(presenter);

        var updateViewport = typeof(FastTreeDataGrid.Control.Controls.FastTreeDataGrid)
            .GetMethod("UpdateViewport", BindingFlags.Instance | BindingFlags.NonPublic)!;

        listener.Reset();
        updateViewport.Invoke(grid, null);

        var firstRebuildDelta = listener.ConsumeDelta("fasttree_datagrid_cells_rebuilt");
        Assert.True(firstRebuildDelta > 0);
        Assert.Equal(0, listener.ConsumeDelta("fasttree_datagrid_cells_patched"));

        var initialRows = presenter!.VisibleRows;
        Assert.NotEmpty(initialRows);
        var initialRow = initialRows[0];
        var initialCells = initialRow.Cells.ToArray();
        var initialIndices = GetViewportIndices(grid);

        var scrollViewer = GetPrivateField<ScrollViewer>(grid, "_scrollViewer");
        Assert.NotNull(scrollViewer);
        scrollViewer!.Offset = new Vector(initialCells.Length * 80, 0);

        listener.Reset();
        updateViewport.Invoke(grid, null);

        var updatedRows = presenter.VisibleRows;
        Assert.NotEmpty(updatedRows);
        var updatedRow = updatedRows[0];
        var updatedCells = updatedRow.Cells.ToArray();
        var updatedIndices = GetViewportIndices(grid);

        var overlapping = initialIndices.Intersect(updatedIndices).ToArray();
        Assert.NotEmpty(overlapping);

        foreach (var index in overlapping)
        {
            var initialPosition = Array.IndexOf(initialIndices, index);
            var updatedPosition = Array.IndexOf(updatedIndices, index);
            Assert.InRange(initialPosition, 0, initialCells.Length - 1);
            Assert.InRange(updatedPosition, 0, updatedCells.Length - 1);
            Assert.Same(initialCells[initialPosition], updatedCells[updatedPosition]);
        }

        var patchedDelta = listener.ConsumeDelta("fasttree_datagrid_cells_patched");
        var rebuildDelta = listener.ConsumeDelta("fasttree_datagrid_cells_rebuilt");

        Assert.True(rebuildDelta <= firstRebuildDelta);
        Assert.True(
            patchedDelta > 0 || rebuildDelta == 0,
            $"Expected patched cells to be reported; patched={patchedDelta}, rebuilt={rebuildDelta}");
    }

    private static FastTreeDataGrid.Control.Controls.FastTreeDataGrid CreateConfiguredGrid(int columnCount, int rowCount)
    {
        var grid = new FastTreeDataGrid.Control.Controls.FastTreeDataGrid
        {
            Width = 1600,
            Height = 400,
        };

        for (var i = 0; i < columnCount; i++)
        {
            grid.Columns.Add(new FastTreeDataGridColumn
            {
                ValueKey = $"Col{i}",
                Header = $"Column {i}",
                PixelWidth = 120,
                MinWidth = 80,
                MaxWidth = 180,
                SizingMode = ColumnSizingMode.Pixel,
            });
        }

        var rows = new List<FastTreeDataGridRow>(rowCount);
        for (var r = 0; r < rowCount; r++)
        {
            var values = new Dictionary<string, object?>(columnCount);
            for (var c = 0; c < columnCount; c++)
            {
                values[$"Col{c}"] = $"R{r}C{c}";
            }

            var provider = new TestRowValueProvider(values);
            rows.Add(new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null));
        }

        var source = new TestSource(rows);
        SetPrivateField(grid, "_itemsSource", source);
        SetPrivateField(grid, "_isAttachedToVisualTree", true);

        var presenter = new FastTreeDataGridPresenter();
        presenter.SetOwner(grid);
        SetPrivateField(grid, "_presenter", presenter);

        grid.Measure(new Size(grid.Width, grid.Height));
        grid.Arrange(new Rect(0, 0, grid.Width, grid.Height));

        var scrollViewer = new ScrollViewer();
        SetPrivateField(grid, "_scrollViewer", scrollViewer);
        scrollViewer.Offset = Vector.Zero;
        var viewportProperty = typeof(ScrollViewer).GetProperty(
            "Viewport",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(viewportProperty);
        viewportProperty!.SetValue(scrollViewer, new Size(grid.Width, grid.Height));

        InvokePrivateMethod(grid, "RecalculateColumns");

        return grid;
    }

    private static int[] GetViewportIndices(FastTreeDataGrid.Control.Controls.FastTreeDataGrid grid)
    {
        var state = GetPrivateField<object>(grid, "_currentColumnViewportState");
        var indicesProperty = state.GetType().GetProperty("Indices", BindingFlags.Instance | BindingFlags.Public)!;
        return (int[])indicesProperty.GetValue(state)!;
    }

    private static FastTreeDataGridInlineColumnSource GetColumnSource(FastTreeDataGrid.Control.Controls.FastTreeDataGrid grid)
    {
        var field = typeof(FastTreeDataGrid.Control.Controls.FastTreeDataGrid)
            .GetField("_columnSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<FastTreeDataGridInlineColumnSource>(field!.GetValue(grid));
    }

    private static void InvokePrivateMethod(object instance, string methodName)
    {
        instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(instance, null);
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(instance, value);
    }

    private static T GetPrivateField<T>(object instance, string fieldName) where T : class
    {
        return (T?)instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;
    }

    private sealed class TestSource : IFastTreeDataGridSource
    {
        private readonly List<FastTreeDataGridRow> _rows;

        public TestSource(List<FastTreeDataGridRow> rows)
        {
            _rows = rows;
        }

        public event EventHandler? ResetRequested;
        public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
        public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

        public int RowCount => _rows.Count;
        public bool SupportsPlaceholders => false;

        public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) => new(RowCount);

        public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
        {
            var list = new List<FastTreeDataGridRow>();
            var end = Math.Min(request.StartIndex + request.Count, RowCount);
            for (var i = request.StartIndex; i < end; i++)
            {
                var row = _rows[i];
                list.Add(row);
                RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(i, row));
            }

            return new ValueTask<FastTreeDataGridPageResult>(new FastTreeDataGridPageResult(list, Array.Empty<int>(), completion: null, cancellation: null));
        }

        public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
        {
            Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
            return Task.CompletedTask;
        }

        public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
        {
            if ((uint)index < (uint)_rows.Count)
            {
                row = _rows[index];
                return true;
            }

            row = default!;
            return false;
        }

        public bool IsPlaceholder(int index) => false;

        public FastTreeDataGridRow GetRow(int index)
        {
            return _rows[index];
        }

        public void ToggleExpansion(int index)
        {
        }

        public void RequestReset()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class TestRowValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly Dictionary<string, object?> _values;

        public TestRowValueProvider(Dictionary<string, object?> values)
        {
            _values = values;
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

        public object? GetValue(object? item, string key)
        {
            return _values.TryGetValue(key, out var value) ? value : null;
        }
    }

    private sealed class MetricCollector : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Dictionary<string, long> _values = new();

        public MetricCollector()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (!ReferenceEquals(instrument.Meter, FastTreeDataGridVirtualizationDiagnostics.Meter))
                    {
                        return;
                    }

                    if (instrument.Name is "fasttree_datagrid_cells_rebuilt" or "fasttree_datagrid_cells_patched")
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            {
                if (!_values.TryGetValue(instrument.Name, out var current))
                {
                    current = 0;
                }

                _values[instrument.Name] = current + value;
            });

            _listener.Start();
        }

        public void Reset()
        {
            _values["fasttree_datagrid_cells_rebuilt"] = 0;
            _values["fasttree_datagrid_cells_patched"] = 0;
        }

        public long ConsumeDelta(string name)
        {
            _values.TryGetValue(name, out var value);
            _values[name] = 0;
            return value;
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
