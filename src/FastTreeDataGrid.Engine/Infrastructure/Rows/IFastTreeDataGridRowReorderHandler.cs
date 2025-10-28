using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

public interface IFastTreeDataGridRowReorderHandler
{
    bool CanReorder(FastTreeDataGridRowReorderRequest request);

    Task<FastTreeDataGridRowReorderResult> ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken);
}
