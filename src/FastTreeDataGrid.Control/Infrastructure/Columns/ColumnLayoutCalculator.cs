using System;
using System.Collections.Generic;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Infrastructure;

internal static class ColumnLayoutCalculator
{
    public static IReadOnlyList<double> Calculate(IReadOnlyList<FastTreeDataGridColumn> columns, double availableWidth)
    {
        if (columns.Count == 0)
        {
            return Array.Empty<double>();
        }

        var widths = new double[columns.Count];
        var starColumns = new List<int>();
        double fixedSpace = 0;
        double starTotal = 0;

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            double width;
            switch (column.SizingMode)
            {
                case ColumnSizingMode.Pixel:
                    width = column.PixelWidth;
                    break;
                case ColumnSizingMode.Auto:
                    width = column.CachedAutoWidth;
                    break;
                case ColumnSizingMode.Star:
                    starColumns.Add(i);
                    starTotal += column.StarValue <= 0 ? 1 : column.StarValue;
                    continue;
                default:
                    width = 100;
                    break;
            }

            width = Math.Clamp(width, column.MinWidth, column.MaxWidth);
            widths[i] = width;
            fixedSpace += width;
        }

        var remaining = availableWidth - fixedSpace;
        if (starColumns.Count > 0)
        {
            if (remaining < 0)
            {
                remaining = 0;
            }

            foreach (var index in starColumns)
            {
                var column = columns[index];
                var ratio = column.StarValue <= 0 ? 1 : column.StarValue / starTotal;
                var width = remaining * ratio;
                width = Math.Clamp(width, column.MinWidth, column.MaxWidth);
                widths[index] = width;
            }
        }

        var total = 0d;
        for (var i = 0; i < widths.Length; i++)
        {
            var width = widths[i];
            if (width <= 0)
            {
                width = columns[i].MinWidth;
                widths[i] = width;
            }

            columns[i].ActualWidth = width;
            total += width;
        }

        if (total < availableWidth && starColumns.Count == 0 && columns.Count > 0)
        {
            var delta = availableWidth - total;
            widths[^1] += delta;
            columns[^1].ActualWidth = widths[^1];
        }

        return widths;
    }
}
