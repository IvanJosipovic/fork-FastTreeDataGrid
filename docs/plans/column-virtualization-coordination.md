# Column Virtualization Coordination Review

## Findings

1. `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs`:1174-1194  
   - Every column invalidation or materialization posts a full `RequestViewportUpdate()`. Each descriptor completion re-runs the entire row/column pipeline, so background column loads cause repeated vertical refreshes. The benefit of placeholders is lost because the control keeps rebuilding the viewport while descriptors materialize.

2. `src/FastTreeDataGrid.Control/Controls/FastTreeDataGridPresenter.cs`:168-187  
   - `UpdateContent` rebuilds every `RowRenderInfo` on each viewport update. With asynchronous columns, these updates happen more frequently, multiplying the cost of cell recreation even when only a single column changes.

3. `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs`:3125-3127  
   - During rendering we re-query the column source for every row/column pair to check placeholder state. This introduces `O(rows × columns)` locking/lookup overhead each frame and hurts vertical scrolling performance as soon as column virtualization is enabled.

4. `src/FastTreeDataGrid.Control/Infrastructure/Columns/FastTreeDataGridInlineColumnSource.cs`:326-365  
   - The source clears its descriptor immediately when a property changes. Visible columns downgrade to placeholders until the async pass finishes, forcing the control to build each cell twice (placeholder, then real data) and creating noticeable flicker.

## Proposed Architecture

1. **Viewport Coordinator**  
   - Introduce a coordinator that coalesces row and column work per dispatcher turn. Column materialization enqueues column indices; the coordinator patches cells in-place instead of triggering a fresh viewport pass, eliminating the cancel/re-request churn we see today.

2. **Descriptor Versioning**  
   - Maintain `CurrentDescriptor` and `PendingDescriptor` per column. Keep the live descriptor active until the replacement is ready, then swap atomically. Callers can choose whether to present the pending state or continue showing the current descriptor to avoid placeholder regressions.

3. **Viewport Snapshots**  
   - Capture visible column descriptors once per frame (`ColumnViewportSnapshot`) and hand that immutable list to the row renderer. The presenter reuses the snapshot across all rows, avoiding repeated source queries while a frame is being rendered.

4. **Presenter Diff Updates**  
   - Extend `FastTreeDataGridPresenter` with diff-based APIs (`ApplyColumnChanges`, `ApplyRowChanges`) so we update only the cells whose descriptors changed. Async column loads can swap placeholders for real cells without rebuilding the entire viewport.

5. **Scheduler Synchronization**  
   - Share a lightweight “viewport ticket” between row and column schedulers. When both axes change, issue a single combined ticket so page requests complete together, preventing repeated cancellation loops and stabilizing the loading signals.

## Next Steps

1. Implement the viewport coordinator to queue column invalidations/materializations and collapse them into a single `UpdateViewport` per dispatcher cycle.
2. Refactor the presenter to reuse row structures and support granular column patches; add regression tests for placeholder-to-materialized transitions.
3. Update the column source to keep descriptors live during async refreshes by exposing pending/current descriptor state.
4. Add diagnostics that log paired row/column ticket lifetimes and placeholder durations, then validate the redesigned pipeline inside the demo scenarios.
