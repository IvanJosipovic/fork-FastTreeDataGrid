using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Provides grouping operations for data sources capable of serving grouped views.
/// </summary>
public interface IFastTreeDataGridGroupingHandler
{
    Task ApplyGroupingAsync(FastTreeDataGridGroupingRequest request, CancellationToken cancellationToken);
}
