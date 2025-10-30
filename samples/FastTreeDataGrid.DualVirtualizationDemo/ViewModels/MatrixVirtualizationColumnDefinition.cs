using System;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;

namespace FastTreeDataGrid.DualVirtualizationDemo.ViewModels;

public sealed class MatrixVirtualizationColumnDefinition
{
    public MatrixVirtualizationColumnDefinition(
        string header,
        string valueKey,
        ColumnSizingMode sizingMode,
        double pixelWidth,
        double minWidth,
        double maxWidth,
        FastTreeDataGridPinnedPosition pinnedPosition)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        ValueKey = valueKey ?? throw new ArgumentNullException(nameof(valueKey));
        SizingMode = sizingMode;
        PixelWidth = pixelWidth;
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        PinnedPosition = pinnedPosition;
    }

    public string Header { get; }

    public string ValueKey { get; }

    public ColumnSizingMode SizingMode { get; }

    public double PixelWidth { get; }

    public double MinWidth { get; }

    public double MaxWidth { get; }

    public FastTreeDataGridPinnedPosition PinnedPosition { get; }
}
