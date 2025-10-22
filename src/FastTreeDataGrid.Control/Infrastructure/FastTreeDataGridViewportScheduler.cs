using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

internal sealed class FastTreeDataGridViewportScheduler : IDisposable
{
    private readonly IFastTreeDataVirtualizationProvider _provider;
    private FastTreeDataGridVirtualizationSettings _settings;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _inFlightPages = new();
    private readonly object _requestLock = new();
    private FastTreeDataGridViewportRequest _lastRequest;
    private bool _disposed;

    public FastTreeDataGridViewportScheduler(IFastTreeDataVirtualizationProvider provider, FastTreeDataGridVirtualizationSettings settings)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _settings = settings ?? new FastTreeDataGridVirtualizationSettings();
    }

    public void UpdateSettings(FastTreeDataGridVirtualizationSettings settings)
    {
        _settings = settings ?? new FastTreeDataGridVirtualizationSettings();
    }

    public void Request(FastTreeDataGridViewportRequest request)
    {
        if (_disposed)
        {
            return;
        }

        lock (_requestLock)
        {
            if (request == _lastRequest)
            {
                return;
            }

            _lastRequest = request;
        }

        var pageSize = Math.Max(1, _settings.PageSize);
        var end = request.StartIndex + request.Count;
        for (var start = request.StartIndex; start < end; start += pageSize)
        {
            var length = Math.Min(pageSize, end - start);
            QueuePage(start, length);
        }

        var radius = Math.Max(request.PrefetchRadius, _settings.PrefetchRadius);
        if (radius > 0)
        {
            QueuePage(Math.Max(0, request.StartIndex - radius), Math.Min(pageSize, radius));
            QueuePage(end, Math.Min(pageSize, radius));
        }
    }

    public void CancelAll()
    {
        foreach (var (_, cts) in _inFlightPages)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
            }
        }

        _inFlightPages.Clear();
    }

    private void QueuePage(int startIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (_settings.MaxConcurrentLoads > 0 && _inFlightPages.Count >= _settings.MaxConcurrentLoads)
        {
            return;
        }

        if (_settings.MaxPages > 0 && _inFlightPages.Count >= _settings.MaxPages)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        var request = new FastTreeDataGridPageRequest(startIndex, count);

        while (true)
        {
            if (_inFlightPages.TryAdd(startIndex, cts))
            {
                break;
            }

            if (_inFlightPages.TryGetValue(startIndex, out var existing))
            {
                try
                {
                    existing.Cancel();
                }
                catch
                {
                }

                if (_inFlightPages.TryUpdate(startIndex, cts, existing))
                {
                    break;
                }
            }
            else
            {
                continue;
            }
        }

        _ = DispatchAsync(request, cts);
    }

    private async Task DispatchAsync(FastTreeDataGridPageRequest request, CancellationTokenSource cts)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("start_index", request.StartIndex),
            new("count", request.Count),
        };

        FastTreeDataGridVirtualizationDiagnostics.PageRequests.Add(1, tags);
        FastTreeDataGridVirtualizationDiagnostics.InFlightRequests.Add(1, tags);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _provider.PrefetchAsync(request, cts.Token).ConfigureAwait(false);
            await _provider.GetPageAsync(request, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            FastTreeDataGridVirtualizationDiagnostics.Log("Scheduler", $"Page request cancelled ({request.StartIndex},{request.Count})", null, tags);
        }
        catch (Exception ex)
        {
            FastTreeDataGridVirtualizationDiagnostics.Log("Scheduler", "Page request failed", ex, tags);
        }
        finally
        {
            stopwatch.Stop();
            FastTreeDataGridVirtualizationDiagnostics.PageFetchDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
            FastTreeDataGridVirtualizationDiagnostics.InFlightRequests.Add(-1, tags);
            _inFlightPages.TryRemove(request.StartIndex, out _);
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CancelAll();
        _disposed = true;
    }
}
