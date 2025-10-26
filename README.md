# FastTreeDataGrid

FastTreeDataGrid is a high-performance tree data grid for Avalonia UI that renders hierarchical datasets directly onto a canvas-backed surface. The control pairs a pluggable FlatTreeDataGrid engine with an immediate-mode widget system so large trees stay responsive while delivering rich cell visuals.

## Key Capabilities
- Canvas-backed virtualization reuses a compact pool of presenters to cover the viewport without allocating per-row controls.
- FlatTreeDataGrid engine handles expansion, filtering, sorting, and fast rebuilds of the visible slice without touching the visual tree.
- Immediate-mode widgets render text, icons, gauges, and input affordances without Avalonia layout passes, templated controls, or data bindings.
- ItemsControlWidget, ListBoxWidget, and TreeViewWidget mirror Avalonia's items/navigation surfaces with pooled widgets, preserving item templates, selection, and hierarchical expansion on the canvas pipeline.
- Text widgets support wrapped labels, selectable text, and rich document spans rendered directly on the canvas.
- Flexible columns support pixel, star, and auto sizing, hierarchical indentation, custom cell templates, and selection hooks.
- Pluggable row layouts and data sources adapt to uniform, adaptive, or variable row heights and to static, async, or streaming data feeds.
- Provider-agnostic virtualization lets you plug in ModelFlow, REST-backed, or custom engines via `FastTreeDataGridVirtualizationProviderRegistry`.

## Architecture at a Glance

### FastTreeDataGrid control
`FastTreeDataGrid` is a lightweight templated control whose template hosts a header canvas and a body canvas. Each scroll operation repositions a small pool of header, row, and cell presenters; pixels are computed from column widths and row layout rather than Avalonia's layout system. Selection, sorting, and expansion state live on the control and feed the rendering pipeline through a tiny set of events.

### FlatTreeDataGrid engine
The FlatTreeDataGrid engine powers data shaping. `FastTreeDataGridFlatSource<T>` and related sources implement `IFastTreeDataGridSource`, flattening arbitrary hierarchies into a stable list of `FastTreeDataGridRow` instances. The engine tracks expansion, preserves keys across refreshes, supports filtering, sorting, and async resets, and notifies the control via a single `ResetRequested` event. Row objects cache hierarchy level, expansion state, and optional value providers so widgets can read values without bindings.

### Widget architecture
Cells render `Widget` instances described by `WidgetTemplate`. Widgets are immediate-mode renderers that draw directly to the canvas using structures from `WidgetDescriptors`, feed input through `WidgetInput`, and style themselves via `WidgetStyleManager`. Because widgets are not Avalonia controls, they avoid the templated-control system, global styles, routed events, and bindings. Instead, widgets ask the row's `IFastTreeDataGridValueProvider` for values and invalidate themselves with fine-grained notifications.

### Layout pipeline
Row positioning is delegated to implementations of `IFastTreeDataGridRowLayout`. Layouts compute visible ranges, per-row heights, and cumulative offsets so the presenter can place rows precisely. Layouts plug into the control, bind to the current `IFastTreeDataGridSource`, and can invalidate ranges when data changes.

## Items & Navigation Widgets

The widget layer now mirrors Avalonia's items-controls so you can migrate views without leaving the pooled canvas surface.

- `ItemsControlWidget` accepts an `ItemsSource`, optional `ItemChildrenSelector`, and any `WidgetTemplate` or factory for content generation. It virtualizes items through the existing `FastTreeDataGrid` sources, so mutations (`INotifyCollectionChanged`, resets, async feeds) flow through the same pipeline.
- `ListBoxWidget` builds on that shim with single-selection visuals and pointer-driven selection changes that follow Fluent brushes.
- `TreeViewWidget` adds hierarchical indentation, expander glyphs, and convenience helpers like `ExpandToLevel` while reusing the same value providers.

```csharp
var tree = new TreeViewWidget
{
    ItemsSource = viewModel.RootNodes,
    ItemChildrenSelector = item => item is ProjectNode node ? node.Children : Array.Empty<ProjectNode>(),
    ItemTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget { EmSize = 13, Trimming = TextTrimming.CharacterEllipsis }),
    DesiredWidth = 260,
    DesiredHeight = 200,
};

tree.ExpandToLevel(1);
```

The sample gallery (`Widgets` tab) now includes boards that showcase these shims, including a hierarchical navigation tree, demonstrating how existing Avalonia `ListBox` or `TreeView` screens can move onto the widget renderer without sacrificing virtualization.

### Wrapper selection quick guide

- `ItemsControlWidget` – reach for this when you need a pooled, read-only list and plan to handle selection upstream.
- `ListBoxWidget` – keeps Avalonia's single-selection gestures and Fluent brushes while staying on the widget renderer.
- `TreeViewWidget` – handles hierarchical data with pooled expander glyphs, indentation, and lazy loading helpers.
- `TabControlWidget` + `TabStripWidget` – delivers tab navigation with Alt/arrow/Home/End keys and indicator styling sourced from the widget palette.
- `MenuBarWidget` + `MenuWidget` – builds command surfaces with access keys, accelerators, and overlay hosting without templated controls.

The demo's new **Widgets Gallery** tab stitches these wrappers together with explanatory boards so you can compare their APIs, palette bindings, and recommended usage at a glance.

## Pluggable layout and item sources

### Row layout options
- `FastTreeDataGridUniformRowLayout` – default constant-height layout for dense tabular data.
- `FastTreeDataGridVariableRowLayout` – layout that derives heights from providers for scenarios with tall summary rows or group headers.
- `FastTreeDataGridAdaptiveRowLayout` – samples row heights in blocks to estimate scroll extents for very large sets with mixed heights.
- Custom layouts – implement `IFastTreeDataGridRowLayout` to integrate domain-specific sizing (e.g., timeline grids or calendar rows).

### Item source options
All sources implement `IFastTreeDataGridSource` so they can be swapped without changing the control.
- `FastTreeDataGridFlatSource<T>` – deterministic flattening of in-memory hierarchies. Great for file systems, configuration trees, or cached API responses.
- `FastTreeDataGridAsyncSource<T>` – wraps async factories so initial load happens off the UI thread while providing the same flat-tree API.
- `FastTreeDataGridStreamingSource<T>` – listens to live feeds (`IObservable`, channels, async enumerables) and applies inserts/removes to the flat list.
- `FastTreeDataGridHybridSource<T>` – combines a snapshot load with real-time updates; ideal for dashboards that hydrate once then listen for deltas.
- `FastTreeDataGridDynamicSource<T>` – base class for custom dynamic sources; inherit when you need bespoke change tracking or background processing.

## Public API guide

The surface area stays small: configure a `FastTreeDataGrid`, provide columns, and hand it an `IFastTreeDataGridSource`. Widgets bridge the source to visuals.

### Basic scenario: static tree with text widgets
Create a flat source and bind it in XAML:

```csharp
var filesSource = new FastTreeDataGridFlatSource<FileNode>(
    rootFolders,
    node => node.Children,
    keySelector: node => node.Path);

Files.Source = filesSource;
```

```xml
<FastTreeDataGrid ItemsSource="{Binding Files.Source}"
                  RowHeight="28"
                  IndentWidth="18">
  <FastTreeDataGrid.Columns>
    <FastTreeDataGridColumn Header="Name"
                            IsHierarchy="True"
                            SizingMode="Star"
                            ValueKey="{x:Static local:FileNode.KeyName}">
      <FastTreeDataGridColumn.CellTemplate>
        <WidgetTemplate>
          <FormattedTextWidget EmSize="13"
                               Trimming="CharacterEllipsis" />
        </WidgetTemplate>
      </FastTreeDataGridColumn.CellTemplate>
    </FastTreeDataGridColumn>
    <FastTreeDataGridColumn Header="Size"
                            SizingMode="Pixel"
                            PixelWidth="120"
                            ValueKey="{x:Static local:FileNode.KeySize}" />
  </FastTreeDataGrid.Columns>
</FastTreeDataGrid>
```

Rows expose `IFastTreeDataGridValueProvider` so widgets can pull `KeyName` and `KeySize` without bindings; when a value changes, the row raises `ValueInvalidated` and the widget redraws.

### Intermediate scenario: custom widgets, sorting, and filtering
Respond to header clicks and drive the FlatTreeDataGrid engine directly:

```csharp
private readonly FastTreeDataGridFlatSource<FileNode> _files;

private void OnFilesSortRequested(object? sender, FastTreeDataGridSortEventArgs e)
{
    _files.Sort(e.CanSort
        ? (x, y) =>
        {
            var left = x.ValueProvider?.GetValue(x.Item, FileNode.KeyName) as string ?? string.Empty;
            var right = y.ValueProvider?.GetValue(y.Item, FileNode.KeyName) as string ?? string.Empty;
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }
        : null);
}

public void ApplyFilter(string searchText)
{
    _files.SetFilter(row =>
    {
        var name = row.ValueProvider?.GetValue(row.Item, FileNode.KeyName) as string;
        return !string.IsNullOrWhiteSpace(searchText) &&
               name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
    });
}
```

Widgets can become interactive—`ButtonWidget`, `CheckBoxWidget`, `SliderWidget`, and `BadgeWidget` ship in the box, and custom widgets can inspect pointer/keyboard events via `WidgetInputContext`.

### Advanced scenario: live updates, variable heights, and custom value providers
Combine a hybrid source with a variable row layout and domain widgets for dashboards or monitoring tools:

```csharp
var liveSource = new FastTreeDataGridHybridSource<MetricsNode>(
    loader: LoadInitialMetricsAsync,
    updates: metricsChannel.Reader,
    childrenSelector: node => node.Children,
    keySelector: node => node.Id);

metricsGrid.ItemsSource = liveSource;
metricsGrid.RowLayout = new FastTreeDataGridVariableRowLayout(
    new FastTreeDataGridFuncVariableRowHeightProvider((row, _, defaultHeight) =>
        row.IsGroup ? defaultHeight * 1.5 : defaultHeight));

metricsGrid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "CPU",
    ValueKey = MetricsNode.KeyCpu,
    SizingMode = ColumnSizingMode.Star,
    CellTemplate = new FuncWidgetTemplate(() => new ProgressWidget { Max = 100 })
});
```

Each `MetricsNode` implements `IFastTreeDataGridValueProvider`, raising `ValueInvalidated` when telemetry arrives. The variable layout asks the provider for heights, and widgets redraw immediately without traversing Avalonia's layout or binding system.

## Performance strategy

FastTreeDataGrid prioritises frame time predictability:

- No Avalonia layout: row and cell coordinates are computed analytically, so scrolling only moves existing presenters.
- No templated-control system: cells are widgets, not controls; they skip template instantiation, styling lookups, and routed events.
- No standard Avalonia controls: widgets draw straight to the canvas with retained assets and pooled brushes.
- No bindings: data flows through `IFastTreeDataGridValueProvider` and explicit value keys, eliminating binding allocations and change propagation costs.
- Batched measure: column widths and row heights are recomputed incrementally, and flat sources reuse nodes across resets via stable keys.

## Documentation

- [Layout & Virtualizing Widgets](docs/widgets/layout-widgets.md)
- [Text Widgets](docs/widgets/text-widgets.md)
- [Media & Icon Widgets](docs/widgets/media-widgets.md)
- [Menu Widgets](docs/widgets/menu-widgets.md)
- [Providers & Virtualization Integration](docs/virtualization/providers.md)
- [Metrics & Diagnostics](docs/virtualization/metrics.md)
- [Benchmarks](docs/virtualization/benchmarks.md)
- [Virtualization Migration Guide](docs/virtualization/migration.md)
- [Changelog](docs/changelog.md)

## Virtualization Integration Best Practices

- Register your data engine (ModelFlow, REST, gRPC, etc.) with `FastTreeDataGridVirtualizationProviderRegistry` so the grid auto-discovers the correct provider at runtime.
- Configure `FastTreeDataGrid.VirtualizationSettings` per control to tune page size, prefetch radius, concurrency, and dispatcher priority.
- Emit metrics via `FastTreeDataGridVirtualizationDiagnostics` (MeterListener/OpenTelemetry) to watch fetch latency, placeholder density, and reset frequency.
- Run the BenchmarkDotNet suite (`benchmarks/FastTreeDataGrid.Benchmarks`) against large test sets to validate provider throughput before shipping.
- Keep row value providers lightweight—avoid synchronous network calls from `IFastTreeDataGridValueProvider` implementations.
- Prefer placeholder-aware widgets to avoid accessing null data while virtualization is inflight.

## Getting started

1. Restore and build the solution:

   ```bash
   dotnet build
   ```

2. Run the demo application:

   ```bash
   dotnet run --project samples/FastTreeDataGrid.Demo
   ```

3. Explore the sample tabs—including the new **Widgets Gallery** scenario explorer—to see flat tree rendering, streaming updates, widget boards, and variable-height rows in action.

## Samples

`samples/FastTreeDataGrid.Demo` showcases:
- File-system and country browsers backed by `FastTreeDataGridFlatSource<T>`.
- Live dashboards using `FastTreeDataGridStreamingSource<T>` and `FastTreeDataGridHybridSource<T>`.
- A widget gallery highlighting text, icon, badge, checkbox, slider, and custom draw widgets.
- A Widgets Gallery scenario explorer that maps each widget family to sample boards, palette notes, and migration tips.
- Variable-height and adaptive row layouts alongside uniform grids.
- Virtualization tab featuring a 1B-row pseudo-random provider and a REST-backed Hacker News provider that showcase the virtualization stack.

## Validation

- Run `dotnet test tests/FastTreeDataGrid.Control.Tests/FastTreeDataGrid.Control.Tests.csproj` to exercise widget layout regressions (expander toggles, scroll viewer viewport notifications, and existing picker coverage).
- Run `dotnet run --project benchmarks/FastTreeDataGrid.Benchmarks -c Release` to execute the BenchmarkDotNet suite, including the new `WidgetInteractionBenchmarks` that stress tab switching, menu refreshes, expander toggles, and scroll viewport updates.

## Next steps

- Add column resizing and reordering interactions.
- Support variable row templates and dynamic presenter composition.
- Integrate keyboard navigation (arrow keys, space/enter, multi-select).
- Investigate deferred data loading hooks for deep or infinite trees.
