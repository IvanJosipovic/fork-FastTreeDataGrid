## Column Viewport Performance Plan

### Current Bottlenecks
- `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs:3256` rebuilds every cell on each viewport tick, triggering a full `BuildCell` pass across all visible rows and columns.
- `src/FastTreeDataGrid.Control/Controls/FastTreeDataGridPresenter.cs:205` clears `RowRenderInfo` instances on each `UpdateContent`, so per-cell state is discarded before horizontal diffs can apply.
- `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs:3194` only tries the diff path when `_pendingColumnWork` exists; pure scrolling never enqueues work, so the expensive rebuild path always executes.
- `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs:3335` rents a `FormattedTextWidget`, wraps it in a new `BorderWidget`, and formats text every time, so even cached placeholders incur allocations and string work.

### Desired Behavior
- Column virtualization should mirror row virtualization: only visible viewport intersections should be built, and cached visuals must be reused as the viewport slides.
- The control should diff column snapshots between frames, reusing descriptors and skipping redundant `TryGetMaterializedColumn` calls.
- Presenter structures (`RowRenderInfo`/`CellRenderInfo`) should persist across frames so patches can update existing cells instead of recreating them.
- Async column materialization must integrate with the diff pipeline so placeholders transition smoothly without forcing a full viewport rebuild.

### Proposed Changes
1. **Column Snapshot + Diff**
   - Capture a `ColumnViewportState` each frame containing visible indices, descriptors, widths, and placeholder flags.
   - Diff successive snapshots to identify columns entering/leaving the viewport or changing metrics; use the diff to drive presenter updates.

2. **Persistent Row/Cell Structures**
   - Extend `FastTreeDataGridPresenter` with a `RefreshColumns(delta)` API that reuses existing `RowRenderInfo` objects.
   - During horizontal scroll, skip the blanket `UpdateContent` call; only rebuild cell contents for columns flagged in the diff.

3. **Reusable Cell Containers**
   - Refactor `BuildCell` so cell widgets/borders are created once and cached inside `CellRenderInfo`.
   - Subsequent column updates mutate the widget (text, width, selection state) without allocating new containers.

4. **Scheduler Integration**
   - Feed column viewport changes into `_pendingColumnWork` so the presenter patch path executes during steady scrolls.
   - Ensure column placeholder transitions enqueue targeted invalidations instead of forcing full viewport refreshes.

5. **Instrumentation & Tests**
   - Add counters for `CellsRebuilt` vs `CellsPatched` to validate reuse during scroll benchmarks.
   - Create regression tests that scroll across hundreds of columns and assert widget reuse and stable performance metrics.

### Expected Outcome
Implementing the above brings column virtualization to parity with the row pipeline: horizontal scrolling recalculates only new intersections, placeholder transitions occur without thrashing, and wide grids (400+ columns) maintain smooth framerates.
