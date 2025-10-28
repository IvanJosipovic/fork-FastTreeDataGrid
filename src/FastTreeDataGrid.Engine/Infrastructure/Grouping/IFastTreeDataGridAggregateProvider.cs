using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Provides aggregate calculations for grouped rows.
/// </summary>
public interface IFastTreeDataGridAggregateProvider
{
    /// <summary>
    /// Calculates an aggregate result synchronously.
    /// </summary>
    /// <param name="context">Context for the target group.</param>
    /// <returns>The computed aggregate result.</returns>
    FastTreeDataGridAggregateResult Calculate(FastTreeDataGridGroupContext context);

    /// <summary>
    /// Calculates an aggregate asynchronously.
    /// </summary>
    /// <param name="context">Context for the target group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed aggregate result.</returns>
    ValueTask<FastTreeDataGridAggregateResult> CalculateAsync(FastTreeDataGridGroupContext context, CancellationToken cancellationToken) =>
        new(Calculate(context));
}
