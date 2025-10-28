using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public interface IFastTreeDataGridDispatcher
{
    bool CheckAccess();

    void Post(Action action, FastTreeDataGridDispatchPriority priority);
}
