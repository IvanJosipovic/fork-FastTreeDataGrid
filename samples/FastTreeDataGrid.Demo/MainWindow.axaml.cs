using System;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        if (this.FindControl<GridControl>("FilesGrid") is { } filesGrid)
        {
            ConfigureFileColumns(filesGrid);
        }

        if (this.FindControl<GridControl>("CountriesGrid") is { } countriesGrid)
        {
            ConfigureCountryColumns(countriesGrid);
        }

        if (this.FindControl<GridControl>("CryptoGrid") is { } cryptoGrid)
        {
            ConfigureCryptoColumns(cryptoGrid);
        }
    }

    private static void ConfigureFileColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 28;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Name",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = FileSystemNode.KeyName,
            MinWidth = 280,
            IsHierarchy = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Type",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = FileSystemNode.KeyType,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Size",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = FileSystemNode.KeySize,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Modified",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = FileSystemNode.KeyModified,
        });
    }

    private static void ConfigureCountryColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 16;
        grid.RowHeight = 28;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Country",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CountryNode.KeyName,
            MinWidth = 320,
            IsHierarchy = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Region",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 160,
            ValueKey = CountryNode.KeyRegion,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Population",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyPopulation,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Area",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyArea,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "GDP",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyGdp,
        });
    }

    private static void ConfigureCryptoColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 28;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Symbol",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CryptoTickerNode.KeySymbol,
            MinWidth = 200,
            IsHierarchy = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Quote",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 100,
            MinWidth = 90,
            ValueKey = CryptoTickerNode.KeyQuote,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Last Price",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CryptoTickerNode.KeyPrice,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Change",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = CryptoTickerNode.KeyChange,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Volume",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = CryptoTickerNode.KeyVolume,
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Dispose();
        }
    }
}
