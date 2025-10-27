# FastTreeDataGrid

FastTreeDataGrid is a high-performance tree data grid for Avalonia UI that renders hierarchical datasets directly onto a canvas-backed surface. The control pairs a pluggable FlatTreeDataGrid engine with an immediate-mode widget system so large trees stay responsive while delivering rich cell visuals.

## Feature Overview
| Feature | Highlights | Primary APIs | More info |
| --- | --- | --- | --- |
| Canvas-backed virtualization | Reuses pooled canvas presenters for smooth scrolling and zero layout churn. | `FastTreeDataGrid`, `FastTreeDataGridVirtualizationSettings` | [Canvas-backed virtualization](#feature-canvas-backed-virtualization) |
| Flat source engine | Flattens hierarchical data, tracks expansion, sorting, and filtering. | `FastTreeDataGridFlatSource<T>`, `IFastTreeDataGridSource` | [Flat source engine](#feature-flat-source-engine) |
| Immediate-mode widgets | Draw text, icons, gauges, and inputs without templated controls. | `Widget`, `WidgetTemplate`, `IFastTreeDataGridValueProvider` | [Immediate-mode widgets](#feature-immediate-mode-widgets) |
| Items & navigation widgets | Drop-in ListBox/TreeView replacements on the widget renderer. | `ItemsControlWidget`, `ListBoxWidget`, `TreeViewWidget` | [Items & navigation widgets](#feature-items--navigation-widgets) |
| Flexible column system | Combine pixel/star sizing, templates, and selection hooks. | `FastTreeDataGridColumn`, `ColumnSizingMode` | [Flexible column system](#feature-flexible-column-system) |
| Row layouts & data sources | Mix uniform/variable heights with static or streaming feeds. | `IFastTreeDataGridRowLayout`, `FastTreeDataGridHybridSource<T>` | [Row layouts & data sources](#feature-row-layouts--data-sources) |
| Provider-agnostic virtualization | Integrate REST/ModelFlow providers and capture diagnostics. | `FastTreeDataGridVirtualizationProviderRegistry`, `FastTreeDataGridVirtualizationDiagnostics` | [Provider-agnostic virtualization](#feature-provider-agnostic-virtualization) |

## Feature: Canvas-backed Virtualization
FastTreeDataGrid hosts a header canvas and a body canvas and reuses a compact pool of presenters to cover the viewport. Every scroll operation simply repositions the existing header, row, and cell presenters; offsets are computed from column widths and the active row layout instead of Avalonia's layout system. Selection, sorting, and expansion state live on the control and flow through a small set of events, keeping large hierarchies responsive.

### Quick usage
- Place `FastTreeDataGrid` in your view (XAML or code) and bind `ItemsSource` to an `IFastTreeDataGridSource`.
- Choose a row layout (uniform or variable) so the control can compute offsets without Avalonia measure passes.
- Tune `VirtualizationSettings` (page size, prefetch radius, concurrency) to align fetching with your data provider.

```csharp
var grid = new FastTreeDataGrid
{
    ItemsSource = flatSource, // e.g., new FastTreeDataGridFlatSource<T>(...)
    RowLayout = new FastTreeDataGridUniformRowLayout(),
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 256,
        PrefetchRadius = 2,
        Concurrency = 2
    }
};
```

## Feature: Flat Source Engine
The FlatTreeDataGrid engine flattens arbitrary hierarchies into a stable list of `FastTreeDataGridRow` instances while tracking expansion, preserving keys across refreshes, and issuing a single `ResetRequested` notification when data changes. Sources expose `IFastTreeDataGridValueProvider` so widgets can read values without bindings and react to fine-grained invalidations.

### Quick usage
- Create a `FastTreeDataGridFlatSource<T>` with your root items and a `childrenSelector`.
- Optionally provide `keySelector` so the source can diff nodes across refreshes, plus `Sort` and `SetFilter` handlers.
- Assign the source to `FastTreeDataGrid.ItemsSource` and let the grid request pages through virtualization.

```csharp
var files = new FastTreeDataGridFlatSource<FileNode>(
    viewModel.RootNodes,
    node => node.Children,
    keySelector: node => node.Id);

files.Sort((left, right) =>
{
    var leftName = left.ValueProvider?.GetValue(left.Item, FileNode.KeyName) as string ?? string.Empty;
    var rightName = right.ValueProvider?.GetValue(right.Item, FileNode.KeyName) as string ?? string.Empty;
    return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
});

var searchText = "log";
files.SetFilter(row =>
{
    var name = row.ValueProvider?.GetValue(row.Item, FileNode.KeyName) as string;
    return string.IsNullOrWhiteSpace(searchText) ||
           (name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
});

grid.ItemsSource = files;
```

## Feature: Immediate-mode Widgets
Cells render `Widget` instances defined by `WidgetTemplate` so drawing happens directly on the canvas without templated controls, routed events, or bindings. Widgets stream values from the row's `IFastTreeDataGridValueProvider`, style themselves via `WidgetStyleManager`, and support wrapped labels, selectable text, icons, badges, sliders, and other affordances.

### Quick usage
- Define a reusable `IWidgetTemplate` (or `WidgetFactory`) that builds the cell visual.
- Emit value lookups through `ValueKey` and react to invalidations inside the widget.
- Apply palette or style overrides via `WidgetStyleManager` or by composing widgets.

```csharp
var nameTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
{
    ValueKey = FileNode.KeyName,
    EmSize = 13,
    Trimming = TextTrimming.CharacterEllipsis,
    IsSelectable = true
});

var gaugeTemplate = new FuncWidgetTemplate(() => new ProgressWidget
{
    Max = 100,
    ValueKey = MetricsNode.KeyCpu
});
```

Widgets can also capture pointer and keyboard input (`WidgetInputContext`) so `ButtonWidget`, `CheckBoxWidget`, `SliderWidget`, `BadgeWidget`, and custom widgets stay interactive without leaving the canvas pipeline.

## Feature: Items & Navigation Widgets
The widget layer mirrors Avalonia's items controls so you can migrate views without leaving the pooled canvas surface. `ItemsControlWidget` virtualizes arbitrary item lists, `ListBoxWidget` layers in single-selection gestures that follow Fluent brushes, and `TreeViewWidget` adds indentation and expander glyphs while reusing the same value providers. The demo's **Widgets Gallery** tab showcases these wrappers with live boards and migration tips.

### Quick usage
- Choose the wrapper (`ItemsControlWidget`, `ListBoxWidget`, or `TreeViewWidget`) that matches the interaction model you need.
- Bind `ItemsSource` and `ItemChildrenSelector` (for hierarchies) so the widget can walk your data.
- Provide a `WidgetTemplate` that renders each item; call helpers like `ExpandToLevel` to bootstrap expansion.

```csharp
var tree = new TreeViewWidget
{
    ItemsSource = viewModel.RootNodes,
    ItemChildrenSelector = item => item is ProjectNode node ? node.Children : Array.Empty<ProjectNode>(),
    ItemTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
    {
        EmSize = 13,
        Trimming = TextTrimming.CharacterEllipsis
    }),
    DesiredWidth = 260,
    DesiredHeight = 200,
};

tree.ExpandToLevel(1);
```

**Wrapper selection quick guide**
- `ItemsControlWidget` – reach for this when you need a pooled, read-only list and plan to handle selection upstream.
- `ListBoxWidget` – keeps Avalonia's single-selection gestures and Fluent brushes while staying on the widget renderer.
- `TreeViewWidget` – handles hierarchical data with pooled expander glyphs, indentation, and lazy loading helpers.
- `TabControlWidget` + `TabStripWidget` – delivers tab navigation with Alt/arrow/Home/End keys and indicator styling sourced from the widget palette.
- `MenuBarWidget` + `MenuWidget` – builds command surfaces with access keys, accelerators, and overlay hosting without templated controls.

## Feature: Flexible Column System
Columns support pixel, star, and auto sizing, hierarchical indentation, selection hooks, and custom editing templates. You can pin columns, opt in to sorting or filtering, or pool widgets by supplying a `WidgetFactory`, and all sizing is computed analytically to avoid Avalonia layout passes.

### Quick usage
- Define each column with `SizingMode` (`Pixel`, `Star`, or `Auto`) plus optional min/max constraints.
- Provide `ValueKey`, `CellTemplate`, or `WidgetFactory` so the column knows how to render and sort values.
- Toggle behaviors such as `CanUserSort`, `CanUserResize`, or `PinnedPosition` to match your UX.

```csharp
grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Name",
    ValueKey = FileNode.KeyName,
    IsHierarchy = true,
    SizingMode = ColumnSizingMode.Star,
    StarValue = 2,
    CellTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
    {
        EmSize = 13,
        Trimming = TextTrimming.CharacterEllipsis
    })
});

grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Modified",
    ValueKey = FileNode.KeyModified,
    SizingMode = ColumnSizingMode.Pixel,
    PixelWidth = 140,
    CellTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
    {
        ValueKey = FileNode.KeyModified,
        Format = "g"
    }),
    CanUserSort = true,
    CanUserFilter = true
});
```

XAML projections follow the same pattern—define columns, pick sizing modes, and attach widget templates so values flow through `IFastTreeDataGridValueProvider`.

## Feature: Row Layouts & Data Sources
Row positioning is delegated to implementations of `IFastTreeDataGridRowLayout`. Uniform layouts keep rows the same height for maximum throughput, while variable layouts ask providers for per-row heights so dashboards and grouped summaries get the real estate they need. Sources can be static lists, `FastTreeDataGridFlatSource<T>`, hybrid sources that mix snapshots with live updates, or fully custom providers that emit rows asynchronously.

### Quick usage
- Pick a row layout (`FastTreeDataGridUniformRowLayout`, `FastTreeDataGridVariableRowLayout`, or `FastTreeDataGridAdaptiveRowLayout`) that matches your density and affordances.
- If you need variable heights, provide an `IFastTreeDataGridVariableRowHeightProvider` or the func-based helper to compute per-row values.
- Combine the layout with the right source type (flat, async, streaming, hybrid) so virtualization knows how to request pages.

```csharp
var filesSource = new FastTreeDataGridFlatSource<FileNode>(
    rootFolders,
    node => node.Children,
    keySelector: node => node.Path);

var layout = new FastTreeDataGridVariableRowLayout(
    new FastTreeDataGridFuncVariableRowHeightProvider((row, _, defaultHeight) =>
        row.IsGroup ? defaultHeight * 1.5 : defaultHeight));

var filesGrid = new FastTreeDataGrid
{
    ItemsSource = filesSource,
    RowLayout = layout
};
```

**Row layout options**
- `FastTreeDataGridUniformRowLayout` – default constant-height layout for dense tabular data.
- `FastTreeDataGridVariableRowLayout` – derives heights from providers for scenarios with tall summary rows or group headers.
- `FastTreeDataGridAdaptiveRowLayout` – samples row heights in blocks to estimate scroll extents for very large sets with mixed heights.
- Custom layouts – implement `IFastTreeDataGridRowLayout` to integrate domain-specific sizing (timeline grids, calendar rows, etc.).

**Available sources**
- `FastTreeDataGridFlatSource<T>` – deterministic flattening of in-memory hierarchies; great for file systems, configuration trees, or cached API responses.
- `FastTreeDataGridAsyncSource<T>` – wraps async factories so initial load happens off the UI thread while providing the same flat-tree API.
- `FastTreeDataGridStreamingSource<T>` – listens to live feeds (`IObservable`, channels, async enumerables) and applies inserts/removes to the flat list.
- `FastTreeDataGridHybridSource<T>` – combines a snapshot load with real-time updates; ideal for dashboards that hydrate once then listen for deltas.
- `FastTreeDataGridDynamicSource<T>` – base class for bespoke dynamic sources when you need custom change tracking or background processing.

## Feature: Provider-agnostic Virtualization
FastTreeDataGrid virtualization is provider-agnostic: register factories with `FastTreeDataGridVirtualizationProviderRegistry` so the grid can discover the right `IFastTreeDataVirtualizationProvider` at runtime. Diagnostics via `FastTreeDataGridVirtualizationDiagnostics` surface fetch latency, placeholder density, and reset frequency so you can harden remote data sources before shipping.

### Quick usage
- Register a provider factory that adapts your data engine to `IFastTreeDataVirtualizationProvider`.
- Configure control-level `VirtualizationSettings` to describe page size, prefetch radius, and concurrency for the provider.
- Feed the grid a streaming-friendly source (e.g., `FastTreeDataGridHybridSource<T>`) and subscribe to diagnostics to monitor the pipeline.

```csharp
using var registration = FastTreeDataGridVirtualizationProviderRegistry.Register((source, settings) =>
{
    if (source is MetricsClient client)
    {
        // MetricsVirtualizationProvider implements IFastTreeDataVirtualizationProvider.
        return new MetricsVirtualizationProvider(client, settings);
    }

    return null;
});

var metricsGrid = new FastTreeDataGrid
{
    ItemsSource = new FastTreeDataGridHybridSource<MetricsNode>(
        loader: LoadInitialMetricsAsync,
        updates: metricsChannel.Reader,
        childrenSelector: node => node.Children,
        keySelector: node => node.Id),
    RowLayout = new FastTreeDataGridVariableRowLayout(
        new FastTreeDataGridFuncVariableRowHeightProvider((row, _, defaultHeight) =>
            row.IsGroup ? defaultHeight * 1.5 : defaultHeight)),
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 512,
        PrefetchRadius = 3
    }
};

metricsGrid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "CPU",
    ValueKey = MetricsNode.KeyCpu,
    SizingMode = ColumnSizingMode.Star,
    CellTemplate = new FuncWidgetTemplate(() => new ProgressWidget { Max = 100 })
});
```

**Integration tips**
- Register your data engine (ModelFlow, REST, gRPC, etc.) with `FastTreeDataGridVirtualizationProviderRegistry` so the grid auto-discovers the correct provider at runtime.
- Configure `FastTreeDataGrid.VirtualizationSettings` per control to tune page size, prefetch radius, concurrency, and dispatcher priority.
- Emit metrics via `FastTreeDataGridVirtualizationDiagnostics` (MeterListener/OpenTelemetry) to watch fetch latency, placeholder density, and reset frequency.
- Run the BenchmarkDotNet suite (`benchmarks/FastTreeDataGrid.Benchmarks`) against large test sets to validate provider throughput before shipping.
- Keep row value providers lightweight—avoid synchronous network calls from `IFastTreeDataGridValueProvider` implementations.
- Prefer placeholder-aware widgets to avoid accessing null data while virtualization is inflight.

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
