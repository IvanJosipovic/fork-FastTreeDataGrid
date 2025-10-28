using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridHybridSource<T> : FastTreeDataGridStreamingSource<T>
{
    private readonly Func<CancellationToken, Task<IEnumerable<T>>> _initialLoader;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;
    private bool _disposed;

    public FastTreeDataGridHybridSource(
        Func<CancellationToken, Task<IEnumerable<T>>> initialLoader,
        Func<T, IEnumerable<T>> childrenSelector,
        Func<T, string>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
        : base(null, childrenSelector, keySelector, keyComparer)
    {
        _initialLoader = initialLoader ?? throw new ArgumentNullException(nameof(initialLoader));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            var items = await _initialLoader(cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<T>();
            ApplyUpdate(list =>
            {
                list.Clear();
                foreach (var item in items)
                {
                    list.Add(item);
                }
            }, preserveExpansion: false);

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        _initializationLock.Dispose();
        base.Dispose(disposing);
    }
}
