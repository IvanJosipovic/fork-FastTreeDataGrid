using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Virtualization;

public sealed class VirtualizationSamplesViewModel
{
    public VirtualizationSamplesViewModel()
    {
        RandomSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 512,
            PrefetchRadius = 4,
            MaxPages = 64,
            MaxConcurrentLoads = 8,
            ResetThrottleDelayMilliseconds = 80,
        };
        RandomSource = new RandomVirtualizationSource(1_000_000_000);

        HackerNewsSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 64,
            PrefetchRadius = 2,
            MaxPages = 32,
            MaxConcurrentLoads = 8,
            ResetThrottleDelayMilliseconds = 80,
        };
        HackerNewsSource = new HackerNewsVirtualizationSource();
    }

    public RandomVirtualizationSource RandomSource { get; }

    public FastTreeDataGridVirtualizationSettings RandomSettings { get; }

    public string RandomSummary => "Virtualized pseudo-random data (1,000,000,000 rows) generated deterministically on demand.";

    public HackerNewsVirtualizationSource HackerNewsSource { get; }

    public FastTreeDataGridVirtualizationSettings HackerNewsSettings { get; }

    public string HackerNewsSummary => "Top Hacker News stories fetched lazily from the public REST API when rows enter the viewport.";
}
