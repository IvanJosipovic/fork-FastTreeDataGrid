using System;
using FastTreeDataGrid.VirtualizationDemo.ViewModels.VariableHeights;
using FastTreeDataGrid.VirtualizationDemo.ViewModels.Virtualization;
using FastTreeDataGrid.VirtualizationDemo.ViewModels.Extensibility;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        VariableHeights = new VariableHeightRowsViewModel();
        VariableHeightsAdaptive = new VariableHeightRowsViewModel(groupCount: 320, itemsPerGroup: 800);
        Virtualization = new VirtualizationSamplesViewModel();
        Adapters = new AdaptersSamplesViewModel();
        Extensibility = new ExtensibilitySamplesViewModel();
    }

    public VariableHeightRowsViewModel VariableHeights { get; }

    public VariableHeightRowsViewModel VariableHeightsAdaptive { get; }

    public VirtualizationSamplesViewModel Virtualization { get; }

    public AdaptersSamplesViewModel Adapters { get; }

    public ExtensibilitySamplesViewModel Extensibility { get; }

    public void Dispose()
    {
        Extensibility.Dispose();
    }
}
