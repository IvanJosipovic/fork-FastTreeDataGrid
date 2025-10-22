# FastTreeDataGrid Virtualization Providers

FastTreeDataGrid exposes a lightweight virtualization contract (`IFastTreeDataVirtualizationProvider`) so
applications can plug in different paging engines—ModelFlow, custom REST backends, in-memory sources—without
changing the control. This document outlines recommended integration patterns.

## Registration

Providers are discovered through `FastTreeDataGridVirtualizationProviderRegistry`. The control registers a
default factory that handles `IFastTreeDataVirtualizationProvider` instances directly or wraps any
`IFastTreeDataGridSource` using `FastTreeDataGridSourceVirtualizationProvider`.

Applications can register additional factories at startup:

```csharp
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;

// Typically during app startup/DI container configuration
IDisposable registration = FastTreeDataGridVirtualizationProviderRegistry.Register(
    (source, settings) =>
    {
        if (source is MyModelFlowDataSource modelFlow)
        {
            return new MyModelFlowProvider(modelFlow, settings);
        }

        return null; // allow next factory to try
    });

// Dispose registration when the application shuts down if necessary.
```

Factories are evaluated last-in-first-out, so more specific registrations should be added after the defaults.

## Dependency Injection Example

When using a DI container, register the factory as part of your composition root:

```csharp
services.AddSingleton(provider =>
{
    return FastTreeDataGridVirtualizationProviderRegistry.Register((source, settings) =>
    {
        var factory = provider.GetRequiredService<IMyProviderFactory>();
        return factory.TryCreateProvider(source, settings);
    });
});
```

`IMyProviderFactory` can resolve other services (e.g., HTTP clients, caching policies) and decide whether it
can handle the provided source object.

## Adapter Templates

### ModelFlow Adapter

```csharp
public sealed class MyModelFlowProvider : FastTreeDataGridModelFlowAdapterBase<MyViewModel>
{
    private readonly MyModelFlowDataSource _dataSource;

    public MyModelFlowProvider(MyModelFlowDataSource dataSource, FastTreeDataGridVirtualizationSettings settings)
        : base(viewModel => CreateRow(viewModel))
    {
        _dataSource = dataSource;
    }

    protected override Task EnsureInitializedCoreAsync(CancellationToken cancellationToken) =>
        _dataSource.EnsureInitializedAsync(cancellationToken);

    protected override Task<int> GetCountCoreAsync(CancellationToken cancellationToken) =>
        _dataSource.GetCountAsync(cancellationToken);

    protected override Task<ViewModelPage> GetPageCoreAsync(int startIndex, int count, CancellationToken cancellationToken)
    {
        return _dataSource.FetchPageAsync(startIndex, count, cancellationToken);
    }

    protected override Task ApplySortFilterCoreAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken) =>
        _dataSource.ApplySortFilterAsync(request, cancellationToken);

    protected override Task<int> LocateRowIndexCoreAsync(object? item, CancellationToken cancellationToken) =>
        _dataSource.LocateViewModelAsync(item, cancellationToken);

    private static FastTreeDataGridRow CreateRow(MyViewModel? viewModel)
    {
        return new FastTreeDataGridRow(viewModel, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
    }
}
```

The adapter base handles grid integration (events, placeholders, selection). Derived classes focus on
translating between external engines and the provider contract.

### REST/HTTP Adapter

```csharp
public sealed class RestVirtualizationProvider : IFastTreeDataVirtualizationProvider
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly FastTreeDataGridVirtualizationSettings _settings;
    private readonly List<FastTreeDataGridRow> _cache = new();

    public RestVirtualizationProvider(HttpClient httpClient, Uri endpoint, FastTreeDataGridVirtualizationSettings settings)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _settings = settings;
    }

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;
    public event EventHandler<FastTreeDataGridCountChangedEventArgs>? CountChanged;

    public bool IsInitialized { get; private set; }
    public bool SupportsMutations => false;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        await GetRowCountAsync(cancellationToken).ConfigureAwait(false);
        IsInitialized = true;
    }

    public async ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<int>(new Uri(_endpoint, "count"), cancellationToken).ConfigureAwait(false);
        CountChanged?.Invoke(this, new FastTreeDataGridCountChangedEventArgs(response));
        return response;
    }

    public async ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        var uri = new Uri(_endpoint, $"page?offset={request.StartIndex}&count={request.Count}");
        var rows = await _httpClient.GetFromJsonAsync<BenchmarkRow[]>(uri, cancellationToken).ConfigureAwait(false) ?? Array.Empty<BenchmarkRow>();

        var list = new List<FastTreeDataGridRow>(rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var gridRow = new FastTreeDataGridRow(row, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
            list.Add(gridRow);
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(request.StartIndex + i, gridRow));
        }

        return new FastTreeDataGridPageResult(list, Array.Empty<int>(), completion: null, cancellation: null);
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row) => _cache.Count > index ? (row = _cache[index], true) : (row = default!, false);
    public bool IsPlaceholder(int index) => false;
    public Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<int> LocateRowIndexAsync(object? item, CancellationToken cancellationToken) => Task.FromResult(-1);
    public Task CreateAsync(object viewModel, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());
    public Task UpdateAsync(object viewModel, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());
    public Task DeleteAsync(object viewModel, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Virtualization Settings

`FastTreeDataGrid.VirtualizationSettings` controls page size, prefetch radius, cache limits, concurrency,
reset throttle delay, and dispatcher priority. Configure it when creating the control or via styles:

```csharp
var grid = new FastTreeDataGrid
{
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 256,
        PrefetchRadius = 4,
        MaxPages = 80,
        MaxConcurrentLoads = 6,
        ResetThrottleDelayMilliseconds = 100,
        DispatcherPriority = DispatcherPriority.Input,
    },
};

grid.ItemsSource = myModelFlowDataSource;
```

## Diagnostics & Metrics

`FastTreeDataGridVirtualizationDiagnostics` publishes metrics via `System.Diagnostics.Metrics`:

- Counter `fasttree_datagrid_page_requests`
- Histogram `fasttree_datagrid_page_fetch_duration_ms`
- UpDownCounter `fasttree_datagrid_inflight_requests`

Hook into them using `MeterListener` or OpenTelemetry exporters:

```csharp
var listener = new MeterListener();
listener.InstrumentPublished = (instrument, l) =>
{
    if (instrument.Meter == FastTreeDataGridVirtualizationDiagnostics.Meter)
    {
        l.EnableMeasurementEvents(instrument);
    }
};
listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
{
    // export histogram values
});
listener.Start();
```

You can also capture log entries by providing a callback:

```csharp
FastTreeDataGridVirtualizationDiagnostics.LogCallback = entry =>
{
    logger.LogInformation("{Category}: {Message}", entry.Category, entry.Message);
};
```

## Summary

- Register providers using the registry to keep FastTreeDataGrid dependency-free.
- Use DI-friendly factory delegates to resolve engine-specific services.
- Derive from `FastTreeDataGridModelFlowAdapterBase<T>` for minimal boilerplate when targeting ModelFlow-like engines.
- Tune `VirtualizationSettings` per control for cache/prefetch/dispatch policies.
