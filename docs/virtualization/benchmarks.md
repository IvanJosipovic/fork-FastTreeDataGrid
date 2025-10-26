# FastTreeDataGrid Benchmarks

Automated stress benchmarks live in `benchmarks/FastTreeDataGrid.Benchmarks`. The suite uses BenchmarkDotNet to validate paging throughput across large data sets and access patterns.

## Running Benchmarks

```bash
dotnet run --project benchmarks/FastTreeDataGrid.Benchmarks -c Release
```

Benchmarks exercise sequential paging and random access scenarios using `FastTreeDataGridSourceVirtualizationProvider`. Adjust the `RowCount` and `PageSize` parameters in `VirtualizationBenchmarks` to mimic your workloads or plug in alternative providers via `FastTreeDataGridVirtualizationProviderRegistry`.

BenchmarkDotNet emits reports to `benchmarks/FastTreeDataGrid.Benchmarks/bin/Release/net8.0/BenchmarkDotNet.Artifacts`. Inspect the `*.md` results for allocation and latency data.

The suite also includes `WidgetInteractionBenchmarks`, which toggles `ExpanderWidget`, scrolls `ScrollViewerWidget`, refreshes `MenuWidget`, and switches `TabControlWidget` tabs to track interaction allocations against the pooled widget infrastructure.

## Adding Custom Scenarios

1. Create a new benchmark class under `benchmarks/FastTreeDataGrid.Benchmarks`.
2. Reference your provider or data source.
3. Decorate benchmark methods with `[Benchmark]` and expose parameters with `[Params]` as needed.
4. Run the suite to verify performance.

For provider-specific benchmarks, register your custom provider factory before instantiating the grid or source so that `FastTreeDataGridSourceVirtualizationProvider` is replaced automatically.
