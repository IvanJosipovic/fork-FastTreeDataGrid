using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridSortFilterHandler
{
    Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken);
}
