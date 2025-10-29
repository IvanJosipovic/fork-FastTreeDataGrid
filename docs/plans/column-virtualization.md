# Column Virtualization Notes

## Current Logic

Column handling in `FastTreeDataGrid` is very different from row virtualization, which is why horizontal scrolling feels heavier:

- **Row virtualization** uses an `IFastTreeDataGridSource` plus the `FastTreeDataGridViewportScheduler`. That pair handles page requests, prefetching, placeholders, throttled resets, and relies on `TryGetMaterializedRow` for previously cached rows. Every scheduling call bundles page requests, supports placeholders, throttles resets, and relies on `TryGetMaterializedRow` for previously cached rows. That tight loop lives in `FastTreeDataGrid.cs` around `_viewportScheduler.Request(...)`, so the presenter receives only the minimal set of rows to render.
- **Column handling** is a one-shot calculation. When the viewport changes (`UpdateViewport`), the grid runs `ColumnLayoutCalculator.Calculate(...)` and then `UpdateVisibleColumns(...)` to keep track of which columns overlap the viewport. However, the presenter still rebuilds every column for each visible row (`foreach` over `_visibleColumnIndices` inside the main row loop). There’s no equivalent pipeline that streams columns; the grid simply iterates all columns deemed visible and recreates every cell widget/control whenever the horizontal offset changes. Even pinned columns are reprocessed on each refresh.
- **Rendering cost**: Per-cell work includes widget creation, text formatting, and validation checks. For complex cells (like the Excel demo’s widgets), that means constructing multiple widgets per column per row whenever you scroll horizontally.
- **No prefetch or placeholder logic for columns**: row virtualization can deliver placeholders and later raise invalidations; columns just redraw synchronously. There’s no `_viewportScheduler` equivalent for columns, so horizontal scrolling forces the UI thread to rebuild every cell immediately.

In short, row virtualization is backed by an async pipeline with background fetch, caching, placeholders, and throttling. Column “virtualization” currently just hides columns outside the viewport but still rebuilds all visible cells synchronously. That’s why horizontal scroll feels more expensive.

## Planned Pipeline

Extending column virtualization to match the row pipeline would require significant refactoring:

1. [x] Define a column provider abstraction (`IFastTreeDataGridColumnSource`) to page column metadata independently of the control.
2. [x] Add a column viewport scheduler to monitor horizontal scrolling, issue prefetch/load requests, and handle placeholders/throttled invalidations.
3. [x] Extend the presenter to cache cell render info keyed by `(rowIndex, columnIndex)` so columns brought into view can reuse cached widgets instead of rebuilding everything.
4. [x] Update selection, editing, and virtualization glue so delayed column realization preserves user interaction, including keyboard navigation and editing.

## Implementation Plan

1. [x] **Column Provider API**
   - [x] Create `IFastTreeDataGridColumnSource` mirroring the row source contract (count, page fetch, placeholder support).
   - [x] Implement a concrete provider that wraps the existing `_columns` collection while supporting incremental materialization.

2. [x] **Column Viewport Scheduler**
   - [x] Introduce `FastTreeDataGridColumnViewportScheduler` that watches horizontal offset changes, computes visible ranges, and issues prefetch/reset requests at throttled intervals.
   - [x] Integrate the scheduler with the new column provider and the main control lifecycle (attach/detach, invalidation events).

3. [x] **Presenter Caching**
   - [x] Update `FastTreeDataGridPresenter` to maintain a cache of `CellRenderInfo` keyed by `(rowIndex, columnIndex)`.
   - [x] Rework the render/update path to reuse cached widgets when columns enter the viewport, falling back to placeholders until data is ready.

4. [ ] **Control Integration**
   - [x] Replace direct column iteration in `FastTreeDataGrid` with scheduler-driven realization.
   - [x] Subscribe to column scheduler events to request column pages and refresh placeholders.
   - [x] Teach `FastTreeDataGrid` to manage column placeholders and issue invalidations when columns materialize.
   - [x] Update row/column coordination so the presenter receives synchronized row+column viewports.
   - [x] Ensure selection, editing, automation, and type search respect temporarily unavailable columns while placeholders are displayed.

5. [ ] **Performance & Testing**
   - [x] Add instrumentation to measure column prefetch latency and horizontal scroll responsiveness.
   - [ ] Capture diagnostics that correlate row/column fetch timing for regression tracking.
   - [ ] Expand tests to cover column placeholder interactions, keyboard navigation, and editing across delayed columns.
   - [ ] Create unit/integration tests covering placeholder rendering, invalidation, editing, and selection across column virtualization boundaries.
