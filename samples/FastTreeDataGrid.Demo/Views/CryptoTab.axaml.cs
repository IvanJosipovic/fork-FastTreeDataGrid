using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class CryptoTab : UserControl
{
    public CryptoTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("CryptoGrid") is { } grid)
        {
            ConfigureCryptoColumns(grid);
        }
    }

    private void ConfigureCryptoColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 28;
        grid.AggregateDescriptors.Clear();
        grid.GroupDescriptors.Clear();
        grid.SortRequested -= OnCryptoSortRequested;
        grid.SortRequested += OnCryptoSortRequested;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Symbol",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CryptoTickerNode.KeySymbol,
            MinWidth = 200,
            IsHierarchy = true,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Quote",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 100,
            MinWidth = 90,
            ValueKey = CryptoTickerNode.KeyQuote,
            CanUserSort = true,
            FilterPlaceholder = "Filter quote",
        });

        var priceColumn = new FastTreeDataGridColumn
        {
            Header = "Last Price",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CryptoTickerNode.KeyPrice,
            CanUserSort = true,
        };
        var priceAggregate = CreateAverageDescriptor(
            CryptoTickerNode.KeyPrice,
            "Avg",
            ticker => ticker.LastPrice,
            value => value.ToString("0.0000", CultureInfo.CurrentCulture));
        priceColumn.AggregateDescriptors.Add(priceAggregate);
        grid.Columns.Add(priceColumn);

        var changeColumn = new FastTreeDataGridColumn
        {
            Header = "Change",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = CryptoTickerNode.KeyChange,
            CanUserSort = true,
        };
        var changeAggregate = CreateAverageDescriptor(
            CryptoTickerNode.KeyChange,
            "Avg",
            ticker => ticker.ChangePercent,
            value => value.ToString("0.00%", CultureInfo.CurrentCulture));
        changeColumn.AggregateDescriptors.Add(changeAggregate);
        grid.Columns.Add(changeColumn);

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "24h Trend",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = CryptoTickerNode.KeyChangeChart,
            CanUserSort = false,
            CanUserGroup = false,
            WidgetFactory = (_, _) => new ChartWidget
            {
                Key = CryptoTickerNode.KeyChangeChart,
                StrokeThickness = 1.25,
                Margin = new Thickness(4, 6, 4, 6),
            },
        });

        var volumeColumn = new FastTreeDataGridColumn
        {
            Header = "Volume",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = CryptoTickerNode.KeyVolume,
            CanUserSort = true,
        };
        var volumeAggregate = CreateSumDescriptor(
            CryptoTickerNode.KeyVolume,
            "Σ",
            ticker => ticker.QuoteVolume,
            value => value.ToString("N0", CultureInfo.CurrentCulture));
        volumeColumn.AggregateDescriptors.Add(volumeAggregate);
        grid.Columns.Add(volumeColumn);

        grid.AggregateDescriptors.Add(CloneAggregate(priceAggregate));
        grid.AggregateDescriptors.Add(CloneAggregate(changeAggregate));
        grid.AggregateDescriptors.Add(CloneAggregate(volumeAggregate));

        if (grid.GroupDescriptors.Count == 0)
        {
            var descriptor = new FastTreeDataGridGroupDescriptor
            {
                ColumnKey = CryptoTickerNode.KeyQuote,
                Adapter = new FastTreeDataGridValueGroupAdapter(CryptoTickerNode.KeyQuote),
                SortDirection = FastTreeDataGridSortDirection.Ascending,
                IsExpanded = true,
            };

            foreach (var aggregate in grid.AggregateDescriptors)
            {
                descriptor.AggregateDescriptors.Add(CloneAggregate(aggregate));
            }

            grid.GroupDescriptors.Add(descriptor);
        }
    }

    private void OnCryptoSortRequested(object? sender, FastTreeDataGridSortEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var handled = viewModel.Crypto.ApplySort(e.Descriptions);

        if (sender is GridControl grid)
        {
            if (!handled || e.Descriptions.Count == 0)
            {
                grid.ClearSortState();
            }
            else
            {
                grid.SetSortState(e.Descriptions);
            }
        }
    }

    private static FastTreeDataGridAggregateDescriptor CreateAverageDescriptor(
        string columnKey,
        string label,
        Func<CryptoTickerNode, decimal> selector,
        Func<decimal, string> formatter)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = columnKey,
            Placement = FastTreeDataGridAggregatePlacement.GroupAndGrid,
            Label = label,
            Aggregator = rows =>
            {
                decimal sum = 0;
                var count = 0;

                foreach (var row in rows)
                {
                    if (row.Item is CryptoTickerNode ticker)
                    {
                        sum += selector(ticker);
                        count++;
                    }
                }

                return count > 0 ? sum / count : 0m;
            },
            Formatter = value => value switch
            {
                decimal dec => formatter(dec),
                double dbl => formatter((decimal)dbl),
                float fl => formatter((decimal)fl),
                _ => value?.ToString() ?? string.Empty,
            },
        };
    }

    private static FastTreeDataGridAggregateDescriptor CreateSumDescriptor(
        string columnKey,
        string label,
        Func<CryptoTickerNode, decimal> selector,
        Func<decimal, string> formatter)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = columnKey,
            Placement = FastTreeDataGridAggregatePlacement.GroupAndGrid,
            Label = label,
            Aggregator = rows =>
            {
                decimal sum = 0;
                foreach (var row in rows)
                {
                    if (row.Item is CryptoTickerNode ticker)
                    {
                        sum += selector(ticker);
                    }
                }

                return sum;
            },
            Formatter = value => value switch
            {
                decimal dec => formatter(dec),
                double dbl => formatter((decimal)dbl),
                float fl => formatter((decimal)fl),
                _ => value?.ToString() ?? string.Empty,
            },
        };
    }

    private static FastTreeDataGridAggregateDescriptor CloneAggregate(FastTreeDataGridAggregateDescriptor source)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = source.ColumnKey,
            Placement = source.Placement,
            Aggregator = source.Aggregator,
            Formatter = source.Formatter,
            Label = source.Label,
            Provider = source.Provider,
        };
    }
}
