# FastTreeDataGrid Data Virtualization Integration Plan

_Note: After completing each implementation step, mark the associated milestone/task as done._

## Capability Matrix — ModelFlow vs FastTreeDataGrid

| Capability | ModelFlow Components | FastTreeDataGrid Current State | Integration Gap / Notes |
| --- | --- | --- | --- |
| Data binding & notifications | `VirtualizingObservableCollection<DataItem<T>>` with `INotifyCollectionChanged`/`INotifyPropertyChanged` propagation | `IFastTreeDataGridSource` exposes synchronous `RowCount`, `GetRow`, and coarse `ResetRequested` event | Need observable bridge that relays per-page changes and fine-grained notifications |
| Placeholder & loading state | `DataItem<T>.IsLoading`, `GetPlaceHolder`, placeholder swapping in `PaginationManager` | `FastTreeDataGridRow` wraps concrete item; UI assumes materialized data | Introduce placeholder-aware rows/cells and propagate loading flags to presenters |
| Count retrieval & initialization | `PaginationManager.GetCountAsync`, `DataSource.EnsureInitialisedAsync` | Synchronous `RowCount` lookup; grid blocks until data available | Extend source contract with async count handshake to avoid UI thread stalls |
| Page request orchestration | `PaginationManager` driving `IPagedSourceProvider{Async}` fetches per page | Sources flatten entire tree in-memory (`FastTreeDataGridFlatSource`, `DynamicSource`) | Build virtualization provider adapter so viewport requests trigger targeted page loads |
| Cache & eviction policies | `PageReclaimOnTouched`, `MaxPages`, `PageDelta` tracking, reclaim actions | No data-level cache; all nodes retained in lists | Surface cache knobs and honor eviction callbacks from ModelFlow |
| Prefetch & background actions | `VirtualizationManager`, `IVirtualizationAction` scheduling for prefetch/reclaim/reset | No background scheduling abstraction; relies on Avalonia dispatcher for UI only | Integrate action queue with grid viewport events to request ahead/behind pages |
| Filtering & sorting | `SortDescriptionList`, `SetFilterQuery`, deferred IQueryable execution | `FastTreeDataGridFlatSource.Sort/SetFilter` operate eagerly on in-memory lists | Route grid sort/filter commands through ModelFlow queries to keep work server-side |
| Mutation & delta tracking | `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `PageDelta` reconciliation | Streaming source applies updates by mutating local list snapshots | Provide mutation pipeline that forwards edits to ModelFlow datasources and reacts to deltas |
| Threading & dispatch | `VirtualizationManager` marshals callbacks via configurable `UiThreadExecuteAction` | Grid assumes Avalonia UI thread access and synchronous data requests | Establish dispatcher bridge and ensure async completions return to UI thread safely |
| Viewport scheduling | Expects host to request indexes; supports async access per item | Row layouts compute visible indices but immediately call `GetRow` synchronously | Add viewport scheduler that batches index requests and awaits page materialization |

## Lifecycle Touchpoints for Hosting `VirtualizingObservableCollection<T>`

- **Initialization / Attachment**
  - Configure ModelFlow `VirtualizationManager.UiThreadExecuteAction` and register grid-specific dispatcher before instantiating the datasource.
  - Bind the grid to the virtualization provider, triggering `PaginationManager.GetCountAsync` asynchronously; await initial count before enabling selection/interaction.
  - Subscribe to ModelFlow collection change notifications and cache configuration events when the grid control attaches to the visual tree.
- **Page Fetch & Viewport Synchronization**
  - On viewport change, translate the visible index window into `GetAtAsync`/page requests via the provider adapter.
  - Emit placeholder rows immediately, updating row layout and presenters to reflect loading state while `PaginationManager` resolves pages off-thread.
  - Handle completion callbacks on the UI dispatcher, updating row visuals and recalculating selection offsets if deltas were applied.
- **Reset / Invalidation**
  - Respond to ModelFlow reset events (`IAsyncResetProvider`, `FilterQueryCleared`) by suspending viewport updates, clearing placeholder caches, and replaying the initialization handshake.
  - Support throttled resets to avoid cascading work during rapid filter or sort changes; ensure selection/expansion states are reconciled using `PageDelta` data.
- **Disposal / Detach**
  - Unsubscribe from `VirtualizationManager` actions, collection change events, and count notifications when the grid is unloaded or source replaced.
  - Cancel outstanding page requests, dispose per-page `CancellationTokenSource` instances, and clear placeholder rows to prevent retaining stale references.
  - Optionally flush telemetry counters and release provider adapters so subsequent attachments can reinitialize cleanly.

## Performance & UX Success Criteria

- **Latency Budget**
  - Initial viewport population displays placeholders within 16 ms of a scroll or load event; materialized data should replace placeholders within 150 ms for cached pages and 500 ms for cold fetches over asynchronous providers.
  - Sorting/filtering re-queries must surface updated placeholders within 50 ms and complete visible row hydration within 750 ms under typical enterprise datasets (1M rows) when backed by ModelFlow paging.
- **Placeholder Experience**
  - Placeholder rows render distinct skeleton states without blocking selection or scroll; they must transition to real data without flicker and preserve row height/column alignment.
  - Placeholder density adapts to viewport size, ensuring no blank gaps appear even when page fetches overlap with rapid scrolling.
  - Row presenters tint loading rows with a neutral overlay while widgets skip value binding, relying on provider callbacks to redraw once materialized.
- **Row Layout Responsiveness**
  - Row layouts invalidate cached measurements when placeholder rows appear so materialized content triggers fresh height calculations on arrival.
- **State Persistence**
  - Selection restoration uses provider index lookups after throttled resets so focus/selection survive cache churn; tree expansion remains managed by the data source.
- **Selection & State Persistence**
  - Selection anchors persist across page reclamation and delta replay; selecting an item must never revert to `-1` solely due to placeholder swap.
  - Expansion, sort indicators, and column widths remain stable during cache churn; background updates from ModelFlow cannot cause focus loss or unintended scroll jumps.
- **Throughput & Resource Constraints**
  - Maintain steady 60 FPS scrolling with virtualization enabled on mid-tier hardware (e.g., 4-core CPU, integrated GPU) with 200 visible rows.
  - Memory usage stays within configurable cache limits; page reclaim actions should prevent total working set from exceeding 1.5× the size of the configured page cache.
- **Observability & Resilience**
  - Emit telemetry for fetch duration, cache hit ratio, and reset frequency; thresholds trigger warnings (e.g., >10% cache misses in steady-state viewport).
  - Any backend fault (exceptions during page load) surfaces a recoverable placeholder state with actionable error indicators rather than crashing the grid.

## `IFastTreeDataGridSource` Extension Specification

- **Asynchronous Surface**
  - `ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)` mirrors ModelFlow `GetCountAsync` so initial counts avoid blocking the UI thread.
  - `ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)` pulls contiguous row segments and returns placeholder-aware results tied to ModelFlow page fetches.
  - `ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)` allows predictive preloading when the viewport scheduler anticipates upcoming ranges.
- **Placeholder Awareness**
  - `bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)` reports whether a row is currently realized without forcing a fetch.
  - `bool IsPlaceholder(int index)` and `event EventHandler<FastTreeDataGridRowMaterializedEventArgs> RowMaterialized` keep presenters aligned with ModelFlow `DataItem.IsLoading` transitions.
  - `bool SupportsPlaceholders { get; }` enables graceful fallback for legacy sources that materialize data eagerly.
- **Invalidation & Refresh Hooks**
  - `event EventHandler<FastTreeDataGridInvalidatedEventArgs> Invalidated` communicates full or range-based cache busts triggered by filtering, sorting, or external mutations.
  - `Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)` provides the grid a controlled way to request datasource refreshes (e.g., after user edits).
- **Supporting Types**
  - `FastTreeDataGridPageRequest` captures start index, count, optional prefetch radius, and priority hints so providers can feed ModelFlow `PaginationManager`.
  - `FastTreeDataGridPageResult` bundles returned rows, placeholder descriptors, and continuations for completion/cancellation.
  - `FastTreeDataGridInvalidationRequest` scopes invalidations (`Full`, `Range`, `MetadataOnly`) to minimize redundant work.
  - All async completions must marshal back via the dispatcher bridge registered during initialization to uphold thread affinity.

## Virtualization Provider Contract (`IFastTreeDataVirtualizationProvider`)

- **Purpose & Scope**
  - Acts as the bridge between grid viewport mechanics and underlying virtualization engines (ModelFlow or custom).
  - Encapsulates page orchestration, count synchronization, mutation propagation, and placeholder lifecycle.
- **Core Members**
  - `ValueTask InitializeAsync(CancellationToken cancellationToken)` wires provider state, ensures ModelFlow datasource is ready, and registers dispatcher hooks.
  - `ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)` and `ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)` delegate to the datasource while applying provider-level caching/prefetch decisions.
  - `ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)` schedules proactive fetches via `VirtualizationManager`.
  - `Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)` funnels grid-driven invalidations toward the datasource and manages any pending page operations.
- **Observable Surface**
  - `event EventHandler<FastTreeDataGridInvalidatedEventArgs> Invalidated` and `event EventHandler<FastTreeDataGridRowMaterializedEventArgs> RowMaterialized` bubble up datasource signals without exposing ModelFlow internals.
  - `event EventHandler<FastTreeDataGridCountChangedEventArgs>` (new) notifies the grid when total count shifts after async loads or CRUD operations.
- **Mutation Hooks**
  - Optional methods like `Task CreateAsync`, `Task UpdateAsync`, `Task DeleteAsync` pass through to ModelFlow `IEditableProvider` interfaces when available, returning updated indices/deltas.
  - Providers surface `bool SupportsMutations` so the grid can enable or disable editing affordances.
- **Threading & Disposal**
  - Providers must marshal completion callbacks to the registered dispatcher and support `DisposeAsync`/`Dispose` to tear down ModelFlow managers, cancel outstanding page requests, and detach event handlers.
  - Contract includes `bool IsInitialized { get; }` to allow the grid to guard interactions until setup completes.
- **Adapter Responsibilities**
  - Downstream projects can derive from `FastTreeDataGridModelFlowAdapterBase<TViewModel>` (or similar) to bridge their own virtualization engines; the base class handles FastTreeDataGrid contracts while concrete adapters wire up domain-specific logic without pulling external dependencies into this repo.
- **UI Integration**
  - `FastTreeDataGrid` hosts the provider, wiring `Invalidated`, `RowMaterialized`, and `CountChanged` callbacks into row layout resets and presenter refreshes so the view reacts immediately without polling.
  - `FastTreeDataGridPresenter` binds to the active provider and triggers incremental redraws when rows materialize or counts shift.
  - A viewport scheduler converts scroll offsets into batched page requests (with prefetch hints) so providers receive consistent, throttled demand without per-cell polling.
  - Placeholder rows render instantly while the scheduler fetches pages; provider callbacks replace them with materialized content on the UI thread for seamless transitions.
- **Cache Coordination**
  - `FastTreeDataGridVirtualizationSettings` lets hosts tune page size, retention, prefetch radius, concurrency, and dispatch priorities without relying on engine-specific APIs; the scheduler honors these settings and encourages providers to reclaim stale pages.
  - Throttled reset/refresh dispatchers ensure rapid viewport or filter changes coalesce into manageable batches, preventing providers from being overwhelmed.
- **Sorting & Filtering Bridge**
  - Grid sort commands emit provider-facing descriptors so engines implementing `ApplySortFilterAsync` can reorder data without FastTreeDataGrid knowing about specific query APIs; visual sort indicators stay in sync with provider responses.
- **Dependency Isolation**
  - No reflection helpers are provided; instead, `FastTreeDataGridModelFlowAdapterBase<TViewModel>` supplies a strongly-typed, opt-in base class. External integrations (ModelFlow, custom engines) implement the abstract members to forward counts/pages/mutations while keeping FastTreeDataGrid free of third-party references.
- **Diagnostics**
  - `FastTreeDataGridVirtualizationDiagnostics` exposes .NET metrics (via `System.Diagnostics.Metrics`) and logging hooks so hosts can monitor page fetch latency, pending loads, and provider health.

## Milestone 1 — Virtualization Foundations
1. [x] Compare external virtualization engines (ModelFlow et al.) against FastTreeDataGrid data/viewport requirements and capture capability gaps.
2. [x] Document lifecycle touchpoints (initialization, page fetch, reset, disposal) required for hosting any async data virtualization source inside the grid.
3. [x] Define performance and UX success criteria (latency budget, placeholder behavior, selection persistence) that all provider integrations must satisfy.

## Milestone 2 — Abstractions & Contracts
4. [x] Extend `IFastTreeDataGridSource` to expose asynchronous count, page acquisition, placeholder state, and invalidation hooks needed by virtualization backends.
5. [x] Introduce a provider contract (`IFastTreeDataVirtualizationProvider`) that maps grid viewport requests to generalized paging semantics.
6. [x] Add events/callbacks for page materialization, count changes, and item mutation so the presenter can react without polling.
7. [x] Supply an adapter base class that downstream engines can inherit to bridge their paging logic into the provider contract without introducing new dependencies here.

## Milestone 3 — Viewport & Paging Pipeline
8. [x] Build a generic viewport scheduler that translates scroll offsets into page windows and batches requests to any virtualization provider.
9. [x] Ensure placeholder rows surface immediately while completed pages flow back via provider callbacks onto the UI thread.
10. [x] Surface cache coordination (page retention, reclaim policies) through provider configuration rather than engine-specific APIs.
11. [x] Implement throttled refresh/reset handling so rapid viewport or filter changes do not overwhelm providers.

## Milestone 4 — UI & Interaction Adaptation
12. [x] Introduce placeholder-aware row and cell presenters (loading skeletons, disabled interactions) driven by provider events.
13. [x] Ensure row layout algorithms (uniform/adaptive/variable height) honor deferred materialization and remeasure when data arrives.
14. [x] Bridge grid sorting/filtering commands to provider-facing abstractions, keeping visual indicators in sync without assuming a specific engine.
15. [x] Preserve selection, expansion, and focus across page reloads by relying on provider index lookups/delta notifications.

## Milestone 5 — Pluggability & Configuration
16. [x] Surface configuration objects for virtualization (page size, prefetch radius, cache limits, dispatching strategy) with sensible defaults.
17. [x] Allow registration/discovery of alternative providers so applications can plug in their own engines.
18. [x] Provide guidance and helper templates for wiring providers via dependency injection or factory patterns.

## Milestone 6 — Quality, Tooling & Documentation
19. [x] Add diagnostic instrumentation (logging hooks, perf counters, debug overlays) to monitor fetch latency and cache hit rates across providers.
20. [x] Create automated stress and scrolling benchmarks that validate throughput and correctness against large datasets regardless of engine.
21. [ ] Produce documentation demonstrating how to integrate different virtualization engines using the new adapter base.
22. [x] Update project documentation (README, samples, release notes) with virtualization integration guidance, best practices, and migration steps.
