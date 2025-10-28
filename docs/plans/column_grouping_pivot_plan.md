# FastTreeDataGrid Column Grouping & Pivot API Plan

## Objective

Deliver a multi-level column grouping experience comparable to pivot table tooling. Users must be able to drag headers into a grouping band, stack multiple grouping layers, reorder/remove groups, and extend behavior through the existing FastTreeDataGrid extensibility patterns.

## Task Breakdown

1. [x] Architecture & Requirements Alignment
   - [x] (1.1) Document UX stories for drag-to-group, multi-level grouping, group toggling, and pivot-like aggregations across tree + flat sources.
     - **Drag-to-Group (Flat Source)**: As a dashboard analyst working with a flat dataset, I drag a column header into the grouping band to instantly reorganize the grid into grouped sections. While dragging, drop targets highlight valid positions; releasing the header shows the new group, auto-expands the first level, and updates the status bar with the active grouping chain.
     - **Drag-to-Group (Hierarchical Source)**: As a financial analyst exploring a hierarchical ledger source, I drag an inner column (e.g., `Region`) into the grouping band. The grid augments the existing tree with an additional group layer while preserving existing expand/collapse state. A preview badge indicates that hierarchical and column grouping will merge.
     - **Multi-Level Grouping Reorder**: As a researcher refining a pivot view, I drag a grouped descriptor inside the band to reorder levels. The UI animates the placeholder to show the new order, and upon drop the grid recalculates group nesting without collapsing already expanded nodes.
     - **Group Toggle Experience**: As an operations lead reviewing grouped results, I click the chevron on a group header or press `Left/Right` to collapse/expand, with the grid virtualizing hidden rows instantly. Focus returns to the group header after the toggle to support keyboard navigation.
     - **Group Removal & Reset**: As a user experimenting with slice-and-dice flows, I drag a grouping chip out of the band (or pick *Remove Group* from the context menu) to revert the grid to its previous state. A transient toast offers *Undo* to restore the descriptor, ensuring quick iteration.
     - **Pivot Aggregation Insights**: As a product manager reviewing metrics, I pin `Country` then `Product` to the grouping band and enable aggregate columns. Group headers display sum/count/average badges with tooltip detail, while group footers show custom cells (e.g., contribution percentage) to mimic pivot table summaries.
     - **Tree & Flat Source Parity**: As a developer integrating the control, I expect identical grouping gestures and visuals whether my source implements tree semantics or a flat list. The control surfaces capability hints when the backing source lacks grouping support, offering an opt-in adapter.
   - [x] (1.2) Audit current header (`src/FastTreeDataGrid.Control/Controls/FastTreeDataGridHeaderPresenter.cs`) and grid (`src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs`) APIs to map integration points and extension hooks.
     - **Header Presenter Events** (`FastTreeDataGridHeaderPresenter.cs:56-89`): Existing signals (`ColumnReorderRequested`, `ColumnPinRequested`, `ColumnFilterRequested`, etc.) bubble all column gestures to the parent control; drag-to-group can follow the same pattern via a new `ColumnGroupRequested` event without rewiring pointer logic.
     - **Pointer Lifecycle Hooks** (`FastTreeDataGridHeaderPresenter.cs:180-590`): Pointer press/move/release handlers already track `_pressedColumnIndex`, `_isReordering`, `_reorderInsertIndex`. We can branch off the reordering flow to detect drags exiting the header band and redirect them to a grouping drop zone model.
     - **Context Menu Infrastructure** (`FastTreeDataGridHeaderPresenter.cs:360-454`): Menu command routing uses `ColumnMenuCommand` and `OnMenuItemClick`; grouping/un-group actions can slot into this command map for keyboard parity.
     - **Grid Attachment Points** (`FastTreeDataGrid.cs:1116-1185`): `_headerPresenter` wires event handlers in `AttachTemplatePartHandlers`; new grouping notifications simply extend this subscription surface.
     - **Data Operations Pipeline** (`FastTreeDataGrid.cs:1378-1444`): `ApplyDataOperationsToProvider` forwards `SortDescriptors`, `FilterDescriptors`, `GroupDescriptors`, and `AggregateDescriptors` to the virtualization provider. Current grouping state lives in `_groupDescriptors`; pivot band interactions should update this collection to reuse the pipeline.
     - **Grouping Controller Contract** (`IFastTreeDataGridGroupingController.cs`, used in `FastTreeDataGrid.cs:209-233`): Expand/collapse commands are abstracted through this interface; group band UI can call existing `ExpandAllGroups`/`CollapseAllGroups`.
     - **Source-Level Grouping Implementation** (`FastTreeDataGridFlatSource.cs:24-1396`): Flat sources already maintain `_groupDescriptors`, `_groupExpansionStates`, and summary rows via `FastTreeDataGridGroupDescriptor`/`FastTreeDataGridAggregateDescriptor`. The new API must remain compatible or supply adapters for sources that do not implement grouping.
     - **Virtualization Provider Bridge** (`FastTreeDataGridSourceVirtualizationProvider.cs:7-120`): Passes grouping operations through to the underlying source; any new grouping strategy must either extend this provider or provide a parallel implementation to honor existing virtualization hooks.
   - [x] (1.3) Capture non-functional goals (virtualization preservation, async data, accessibility, theming) and success metrics.
     - **Virtualization Integrity**: Group band UI must not invalidate the virtualization scheduler; `FastTreeDataGridPresenter` and provider contracts (`IFastTreeDataVirtualizationProvider` in `FastTreeDataGridSourceVirtualizationProvider.cs:7-118`) should operate unchanged, with grouping state expressed through existing `FastTreeDataGridSortFilterRequest` payloads.
     - **Async Data Compatibility**: Drag-to-group interactions must work while data loads asynchronously (`FastTreeDataGrid.cs:1390-1444` builds async `ApplySortFilterAsync` calls); ensure regrouping reuses cancellation tokens and does not block UI threads.
     - **Accessibility Surface**: Group band affordances need automation peers aligned with `FastTreeDataGrid.cs:1876-2055` automation helpers—group headers must expose name/value/patterns and keyboard equivalents for drag/toggle actions.
     - **Theming & Styling**: New visuals should bind to theme resources rather than hard-coded brushes (contrast with existing header brush usage in `FastTreeDataGridHeaderPresenter.cs:30-49`); support light/dark/high-contrast palettes.
     - **Performance Targets**: Maintain sub-16ms frame budget for drag/drop and expand/collapse on datasets up to 100k rows; regrouping should complete within 200ms on benchmark datasets measured via `FastTreeDataGrid.Benchmarks`.
     - **State Persistence**: Grouping changes should round-trip through layout serialization APIs once implemented (see existing column layout persistence hooks in `FastTreeDataGrid.cs:1645-1732`) to guarantee restart fidelity.

2. [x] Public API Surface Design
   - [x] (2.1) Author draft API proposal for grouping descriptors, group state objects, and pivot aggregation providers that can be registered per column.
     - `FastTreeDataGridColumn` additions:
       - `public IFastTreeDataGridGroupAdapter? GroupAdapter { get; set; }` – allows per-column grouping logic (key extraction, display text, sort precedence).
       - `public IReadOnlyList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors { get; }` – declarative aggregates tied to the column, merged into the grid-level aggregation request.
       - `public bool CanUserGroup { get; set; }` (default `true`) – toggles UI affordances for drag-to-group.
     - Grouping descriptor model:
       - `public sealed class FastTreeDataGridGroupingDescriptor` capturing `Column`, `GroupAdapter`, `SortDirection`, `Comparer`, and `IsExpanded` defaults.
       - Descriptor collection exposed via `FastTreeDataGrid.GroupingDescriptors : AvaloniaList<FastTreeDataGridGroupingDescriptor>` with change notifications.
       - New event `GroupingDescriptorsChanged` to mirror existing sort/filter change flows.
     - Runtime state objects:
       - `FastTreeDataGridGroupState` representing an active group layer (column key, level, aggregate context, expansion state, optional custom metadata).
       - `IFastTreeDataGridGroupStateProvider` service to query/update expansion state, enabling adapters or virtualization providers to persist custom payloads.
     - Pivot aggregation providers:
       - `IFastTreeDataGridAggregateProvider` with `CalculateAsync(FastTreeDataGridGroupContext context, CancellationToken token)` returning `FastTreeDataGridAggregateResult`.
       - Built-in implementations for sum/count/average from existing `FastTreeDataGridAggregateDescriptor` helpers.
       - Allow column-level registration via `FastTreeDataGridColumn.AggregateProviders`.
   - [x] (2.2) Define extension interfaces (e.g., `IFastTreeDataGridGroupingAdapter`, `IGroupAggregationStrategy`) ensuring parity with the control’s plugin model.
     - `IFastTreeDataGridGroupAdapter`
       - Members: `GetGroupKey(FastTreeDataGridRow row)`, `GetGroupLabel(object? key)`, `IComparer<object?>? Comparer { get; }`, optional `CanGroup(FastTreeDataGridColumn column)`.
       - Responsibilities: centralizes column-specific key extraction, display labels, and sorting semantics for grouping descriptors.
     - `IFastTreeDataGridGroupRenderer`
       - Members: `Control CreateGroupHeader(FastTreeDataGridGroupState state)`, `Control? CreateGroupFooter(FastTreeDataGridGroupState state)`, `void Update(Control control, FastTreeDataGridGroupState state)`.
       - Enables custom visuals for group headers/footers while integrating with existing widget factory concepts.
     - `IFastTreeDataGridGroupStateProvider`
       - Members: `FastTreeDataGridGroupState GetState(string path)`, `void SetExpanded(string path, bool isExpanded)`, `void Clear()`.
       - Supports persistence/extensibility for group expansion state beyond in-memory defaults.
     - `IFastTreeDataGridAggregateProvider`
       - Members: `FastTreeDataGridAggregateResult Calculate(FastTreeDataGridGroupContext context)`, optional async overload if heavy computation is needed.
       - Allows custom aggregations (percent-of-total, weighted averages) to plug into summaries.
     - `IFastTreeDataGridGroupingBehavior`
       - Members: `bool CanBeginDrag(FastTreeDataGridColumn column)`, `bool TryHandleDrop(FastTreeDataGridColumn column, FastTreeDataGridGroupingDropContext context)`.
       - Provides policy hooks for host applications to govern grouping gestures (e.g., restricted columns, conditional availability).
   - [ ] (2.3) Review API proposal with maintainers and iterate until approved.
     - Prepare RFC including: problem statement, UX stories from 1.1, proposed APIs (2.1/2.2), migration impact, and open questions.
     - Schedule design review with core maintainers; capture feedback, naming adjustments, and backward compatibility concerns.
     - Revise descriptors/interfaces per review outcomes; validate against sample implementations and update plan checkboxes once sign-off is recorded.

3. [ ] Data Pipeline & State Management
   - [x] (3.1) Extend the data source contract (or add adapters) to emit grouped views without breaking existing `IFastTreeDataGridSource` implementations.
     - Inventory current contracts: `IFastTreeDataGridSource` (direct row access, expansion toggles) and `IFastTreeDataGridSortFilterHandler` (sort/filter application). Confirm where grouping metadata is consumed (`FastTreeDataGridSortFilterRequest.GroupDescriptors` in `FastTreeDataGrid.cs:1405-1419`).
     - Design additive interface `IFastTreeDataGridGroupingHandler` exposing `ValueTask<FastTreeDataGridGroupedPageResult> GetGroupedPageAsync(...)` or equivalent so sources can opt-in without modifying `IFastTreeDataGridSource`.
     - Prototype adapter `FastTreeDataGridGroupingAdapter` that wraps legacy sources: intercepts `GroupingDescriptors`, materializes grouped view using in-memory projections, and feeds existing virtualization provider (`FastTreeDataGridSourceVirtualizationProvider`).
     - Update `FastTreeDataGridFlatSource<T>` plan to implement the new handler directly—reuse existing `_groupDescriptors` and `_visibleEntries` but expose efficient snapshot APIs for virtualization.
     - Define capability discovery (`FastTreeDataGridSourceCapabilities` flag or `TryGetService<T>`) so the control can surface grouping affordances only when supported.
     - Outline migration guidance ensuring current consumers of `IFastTreeDataGridSource` remain unaffected; adapters become default for legacy sources until they implement the new handler.
   - [x] (3.2) Implement grouping state container tracking descriptors, expansion state, and aggregates so that virtualization can query lightweight snapshots.
     - Define `FastTreeDataGridGroupingStateStore` responsible for storing descriptor stack, per-group expansion flags, cached aggregates, and last materialized counts.
     - Ensure store exposes readonly snapshot struct (`FastTreeDataGridGroupingSnapshot`) consumable by virtualization provider without locking.
     - Integrate with `FastTreeDataGridGroupStateProvider` to synchronize UI (band chips, row presenters) with data layer; support serialization hooks for persistence.
     - Provide invalidation triggers when descriptors change, aggregates recompute, or external adapters update state; forward these through existing `FastTreeDataGridInvalidatedEventArgs`.
     - Design thread-safe update pattern (e.g., `ReaderWriterLockSlim` or dispatcher marshalling) to avoid contention under async refresh.
   - [x] (3.3) Introduce change notifications for grouping mutations to invalidate caches efficiently.
     - Add `GroupingStateChanged` event on `FastTreeDataGridGroupingStateStore` with payload describing descriptor diffs, expansion delta, and aggregate invalidation scopes.
     - Ensure `FastTreeDataGrid` forwards notifications to virtualization provider (`IFastTreeDataVirtualizationProvider`) so cached pages can refresh or recycle as needed.
     - Update `FastTreeDataGridSourceVirtualizationProvider` to translate grouping notifications into `FastTreeDataGridInvalidatedEventArgs` (range or metadata invalidations).
     - Provide throttling/debouncing strategy for rapid drag/drop changes to avoid redundant refreshes (e.g., combine updates within 50 ms on UI thread).
     - Document notification sequencing relative to sort/filter events to prevent race conditions in adapters.

4. [x] Group Band UI Shell
   - [x] (4.1) Design the group box visual host (header-aligned dock above the grid) honoring theming resources and high-contrast requirements.
    - Introduce `FastTreeDataGridGroupingBand` control built atop Avalonia primitives (`Border` + `ItemsControl`/`WrapPanel`) so grouping chips are true controls that inherit theming, input, and automation support.
    - Provide default styles for light/dark/high-contrast palettes; expose resource keys (`FastTreeDataGridGroupingBandBackgroundBrush`, etc.) for customization.
    - Ensure layout works with pinned columns (band content offset matches header presenter scroll/pin logic) and respects `HeaderHeight`.
  - [x] (4.2) Implement header drag detection and visual cues for drop targets using Avalonia drag-and-drop infrastructure.
    - Use Avalonia `DragDrop` APIs (`DragDrop.DoDragDrop`, `DragDrop.SetAllowDrop`, `DragDrop.AddDropHandler`) to initiate column drags from the header and surface hit-testing onto the grouping band.
    - Render drop indicator (ghost chip) within `FastTreeDataGridGroupingBand` using data from Avalonia `DragEventArgs` (position, effects).
    - Integrate with `IFastTreeDataGridGroupingBehavior` to validate drops before committing descriptors, updating drag `Effects` for invalid targets.
  - [x] (4.3) Provide reorder/remove affordances (drag-within band, context menu) and keyboard equivalents for accessibility.
    - Allow chips inside `FastTreeDataGridGroupingBand` to reorder via Avalonia drag-and-drop gestures; update `GroupingDescriptors` accordingly while preserving expansion state.
     - Add context menu actions (`Move Up`, `Move Down`, `Remove`, `Clear All`) leveraging same command infrastructure as header menu.
     - Offer keyboard commands (e.g., `Ctrl+Alt+Up/Down` to reorder, `Delete` to remove) and expose automation patterns (`Invoke`, `Selection`) for screen readers.

5. [ ] Group Row Rendering & Virtualization
   - [x] (5.1) Create reusable row presenter(s) for group headers/footers compatible with current virtualization scheduler.
     - Implemented `FastTreeDataGridGroupRowPresenter` and `FastTreeDataGridGroupSummaryPresenter` with widget pooling via `FastTreeDataGridPresenter`.
     - Integrated presenters into the viewport pipeline so grouped rows reuse layout measurements while honoring column auto-sizing.
     - `IFastTreeDataGridGroupMetadata` exposes item count metadata for default presenter badges; custom renderers can build on the same contract.
   - [x] (5.2) Ensure group rows can host aggregate summaries and custom templates without breaking cell recycling.
     - Group header/footer controls now use dedicated pools keyed by role, preventing template swaps from mixing recycled instances.
     - Columns expose `GroupHeader*`/`GroupFooter*` widget and control templates so custom renderers can supply badges, charts, or asynchronous placeholders per band.
     - Template changes trigger viewport refreshes and purge the corresponding control pools to avoid stale visuals during virtualization updates.
   - [x] (5.3) Add lazy materialization for deeply nested groups to avoid rendering costs when collapsed.
     - `FastTreeDataGridFlatSource` now short-circuits deeper group materialization when a parent is collapsed, storing raw rows on the collapsed node so counts/aggregates stay accurate without allocating nested `GroupView`s.
     - `FastTreeDataGrid` issues viewport prefetch requests after a group expands (with a small cooldown) so virtualization hydrates the new subtree before the user scrolls.
     - Prefetch throttling prevents duplicate requests during rapid toggle gestures, reducing churn while expansion state is flapping.

6. [ ] Interaction & Behavior Layer
   - [x] (6.1) Support multi-level grouping reorder (drag to new level, drag between levels) and ensure state updates broadcast to the data pipeline.
     - Allow `FastTreeDataGridGroupingBand` to reorder descriptors via drag-and-drop, updating `_groupDescriptors` while preserving descriptor metadata (sort order, aggregates).
     - When a descriptor moves across levels, raise `GroupingStateChanged` with reorder payload and request data pipeline refresh through `ApplyDataOperationsToProvider`.
     - Update keyboard and automation pathways (`IFastTreeDataGridGroupingBehavior`) to invoke the same reorder logic.
   - [x] (6.2) Add expansion/collapse, select, and context menu commands for group rows with keyboard + automation parity.
     - Hook group row presenters into existing toggle mechanisms (`ToggleExpansion`) and expose commands (`Expand`, `Collapse`, recursive helpers).
     - Wire keyboard shortcuts (e.g., `Left/Right`, `+/-` with optional Ctrl for recursive, `*` to expand/collapse all) and automation patterns to call into grouping state store; ensure virtualization updates via notifications.
     - Provide grouping band chip actions (reorder/remove) and row-level context menu commands (expand/collapse, recursive, expand/collapse all).
   - [x] (6.3) Integrate grouping with existing sorting pipeline so grouped states can be sorted within levels (including multi-column sort compatibility).
     - Ensure `GroupingDescriptors` store per-level sort direction; when descriptors change, update `_sortDescriptions` where necessary.
     - Modify `ApplyDataOperationsToProvider` flow to pass hierarchical sort metadata alongside grouping keys, keeping compatibility with multi-column sorting.
     - Update aggregation summary ordering to respect nested sort preferences without re-materializing entire datasets.

7. [ ] Pivot Aggregations & Calculations
   - [x] (7.1) Define aggregation contract (built-in sum/count/avg, custom provider hook) that operates on grouped records.
     - Finalize `IFastTreeDataGridAggregateProvider` contract: synchronous `Calculate` plus optional `ValueTask<FastTreeDataGridAggregateResult> CalculateAsync`.
     - Provide built-in providers (sum, count, average, min/max, custom formatter) leveraging `FastTreeDataGridAggregateDescriptor`.
     - Map descriptors to columns and grouping levels, allowing multiple aggregates per level with unique keys.
   - [x] (7.2) Implement aggregate computation pipeline with caching/invalidation policies aligned to virtualization.
     - Cache aggregate `VisibleEntry` instances by descriptor/path/placement so repeated materialization reuses results during expansion and virtualization passes.
     - Invalidate cache entries when underlying rows change (sort/filter/group mutation events) or when providers request explicit refresh.
     - Support incremental updates: reuse cached aggregates for unaffected branches to minimize recomputation.
   - [x] (7.3) Render aggregates in group headers/footers and expose APIs for consumers to customize formatting/layout.
     - Extend `FastTreeDataGridGroupRowPresenter` to render aggregate badges/footers using renderer hooks or column templates.
     - Allow consumers to customize placement (header badge, footer row, inline cell) via descriptor metadata.
     - Provide placeholder visuals while async aggregates compute; refresh UI when results arrive without re-creating rows.

8. [ ] Persistence & State Restoration
   - [x] (8.1) Extend layout serialization to capture grouping descriptors, order, and expansion state.
     - Added `FastTreeDataGridGroupingLayout` compact DTO plus `GetGroupingLayout`/`ApplyGroupingLayout` APIs so apps can persist grouping order alongside expansion state.
     - Layout descriptors capture column keys, sort direction, default expansion, and string metadata; expansion deltas rebuild `FastTreeDataGridGroupingStateStore` on restore.
     - Restoring layouts rehydrates descriptors via existing column adapters and triggers data pipeline updates to ensure virtualization reflects the serialized state.
   - [x] (8.2) Provide load/save helpers so applications can persist grouping setups (e.g., user preferences, workspace restoration).
     - `FastTreeDataGridGroupingLayoutSerializer` exposes JSON helpers and `FastTreeDataGrid` now surfaces `GetGroupingLayout`/`ApplyGroupingLayout`.
     - Grouping demo includes Save / Restore / Clear actions that serialize layouts to JSON and rehydrate the grid, showcasing persistence workflow.
   - [x] (8.3) Verify serialization stability across schema changes (forward/backward compatibility checks).
     - Grouping layouts carry an explicit version field; JSON serializer defaults missing version to 1 and rejects newer layouts with a clear exception.
     - Added unit tests covering serialization round-trips, missing-version compatibility, and unsupported-version rejection to guard future changes.

9. [ ] Performance & Scalability Validation
   - [x] (9.1) Benchmark grouping operations on large datasets (100k+ rows) with varying depth using `FastTreeDataGrid.Benchmarks`.
     - Extend existing benchmark suite to add scenarios for: single-level grouping, multi-level grouping, and regrouping after descriptor reorder.
     - Capture metrics for CPU time, allocation counts, and frame rendering cost (via `BenchmarkDotNet` + optional rendering harness).
     - Compare results against baseline (no grouping) and set target thresholds (≤20% slowdown for regrouping operations).
     - Added `GroupingBenchmarks` exercising two-level and three-level grouping with revenue aggregates to validate caching.
   - [x] (9.2) Profile virtualization interplay to avoid cache churn, measuring GC pressure and frame times.
     - Added virtualization diagnostics counters (`fasttree_datagrid_cache_hits/misses`) and prefetch logging to trace expansion behavior.
     - Guide documents how to hook `FastTreeDataGridVirtualizationDiagnostics` for custom monitoring pipelines.
     - Remaining profiling deltas flow through performance backlog informed by new instrumentation.
   - [x] (9.3) Tune async refresh paths to handle remote data sources without blocking UI threads.
     - Grouping/sort requests now cancel any pending work before queuing new operations, preventing slow async sources from piling up.
     - `docs/widgets/fasttreedatagrid-grouping.md` captures guidance on cancellation-aware sources, batching, and monitoring.
     - Remaining async stress results feed back through the diagnostics and documentation pipeline.

10. [ ] Extensibility & Plugin Story
    - [x] (10.1) Document and sample custom grouping adapters (e.g., time bucketing, hierarchical descriptors).
      - Guide now includes a dedicated section on custom adapters with code sample (`RevenueBucketGroupAdapter`).
      - Grouping demo ships a “Region ▸ Revenue Band” preset that applies the adapter and highlights bucketed aggregation.
      - Existing persistence helpers interoperate with adapter metadata, demonstrating end-to-end workflow.
    - [x] (10.2) Expose override points for styling group bands, row templates, and aggregate visuals.
      - Grouping guide now documents resource keys (`FastTreeDataGrid.GroupingBandBackground`, chip styles, drop indicator brush) and demonstrates template overrides.
      - Sample shows how to attach custom row templates and resource dictionaries from application resources.
      - Developers can extend `FastTreeDataGrid.GroupRowTemplate` / `FastTreeDataGrid.GroupSummaryTemplate` per the updated guidance.
    - [x] (10.3) Ensure new APIs interoperate with existing widget factories and column metadata.
      - Validate integration with `FastTreeDataGridColumn.WidgetFactory` to ensure custom cell renderers work inside group headers and summaries.
        - Added `FastTreeDataGridGroupingDescriptorFactory` to centralise descriptor creation and keep metadata (widget factories, aggregates, comparer) in sync.
        - New control tests under `FastTreeDataGridGroupingInteropTests` assert that grouped rows still surface value providers for widget factories and that aggregate delegates remain callable.
      - Update column metadata documentation to explain interplay between grouping, sorting, filtering, and editing templates.
        - `docs/widgets/fasttreedatagrid-grouping.md` now includes a column metadata section covering value keys, widgets, filters, and aggregates.
      - Add regression scenarios confirming that enabling grouping does not break existing column plugins (filters, editors, reorder handlers).
        - Tests exercise grouping+filter pipelines to ensure existing predicates operate with grouped sources.

11. [ ] Testing Strategy
    - [x] (11.1) Add unit tests covering grouping descriptors, state transitions, and aggregation correctness.
      - Test descriptor creation, equality, reorder semantics, and persistence serialization.
        - `FastTreeDataGridGroupingLayoutTests` now assert serialization round-trips preserve grouping order.
      - Validate `FastTreeDataGridGroupingStateStore` handles expansion toggles, aggregate cache updates, and notification payloads.
        - Added `FastTreeDataGridGroupingStateStoreTests` covering descriptor snapshots, expansion updates, and reset notifications.
      - Verify aggregate providers produce expected results (including async providers) with mock data sources.
        - Extended `FastTreeDataGridGroupingTests` with provider-backed aggregate coverage to confirm custom providers surface formatted summaries.
    - [ ] (11.2) Introduce interaction tests for drag-to-group, reorder, and keyboard flows (UI automation).
      - Use UI automation harness to simulate drag of headers into grouping band, verifying descriptor updates and drop indicators.
      - Cover reordering within the band and removal via context menu/keyboard, ensuring virtualization updates correctly.
      - Include accessibility assertions (focus management, automation patterns) for band chips and group rows.
    - [ ] (11.3) Create regression scenarios for serialization, virtualization interaction, and async updates.
      - Snapshot tests for saved grouping layouts (descriptors, expansion state) and round-trip loading.
      - Stress tests combining async refresh, grouping changes, and virtualization scrolling to detect race conditions.
      - Guard existing features (sorting, filtering, editing) via integration suites with grouping enabled.

12. [ ] Documentation & Adoption
    - [x] (12.1) Publish developer guide detailing grouping setup, API entry points, and customization options under `docs/widgets` or `docs/plans`.
      - Added `docs/widgets/fasttreedatagrid-grouping.md` covering enablement, descriptors, customization hooks, persistence APIs, and best practices.
    - [x] (12.2) Update samples (e.g., `samples/FastTreeDataGrid.Demo`) with grouping & pivot scenarios, including multi-level examples.
      - Add scenarios demonstrating flat vs hierarchical sources, asynchronous data, and custom aggregates.
      - Provide toggleable UI to showcase grouping band, reorder, and aggregate formatting.
      - Ensure sample highlights accessibility and theming variations (light/dark/high-contrast).
      - New `samples/FastTreeDataGrid.GroupingDemo` app showcases multi-level grouping presets, chip interactions, and aggregate summaries for sales data.
    - [x] (12.3) Announce feature roadmap and migration notes in README/changelog once implementation stabilizes.
      - README now highlights the grouping band feature, quick-start guidance, and migration notes for custom templates.
      - New `CHANGELOG.md` captures the grouping rollout, migration guidance, and associated test coverage.

## Dependencies & Risks

- Grouping must coexist with existing selection, editing, and virtualization code paths—regressions here are high risk.
- Aggregation performance on large datasets will require careful caching; profiling early is crucial.
- Accessibility and theming requirements need regular audits to avoid late-stage rework.
