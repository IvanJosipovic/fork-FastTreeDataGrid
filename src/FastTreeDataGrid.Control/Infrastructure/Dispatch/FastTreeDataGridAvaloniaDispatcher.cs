using System;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Infrastructure;

internal static class FastTreeDataGridAvaloniaDispatcher
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (FastTreeDataGridDispatcherProvider.Dispatcher is FastTreeDataGridSynchronousDispatcher)
        {
            FastTreeDataGridDispatcherProvider.Dispatcher = new AvaloniaDispatcher();
        }
    }

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
