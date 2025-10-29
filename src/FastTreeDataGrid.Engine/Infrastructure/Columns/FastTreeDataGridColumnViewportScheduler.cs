using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridColumnViewportScheduler : IDisposable
{
    private readonly IFastTreeDataGridColumnSource _source;
    private FastTreeDataGridVirtualizationSettings _settings;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _inFlightPages = new();
    private readonly object _requestLock = new();
    private FastTreeDataGridColumnViewportRequest _lastRequest;
    private bool _disposed;
    private int _currentRequestId;
    private int _currentTargetCount;
    private int _currentCompletedCount;

    public FastTreeDataGridColumnViewportScheduler(IFastTreeDataGridColumnSource source, FastTreeDataGridVirtualizationSettings settings)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _settings = settings ?? new FastTreeDataGridVirtualizationSettings();
    }

    public event EventHandler<FastTreeDataGridLoadingStateEventArgs>? LoadingStateChanged;

    public void UpdateSettings(FastTreeDataGridVirtualizationSettings settings)
    {
        _settings = settings ?? new FastTreeDataGridVirtualizationSettings();
    }

    public void Request(FastTreeDataGridColumnViewportRequest request)
    {
        if (_disposed)
        {
            return;
        }

        lock (_requestLock)
        {
            if (request.Equals(_lastRequest))
            {
                return;
            }

            _lastRequest = request;
        }

        CancelAll();

        var requestId = Interlocked.Increment(ref _currentRequestId);
        Volatile.Write(ref _currentCompletedCount, 0);
        Volatile.Write(ref _currentTargetCount, 0);

        var scheduledPages = 0;
        var pageSize = Math.Max(1, _settings.PageSize);
        var end = request.StartIndex + request.Count;

        var requestStopwatch = Stopwatch.StartNew();

        for (var start = request.StartIndex; start < end; start += pageSize)
        {
            var length = Math.Min(pageSize, end - start);
            if (QueuePage(start, length, requestId))
            {
                scheduledPages++;
            }
        }

        var radius = Math.Max(request.PrefetchRadius, _settings.PrefetchRadius);
        if (radius > 0)
        {
            var backStart = Math.Max(0, request.StartIndex - radius);
            var backCount = Math.Min(pageSize, request.StartIndex - backStart);
            if (backCount > 0 && QueuePage(backStart, backCount, requestId))
            {
                scheduledPages++;
            }

            var forwardStart = end;
            var forwardCount = Math.Min(pageSize, radius);
            if (forwardCount > 0 && QueuePage(forwardStart, forwardCount, requestId))
            {
                scheduledPages++;
            }
        }

        Volatile.Write(ref _currentTargetCount, scheduledPages);
        RaiseLoadingStateChanged();
        _ = MonitorTicketAsync(requestId, request, requestStopwatch);
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
        Volatile.Write(ref _currentTargetCount, 0);
        Volatile.Write(ref _currentCompletedCount, 0);
        RaiseLoadingStateChanged();
    }

    public void ResetLastRequest()
    {
        lock (_requestLock)
        {
            _lastRequest = default;
        }
    }

    private bool QueuePage(int startIndex, int count, int requestId)
    {
        if (count <= 0)
        {
            return false;
        }

        if (_settings.MaxConcurrentLoads > 0 && _inFlightPages.Count >= _settings.MaxConcurrentLoads)
        {
            return false;
        }

        if (_settings.MaxPages > 0 && _inFlightPages.Count >= _settings.MaxPages)
        {
            return false;
        }

        var cts = new CancellationTokenSource();
        var request = new FastTreeDataGridPageRequest(startIndex, count);
        var added = false;

        while (true)
        {
            if (_inFlightPages.TryAdd(startIndex, cts))
            {
                added = true;
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
                    added = true;
                    break;
                }
            }
            else
            {
                continue;
            }
        }

        if (added)
        {
            _ = DispatchAsync(request, cts, requestId);
            RaiseLoadingStateChanged();
            return true;
        }

        cts.Dispose();
        return false;
    }

    private async Task DispatchAsync(FastTreeDataGridPageRequest request, CancellationTokenSource cts, int requestId)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("start_index", request.StartIndex),
            new("count", request.Count),
        };

        FastTreeDataGridVirtualizationDiagnostics.PageRequests.Add(1, tags);
        FastTreeDataGridVirtualizationDiagnostics.InFlightRequests.Add(1, tags);
        var stopwatch = Stopwatch.StartNew();
        var cancelled = false;
        var counted = false;

        try
        {
            await _source.PrefetchAsync(request, cts.Token).ConfigureAwait(false);
            var page = await _source.GetPageAsync(request, cts.Token).ConfigureAwait(false);
            if (page.Completion is { } completionTask)
            {
                await completionTask.ConfigureAwait(false);
            }
            counted = true;
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            FastTreeDataGridVirtualizationDiagnostics.Log("ColumnScheduler", $"Page request cancelled ({request.StartIndex},{request.Count})", null, tags);
        }
        catch (Exception ex)
        {
            FastTreeDataGridVirtualizationDiagnostics.Log("ColumnScheduler", ex.Message, ex, tags);
        }
        finally
        {
            stopwatch.Stop();
            FastTreeDataGridVirtualizationDiagnostics.InFlightRequests.Add(-1, tags);
            FastTreeDataGridVirtualizationDiagnostics.PageFetchDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            if (_inFlightPages.TryRemove(request.StartIndex, out var existingCts))
            {
                existingCts.Dispose();
            }

            if (counted && requestId == Volatile.Read(ref _currentRequestId))
            {
                Interlocked.Increment(ref _currentCompletedCount);
            }

            if (!cancelled)
            {
                RaiseLoadingStateChanged();
            }
        }
    }

    private async Task MonitorTicketAsync(int requestId, FastTreeDataGridColumnViewportRequest request, Stopwatch requestStopwatch)
    {
        try
        {
            while (true)
            {
                if (Volatile.Read(ref _currentRequestId) != requestId)
                {
                    break;
                }

                var target = Math.Max(0, Volatile.Read(ref _currentTargetCount));
                var completed = Math.Min(Volatile.Read(ref _currentCompletedCount), target);
                if (target == 0 || completed >= target)
                {
                    break;
                }

                await Task.Delay(10).ConfigureAwait(false);
            }
        }
        finally
        {
            requestStopwatch.Stop();
            var tags = new KeyValuePair<string, object?>[]
            {
                new("start_index", request.StartIndex),
                new("count", request.Count),
            };
            FastTreeDataGridVirtualizationDiagnostics.ColumnTicketLifetime.Record(requestStopwatch.Elapsed.TotalMilliseconds, tags);
        }
    }

    private void RaiseLoadingStateChanged()
    {
        var handler = LoadingStateChanged;
        if (handler is null)
        {
            return;
        }

        var inFlight = _inFlightPages.Count;
        var target = Math.Max(0, Volatile.Read(ref _currentTargetCount));
        var completed = Math.Min(Volatile.Read(ref _currentCompletedCount), target);
        var requestId = Volatile.Read(ref _currentRequestId);
        double progress;

        if (target <= 0)
        {
            progress = inFlight > 0 ? double.NaN : 1d;
        }
        else
        {
            progress = Math.Clamp((double)completed / target, 0d, 1d);
        }

        handler.Invoke(this, new FastTreeDataGridLoadingStateEventArgs(requestId, inFlight, completed, target, progress));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelAll();
    }
}
