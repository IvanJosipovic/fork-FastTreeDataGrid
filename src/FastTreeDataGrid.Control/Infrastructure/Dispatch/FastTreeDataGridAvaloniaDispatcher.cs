using System;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Infrastructure;

internal static class FastTreeDataGridAvaloniaDispatcher
{
    #pragma warning disable CA2255 // Module initializers are intentional here to swap dispatcher when the control assembly loads.
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (FastTreeDataGridDispatcherProvider.Dispatcher is FastTreeDataGridSynchronousDispatcher)
        {
            FastTreeDataGridDispatcherProvider.Dispatcher = new AvaloniaDispatcher();
        }
    }
    #pragma warning restore CA2255

    private sealed class AvaloniaDispatcher : IFastTreeDataGridDispatcher
    {
        public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

        public void Post(Action action, FastTreeDataGridDispatchPriority priority)
        {
            if (action is null)
            {
                return;
            }

            var mappedPriority = priority switch
            {
                FastTreeDataGridDispatchPriority.Background => DispatcherPriority.Background,
                _ => DispatcherPriority.Normal,
            };

            Dispatcher.UIThread.Post(() => action(), mappedPriority);
        }
    }

    internal static DispatcherPriority ToAvaloniaPriority(this FastTreeDataGridDispatchPriority priority) =>
        priority switch
        {
            FastTreeDataGridDispatchPriority.Background => DispatcherPriority.Background,
            _ => DispatcherPriority.Normal,
        };
}
