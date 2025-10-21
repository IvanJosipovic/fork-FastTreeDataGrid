using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Charts;

public sealed class ChartSamplesViewModel
{
    private readonly IReadOnlyList<ChartSampleNode> _samples;

    public ChartSamplesViewModel()
    {
        _samples = ChartSamplesFactory.Create();
        Source = new FastTreeDataGridFlatSource<ChartSampleNode>(_samples, node => node.Children);
    }

    public IFastTreeDataGridSource Source { get; }
}
