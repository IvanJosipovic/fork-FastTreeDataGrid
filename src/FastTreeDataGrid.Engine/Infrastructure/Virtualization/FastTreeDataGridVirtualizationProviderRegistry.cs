using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridVirtualizationProviderRegistration : IDisposable
{
    private readonly Action _onDispose;
    private bool _disposed;

    public FastTreeDataGridVirtualizationProviderRegistration(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _onDispose();
        _disposed = true;
    }
}

public static class FastTreeDataGridVirtualizationProviderRegistry
{
    private static readonly object s_sync = new();
    private static readonly List<Func<object, FastTreeDataGridVirtualizationSettings, IFastTreeDataVirtualizationProvider?>> s_factories = new()
    {
        DefaultFactory,
    };

    public static IDisposable Register(Func<object, FastTreeDataGridVirtualizationSettings, IFastTreeDataVirtualizationProvider?> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (s_sync)
        {
            s_factories.Add(factory);
        }

        return new FastTreeDataGridVirtualizationProviderRegistration(() =>
        {
            lock (s_sync)
            {
                s_factories.Remove(factory);
            }
        });
    }

    public static IFastTreeDataVirtualizationProvider? Create(object? source, FastTreeDataGridVirtualizationSettings settings)
    {
        if (source is null)
        {
            return null;
        }

        lock (s_sync)
        {
            for (var i = s_factories.Count - 1; i >= 0; i--)
            {
                var factory = s_factories[i];
                var provider = factory(source, settings);
                if (provider is not null)
                {
                    return provider;
                }
            }
        }

        return null;
    }

    private static IFastTreeDataVirtualizationProvider? DefaultFactory(object source, FastTreeDataGridVirtualizationSettings settings)
    {
        if (source is IFastTreeDataVirtualizationProvider provider)
        {
            return provider;
        }

        if (source is IFastTreeDataGridSource gridSource)
        {
            return new FastTreeDataGridSourceVirtualizationProvider(gridSource);
        }

        return null;
    }
}
