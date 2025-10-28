using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public abstract class FastTreeDataGridDynamicSource<T> : IFastTreeDataGridSource, IDisposable
{
    private bool _disposed;
    private readonly FastTreeDataGridFlatSource<T> _inner;
    private readonly object _eventLock = new();
    private EventHandler? _resetRequested;
    private bool _resetPending;
    private EventHandler<FastTreeDataGridInvalidatedEventArgs>? _invalidated;
    private EventHandler<FastTreeDataGridRowMaterializedEventArgs>? _rowMaterialized;

    protected FastTreeDataGridDynamicSource(
        IEnumerable<T> initialItems,
        Func<T, IEnumerable<T>> childrenSelector,
        Func<T, string>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
    {
        if (childrenSelector is null)
        {
            throw new ArgumentNullException(nameof(childrenSelector));
        }

        _inner = new FastTreeDataGridFlatSource<T>(
            initialItems ?? Array.Empty<T>(),
            childrenSelector,
            keySelector,
            keyComparer);
        _inner.ResetRequested += OnInnerResetRequested;
        _inner.Invalidated += OnInnerInvalidated;
        _inner.RowMaterialized += OnInnerRowMaterialized;
    }

    protected FastTreeDataGridFlatSource<T> Inner => _inner;

    public event EventHandler? ResetRequested
    {
        add
        {
            EventHandler? handlerToInvoke = null;
            lock (_eventLock)
            {
                _resetRequested += value;
                if (_resetPending && value is not null)
                {
                    handlerToInvoke = value;
                    _resetPending = false;
                }
            }

            handlerToInvoke?.Invoke(this, EventArgs.Empty);
        }
        remove
        {
            lock (_eventLock)
            {
                _resetRequested -= value;
            }
        }
    }

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated
    {
        add
        {
            lock (_eventLock)
            {
                _invalidated += value;
            }
        }
        remove
        {
            lock (_eventLock)
            {
                _invalidated -= value;
            }
        }
    }

    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized
    {
        add
        {
            lock (_eventLock)
            {
                _rowMaterialized += value;
            }
        }
        remove
        {
            lock (_eventLock)
            {
                _rowMaterialized -= value;
            }
        }
    }

    public int RowCount => _inner.RowCount;

    public bool SupportsPlaceholders => _inner.SupportsPlaceholders;

    public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) =>
        _inner.GetRowCountAsync(cancellationToken);

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
        _inner.GetPageAsync(request, cancellationToken);

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
        _inner.PrefetchAsync(request, cancellationToken);

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) =>
        _inner.InvalidateAsync(request, cancellationToken);

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row) =>
        _inner.TryGetMaterializedRow(index, out row);

    public bool IsPlaceholder(int index) => _inner.IsPlaceholder(index);

    public FastTreeDataGridRow GetRow(int index) => _inner.GetRow(index);

    public void ToggleExpansion(int index) => _inner.ToggleExpansion(index);

    public void Sort(Comparison<FastTreeDataGridRow>? comparison) => _inner.Sort(comparison);

    public void SetFilter(Predicate<FastTreeDataGridRow>? filter, bool expandMatches = true) =>
        _inner.SetFilter(filter, expandMatches);

    protected void ResetWithSnapshot(IEnumerable<T> items, bool preserveExpansion = true)
    {
        var snapshot = items as IList<T> ?? (items?.ToList() ?? new List<T>());
        _inner.Reset(snapshot, preserveExpansion);
    }

    private void OnInnerResetRequested(object? sender, EventArgs e)
    {
        EventHandler? handler;
        lock (_eventLock)
        {
            handler = _resetRequested;
            if (handler is null)
            {
                _resetPending = true;
                return;
            }
        }

        handler.Invoke(this, e);

        lock (_eventLock)
        {
            _resetPending = false;
        }
    }

    private void OnInnerInvalidated(object? sender, FastTreeDataGridInvalidatedEventArgs e)
    {
        EventHandler<FastTreeDataGridInvalidatedEventArgs>? handler;
        lock (_eventLock)
        {
            handler = _invalidated;
        }

        handler?.Invoke(this, e);
    }

    private void OnInnerRowMaterialized(object? sender, FastTreeDataGridRowMaterializedEventArgs e)
    {
        EventHandler<FastTreeDataGridRowMaterializedEventArgs>? handler;
        lock (_eventLock)
        {
            handler = _rowMaterialized;
        }

        handler?.Invoke(this, e);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        _inner.ResetRequested -= OnInnerResetRequested;
        _inner.Invalidated -= OnInnerInvalidated;
        _inner.RowMaterialized -= OnInnerRowMaterialized;
        lock (_eventLock)
        {
            _resetRequested = null;
            _resetPending = false;
            _invalidated = null;
            _rowMaterialized = null;
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
