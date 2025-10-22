# Virtualization Migration Guide

This guide helps projects upgrade from earlier FastTreeDataGrid versions (pre-provider architecture) to the new virtualization stack introduced with `FastTreeDataGridVirtualizationProviderRegistry` and `FastTreeDataGridVirtualizationSettings`.

## 1. Remove Legacy Source Hooks

Previous versions required direct references to `FastTreeDataGridAsyncSource` or custom `IFastTreeDataGridSource` implementations to handle paging manually. Replace manual page fetching with a dedicated virtualization provider:

```diff
- grid.ItemsSource = new LegacyPagedSource(myService);
+ grid.ItemsSource = myModelFlowDataSource;
+ FastTreeDataGridVirtualizationProviderRegistry.Register((source, settings) =>
+     source is MyModelFlowDataSource modelFlow ? new MyModelFlowProvider(modelFlow, settings) : null);
```

## 2. Configure Virtualization Settings

Set `FastTreeDataGrid.VirtualizationSettings` to control page size, prefetch radius, and throttling. For identical behavior to the legacy implementation, mirror previous batch sizes:

```csharp
grid.VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
{
    PageSize = 200,
    PrefetchRadius = 2,
    MaxPages = 40,
    ResetThrottleDelayMilliseconds = 120,
};
```

## 3. Update Sort/Filter Integration

If the previous solution called service APIs directly from column sort handlers, move that logic into `IFastTreeDataVirtualizationProvider.ApplySortFilterAsync` so pagination stays centralized. Columns can supply row comparisons via `FastTreeDataGridColumn.SortComparison` when server-side sorting is not available.

## 4. Restore Selection After Resets

Virtualization providers should implement `LocateRowIndexAsync` (or derive from `FastTreeDataGridModelFlowAdapterBase<T>`) so the grid can restore selection when caches refresh. If the previous solution persisted indices manually, migrate that logic into the provider.

## 5. Enable Metrics & Logging

The new stack emits metrics via `FastTreeDataGridVirtualizationDiagnostics`. Configure a `MeterListener` or OpenTelemetry exporter to monitor latency and cache health. This replaces ad-hoc stopwatch logging.

## 6. Validate with Benchmarks

Run `dotnet run --project benchmarks/FastTreeDataGrid.Benchmarks -c Release` to stress test the new provider. Compare throughput against legacy metrics to ensure the migration meets expectations.

## 7. Optional: Dispose Registrations

Factories registered with `FastTreeDataGridVirtualizationProviderRegistry` return an `IDisposable`. Dispose them when shutting down the host if providers should be unregistered dynamically.

Following these steps ensures your application benefits from consistent virtualization behavior, centralized instrumentation, and simplified provider integration.
