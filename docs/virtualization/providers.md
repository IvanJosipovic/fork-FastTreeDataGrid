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
using FastTreeDataGrid.Engine.Infrastructure;

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

### SQLite Background Seeder

The following sample seeds a local SQLite database with one million rows on a background thread during
application startup and virtualizes the data as the grid requests it. Rows that have not been materialized yet
are presented as `"Loading..."`, so the UI communicates progress while pages stream in.

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Infrastructure;
using Microsoft.Data.Sqlite;

public static class SqliteVirtualizationBootstrapper
{
    public const int TargetRowCount = 1_000_000;

    private static readonly ConcurrentDictionary<string, Task> s_seedTasks = new(StringComparer.Ordinal);

    public static string CreateConnectionString(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        };
        return builder.ToString();
    }

    public static Task EnsureSeededAsync(string connectionString, CancellationToken cancellationToken)
    {
        return s_seedTasks.GetOrAdd(connectionString, cs => Task.Run(() => SeedAsync(cs, cancellationToken), cancellationToken));
    }

    private static async Task SeedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (!string.IsNullOrEmpty(dataSource))
        {
            var directory = Path.GetDirectoryName(dataSource);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var create = connection.CreateCommand())
        {
            create.CommandText =
                """
                CREATE TABLE IF NOT EXISTS Benchmarks (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Score REAL NOT NULL
                );
                """;
            await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var index = connection.CreateCommand())
        {
            index.CommandText =
                """
                CREATE INDEX IF NOT EXISTS IX_Benchmarks_Name ON Benchmarks(Name);
                """;
            await index.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(*) FROM Benchmarks;";
            var existing = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (existing >= TargetRowCount)
            {
                return;
            }

            using var transaction = connection.BeginTransaction();
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO Benchmarks(Id, Name, Score) VALUES ($id, $name, $score);";
            var idParameter = insert.Parameters.Add("$id", SqliteType.Integer);
            var nameParameter = insert.Parameters.Add("$name", SqliteType.Text);
            var scoreParameter = insert.Parameters.Add("$score", SqliteType.Real);
            insert.Prepare();

            for (var i = existing; i < TargetRowCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                idParameter.Value = i;
                nameParameter.Value = $"Row #{i:000000}";
                scoreParameter.Value = Math.Sin(i) * 100;

                insert.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}

public sealed class SqliteVirtualizationSource : IFastTreeDataGridSource
{
    private readonly string _connectionString;
    private readonly Task _seedTask;
    private readonly ConcurrentDictionary<int, SqliteRowEntry> _rows = new();

    public SqliteVirtualizationSource(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _seedTask = SqliteVirtualizationBootstrapper.EnsureSeededAsync(connectionString, CancellationToken.None);
        RowCount = SqliteVirtualizationBootstrapper.TargetRowCount;
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount { get; }

    public bool SupportsPlaceholders => true;

    public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) =>
        new(RowCount);

    public async ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request.Count <= 0 || request.StartIndex >= RowCount)
        {
            return FastTreeDataGridPageResult.Empty;
        }

        var start = Math.Max(0, request.StartIndex);
        var count = Math.Min(request.Count, RowCount - start);
        var rows = new List<FastTreeDataGridRow>(count);
        var requiresFetch = false;

        for (var index = start; index < start + count; index++)
        {
            var entry = GetOrCreateEntry(index);
            rows.Add(entry.Row);
            requiresFetch |= !entry.ValueProvider.IsMaterialized;
        }

        if (requiresFetch)
        {
            await FetchRangeAsync(start, count, cancellationToken).ConfigureAwait(false);
        }

        return new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null);
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        // Optional: queue FetchRangeAsync on a background task to populate cache ahead of time.
        return ValueTask.CompletedTask;
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        var entry = GetOrCreateEntry(index);
        row = entry.Row;
        return entry.ValueProvider.IsMaterialized;
    }

    public bool IsPlaceholder(int index) => !GetOrCreateEntry(index).ValueProvider.IsMaterialized;

    public FastTreeDataGridRow GetRow(int index) => GetOrCreateEntry(index).Row;

    public void ToggleExpansion(int index)
    {
        _ = index;
    }

    private SqliteRowEntry GetOrCreateEntry(int index)
    {
        return _rows.GetOrAdd(index, static (i, _) =>
        {
            var provider = new SqliteRowValueProvider(i);
            var row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
            return new SqliteRowEntry(row, provider);
        }, (object?)null);
    }

    private async Task FetchRangeAsync(int start, int count, CancellationToken cancellationToken)
    {
        await _seedTask.WaitAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Name, Score
            FROM Benchmarks
            ORDER BY Id
            LIMIT $count OFFSET $offset;
            """;
        command.Parameters.AddWithValue("$count", count);
        command.Parameters.AddWithValue("$offset", start);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var indexCursor = start;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var entry = GetOrCreateEntry(indexCursor);
            var wasMaterialized = entry.ValueProvider.IsMaterialized;

            entry.ValueProvider.Update(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetDouble(2));

            if (!wasMaterialized)
            {
                RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(indexCursor, entry.Row));
            }

            indexCursor++;
        }
    }

    private sealed record SqliteRowEntry(FastTreeDataGridRow Row, SqliteRowValueProvider ValueProvider);

    private sealed class SqliteRowValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly int _index;
        private int _id;
        private string _name = "Loading...";
        private double? _score;
        private bool _isMaterialized;

        public SqliteRowValueProvider(int index)
        {
            _index = index;
        }

        public bool IsMaterialized => _isMaterialized;

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

        public object? GetValue(object? item, string key) =>
            key switch
            {
                SqliteVirtualizationColumns.KeyId => _isMaterialized ? _id : _index,
                SqliteVirtualizationColumns.KeyName => _name,
                SqliteVirtualizationColumns.KeyScore => _isMaterialized ? _score : null,
                SqliteVirtualizationColumns.KeyStatus => _isMaterialized ? "Loaded" : "Loading...",
                _ => null,
            };

        public void Update(int id, string name, double score)
        {
            _id = id;
            _name = name;
            _score = score;
            _isMaterialized = true;

            Notify(SqliteVirtualizationColumns.KeyId);
            Notify(SqliteVirtualizationColumns.KeyName);
            Notify(SqliteVirtualizationColumns.KeyScore);
            Notify(SqliteVirtualizationColumns.KeyStatus);
        }

        private void Notify(string key)
        {
            ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
        }
    }
}

public static class SqliteVirtualizationColumns
{
    public const string KeyId = "Id";
    public const string KeyName = "Name";
    public const string KeyScore = "Score";
    public const string KeyStatus = "Status";
}
```

Create the source, optionally kick off seeding at startup, and assign it to the grid. The default registry wraps
`IFastTreeDataGridSource` instances in `FastTreeDataGridSourceVirtualizationProvider`, so no extra wiring is needed:

```csharp
var databasePath = Path.Combine(appDataDirectory, "virtualization.db");
var connectionString = SqliteVirtualizationBootstrapper.CreateConnectionString(databasePath);

var sqliteSource = new SqliteVirtualizationSource(connectionString);
_ = SqliteVirtualizationBootstrapper.EnsureSeededAsync(connectionString, CancellationToken.None);

var grid = new FastTreeDataGrid
{
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 512,
        PrefetchRadius = 3,
    },
    ItemsSource = sqliteSource,
};
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
