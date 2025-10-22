using Avalonia;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
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
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Last Price",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CryptoTickerNode.KeyPrice,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Change",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = CryptoTickerNode.KeyChange,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "24h Trend",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = CryptoTickerNode.KeyChangeChart,
            CanUserSort = false,
            WidgetFactory = (_, _) => new ChartWidget
            {
                Key = CryptoTickerNode.KeyChangeChart,
                StrokeThickness = 1.25,
                Margin = new Thickness(4, 6, 4, 6),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Volume",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = CryptoTickerNode.KeyVolume,
            CanUserSort = true,
        });
    }

    private void OnCryptoSortRequested(object? sender, FastTreeDataGridSortEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var handled = viewModel.Crypto.ApplySort(e.Column, e.Direction);

        if (sender is GridControl grid)
        {
            if (!handled || e.Direction == FastTreeDataGridSortDirection.None)
            {
                grid.ClearSortState();
            }
            else
            {
                grid.SetSortState(e.ColumnIndex, e.Direction);
            }
        }
    }
}
