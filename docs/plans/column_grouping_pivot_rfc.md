# RFC: Column Grouping & Pivot API for FastTreeDataGrid

## 1. Problem Statement

FastTreeDataGrid currently supports column sorting, filtering, and pinning but lacks a pivot-style grouping workflow. Power users expect to drag headers into a grouping band, stack multiple grouping levels, and view aggregate summaries per group. Without a first-class grouping API, application teams rewrite similar behavior, often compromising virtualization, async loading, or theming fidelity.

## 2. Experience Goals (from Plan §1.1)

1. Drag-to-group for flat sources with visual drop cues, automatic first-level expansion, and status feedback.
2. Drag-to-group for hierarchical sources that merges column grouping with existing tree expansion without resetting state.
3. Multi-level grouping reorder via drag gestures that preserve expanded nodes.
4. Group toggling via chevron or keyboard (`Left/Right`), respecting virtualization and focus handling.
5. Group removal/reset through drag-out or context menu actions with undo affordances.
6. Pivot aggregation insights showing badges/footers with sum/count/average (and custom formatting).
7. Parity between tree and flat sources, including capability hints when adapters are required.

## 3. Proposed API Surface (Plan §2.1/§2.2)

### 3.1 Column & Descriptor Additions
- `FastTreeDataGridColumn.CanUserGroup` toggle and `GroupAdapter` assignment.
- `FastTreeDataGridColumn.AggregateDescriptors` and optional `AggregateProviders`.
- `FastTreeDataGridGroupingDescriptor` records column, adapter, sort direction, comparer, and expansion defaults.
- `FastTreeDataGrid.GroupingDescriptors : AvaloniaList<FastTreeDataGridGroupingDescriptor>` plus `GroupingDescriptorsChanged` event.

### 3.2 Runtime State & Services
- `FastTreeDataGridGroupState` describing active group layers (level, key, aggregates, metadata).
- `IFastTreeDataGridGroupStateProvider` for querying/updating expansion state across sessions.

### 3.3 Extensibility Interfaces
- `IFastTreeDataGridGroupAdapter`: key extraction, labeling, comparer, and optional eligibility checks.
- `IFastTreeDataGridGroupRenderer`: custom header/footer visuals aligning with widget factories.
- `IFastTreeDataGridAggregateProvider`: extensible aggregate calculations (sync/async).
- `IFastTreeDataGridGroupingBehavior`: host policies for drag initiation and drop handling.

## 4. Migration & Compatibility

- Existing grids operate unchanged; grouping UI remains disabled unless `CanUserGroup` is true or descriptors are populated.
- Virtualization providers that already honor `FastTreeDataGridSortFilterRequest.GroupDescriptors` continue to function; adapters supplied for sources lacking grouping support.
- New events/properties follow additive pattern to avoid breaking binary compatibility.
- Documentation and samples will highlight upgrade steps; no required XAML/CS changes for consumers not adopting grouping.

## 5. Open Questions

1. Should grouping descriptors support persistence via existing layout serialization, or is a separate store preferable?
2. Do we require async aggregate providers in v1, or can we defer until real demand emerges?
3. How should keyboard-only users initiate drag-to-group—dedicated commands or context menu actions?
4. What default theming resources are needed for the grouping band to blend with light/dark/high-contrast palettes?
5. Should we expose telemetry/hooks so applications can log grouping changes for analytics?

## 6. Next Steps

1. Gather maintainer feedback on API naming, packaging, and layering boundaries.
2. Prototype drag-to-group flow using existing header presenter events to validate interaction contracts.
3. Bench test grouping updates on large datasets to confirm virtualization targets.

