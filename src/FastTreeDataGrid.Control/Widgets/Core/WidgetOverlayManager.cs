using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public enum WidgetOverlayPlacement
{
    BottomStart,
    RightStart,
}

public sealed record WidgetOverlayOptions(
    Widget? Owner = null,
    bool CloseOnPointerDownOutside = true,
    bool CloseOnEscape = true,
    bool MatchWidthToAnchor = false,
    Thickness Offset = default,
    double? MaxWidth = null,
    double? MaxHeight = null,
    Action<Widget>? OnClosed = null,
    Action<Widget>? OnOpened = null);

public interface IWidgetOverlayHost
{
    bool ShowOverlay(Widget widget, Rect anchor, WidgetOverlayPlacement placement, WidgetOverlayOptions options);

    bool HideOverlay(Widget widget);

    void HideOwnedOverlays(Widget owner);
}

public static class WidgetOverlayManager
{
    [ThreadStatic]
    private static IWidgetOverlayHost? s_currentHost;

    private static readonly object s_sync = new();
    private static readonly List<WeakReference<IWidgetOverlayHost>> s_hosts = new();

    public static IDisposable RegisterHost(IWidgetOverlayHost host)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        WeakReference<IWidgetOverlayHost> weak;
        lock (s_sync)
        {
            weak = new WeakReference<IWidgetOverlayHost>(host);
            s_hosts.Add(weak);
        }

        return new HostRegistration(weak);
    }

    public static IDisposable PushCurrentHost(IWidgetOverlayHost? host)
    {
        var previous = s_currentHost;
        s_currentHost = host;
        return new HostScope(previous);
    }

    public static bool ShowOverlay(Widget widget, Rect anchor, WidgetOverlayPlacement placement, WidgetOverlayOptions options)
    {
        if (widget is null)
        {
            throw new ArgumentNullException(nameof(widget));
        }

        var handled = false;
        InvokeHosts(host =>
        {
            if (host.ShowOverlay(widget, anchor, placement, options))
            {
                handled = true;
                return true;
            }

            return false;
        });

        return handled;
    }

    public static void HideOverlay(Widget widget)
    {
        if (widget is null)
        {
            throw new ArgumentNullException(nameof(widget));
        }

        InvokeHosts(host => host.HideOverlay(widget));
    }

    public static void HideOwnedOverlays(Widget owner)
    {
        if (owner is null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        InvokeHosts(host =>
        {
            host.HideOwnedOverlays(owner);
            return false;
        });
    }

    private static void InvokeHosts(Func<IWidgetOverlayHost, bool> callback)
    {
        if (s_currentHost is not null)
        {
            if (callback(s_currentHost))
            {
                return;
            }
        }

        WeakReference<IWidgetOverlayHost>[] snapshot;
        lock (s_sync)
        {
            CleanupHosts();
            snapshot = s_hosts.ToArray();
        }

        foreach (var weak in snapshot)
        {
            if (!weak.TryGetTarget(out var target))
            {
                continue;
            }

            if (ReferenceEquals(target, s_currentHost))
            {
                continue;
            }

            if (callback(target))
            {
                break;
            }
        }
    }

    private static void CleanupHosts()
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
        private readonly IWidgetOverlayHost? _previous;
        private bool _disposed;

        public HostScope(IWidgetOverlayHost? previous)
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

    private sealed class HostRegistration : IDisposable
    {
        private readonly WeakReference<IWidgetOverlayHost> _reference;
        private bool _disposed;

        public HostRegistration(WeakReference<IWidgetOverlayHost> reference)
        {
            _reference = reference;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (s_sync)
            {
                for (var i = s_hosts.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(s_hosts[i], _reference))
                    {
                        s_hosts.RemoveAt(i);
                        break;
                    }
                }
            }

            _disposed = true;
        }
    }
}
