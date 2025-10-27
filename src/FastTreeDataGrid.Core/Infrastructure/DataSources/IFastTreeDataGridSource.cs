using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridSource
{
    event EventHandler? ResetRequested;
    event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    int RowCount { get; }
    bool SupportsPlaceholders { get; }

    ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken);
    ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken);
    ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken);
    Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken);

    bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row);
    bool IsPlaceholder(int index);

    FastTreeDataGridRow GetRow(int index);

    void ToggleExpansion(int index);
}
