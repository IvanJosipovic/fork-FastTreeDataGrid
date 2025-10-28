using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridSourceVirtualizationProvider : IFastTreeDataVirtualizationProvider, IFastTreeDataGridGroupingController, IFastTreeDataGridRowReorderHandler, IFastTreeDataGridGroupingNotificationSink
{
    private readonly IFastTreeDataGridSource _source;
    private bool _disposed;
    private bool _initialized;
    private int _lastPublishedCount = -1;
    private readonly object _eventLock = new();

    public FastTreeDataGridSourceVirtualizationProvider(IFastTreeDataGridSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _source.Invalidated += OnSourceInvalidated;
        _source.RowMaterialized += OnSourceRowMaterialized;
        _source.ResetRequested += OnSourceResetRequested;
    }

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;
    public event EventHandler<FastTreeDataGridCountChangedEventArgs>? CountChanged;

    public bool IsInitialized => _initialized;

    public bool SupportsMutations => false;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_initialized)
        {
            return;
        }

        var count = await _source.GetRowCountAsync(cancellationToken).ConfigureAwait(false);
        PublishCount(count);
        _initialized = true;
    }

    public async ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = await _source.GetRowCountAsync(cancellationToken).ConfigureAwait(false);
        PublishCount(count);
        return count;
    }

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
        _source.GetPageAsync(request, cancellationToken);

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
        _source.PrefetchAsync(request, cancellationToken);

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) =>
        _source.InvalidateAsync(request, cancellationToken);

    public Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken)
    {
        if (_source is null)
        {
            return Task.CompletedTask;
        }

        if (_source is IFastTreeDataGridGroupingHandler groupingHandler)
        {
            var groupingRequest = FastTreeDataGridGroupingRequest.FromSortFilterRequest(request);
            return groupingHandler.ApplyGroupingAsync(groupingRequest, cancellationToken);
        }

        if (_source is IFastTreeDataGridSortFilterHandler handler)
        {
            return handler.ApplySortFilterAsync(request, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row) =>
        _source.TryGetMaterializedRow(index, out row);

    public bool IsPlaceholder(int index) => _source.IsPlaceholder(index);

    public Task<int> LocateRowIndexAsync(object? item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item is null)
        {
            return Task.FromResult(-1);
        }

        for (var i = 0; i < _source.RowCount; i++)
        {
            var row = _source.GetRow(i);
            if (Equals(row.Item, item))
            {
                return Task.FromResult(i);
            }
        }

        return Task.FromResult(-1);
    }

    public Task CreateAsync(object viewModel, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException("Create operations are not supported by this provider."));

    public Task UpdateAsync(object viewModel, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException("Update operations are not supported by this provider."));

    public Task DeleteAsync(object viewModel, CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException("Delete operations are not supported by this provider."));

    public void ExpandAllGroups()
    {
        if (_source is IFastTreeDataGridGroupingController grouping)
        {
            grouping.ExpandAllGroups();
        }
    }

    public void CollapseAllGroups()
    {
        if (_source is IFastTreeDataGridGroupingController grouping)
        {
            grouping.CollapseAllGroups();
        }
    }

    public void ApplyGroupExpansionLayout(IEnumerable<FastTreeDataGridGroupingExpansionState> states, bool defaultExpanded)
    {
        if (_source is IFastTreeDataGridGroupingController grouping)
        {
            grouping.ApplyGroupExpansionLayout(states, defaultExpanded);
        }
    }

    bool IFastTreeDataGridRowReorderHandler.CanReorder(FastTreeDataGridRowReorderRequest request)
    {
        return _source is IFastTreeDataGridRowReorderHandler handler && handler.CanReorder(request);
    }

    Task<FastTreeDataGridRowReorderResult> IFastTreeDataGridRowReorderHandler.ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken)
    {
        return _source is IFastTreeDataGridRowReorderHandler handler
            ? handler.ReorderAsync(request, cancellationToken)
            : Task.FromResult(FastTreeDataGridRowReorderResult.Cancelled);
    }

    void IFastTreeDataGridGroupingNotificationSink.OnGroupingStateChanged(FastTreeDataGridGroupingStateChangedEventArgs args)
    {
        var handler = Invalidated;
        if (handler is null)
        {
            return;
        }

        var request = args.Kind == FastTreeDataGridGroupingChangeKind.GroupStateChanged
            ? new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.MetadataOnly)
            : new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full);

        handler(this, new FastTreeDataGridInvalidatedEventArgs(request));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _source.Invalidated -= OnSourceInvalidated;
        _source.RowMaterialized -= OnSourceRowMaterialized;
        _source.ResetRequested -= OnSourceResetRequested;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void OnSourceInvalidated(object? sender, FastTreeDataGridInvalidatedEventArgs e)
    {
        var handler = Invalidated;
        handler?.Invoke(this, e);
        PublishCount(_source.RowCount);
    }

    private void OnSourceRowMaterialized(object? sender, FastTreeDataGridRowMaterializedEventArgs e)
    {
        var handler = RowMaterialized;
        handler?.Invoke(this, e);
    }

    private void OnSourceResetRequested(object? sender, EventArgs e)
    {
        var handler = Invalidated;
        handler?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full)));
        PublishCount(_source.RowCount);
    }

    private void PublishCount(int newCount)
    {
        if (newCount < 0)
        {
            return;
        }

        EventHandler<FastTreeDataGridCountChangedEventArgs>? handler;
        lock (_eventLock)
        {
            if (_lastPublishedCount == newCount)
            {
                return;
            }

            _lastPublishedCount = newCount;
            handler = CountChanged;
        }

        handler?.Invoke(this, new FastTreeDataGridCountChangedEventArgs(newCount));
    }

}
