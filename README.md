# FastTreeDataGrid Prototype

This repository hosts an experimental `FastTreeDataGrid` control for Avalonia UI that renders an entire tree grid surface on canvases. Rows, cells, and headers are positioned explicitly in pixel-space, enabling lightweight virtualization without relying on Avalonia's layout system for presenter placement.

## Highlights
- Canvas-based layout reuses a compact pool of row and cell presenters.
- Column sizing supports `Auto`, fixed pixel, and star distributions.
- Auto-sized columns measure cell content on the fly and promote wider widths incrementally.
- Hierarchical data is provided via a flat tree source that expands and collapses branches without rebuilding visuals.
- Minimal row selection combined with click/double-click expansion behaviour.

## Repository Layout
- `src/FastTreeDataGrid.Control` – control library containing the virtualization, presenters, and default styles.
- `samples/FastTreeDataGrid.Demo` – Avalonia app showcasing the control with synthetic hierarchical data.
- `PLAN.md` – milestone breakdown and task tracking for the prototype effort.

## Getting Started
1. Restore and build:
   ```bash
   dotnet build
   ```
2. Run the demo application:
   ```bash
   dotnet run --project samples/FastTreeDataGrid.Demo
   ```
3. Interact with the grids:
   - Explore the "Files" and "Countries" tabs showcasing flat-tree data rendered via widgets.
   - Single-click a row to select it.
   - Click near the left indent or double-click a row to expand/collapse its branch.
   - Scroll to verify row repositioning and header synchronisation.

## Implementation Notes
- **Virtualization** – The control maintains a canvas-sized surface and repositions a small pool of row presenters to cover the current viewport plus a buffer. Logical indices map directly to pixel offsets using a consistent row height (default 28px).
- **Columns** – Column widths are recalculated whenever the viewport or content dictates; auto columns grow as wider content is encountered. Star columns apportion remaining width after fixed/auto columns are settled.
- **Tree Source** – `FastTreeDataGridFlatSource<T>` keeps a flat list of nodes for instantaneous sorting/filtering while rebuilding the visible slice after expand/collapse, and can now rebuild from new snapshots while preserving expansion when a key selector is supplied.
- **Data Sources** – New adapters make the grid pluggable: `FastTreeDataGridAsyncSource<T>` wraps asynchronous queries, `FastTreeDataGridStreamingSource<T>` listens to observable or async change feeds, and `FastTreeDataGridHybridSource<T>` combines an initial query with real-time updates. The demo now ships with a widget metrics page, a high-churn “Dynamic Metrics” view, and a “Live Mutations” page that showcases continuous additions/removals fan-out to all three adapter types.
- **Selection & Interaction** – Selected rows apply a pseudo-class for styling. Expansion is triggered via double-click or clicking inside the indent gutter to avoid accidentally collapsing while scrolling.
- **Widgets** – Cells render lightweight `TextWidget` instances (or factory-provided widgets) directly, avoiding Avalonia layout and data template costs. Built-in helpers now include `IconWidget`, `GeometryWidget`, `ButtonWidget`, `CheckBoxWidget`, `ProgressWidget`, and `CustomDrawWidget`.
- **Layouts** – `SurfaceWidget` now drives `StackLayoutWidget`, `WrapLayoutWidget`, `GridLayoutWidget`, and `DockLayoutWidget`, providing padding/spacing-aware arrangements without invoking Avalonia layout.
- **Avalonia Controls** – Lightweight `ToggleSwitchWidget`, `RadioButtonWidget`, `SliderWidget`, and `BadgeWidget` mirror common Avalonia controls while retaining the fast, immediate-mode rendering pipeline.
- **Widget Styling & Input** – A bespoke `WidgetStyleManager` enables theme-aware styling, pointer/keyboard input dispatching, and fast per-widget visual states (normal, hover, pressed, disabled) without touching Avalonia’s styling pipeline.
- **Widget Gallery** – The sample app ships with a dedicated tab showcasing the built-in widget types and how to feed them via value providers.
- **Value Providers** – Rows expose the `IFastTreeDataGridValueProvider` interface so widgets fetch values by key and respond to fine-grained invalidation events without relying on Avalonia bindings.
- **Styling** – Default look is defined in `Themes/Generic.xaml` and merged automatically when the control assembly is referenced.

## Next Steps
1. Add column resizing and reordering interactions.
2. Support variable row heights and dynamic row templates.
3. Integrate keyboard navigation (arrow keys, space/enter to toggle, multi-select).
4. Investigate deferred data loading hooks for very deep trees.
