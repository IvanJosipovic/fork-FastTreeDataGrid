using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;

namespace FastTreeDataGrid.DualVirtualizationDemo.ViewModels;

public sealed class DualVirtualizationViewModel
{
    private const int MetricColumnCount = 240;
    private const int RowCountValue = 2_000_000;
    private static readonly TimeSpan FetchDelay = TimeSpan.FromMilliseconds(45);

    public DualVirtualizationViewModel()
    {
        Settings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 256,
            PrefetchRadius = 3,
            MaxPages = 64,
            MaxConcurrentLoads = 6,
            ResetThrottleDelayMilliseconds = 90,
            ShowLoadingOverlay = true,
            ShowPlaceholderSkeletons = true,
        };

        ColumnDefinitions = CreateColumnDefinitions();
        Source = new MatrixVirtualizationSource(RowCountValue, MetricColumnCount, FetchDelay);
        Summary = "Simulates a wide analytics matrix where millions of fact rows stream from a virtualized provider while hundreds of metric columns are materialized on demand.";
        RowDescription = $"Rows: {RowCountValue.ToString("N0", CultureInfo.InvariantCulture)} virtualized";
        ColumnDescription = $"Columns: {ColumnDefinitions.Count.ToString("N0", CultureInfo.InvariantCulture)} total ({MetricColumnCount.ToString("N0", CultureInfo.InvariantCulture)} metric columns)";
        LatencyDescription = $"Simulated fetch latency: {FetchDelay.TotalMilliseconds.ToString("N0", CultureInfo.InvariantCulture)} ms/page";
    }

    public MatrixVirtualizationSource Source { get; }

    public FastTreeDataGridVirtualizationSettings Settings { get; }

    public IReadOnlyList<MatrixVirtualizationColumnDefinition> ColumnDefinitions { get; }

    public string Summary { get; }

    public string RowDescription { get; }

    public string ColumnDescription { get; }

    public string LatencyDescription { get; }

    private static IReadOnlyList<MatrixVirtualizationColumnDefinition> CreateColumnDefinitions()
    {
        var definitions = new List<MatrixVirtualizationColumnDefinition>(MetricColumnCount + 2)
        {
            new(
                header: "Row",
                valueKey: MatrixVirtualizationColumns.KeyRowLabel,
                sizingMode: ColumnSizingMode.Pixel,
                pixelWidth: 140,
                minWidth: 120,
                maxWidth: 220,
                pinnedPosition: FastTreeDataGridPinnedPosition.Left),
            new(
                header: "Average",
                valueKey: MatrixVirtualizationColumns.KeyAverage,
                sizingMode: ColumnSizingMode.Pixel,
                pixelWidth: 130,
                minWidth: 110,
                maxWidth: 240,
                pinnedPosition: FastTreeDataGridPinnedPosition.None),
        };

        for (var i = 0; i < MetricColumnCount; i++)
        {
            definitions.Add(new MatrixVirtualizationColumnDefinition(
                header: $"Metric {i + 1}",
                valueKey: MatrixVirtualizationColumns.GetMetricKey(i),
                sizingMode: ColumnSizingMode.Pixel,
                pixelWidth: 110,
                minWidth: 90,
                maxWidth: 260,
                pinnedPosition: FastTreeDataGridPinnedPosition.None));
        }

        return definitions;
    }
}
