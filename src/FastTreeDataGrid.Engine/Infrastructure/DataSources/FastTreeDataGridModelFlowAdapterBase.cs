using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Provides shared plumbing for adapting external data-virtualization engines (such as ModelFlow)
/// to <see cref="IFastTreeDataVirtualizationProvider"/> without the FastTreeDataGrid library taking
/// a direct dependency on those engines. Derived classes supply the concrete data access logic while
/// this base handles event fan-out and row construction.
/// </summary>
/// <typeparam name="TViewModel">The underlying view-model (or data item) type exposed by the external datasource.</typeparam>
public abstract class FastTreeDataGridModelFlowAdapterBase<TViewModel> : IFastTreeDataVirtualizationProvider
{
    private readonly Func<TViewModel?, FastTreeDataGridRow> _rowFactory;
    private readonly Func<TViewModel?, FastTreeDataGridRow>? _placeholderRowFactory;
    private bool _initialized;
    private bool _disposed;

    protected FastTreeDataGridModelFlowAdapterBase(
        Func<TViewModel?, FastTreeDataGridRow> rowFactory,
        Func<TViewModel?, FastTreeDataGridRow>? placeholderRowFactory = null)
    {
        _rowFactory = rowFactory ?? throw new ArgumentNullException(nameof(rowFactory));
        _placeholderRowFactory = placeholderRowFactory;
    }

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;
    public event EventHandler<FastTreeDataGridCountChangedEventArgs>? CountChanged;

    public bool IsInitialized => _initialized;

    public bool SupportsMutations => SupportsMutationsCore;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_initialized)
        {
            return;
        }

        await EnsureInitializedCoreAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;

        var count = await GetCountCoreAsync(cancellationToken).ConfigureAwait(false);
        RaiseCountChanged(count);
    }

    public async ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = await GetCountCoreAsync(cancellationToken).ConfigureAwait(false);
        RaiseCountChanged(count);
        return count;
    }

    public async ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Count <= 0)
        {
            return FastTreeDataGridPageResult.Empty;
        }

        var page = await GetPageCoreAsync(request.StartIndex, request.Count, cancellationToken).ConfigureAwait(false);

        var rows = new List<FastTreeDataGridRow>(page.Items.Count);
        var placeholders = page.PlaceholderIndices?.Count > 0
            ? new HashSet<int>(page.PlaceholderIndices)
            : null;

        for (var i = 0; i < page.Items.Count; i++)
        {
            var viewModel = page.Items[i];
            var isPlaceholder = placeholders?.Contains(i) == true;
            var row = isPlaceholder && _placeholderRowFactory is not null
                ? _placeholderRowFactory(viewModel)
                : _rowFactory(viewModel);
            rows.Add(row);
        }

        return new FastTreeDataGridPageResult(rows, page.PlaceholderIndices ?? Array.Empty<int>(), completion: null, cancellation: null);
    }

    public async ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await PrefetchCoreAsync(request.StartIndex, request.Count, cancellationToken).ConfigureAwait(false);
    }

    public async Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await InvalidateCoreAsync(request, cancellationToken).ConfigureAwait(false);
        RaiseInvalidated(request);
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        if (!TryGetMaterializedViewModelCore(index, out var viewModel))
        {
            row = default!;
            return false;
        }

        row = _rowFactory(viewModel);
        return true;
    }

    public bool IsPlaceholder(int index) => IsPlaceholderCore(index);

    public Task CreateAsync(object viewModel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (viewModel is TViewModel typed)
        {
            return CreateCoreAsync(typed, cancellationToken);
        }

        return Task.FromException(new NotSupportedException("CreateAsync expects a view-model compatible with the adapter"));
    }

    public Task UpdateAsync(object viewModel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (viewModel is TViewModel typed)
        {
            return UpdateCoreAsync(typed, cancellationToken);
        }

        return Task.FromException(new NotSupportedException("UpdateAsync expects a view-model compatible with the adapter"));
    }

    public Task DeleteAsync(object viewModel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (viewModel is TViewModel typed)
        {
            return DeleteCoreAsync(typed, cancellationToken);
        }

        return Task.FromException(new NotSupportedException("DeleteAsync expects a view-model compatible with the adapter"));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    protected void RaiseInvalidated(FastTreeDataGridInvalidationRequest request)
    {
        Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
    }

    protected void RaiseRowMaterialized(int index, TViewModel? viewModel)
    {
        var row = _rowFactory(viewModel);
        RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));
    }

    protected void RaiseCountChanged(int count)
    {
        if (count < 0)
        {
            return;
        }

        CountChanged?.Invoke(this, new FastTreeDataGridCountChangedEventArgs(count));
    }

    protected abstract Task EnsureInitializedCoreAsync(CancellationToken cancellationToken);

    protected abstract Task<int> GetCountCoreAsync(CancellationToken cancellationToken);

    protected abstract Task<ViewModelPage> GetPageCoreAsync(int startIndex, int count, CancellationToken cancellationToken);

    protected virtual Task PrefetchCoreAsync(int startIndex, int count, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task InvalidateCoreAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ApplySortFilterCoreAsync(request, cancellationToken);
    }

    protected virtual Task ApplySortFilterCoreAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<int> LocateRowIndexAsync(object? item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return LocateRowIndexCoreAsync(item, cancellationToken);
    }

    protected virtual Task<int> LocateRowIndexCoreAsync(object? item, CancellationToken cancellationToken) => Task.FromResult(-1);

    protected abstract bool TryGetMaterializedViewModelCore(int index, out TViewModel? viewModel);

    protected abstract bool IsPlaceholderCore(int index);

    protected virtual bool SupportsMutationsCore => false;

    protected virtual Task CreateCoreAsync(TViewModel viewModel, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());

    protected virtual Task UpdateCoreAsync(TViewModel viewModel, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());

    protected virtual Task DeleteCoreAsync(TViewModel viewModel, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());

    protected virtual void DisposeCore()
    {
    }

    protected readonly struct ViewModelPage
    {
        public ViewModelPage(IReadOnlyList<TViewModel?> items, IReadOnlyList<int>? placeholderIndices)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            PlaceholderIndices = placeholderIndices;
        }

        public IReadOnlyList<TViewModel?> Items { get; }

        public IReadOnlyList<int>? PlaceholderIndices { get; }
    }
}
