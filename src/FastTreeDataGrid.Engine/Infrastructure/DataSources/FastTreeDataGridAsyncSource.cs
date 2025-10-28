using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridAsyncSource<T> : FastTreeDataGridDynamicSource<T>
{
    private readonly Func<CancellationToken, Task<IEnumerable<T>>> _loadAsync;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private readonly object _itemsLock = new();
    private List<T> _items;
    private bool _disposed;

    public FastTreeDataGridAsyncSource(
        Func<CancellationToken, Task<IEnumerable<T>>> loadAsync,
        Func<T, IEnumerable<T>> childrenSelector,
        Func<T, string>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
        : base(Array.Empty<T>(), childrenSelector, keySelector, keyComparer)
    {
        _loadAsync = loadAsync ?? throw new ArgumentNullException(nameof(loadAsync));
        _items = new List<T>();
    }

    public bool IsLoading { get; private set; }

    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_itemsLock)
            {
                return _items.ToArray();
            }
        }
    }

    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        LoadCoreAsync(preserveExpansion: false, cancellationToken);

    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        LoadCoreAsync(preserveExpansion: true, cancellationToken);

    private async Task LoadCoreAsync(bool preserveExpansion, CancellationToken cancellationToken)
    {
        await _loadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IsLoading = true;
            var items = await _loadAsync(cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<T>();
            lock (_itemsLock)
            {
                _items = items.ToList();
            }

            ResetSnapshot(preserveExpansion);
        }
        finally
        {
            IsLoading = false;
            _loadSemaphore.Release();
        }
    }

    private void ResetSnapshot(bool preserveExpansion)
    {
        List<T> snapshot;
        lock (_itemsLock)
        {
            snapshot = _items.ToList();
        }

        ResetWithSnapshot(snapshot, preserveExpansion);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        _loadSemaphore.Dispose();
        base.Dispose(disposing);
    }
}
