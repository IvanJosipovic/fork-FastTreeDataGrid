using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Abstraction for paging logical column metadata in a similar fashion to row virtualization sources.
/// </summary>
public interface IFastTreeDataGridColumnSource
{
    event EventHandler? ResetRequested;
    event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    event EventHandler<FastTreeDataGridColumnMaterializedEventArgs>? ColumnMaterialized;

    int ColumnCount { get; }

    bool SupportsPlaceholders { get; }

    ValueTask<int> GetColumnCountAsync(CancellationToken cancellationToken);

    ValueTask<FastTreeDataGridColumnPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken);

    ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken);

    Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken);

    bool TryGetMaterializedColumn(int index, out FastTreeDataGridColumnDescriptor column);

    bool IsPlaceholder(int index);

    FastTreeDataGridColumnDescriptor GetColumn(int index);
}
