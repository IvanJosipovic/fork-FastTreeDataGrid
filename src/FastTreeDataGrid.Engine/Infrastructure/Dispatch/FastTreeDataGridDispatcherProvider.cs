using System;
using System.Threading;

namespace FastTreeDataGrid.Engine.Infrastructure;

public static class FastTreeDataGridDispatcherProvider
{
    private static IFastTreeDataGridDispatcher s_dispatcher = FastTreeDataGridSynchronousDispatcher.Instance;

    public static IFastTreeDataGridDispatcher Dispatcher
    {
        get => Volatile.Read(ref s_dispatcher);
        set => Volatile.Write(ref s_dispatcher, value ?? throw new ArgumentNullException(nameof(value)));
    }
}
