using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FastTreeDataGrid.Control.Infrastructure;

internal readonly record struct FastTreeDataGridVirtualizationLogEntry(
    string Category,
    string Message,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> Tags);

internal static class FastTreeDataGridVirtualizationDiagnostics
{
    private static readonly Meter s_meter = new("FastTreeDataGrid.Virtualization", "1.0.0");

    public static Meter Meter => s_meter;

    public static readonly Counter<long> PageRequests =
        s_meter.CreateCounter<long>("fasttree_datagrid_page_requests", description: "Number of virtualization page requests issued.");

    public static readonly Histogram<double> PageFetchDuration =
        s_meter.CreateHistogram<double>("fasttree_datagrid_page_fetch_duration_ms", unit: "ms", description: "Duration of virtualization page fetches.");

    public static readonly UpDownCounter<long> InFlightRequests =
        s_meter.CreateUpDownCounter<long>("fasttree_datagrid_inflight_requests", description: "Current number of in-flight page requests.");

    public static readonly Histogram<double> ViewportUpdateDuration =
        s_meter.CreateHistogram<double>("fasttree_datagrid_viewport_update_duration_ms", unit: "ms", description: "Duration of viewport update passes.");

    public static readonly Counter<long> ViewportRowsRendered =
        s_meter.CreateCounter<long>("fasttree_datagrid_viewport_rows_rendered", description: "Number of rows processed during viewport updates.");

    public static readonly Counter<long> PlaceholderRowsRendered =
        s_meter.CreateCounter<long>("fasttree_datagrid_placeholder_rows_rendered", description: "Number of placeholder rows rendered during viewport updates.");

    public static readonly Counter<long> ResetCount =
        s_meter.CreateCounter<long>("fasttree_datagrid_resets", description: "Number of datasource resets processed by the grid.");

    public static Action<FastTreeDataGridVirtualizationLogEntry>? LogCallback { get; set; }
        = entry => Debug.WriteLine($"[FastTreeDataGrid] {entry.Category}: {entry.Message}");

    public static void Log(string category, string message, Exception? exception = null, params KeyValuePair<string, object?>[] tags)
    {
        LogCallback?.Invoke(new FastTreeDataGridVirtualizationLogEntry(category, message, exception, tags));
    }
}
