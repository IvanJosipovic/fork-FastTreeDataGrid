using System;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Virtualization;

public sealed class VirtualizationSamplesViewModel
{
    private readonly RandomVirtualizationSource _source;

    public VirtualizationSamplesViewModel()
    {
        _source = new RandomVirtualizationSource(1_000_000_000);
        Settings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 512,
            PrefetchRadius = 4,
            MaxPages = 64,
            MaxConcurrentLoads = 8,
            ResetThrottleDelayMilliseconds = 80,
        };
    }

    public IFastTreeDataGridSource Source => _source;

    public FastTreeDataGridVirtualizationSettings Settings { get; }

    public string Summary => "Virtualized random data (1,000,000,000 rows).";
}
