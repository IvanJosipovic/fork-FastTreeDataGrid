using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataVirtualizationProvider : IDisposable, IAsyncDisposable
{
    event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;
    event EventHandler<FastTreeDataGridCountChangedEventArgs>? CountChanged;

    bool IsInitialized { get; }
    bool SupportsMutations { get; }

    ValueTask InitializeAsync(CancellationToken cancellationToken);
    ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken);
    ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken);
    ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken);
    Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken);

    bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row);
    bool IsPlaceholder(int index);

    Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken);

    Task<int> LocateRowIndexAsync(object? item, CancellationToken cancellationToken);

    Task CreateAsync(object viewModel, CancellationToken cancellationToken);
    Task UpdateAsync(object viewModel, CancellationToken cancellationToken);
    Task DeleteAsync(object viewModel, CancellationToken cancellationToken);
}
