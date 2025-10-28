using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridVirtualizationSettings
{
    private int _pageSize = 128;
    private int _prefetchRadius = 2;
    private int _maxPages = 64;
    private int _maxConcurrentLoads = 4;
    private int _resetThrottleDelay = 120;
    private bool _showLoadingOverlay = true;
    private bool _showPlaceholderSkeletons = true;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Max(1, value);
    }

    public int PrefetchRadius
    {
        get => _prefetchRadius;
        set => _prefetchRadius = Math.Max(0, value);
    }

    public int MaxPages
    {
        get => _maxPages;
        set => _maxPages = Math.Max(1, value);
    }

    public int MaxConcurrentLoads
    {
        get => _maxConcurrentLoads;
        set => _maxConcurrentLoads = Math.Max(1, value);
    }

    public int ResetThrottleDelayMilliseconds
    {
        get => _resetThrottleDelay;
        set => _resetThrottleDelay = Math.Max(0, value);
    }

    public FastTreeDataGridDispatchPriority DispatcherPriority { get; set; } = FastTreeDataGridDispatchPriority.Background;

    public bool ShowLoadingOverlay
    {
        get => _showLoadingOverlay;
        set => _showLoadingOverlay = value;
    }

    public bool ShowPlaceholderSkeletons
    {
        get => _showPlaceholderSkeletons;
        set => _showPlaceholderSkeletons = value;
    }
}
