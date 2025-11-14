using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Infrastructure;
using Microsoft.Data.Sqlite;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Virtualization;

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

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        if (request.Kind == FastTreeDataGridInvalidationKind.Full)
        {
            _rows.Clear();
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        var entry = GetOrCreateEntry(index);
        row = entry.Row;
        return entry.ValueProvider.IsMaterialized;
    }

    public bool IsPlaceholder(int index)
    {
        return !GetOrCreateEntry(index).ValueProvider.IsMaterialized;
    }

    public FastTreeDataGridRow GetRow(int index)
    {
        return GetOrCreateEntry(index).Row;
    }

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
        if (count <= 0 || start < 0 || start >= RowCount)
        {
            return;
        }

        await _seedTask.WaitAsync(cancellationToken).ConfigureAwait(false);

        var end = Math.Min(start + count, RowCount);
        var needsFetch = false;
        for (var index = start; index < end; index++)
        {
            if (!GetOrCreateEntry(index).ValueProvider.IsMaterialized)
            {
                needsFetch = true;
                break;
            }
        }

        if (!needsFetch)
        {
            return;
        }

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
        command.Parameters.AddWithValue("$count", end - start);
        command.Parameters.AddWithValue("$offset", start);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var indexCursor = start;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && indexCursor < end)
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

        public object? GetValue(object? item, string key)
        {
            return key switch
            {
                SqliteVirtualizationColumns.KeyId => _isMaterialized ? _id : _index,
                SqliteVirtualizationColumns.KeyName => _name,
                SqliteVirtualizationColumns.KeyScore => _isMaterialized ? _score : null,
                SqliteVirtualizationColumns.KeyStatus => _isMaterialized ? "Loaded" : "Loading...",
                _ => null,
            };
        }

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
