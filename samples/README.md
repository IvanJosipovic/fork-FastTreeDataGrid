# FastTreeDataGrid Samples

The demo application (`samples/FastTreeDataGrid.Demo`) highlights several scenarios:

- **Virtualization Diagnostics** – demonstrates registering a custom provider via `FastTreeDataGridVirtualizationProviderRegistry`, tuning `VirtualizationSettings`, and inspecting metrics emitted by `FastTreeDataGridVirtualizationDiagnostics`. Includes both a 1B-row pseudo-random provider and a REST-backed Hacker News provider.
- **Flat Tree Browser** – showcases fast filtering, sorting, and expansion with `FastTreeDataGridFlatSource<T>`.
- **Streaming Dashboard** – uses `FastTreeDataGridStreamingSource<T>` to apply live data updates.
- **Hybrid Data Feed** – combines an initial snapshot with incremental updates.
- **Widget Gallery** – displays immediate-mode widgets (text, icons, badges, sliders) rendered without Avalonia controls.

## Running the Demo

```bash
dotnet run --project samples/FastTreeDataGrid.Demo
```

Switch between tabs to explore each scenario. The virtualization tab includes on-screen controls for page size, prefetch radius, and metric logging hooks. Refer to the [virtualization provider guide](../docs/virtualization/providers.md) for integration details.
