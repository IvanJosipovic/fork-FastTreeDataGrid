using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Bridges legacy sort/filter handlers to the grouping handler contract.
/// </summary>
public sealed class FastTreeDataGridGroupingSourceAdapter : IFastTreeDataGridGroupingHandler
{
    private readonly IFastTreeDataGridSortFilterHandler _handler;

    public FastTreeDataGridGroupingSourceAdapter(IFastTreeDataGridSortFilterHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public Task ApplyGroupingAsync(FastTreeDataGridGroupingRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var sortRequest = new FastTreeDataGridSortFilterRequest
        {
            SortDescriptors = request.SortDescriptors,
            FilterDescriptors = request.FilterDescriptors,
            GroupDescriptors = request.GroupDescriptors,
            AggregateDescriptors = request.AggregateDescriptors,
        };

        return _handler.ApplySortFilterAsync(sortRequest, cancellationToken);
    }
}
