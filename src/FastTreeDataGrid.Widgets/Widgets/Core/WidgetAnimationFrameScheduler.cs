using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.Control.Widgets;

public static class WidgetAnimationFrameScheduler
{
    [ThreadStatic]
    private static AvaloniaControl? s_currentHost;

    private static readonly object s_sync = new();
    private static readonly List<WeakReference<AvaloniaControl>> s_hosts = new();

    public static IDisposable RegisterHost(AvaloniaControl host)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var weak = new WeakReference<AvaloniaControl>(host);
        lock (s_sync)
        {
            s_hosts.Add(weak);
        }

        return new HostSubscription(weak);
    }

    public static IDisposable PushCurrentHost(AvaloniaControl? host)
    {
        var previous = s_currentHost;
        s_currentHost = host;
        return new HostScope(previous);
    }

    public static void RequestFrame()
    {
        if (s_currentHost is { } current)
        {
            QueueInvalidate(current);
            return;
        }

        WeakReference<AvaloniaControl>[] snapshot;
        lock (s_sync)
        {
            CleanupDeadHosts();
            snapshot = s_hosts.ToArray();
        }

        foreach (var weak in snapshot)
        {
            if (weak.TryGetTarget(out var host))
            {
                QueueInvalidate(host);
            }
        }
    }

    private static void QueueInvalidate(AvaloniaControl host)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                try
                {
                    host.InvalidateVisual();
                }
                catch
                {
                    // Host may have been disposed; ignore and let cleanup remove it.
                }
            },
            DispatcherPriority.Render);
    }

    private static void CleanupDeadHosts()
    {
        for (var i = s_hosts.Count - 1; i >= 0; i--)
        {
            if (!s_hosts[i].TryGetTarget(out _))
            {
                s_hosts.RemoveAt(i);
            }
        }
    }

    private sealed class HostScope : IDisposable
    {
        private readonly AvaloniaControl? _previous;
        private bool _disposed;

        public HostScope(AvaloniaControl? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            s_currentHost = _previous;
            _disposed = true;
        }
    }

    private sealed class HostSubscription : IDisposable
    {
        private readonly WeakReference<AvaloniaControl> _hostReference;
        private bool _disposed;

        public HostSubscription(WeakReference<AvaloniaControl> hostReference)
        {
            _hostReference = hostReference;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (s_sync)
            {
                s_hosts.Remove(_hostReference);
            }

            _disposed = true;
        }
    }
}
