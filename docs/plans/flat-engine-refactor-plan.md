## Engine Observations
- `FastTreeDataGridFlatSource<T>` flattens hierarchies, applies sort/filter/group descriptors, manages aggregate caching, and drives expansion state through `ScheduleWork`/`SemaphoreSlim` concurrency (`src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:13`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:581`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:944`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:1357`).
- Wrapper sources (`FastTreeDataGridDynamicSource<T>`, `FastTreeDataGridStreamingSource<T>`, `FastTreeDataGridAsyncSource<T>`, `FastTreeDataGridHybridSource<T>`) rely on the flat engine for paging, mutation snapshotting, and stream updates (`src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridDynamicSource.cs:8`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridStreamingSource.cs:8`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridAsyncSource.cs:8`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridHybridSource.cs:8`).
- Virtualization support (`FastTreeDataGridSourceVirtualizationProvider`, `FastTreeDataGridViewportScheduler`, page request/result/diagnostics types) bridges the engine to UI consumers but is otherwise UI-agnostic (`src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridSourceVirtualizationProvider.cs:8`, `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridViewportScheduler.cs:8`, `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridPageResult.cs:8`).
- Shared contracts (row models, grouping descriptors, aggregation contexts, generated summary rows) live under the same namespace despite being reusable engine primitives (`src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRow.cs:6`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridDataOperations.cs:10`, `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupContext.cs:8`, `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridGeneratedRows.cs:1`).

## Separation Strategy
- Carve out a new library (e.g., `FastTreeDataGrid.Engine`) that targets netstandard/net8 and owns all platform-neutral infrastructure now under `FastTreeDataGrid.Core` namespaces.
- Move data-source, descriptor, grouping, virtualization, and row model code into the new project; keep `FastTreeDataGridSourceFactory`, `FastTreeDataGridItemsAdapter`, and other Avalonia-specific helpers inside the existing Avalonia-facing project (`src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridSourceFactory.cs:1`, `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridItemsAdapter.cs:1`).
- Introduce a lightweight dispatcher abstraction so engine code no longer references `Dispatcher.UIThread` or `DispatcherPriority`; Avalonia adapters can wrap the UI thread while non-UI consumers can default to synchronous invocation (`src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:658`, `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridVirtualizationSettings.cs:46`).
- Preserve existing public APIs by re-exporting or forwarding types from the Avalonia project if needed, but update namespaces to reflect engine ownership (e.g., `FastTreeDataGrid.Engine.*` instead of `FastTreeDataGrid.Control.Infrastructure`).

## Implementation Steps
- **Step 1**: Inventory every file under `src/FastTreeDataGrid.Core` and classify as engine vs UI-dependent; document borderline cases like `FastTreeDataGridVirtualizationSettings` and `FastTreeDataGridRowReorderSettings` which currently carry Avalonia types (`src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRowReorderSettings.cs:1`).
- **Step 2**: Create `FastTreeDataGrid.Engine.csproj`, copy engine-classified files, adjust namespaces/usings, and add a dispatcher interface plus default implementation used by `FastTreeDataGridFlatSource` and the virtualization scheduler.
- **Step 3**: Move the remaining Avalonia-only helpers into `FastTreeDataGrid.Control` (completed).
- **Step 4**: Update `FastTreeDataGrid.Control` (and widgets/samples) to reference the new project and fix `using` statements, ensuring generated rows and descriptors are consumed from the engine namespace (`src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs:324`).
- **Step 5**: Add dedicated engine-level unit tests (flattening/grouping/aggregates) and wire them into the solution’s test run (completed).
- **Step 6**: Update documentation and samples to reflect the engine package and its new entry points, and plan versioning/migration guidance for downstream users referencing `FastTreeDataGrid.Control.Infrastructure`.

## Step 1 – File Inventory
Generated build artifacts under `bin/` and `obj/` are excluded from the lists below.

### Engine-ready (no Avalonia dependencies)
**Infrastructure/Descriptors**
- `src/FastTreeDataGrid.Core/Infrastructure/Descriptors/FastTreeDataGridGroupingRequest.cs` — DTO for grouping instructions.
- `src/FastTreeDataGrid.Core/Infrastructure/Descriptors/FastTreeDataGridSortFilterRequest.cs` — DTOs for sort/filter/group descriptors.
- `src/FastTreeDataGrid.Core/Infrastructure/Descriptors/IFastTreeDataGridSortFilterHandler.cs` — Contract for sort/filter engines.
- `src/FastTreeDataGrid.Core/Infrastructure/Descriptors/IFastTreeDataGridGroupingHandler.cs` — Contract bridging grouping requests.

**Infrastructure/Rows**
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRowReorderResult.cs` — Result DTO for reorder operations.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridValueProvider.cs` — Value provider contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridSelectionMode.cs` — Enum for selection configuration.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridSummaryRow.cs` — Summary row marker interface.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRowMaterializedEventArgs.cs` — Event payload for row materialization.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRowReorderEventArgs.cs` — Event payloads for reorder lifecycle.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/ValueInvalidatedEventArgs.cs` — Value invalidation notification.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridTypeSearchEventArgs.cs` — Type-to-search event args.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridGeneratedRows.cs` — Generated group/summary row implementations.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridGroupingController.cs` — Expansion control contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRowReorderRequest.cs` — Reorder request DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridValidationState.cs` — Validation result structs.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridRowReorderHandler.cs` — Reorder handler contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridGroup.cs` — Group marker interface.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRow.cs` — Engine row wrapper with value provider wiring.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridGroupPathProvider.cs` — Group path provider contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridRowHeightAware.cs` — Variable-height provider contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridPinnedPosition.cs` — Enum for pinned state.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridVariableRowHeightProvider.cs` — Variable-height adapter.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/IFastTreeDataGridGroupMetadata.cs` — Group metadata contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridSelectionChangedEventArgs.cs` — Selection-change payload.

**Infrastructure/DataSources**
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridHybridSource.cs` — Hybrid streaming/async source wrapper.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridAsyncSource.cs` — Async loader wrapper.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridDynamicSource.cs` — Base class wrapping `FastTreeDataGridFlatSource<T>`.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/IFastTreeDataGridSource.cs` — Core source contract.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridStreamUpdate.cs` — Streaming update helpers.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridDataOperations.cs` — Sort/filter/group/aggregate descriptor definitions.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridStreamingSource.cs` — Mutable streaming source wrapper.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridModelFlowAdapterBase.cs` — Adapter base for external virtualization engines.

**Infrastructure/Virtualization**
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridViewportRequest.cs` — Immutable viewport request record.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridViewportScheduler.cs` — Scheduler for paging requests.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridFuncVariableRowHeightProvider.cs` — Delegate-based height provider.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridPageRequest.cs` — Page request DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataVirtualizationPage.cs` — Page metadata for adapters.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/IFastTreeDataVirtualizationProvider.cs` — Virtualization provider contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridSourceVirtualizationProvider.cs` — Adapter from source to virtualization provider.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridThrottleDispatcher.cs` — Throttling helper (no UI tie).
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridInvalidationKind.cs` — Invalidation enum.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridVirtualizationProviderRegistry.cs` — Provider discovery registry.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridPageResult.cs` — Page result DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridVirtualizationDiagnostics.cs` — Metrics/diagnostics helpers.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridPagePriority.cs` — Page priority enum.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridDefaultVariableRowHeightProvider.cs` — Default height provider.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridLoadingStateEventArgs.cs` — Loading state event args.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridCountChangedEventArgs.cs` — Count change payload.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridInvalidationRequest.cs` — Invalidation request DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridGroupingSourceAdapter.cs` — Adapter from sort/filter to grouping.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridInvalidatedEventArgs.cs` — Invalidation event args.

**Models/Columns**
- `src/FastTreeDataGrid.Core/Models/Columns/FastTreeDataGridSortDirection.cs` — Sort direction enum.
- `src/FastTreeDataGrid.Core/Models/Columns/ColumnSizingMode.cs` — Column sizing enum.

**Infrastructure/Grouping**
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/IFastTreeDataGridGroupAdapter.cs` — Group adapter contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupingSnapshot.cs` — Snapshot of grouping state.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupContext.cs` — Aggregate context DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridValueGroupAdapter.cs` — Default value-based group adapter.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupingLayout.cs` — Serializable grouping layout.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/IFastTreeDataGridAggregateProvider.cs` — Aggregate provider contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridAggregateResult.cs` — Aggregate result DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/IFastTreeDataGridGroupStateProvider.cs` — Group state storage contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupingStateChangedEventArgs.cs` — Grouping change event args.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/IFastTreeDataGridGroupingNotificationSink.cs` — Grouping notification sink contract.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupingStateStore.cs` — Thread-safe grouping state store.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupState.cs` — Group state DTO.
- `src/FastTreeDataGrid.Core/Infrastructure/Grouping/FastTreeDataGridGroupingChangeKind.cs` — Change-kind enum.

### Borderline (engine logic with Avalonia touch points to replace)
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs` — Core flattening engine but wired to `Dispatcher.UIThread` and `DispatcherPriority`.
- `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridVirtualizationSettings.cs` — Pure settings object except for `DispatcherPriority`.
- `src/FastTreeDataGrid.Core/Infrastructure/Rows/FastTreeDataGridRowReorderSettings.cs` — Mostly engine configuration but depends on `Avalonia.Media` brushes.

### Avalonia-dependent (UI-specific; stays with Avalonia project)
- `src/FastTreeDataGrid.Core/FastTreeDataGrid.Core.csproj` — References the Avalonia package; defines the current UI-oriented build.
- `src/FastTreeDataGrid.Core/AssemblyInfo.cs` — Registers Avalonia XML namespaces.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridSourceFactory.cs` — Builds sources from Avalonia `ITreeDataTemplate`/bindings.
- `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridItemsAdapter.cs` — Avalonia `AvaloniaObject` adapter that materializes sources.

## Step 2 – Engine Project Setup
- Created `src/FastTreeDataGrid.Engine/FastTreeDataGrid.Engine.csproj` targeting `net8.0` and added it to both solutions (`FastTreeDataGrid.sln`, `FastTreeDataGrid.Benchmarks.sln`). Updated `FastTreeDataGrid.Core.csproj` to reference the new engine library.
- Relocated all engine-ready files into the new project, rewrote namespaces/usings from `FastTreeDataGrid.Control.*` to `FastTreeDataGrid.Engine.*`, and updated every consumer (control layer, widgets, samples, benchmarks, tests, analyzers) to import the new namespaces.
- Added dispatcher abstraction to the engine (`IFastTreeDataGridDispatcher`, `FastTreeDataGridDispatchPriority`, `FastTreeDataGridDispatcherProvider`, `FastTreeDataGridSynchronousDispatcher`) and refactored `FastTreeDataGridFlatSource` plus other engine components to use it instead of Avalonia’s dispatcher APIs.
- Moved `FastTreeDataGridVirtualizationSettings` and the flattening engine into the new project, replacing `DispatcherPriority` with the engine priority enum.
- Introduced the bridge interface `IFastTreeDataGridRowReorderSettings` so the engine can read reorder options without referencing Avalonia-specific brushes; `FastTreeDataGridRowReorderSettings` now implements this interface in the control project.
- Added an Avalonia-side dispatcher bootstrapper (`FastTreeDataGridAvaloniaDispatcher`) that plugs the UI thread into the engine via a module initializer and exposes a helper to translate engine dispatch priorities to Avalonia’s `DispatcherPriority`.
- Ensured all samples, tests, and tooling projects continue to compile by adding the necessary `using FastTreeDataGrid.Engine.Infrastructure;` / `FastTreeDataGrid.Control.Infrastructure;` imports where mixed engine/control types are used.

## Step 3 – Avalonia Helper Relocation
- Moved Avalonia-only helpers (`FastTreeDataGridSourceFactory`, `FastTreeDataGridItemsAdapter`, `FastTreeDataGridRowReorderSettings`) into the control project under `Infrastructure/DataSources` and `Infrastructure/Rows`, keeping their namespaces but eliminating Avalonia dependencies from the engine.
- Retired the `FastTreeDataGrid.Core` project entirely—its XML namespace registrations were rehomed into `FastTreeDataGrid.Control/AssemblyInfo.cs`.
- Confirmed the relocated helpers depend on the engine abstractions (`IFastTreeDataGridSource`, dispatcher provider, `IFastTreeDataGridRowReorderSettings`) and that downstream projects build against the engine/control pair after updating project references and XAML `clr-namespace` declarations.
- Ran `dotnet build FastTreeDataGrid.sln` to ensure controls, samples, benchmarks, analyzers, and tests compile cleanly (existing design-time warnings remain).

## Step 5 – Engine Test Coverage
- Added `tests/FastTreeDataGrid.Engine.Tests` with xUnit coverage for hierarchy flattening (expansion/collapse) and grouping aggregates (group rows, group footers, grid footer).
- Registered the test project in `FastTreeDataGrid.sln` and ensured it references `FastTreeDataGrid.Engine`.
- Updated CI/local guidance to run `dotnet test FastTreeDataGrid.sln`, which now executes both engine and control test suites.

## Step 6 – Documentation & Migration Guidance
- Updated docs (`docs/virtualization/providers.md`, `docs/virtualization/metrics.md`) to reference `FastTreeDataGrid.Engine.Infrastructure` and highlight the new engine entry points.
- Expanded `README.md` with a packages overview and migration checklist covering package reference changes, namespace updates, and XAML adjustments now that `FastTreeDataGrid.Core` has been removed.
- Ensured samples already reflect the engine split (control/engine) and the solution + tests build successfully after the documentation changes.

## Next Decisions
1. Decide the target framework and package layout for the engine (standalone NuGet or internal project).
2. Design the dispatcher abstraction API so both Avalonia and headless consumers can plug in without breaking existing behaviour.
