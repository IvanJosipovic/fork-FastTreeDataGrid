using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridSynchronousDispatcher : IFastTreeDataGridDispatcher
{
    public static FastTreeDataGridSynchronousDispatcher Instance { get; } = new();

    private FastTreeDataGridSynchronousDispatcher()
    {
    }

    public bool CheckAccess() => true;

    public void Post(Action action, FastTreeDataGridDispatchPriority priority)
    {
        action?.Invoke();
    }
}
