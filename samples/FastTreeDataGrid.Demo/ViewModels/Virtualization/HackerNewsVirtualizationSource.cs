using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Virtualization;

#pragma warning disable CS0067
public sealed class HackerNewsVirtualizationSource : IFastTreeDataGridSource
{
    private const int MaxStories = 1024;
    private static readonly HttpClient s_client = new()
    {
        BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/"),
        DefaultRequestVersion = new Version(2, 0),
    };

    private readonly ConcurrentDictionary<int, HackerNewsRowData> _rows = new();
    private readonly ConcurrentDictionary<int, Task<HackerNewsRowData>> _fetchTasks = new();
    private readonly SemaphoreSlim _idsGate = new(1, 1);

    private int[]? _storyIds;
    private int _rowCount;

    public HackerNewsVirtualizationSource()
    {
        _ = WarmupAsync();
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount => _rowCount;

    public bool SupportsPlaceholders => true;

    public async ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        await EnsureStoryIdsAsync(cancellationToken).ConfigureAwait(false);
        return _rowCount;
    }

    public async ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        await EnsureStoryIdsAsync(cancellationToken).ConfigureAwait(false);
        if (_storyIds is null || request.Count <= 0)
        {
            return FastTreeDataGridPageResult.Empty;
        }

        var rows = new List<FastTreeDataGridRow>(request.Count);
        var end = Math.Min(request.StartIndex + request.Count, _rowCount);
        for (var index = request.StartIndex; index < end; index++)
        {
            var data = await GetOrCreateRowAsync(index).ConfigureAwait(false);
            rows.Add(data.Row);
        }

        return new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null);
    }

    public async ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        await EnsureStoryIdsAsync(cancellationToken).ConfigureAwait(false);
        if (_storyIds is null)
        {
            return;
        }

        var end = Math.Min(request.StartIndex + request.Count, _rowCount);
        for (var index = request.StartIndex; index < end; index++)
        {
            _ = GetOrCreateRowAsync(index);
        }
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        if (request.Kind == FastTreeDataGridInvalidationKind.Full)
        {
            _storyIds = null;
            _rowCount = 0;
            _rows.Clear();
            _fetchTasks.Clear();
            Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
            _ = WarmupAsync();
        }

        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        if (_rows.TryGetValue(index, out var data))
        {
            row = data.Row;
            return true;
        }

        row = CreatePlaceholderRow(index);
        return false;
    }

    public bool IsPlaceholder(int index) => !_rows.ContainsKey(index);

    public FastTreeDataGridRow GetRow(int index)
    {
        if (_rows.TryGetValue(index, out var data))
        {
            return data.Row;
        }

        return CreatePlaceholderRow(index);
    }

    public void ToggleExpansion(int index)
    {
        _ = index;
    }

    private async Task WarmupAsync()
    {
        try
        {
            await EnsureStoryIdsAsync(CancellationToken.None).ConfigureAwait(false);
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // ignore warmup failures
        }
    }

    private async Task EnsureStoryIdsAsync(CancellationToken cancellationToken)
    {
        if (_storyIds is not null)
        {
            return;
        }

        await _idsGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storyIds is not null)
            {
                return;
            }

            var ids = await s_client.GetFromJsonAsync<int[]>("topstories.json", cancellationToken).ConfigureAwait(false) ?? Array.Empty<int>();
            if (ids.Length > MaxStories)
            {
                Array.Resize(ref ids, MaxStories);
            }

            _storyIds = ids;
            _rowCount = ids.Length;
        }
        finally
        {
            _idsGate.Release();
        }
    }

    private Task<HackerNewsRowData> GetOrCreateRowAsync(int index)
    {
        return _fetchTasks.GetOrAdd(index, FetchRowAsync);
    }

    private async Task<HackerNewsRowData> FetchRowAsync(int index)
    {
        var ids = _storyIds;
        if (ids is null || index < 0 || index >= ids.Length)
        {
            return new HackerNewsRowData(CreatePlaceholderRow(index), null);
        }

        var storyId = ids[index];
        try
        {
            var item = await s_client.GetFromJsonAsync<HackerNewsItem>($"item/{storyId}.json", CancellationToken.None).ConfigureAwait(false);
            if (item is null)
            {
                var placeholder = CreatePlaceholderRow(index);
                return new HackerNewsRowData(placeholder, null);
            }

            var provider = new HackerNewsRowValueProvider(index, item);
            var row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
            var data = new HackerNewsRowData(row, item);
            _rows[index] = data;
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));
            return data;
        }
        catch
        {
            var placeholder = CreatePlaceholderRow(index);
            return new HackerNewsRowData(placeholder, null);
        }
        finally
        {
            _fetchTasks.TryRemove(index, out _);
        }
    }

    private static FastTreeDataGridRow CreatePlaceholderRow(int index)
    {
        var provider = new HackerNewsPlaceholderValueProvider(index);
        return new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
    }

    private sealed record HackerNewsRowData(FastTreeDataGridRow Row, HackerNewsItem? Item);

    private sealed class HackerNewsRowValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly int _index;
        private readonly HackerNewsItem _item;

        public HackerNewsRowValueProvider(int index, HackerNewsItem item)
        {
            _index = index;
            _item = item;
        }

        public void Dispose()
        {
        }

        public object? GetValue(object? row, string key)
        {
            return key switch
            {
                HackerNewsColumns.KeyIndex => _index,
                HackerNewsColumns.KeyTitle => _item.Title,
                HackerNewsColumns.KeyScore => _item.Score,
                HackerNewsColumns.KeyAuthor => _item.By,
                HackerNewsColumns.KeyAge => ComputeAge(_item.Time),
                HackerNewsColumns.KeyUrl => _item.Url,
                _ => null,
            };
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }

        private static string ComputeAge(long unixTime)
        {
            try
            {
                var published = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                var delta = DateTime.UtcNow - published;
                if (delta.TotalDays >= 1)
                {
                    return $"{(int)delta.TotalDays}d ago";
                }

                if (delta.TotalHours >= 1)
                {
                    return $"{(int)delta.TotalHours}h ago";
                }

                if (delta.TotalMinutes >= 1)
                {
                    return $"{(int)delta.TotalMinutes}m ago";
                }

                return "Just now";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    private sealed class HackerNewsPlaceholderValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly int _index;

        public HackerNewsPlaceholderValueProvider(int index)
        {
            _index = index;
        }

        public void Dispose()
        {
        }

        public object? GetValue(object? row, string key)
        {
            return key switch
            {
                HackerNewsColumns.KeyIndex => _index,
                HackerNewsColumns.KeyTitle => "Loading...",
                _ => null,
            };
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
        {
            add { }
            remove { }
        }
    }

    private sealed record HackerNewsItem(string Title, string By, int Score, long Time, string? Url);
}

public static class HackerNewsColumns
{
    public const string KeyIndex = "Index";
    public const string KeyTitle = "Title";
    public const string KeyScore = "Score";
    public const string KeyAuthor = "Author";
    public const string KeyAge = "Age";
    public const string KeyUrl = "Url";
}
#pragma warning restore CS0067
