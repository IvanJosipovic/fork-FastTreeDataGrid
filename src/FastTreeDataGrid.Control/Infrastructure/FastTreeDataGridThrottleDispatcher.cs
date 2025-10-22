using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

internal sealed class FastTreeDataGridThrottleDispatcher : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public FastTreeDataGridThrottleDispatcher(TimeSpan delay)
    {
        _delay = delay;
    }

    public void Enqueue(Func<CancellationToken, Task> callback)
    {
        CancellationTokenSource? previous = null;
        CancellationTokenSource? current;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            previous = _cts;
            _cts = new CancellationTokenSource();
            current = _cts;
        }

        if (previous is not null)
        {
            try
            {
                previous.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                previous.Dispose();
            }
        }

        _ = DispatchAsync(callback, current);
    }

    private async Task DispatchAsync(Func<CancellationToken, Task> callback, CancellationTokenSource cts)
    {
        try
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cts.Token).ConfigureAwait(false);
            }

            await callback(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_gate)
        {
            if (_cts is not null)
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    _cts.Dispose();
                    _cts = null;
                }
            }

            _disposed = true;
        }
    }
}
