using System;
using System.IO;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Virtualization;

public sealed class VirtualizationSamplesViewModel
{
    private static readonly string SqliteDatabasePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastTreeDataGrid.VirtualizationDemo", "virtualization.db");

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

        SqliteSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 512,
            PrefetchRadius = 3,
            MaxPages = 64,
            MaxConcurrentLoads = 6,
            ResetThrottleDelayMilliseconds = 80,
        };

        var connectionString = SqliteVirtualizationBootstrapper.CreateConnectionString(SqliteDatabasePath);
        SqliteSource = new SqliteVirtualizationSource(connectionString);
    }

    public RandomVirtualizationSource RandomSource { get; }

    public FastTreeDataGridVirtualizationSettings RandomSettings { get; }

    public string RandomSummary => "Virtualized pseudo-random data (1,000,000,000 rows) generated deterministically on demand.";

    public HackerNewsVirtualizationSource HackerNewsSource { get; }

    public FastTreeDataGridVirtualizationSettings HackerNewsSettings { get; }

    public string HackerNewsSummary => "Top Hacker News stories fetched lazily from the public REST API when rows enter the viewport.";

    public SqliteVirtualizationSource SqliteSource { get; }

    public FastTreeDataGridVirtualizationSettings SqliteSettings { get; }

    public string SqliteSummary => "SQLite-backed virtualization seeded with 1,000,000 rows on startup. Rows materialize on demand with placeholders until fetched.";

    public int SqliteRowCount => SqliteVirtualizationBootstrapper.TargetRowCount;
}
