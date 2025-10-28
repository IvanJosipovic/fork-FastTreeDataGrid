# FastTreeDataGrid

FastTreeDataGrid is a high-performance tree data grid for Avalonia UI that renders hierarchical datasets directly onto a canvas-backed surface. The control pairs a pluggable FlatTreeDataGrid engine with an immediate-mode widget system so large trees stay responsive while delivering rich cell visuals.

## Feature Overview
| Feature | Highlights | Primary APIs | More info |
| --- | --- | --- | --- |
| Canvas-backed virtualization | Reuses pooled canvas presenters for smooth scrolling and zero layout churn. | `FastTreeDataGrid`, `FastTreeDataGridVirtualizationSettings` | [Canvas-backed virtualization](#feature-canvas-backed-virtualization) |
| Flat source engine | Flattens hierarchical data, tracks expansion, sorting, and filtering. | `FastTreeDataGridFlatSource<T>`, `IFastTreeDataGridSource` | [Flat source engine](#feature-flat-source-engine) |
| Immediate-mode widgets | Draw text, icons, gauges, and inputs without templated controls. | `Widget`, `WidgetTemplate`, `IFastTreeDataGridValueProvider` | [Immediate-mode widgets](#feature-immediate-mode-widgets) |
| Items & navigation widgets | Drop-in ListBox/TreeView replacements on the widget renderer. | `ItemsControlWidget`, `ListBoxWidget`, `TreeViewWidget` | [Items & navigation widgets](#feature-items--navigation-widgets) |
| Column grouping & pivot | Drag headers into a grouping band, layer descriptors, and surface pivot-style aggregates with persistence. | `FastTreeDataGrid.GroupDescriptors`, `FastTreeDataGridGroupingBand`, `FastTreeDataGridGroupingLayout` | [Column grouping & pivot](#feature-column-grouping--pivot) |
| Flexible column system | Combine pixel/star sizing, templates, and selection hooks. | `FastTreeDataGridColumn`, `ColumnSizingMode` | [Flexible column system](#feature-flexible-column-system) |
| Row layouts & data sources | Mix uniform/variable heights with static or streaming feeds. | `IFastTreeDataGridRowLayout`, `FastTreeDataGridHybridSource<T>` | [Row layouts & data sources](#feature-row-layouts--data-sources) |
| Row reorder | Pointer-driven drag & drop with live preview, configurable visuals, and events. | `FastTreeDataGridRowReorderSettings`, `IFastTreeDataGridRowReorderHandler` | [Row reorder](#feature-row-reorder) |
| Provider-agnostic virtualization | Integrate REST/ModelFlow providers and capture diagnostics. | `FastTreeDataGridVirtualizationProviderRegistry`, `FastTreeDataGridVirtualizationDiagnostics` | [Provider-agnostic virtualization](#feature-provider-agnostic-virtualization) |

## Comprehensive Feature Matrix
| Area | Capability | What it does | Key APIs / Notes |
| --- | --- | --- | --- |
| Rendering & Virtualization | Canvas header/body surfaces | Hosts dedicated canvases for headers and rows so scrolling only repositions presenters. | `FastTreeDataGrid` template parts `PART_HeaderPresenter`, `PART_Presenter` |
| Rendering & Virtualization | Presenter pooling | Reuses header, row, and cell presenters instead of creating new controls per row. | `FastTreeDataGridPresenter`, `FastTreeDataGridHeaderPresenter` |
| Rendering & Virtualization | Viewport scheduler | Coordinates viewport changes with page requests, cancellation, and throttling. | `FastTreeDataGridViewportScheduler` |
| Rendering & Virtualization | Invalidation modes | Distinguishes full, range, and metadata-only redraws to minimise work. | `FastTreeDataGridInvalidationRequest`, `FastTreeDataGridInvalidatedEventArgs` |
| Rendering & Virtualization | Row materialization events | Raises callbacks whenever a provider materialises rows so hosts can warm caches. | `IFastTreeDataVirtualizationProvider.RowMaterialized`, `FastTreeDataGridRowMaterializedEventArgs` |
| Rendering & Virtualization | Row count updates | Notifies when total row counts change to keep pagers and summaries in sync. | `IFastTreeDataVirtualizationProvider.CountChanged`, `FastTreeDataGridCountChangedEventArgs` |
| Rendering & Virtualization | Materialised row cache lookup | Allows reuse of already materialised rows without re-fetching. | `IFastTreeDataVirtualizationProvider.TryGetMaterializedRow` |
| Rendering & Virtualization | Prefetch hook | Exposes a prefetch path so providers can warm pages without showing placeholders. | `IFastTreeDataVirtualizationProvider.PrefetchAsync` |
| Rendering & Virtualization | Loading overlay | Shows an optional overlay with progress during long running fetches. | `FastTreeDataGridVirtualizationSettings.ShowLoadingOverlay`, template part `PART_LoadingOverlay` |
| Rendering & Virtualization | Placeholder skeletons | Renders lightweight skeleton bars for placeholder rows when enabled. | `FastTreeDataGridPresenter`, `FastTreeDataGridVirtualizationSettings.ShowPlaceholderSkeletons` |
| Virtualization Settings | Page size (`PageSize`) | Controls how many rows are requested per page and enforces a minimum of one. | `FastTreeDataGridVirtualizationSettings.PageSize` |
| Virtualization Settings | Prefetch radius (`PrefetchRadius`) | Determines how many neighbour pages the scheduler requests around the viewport. | `FastTreeDataGridVirtualizationSettings.PrefetchRadius` |
| Virtualization Settings | Max cached pages (`MaxPages`) | Caps the number of cached pages to protect memory usage. | `FastTreeDataGridVirtualizationSettings.MaxPages` |
| Virtualization Settings | Max concurrent loads (`MaxConcurrentLoads`) | Limits parallel fetches so providers are not overwhelmed. | `FastTreeDataGridVirtualizationSettings.MaxConcurrentLoads` |
| Virtualization Settings | Reset throttle (`ResetThrottleDelayMilliseconds`) | Coalesces rapid invalidations to a single refresh. | `FastTreeDataGridVirtualizationSettings.ResetThrottleDelayMilliseconds`, `FastTreeDataGridThrottleDispatcher` |
| Virtualization Settings | Dispatcher priority | Lets hosts choose the dispatcher priority used for virtualization work. | `FastTreeDataGridVirtualizationSettings.DispatcherPriority` |
| Virtualization Settings | Loading overlay toggle | Enables or suppresses the built-in loading overlay. | `FastTreeDataGridVirtualizationSettings.ShowLoadingOverlay` |
| Virtualization Settings | Skeleton toggle | Enables or suppresses placeholder skeleton rendering. | `FastTreeDataGridVirtualizationSettings.ShowPlaceholderSkeletons` |
| Layout Customization | Row height property | Sets the baseline row height for uniform layouts. | `FastTreeDataGrid.RowHeight` |
| Layout Customization | Indent width property | Controls tree indentation spacing per level. | `FastTreeDataGrid.IndentWidth` |
| Layout Customization | Header height property | Adjusts header canvas height for custom chrome. | `FastTreeDataGrid.HeaderHeight` |
| Layout Customization | Column collection | Offers an observable list for constructing column definitions in code. | `FastTreeDataGrid.Columns` |
| Data Shaping & Engine | Hierarchy flattening | Flattens arbitrary trees into stable rows with level tracking. | `FastTreeDataGridFlatSource<T>`, `FastTreeDataGridRow.Level` |
| Data Shaping & Engine | Stable keys | Tracks nodes by key to keep expansion and selection across resets. | `FastTreeDataGridFlatSource<T>` `keySelector` |
| Data Shaping & Engine | Per-row expansion toggle | Exposes expansion per row index for both data nodes and group rows. | `FastTreeDataGridFlatSource<T>.ToggleExpansion` |
| Data Shaping & Engine | Custom sort pipeline | Reorders rows using caller-supplied comparisons and restores insertion order when cleared. | `FastTreeDataGridFlatSource<T>.Sort` |
| Data Shaping & Engine | Predicate filtering | Filters visible rows with a predicate while tracking matches. | `FastTreeDataGridFlatSource<T>.SetFilter` |
| Data Shaping & Engine | Auto-expand filtered matches | Automatically expands ancestors when filters match descendants. | `FastTreeDataGridFlatSource<T>.SetFilter(expandMatches)` |
| Data Shaping & Engine | Reset with expansion preservation | Reloads data while restoring expansion based on keys. | `FastTreeDataGridFlatSource<T>.Reset` |
| Data Shaping & Engine | Sort/filter/group aggregation requests | Applies batched sort, filter, group, and aggregate descriptors asynchronously. | `FastTreeDataGridFlatSource<T>.ApplySortFilterAsync`, `FastTreeDataGridSortFilterRequest` |
| Data Shaping & Engine | Group descriptor collection | Exposes group descriptors for binding or direct manipulation. | `FastTreeDataGrid.GroupDescriptors` |
| Data Shaping & Engine | Group expansion helpers | Expands or collapses all groups with a single call. | `FastTreeDataGridFlatSource<T>.ExpandAllGroups`, `.CollapseAllGroups` |
| Data Shaping & Engine | Aggregate descriptor collection | Publishes aggregate descriptors for summaries. | `FastTreeDataGrid.AggregateDescriptors` |
| Data Shaping & Engine | Value provider invalidation | Propagates fine-grained value changes to widgets without bindings. | `IFastTreeDataGridValueProvider.ValueInvalidated`, `FastTreeDataGridRow` |
| Data Shaping & Engine | Row measure callbacks | Widgets can request row remeasurements for dynamic layouts. | `FastTreeDataGridRow.RequestMeasure`, `FastTreeDataGridFlatSource<T>.OnNodeRequestMeasure` |
| Data Sources | Flat in-memory source | Eagerly flattens collections for deterministic in-memory browsing. | `FastTreeDataGridFlatSource<T>` |
| Data Sources | Async loader source | Wraps async factories so initial loads happen off the UI thread. | `FastTreeDataGridAsyncSource<T>` |
| Data Sources | Streaming source | Listens to live feeds and applies inserts/removes to the flattened list. | `FastTreeDataGridStreamingSource<T>` |
| Data Sources | Hybrid source | Combines a snapshot load with real-time deltas for dashboards. | `FastTreeDataGridHybridSource<T>` |
| Data Sources | Dynamic source base | Base class for custom dynamic/virtual sources with reuse of flat engine logic. | `FastTreeDataGridDynamicSource<T>` |
| Data Sources | Source factory helpers | Creates flat sources for common patterns with consistent key handling. | `FastTreeDataGridSourceFactory` |
| Row Layouts | Uniform row layout | Provides constant-height rows for maximal throughput. | `FastTreeDataGridUniformRowLayout` |
| Row Layouts | Variable row layout | Lets providers compute per-row heights during virtualization. | `FastTreeDataGridVariableRowLayout` |
| Row Layouts | Func-based height provider | Simplifies variable height scenarios with a delegate helper. | `FastTreeDataGridFuncVariableRowHeightProvider` |
| Row Layouts | Adaptive row layout | Samples heights in chunks to estimate scroll extents for mixed-height sets. | `FastTreeDataGridAdaptiveRowLayout` |
| Row Layouts | Custom layout contract | Developers can author bespoke layouts for domain-specific positioning. | `IFastTreeDataGridRowLayout` |
| Columns & Cells | Header content | Supplies arbitrary header objects or templates. | `FastTreeDataGridColumn.Header` |
| Columns & Cells | Sizing mode selection | Chooses between pixel, star, and auto sizing per column. | `FastTreeDataGridColumn.SizingMode` (`ColumnSizingMode`) |
| Columns & Cells | Fixed pixel width | Locks a column to an explicit pixel width with min clamping. | `FastTreeDataGridColumn.PixelWidth` |
| Columns & Cells | Star sizing weights | Distributes remaining space proportionally across columns. | `FastTreeDataGridColumn.StarValue` |
| Columns & Cells | Minimum width guard | Prevents columns from shrinking below a threshold. | `FastTreeDataGridColumn.MinWidth` |
| Columns & Cells | Maximum width guard | Caps column width or allows unlimited expansion. | `FastTreeDataGridColumn.MaxWidth` |
| Columns & Cells | Value key mapping | Binds widgets to values without using data bindings. | `FastTreeDataGridColumn.ValueKey` |
| Columns & Cells | Hierarchy indentation flag | Marks which column should show tree indentation and expanders. | `FastTreeDataGridColumn.IsHierarchy` |
| Columns & Cells | Widget factory hook | Creates per-cell widgets on demand for pooling-friendly rendering. | `FastTreeDataGridColumn.WidgetFactory` |
| Columns & Cells | Widget template support | Reuses declarative widget templates per column. | `FastTreeDataGridColumn.CellTemplate` |
| Columns & Cells | Control template support | Falls back to Avalonia controls when needed. | `FastTreeDataGridColumn.CellControlTemplate` |
| Columns & Cells | Edit template | Supplies an edit-time Avalonia template. | `FastTreeDataGridColumn.EditTemplate` |
| Columns & Cells | Edit template selector | Chooses edit template dynamically per row. | `FastTreeDataGridColumn.EditTemplateSelector` |
| Columns & Cells | Read-only toggle | Locks a column against edits. | `FastTreeDataGridColumn.IsReadOnly` |
| Columns & Cells | Resize toggle | Enables or disables end-user resizing. | `FastTreeDataGridColumn.CanUserResize` |
| Columns & Cells | Sort toggle | Opts columns into header sorting gestures. | `FastTreeDataGridColumn.CanUserSort` |
| Columns & Cells | Reorder toggle | Allows drag/drop column reordering. | `FastTreeDataGridColumn.CanUserReorder` |
| Columns & Cells | Pin toggle | Opts columns into pin/unpin interactions. | `FastTreeDataGridColumn.CanUserPin` |
| Columns & Cells | Auto-size toggle | Controls if auto measuring can resize the column. | `FastTreeDataGridColumn.CanAutoSize` |
| Columns & Cells | Filter toggle | Enables per-column filtering UX. | `FastTreeDataGridColumn.CanUserFilter` |
| Columns & Cells | Filter placeholder text | Customises the inline filter prompt. | `FastTreeDataGridColumn.FilterPlaceholder` |
| Columns & Cells | Filter descriptor factory | Builds complex filter descriptors from user input. | `FastTreeDataGridColumn.FilterFactory` |
| Columns & Cells | Pinned position | Pins a column to the left or right rail. | `FastTreeDataGridColumn.PinnedPosition` (`FastTreeDataGridPinnedPosition`) |
| Columns & Cells | Sort direction tracking | Stores the active sort direction for visual adorners. | `FastTreeDataGridColumn.SortDirection` |
| Columns & Cells | Sort order index | Tracks multi-column sort order. | `FastTreeDataGridColumn.SortOrder` |
| Columns & Cells | Custom comparison | Supplies a bespoke row comparison. | `FastTreeDataGridColumn.SortComparison` |
| Columns & Cells | Validation key | Links cells to validation metadata. | `FastTreeDataGridColumn.ValidationKey` |
| Columns & Cells | Control pooling | Reuses editing controls internally to avoid allocations. | `FastTreeDataGridColumn` internal pooling |
| Columns & Cells | Text widget pooling | Reuses formatted text widgets per column for efficiency. | `FastTreeDataGridColumn` text widget pool |
| Widget Toolkit | Immediate-mode widget templates | Renders cells via lightweight widget descriptors instead of controls. | `Widget`, `WidgetTemplate`, `IWidgetTemplate` |
| Widget Toolkit | Animation scheduler | Runs widget animations on a shared frame scheduler. | `WidgetAnimationFrameScheduler` |
| Widget Toolkit | Overlay manager | Manages floating overlays for widgets (tooltips, flyouts). | `WidgetOverlayManager`, `IWidgetOverlayHost` |
| Widget Toolkit | Style manager | Applies palette resources to widgets without Avalonia styles. | `WidgetStyleManager` |
| Widget Toolkit | Scroll viewer widget | Adds scrollable surfaces that participate in virtualization. | `ScrollViewerWidget` |
| Widget Toolkit | Virtualizing stack panel | Provides a vertical virtualizing items host. | `VirtualizingStackPanelWidget` |
| Widget Toolkit | Virtualizing carousel panel | Provides a horizontal virtualizing panel for cards/carousels. | `VirtualizingCarouselPanelWidget` |
| Widget Toolkit | Items control widget | Virtualizes item collections on the widget pipeline. | `ItemsControlWidget` |
| Widget Toolkit | ListBox widget | Adds single-selection gestures and Fluent visuals. | `ListBoxWidget` |
| Widget Toolkit | TreeView widget | Adds indentation, expanders, and helper APIs like `ExpandToLevel`. | `TreeViewWidget` |
| Widget Toolkit | Tab control widget | Renders tab headers/content without templated controls. | `TabControlWidget`, `TabStripWidget` |
| Widget Toolkit | Menu widgets | Builds menu bars and flyout menus using pooled widgets. | `MenuBarWidget`, `MenuWidget` |
| Widget Toolkit | Transitioning content | Animates between widget states with fades or slides. | `TransitioningContentWidget`, `WidgetTransitionDescriptor` |
| Widget Toolkit | Text rendering widget | Delivers wrapped, selectable formatted text without Avalonia `TextBlock`. | `FormattedTextWidget` |
| Widget Toolkit | Gauge/progress widget | Renders progress bars directly on the canvas. | `ProgressWidget` |
| Interaction & UX | Selection model injection | Allows custom selection strategies. | `FastTreeDataGrid.SelectionModel`, `IFastTreeDataGridSelectionModel` |
| Interaction & UX | Selection mode | Switches between single and extended selection semantics. | `FastTreeDataGrid.SelectionMode`, `FastTreeDataGridSelectionMode` |
| Interaction & UX | Selected index | Exposes the currently selected row index. | `FastTreeDataGrid.SelectedIndex` |
| Interaction & UX | Selected item | Mirrors the selected row's item for binding. | `FastTreeDataGrid.SelectedItem` |
| Interaction & UX | Selected indices | Publishes a list of selected row indices. | `FastTreeDataGrid.SelectedIndices` |
| Interaction & UX | Type search selector | Configures how type-to-search derives display text. | `FastTreeDataGrid.TypeSearchSelector` |
| Interaction & UX | Type search events | Raises events when buffered type-to-search queries update. | `FastTreeDataGrid.TypeSearchRequested`, `FastTreeDataGridTypeSearchEventArgs` |
| Interaction & UX | Type search auto reset | Clears the search buffer after a short idle interval. | `FastTreeDataGrid` buffered search timeout |
| Interaction & UX | Row drag & drop reorder | Enables live drag handles, preview overlays, and cancellable hooks. | `FastTreeDataGridRowReorderSettings`, `FastTreeDataGrid.RowReordering`, `FastTreeDataGrid.RowReordered` |
| Interaction & UX | Sort request events | Notifies when header gestures request a sort direction. | `FastTreeDataGrid.SortRequested`, `FastTreeDataGridSortEventArgs` |
| Interaction & UX | Filter row visibility | Shows or hides the filter row globally. | `FastTreeDataGrid.IsFilterRowVisible` |
| Interaction & UX | Column filter flyout | Provides a flyout UI for advanced column filtering. | `FastTreeDataGridColumnFilterFlyout` |
| Interaction & UX | Loading state flag | Exposes a boolean flag when virtualization is loading. | `FastTreeDataGrid.IsLoading` |
| Interaction & UX | Loading progress value | Exposes load progress (0–1 or NaN) for binding. | `FastTreeDataGrid.LoadingProgress` |
| Diagnostics & Telemetry | Reset counters | Emits metrics each time the grid resets. | `FastTreeDataGridVirtualizationDiagnostics.ResetCount` |
| Diagnostics & Telemetry | Viewport render metrics | Records rows rendered and placeholder counts per frame. | `FastTreeDataGridVirtualizationDiagnostics.ViewportRowsRendered`, `.PlaceholderRowsRendered` |
| Diagnostics & Telemetry | Page request metrics | Tracks page request counts, concurrency, and durations. | `FastTreeDataGridVirtualizationDiagnostics.PageRequests`, `.PageFetchDuration`, `.InFlightRequests` |
| Diagnostics & Telemetry | Viewport timing metrics | Measures viewport update durations for profiling. | `FastTreeDataGridVirtualizationDiagnostics.ViewportUpdateDuration` |
| Diagnostics & Telemetry | Logging callback | Surfaces scheduler diagnostics via pluggable logging. | `FastTreeDataGridVirtualizationDiagnostics.Log`, `.LogCallback` |
| Diagnostics & Telemetry | Meter integration | Integrates with `System.Diagnostics.Metrics` for OTEL export. | `FastTreeDataGridVirtualizationDiagnostics.Meter` |
| Diagnostics & Telemetry | Benchmark suite | Ships BenchmarkDotNet scenarios for performance validation. | `benchmarks/FastTreeDataGrid.Benchmarks` |
| Integration & Extensibility | Virtualization provider registry | Lets apps register custom providers and dispose them later. | `FastTreeDataGridVirtualizationProviderRegistry.Register`, `FastTreeDataGridVirtualizationProviderRegistration` |
| Integration & Extensibility | Default provider adapter | Automatically wraps `IFastTreeDataGridSource` into a provider. | `FastTreeDataGridVirtualizationProviderRegistry` default factory, `FastTreeDataGridSourceVirtualizationProvider` |
| Integration & Extensibility | Dependency injection friendly | Supports DI by registering provider factories during startup. | See `docs/virtualization/providers.md` DI sample |
| Integration & Extensibility | Virtualization settings injection | Exposes virtualization settings as a mutable struct property. | `FastTreeDataGrid.VirtualizationSettings` |
| Integration & Extensibility | Value provider contract | Lets domain models expose values without bindings. | `IFastTreeDataGridValueProvider`, `ValueInvalidatedEventArgs` |

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

## Feature: Column Grouping & Pivot
The grid now supports a pivot-style grouping band, so users can drag headers above the grid to reshape datasets without writing code. Grouping works with both flat and hierarchical sources and uses the same value-provider pipeline that powers body cells.

### Quick usage
- Enable grouping by populating `FastTreeDataGrid.GroupDescriptors` or by letting users drag headers into the built-in grouping band (`PART_GroupingBand`).
- Reorder grouping levels via drag, Alt+Ctrl+Up/Down, or the grouping chip context menu; removal and "clear all" share the same gestures as the header menu.
- Attach aggregates by populating `FastTreeDataGrid.AggregateDescriptors` or per-column `AggregateDescriptors`; summaries render as footer rows out of the box.

### Customization hooks
- Persist layouts with `GetGroupingLayout()`/`ApplyGroupingLayout(layout)`—column order, sort direction, and expansion state are captured in a compact JSON contract (`FastTreeDataGridGroupingLayout`).
- Swap visuals by overriding `FastTreeDataGrid.GroupingBandBackground`, chip styles, or by supplying `GroupHeaderTemplate` / `GroupFooterTemplate` on individual columns.
- Implement `IFastTreeDataGridGroupAdapter` for custom key projection (e.g., date bucketing) or `IFastTreeDataGridAggregateProvider` for complex or async aggregates.

> **Migration notes**: The grouping band is opt-in; existing layouts continue to work until descriptors are supplied. If you provide a custom control template for `FastTreeDataGrid`, ensure it includes the new `PART_GroupingBandHost` placeholder to surface the band.

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

## Feature: Row Reorder
`FastTreeDataGrid` includes a drag & drop pipeline for reordering rows with professional visuals. Enable it by toggling `RowReorderSettings.IsEnabled`, adjust the indicator and preview brushes, and provide an `IFastTreeDataGridRowReorderHandler` (the flat source already implements one) so the grid can persist the new order.

### Quick usage
- Set behavioural and visual knobs through `RowReorderSettings` (activation threshold, preview opacity, indicator brush, etc.).
- Handle `RowReordering` to cancel or redirect operations and `RowReordered` for telemetry.
- Plug in a custom handler when you need to forward reorders to a backend or enforce domain-specific rules.

```csharp
var grid = new FastTreeDataGrid
{
    ItemsSource = new FastTreeDataGridFlatSource<Node>(nodes, node => node.Children),
    RowReorderSettings = new FastTreeDataGridRowReorderSettings
    {
        IsEnabled = true,
        ActivationThreshold = 4,
        ShowDragPreview = true,
        DropIndicatorBrush = new SolidColorBrush(Colors.DeepSkyBlue),
        DragPreviewOpacity = 0.7,
    }
};

grid.RowReordering += (_, e) =>
{
    if (e.Request.SourceIndices.Any(IsLockedRow))
    {
        e.Cancel = true;
    }
};

grid.RowReordered += (_, e) =>
    Logger.Info($"Rows moved to {string.Join(",", e.Result.NewIndices)}");
```

> ℹ️  See [docs/rows/reordering.md](docs/rows/reordering.md) for a full settings reference, UX guidance, and examples of custom reorder handlers.

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
