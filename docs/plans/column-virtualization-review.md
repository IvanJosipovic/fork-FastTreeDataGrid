# Column Virtualization Review

## Findings

1. `src/FastTreeDataGrid.Control/Infrastructure/Columns/FastTreeDataGridInlineColumnSource.cs`:65-82  
   - Snapshot refresh raises `ColumnMaterialized` for every column. Column property changes (resize, autosize, header edits) rebuild the entire descriptor array, so the control posts a column-reset event for *every* column each time. The scheduler/viewport end up thrashing, which defeats the performance goal.

2. `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs`:2953-3034
   - Column virtualization still renders synchronously. With `SupportsPlaceholders = false` and `GetPageAsync` just slicing the cached array, the scheduler never yields placeholders or pushes work off the UI thread. The viewport loop still rebuilds every visible column immediately, so horizontal scroll perf is basically unchanged.

3. `src/FastTreeDataGrid.Control/Infrastructure/Columns/FastTreeDataGridInlineColumnSource.cs`:19-23
   - The async materialization fields (`_materializationTasks`, `_pendingLoads`, `_materializationStart`) became dead code when we switched to synchronous snapshots. As a result the new `ColumnPrefetchLatency` metric is always zero and column virtualization has no background work.

## Suggestions

- Track descriptor diffs and only fire materialized/invalidated events for affected columns instead of tearing down the whole snapshot on every change.
- Reintroduce an async/placeholder-aware column page pipeline so the scheduler can fetch metadata in the background, mirroring row virtualization.
- If synchronous snapshots are the long-term plan, remove the unused async scaffolding and telemetry hooks to avoid misleading metrics.

## Implementation Plan

1. Snapshot Hygiene
   - Build a descriptor comparator that surfaces only changed indices after column mutations.
   - Raise `ColumnMaterialized`/`Invalidated` events exclusively for those indices to avoid full pipeline churn.

2. Async Column Pipeline
   - Restore an asynchronous column fetch path: queue background materialization with placeholders while work is pending.
   - Update the scheduler to respect placeholder indices and completion tasks so column loads mirror row virtualization behaviour.

3. Metric Alignment
   - Once async loading is back, wire real latency reporting into `ColumnPrefetchLatency`; otherwise strip the unused counters to prevent misleading telemetry.

4. Control Integration
   - Adjust `FastTreeDataGrid` so the viewport loop honours placeholders (skip heavy rendering until descriptors arrive) and only invalidates affected columns.
   - Add targeted tests covering column placeholder navigation, editing, and scroll responsiveness regressions.
