using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using FastTreeDataGrid.Demo.ViewModels.Widgets;
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

        if (this.FindControl<GridControl>("WidgetsGrid") is { } widgetsGrid)
        {
            ConfigureWidgetColumns(widgetsGrid);
        }
    }

    private static void ConfigureWidgetColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 36;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Name",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 160,
            ValueKey = WidgetGalleryNode.KeyName,
            IsHierarchy = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Description",
            SizingMode = ColumnSizingMode.Star,
            MinWidth = 220,
            ValueKey = WidgetGalleryNode.KeyDescription,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Icon",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 80,
            MinWidth = 70,
            ValueKey = WidgetGalleryNode.KeyIcon,
            WidgetFactory = (_, _) => new IconWidget
            {
                Key = WidgetGalleryNode.KeyIcon,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                Padding = 10,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Geometry",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 100,
            MinWidth = 90,
            ValueKey = WidgetGalleryNode.KeyGeometry,
            WidgetFactory = (_, _) => new GeometryWidget
            {
                Key = WidgetGalleryNode.KeyGeometry,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(28, 124, 172)),
                Padding = 10,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Button",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 110,
            ValueKey = WidgetGalleryNode.KeyButton,
            WidgetFactory = (_, _) => new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyButton,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40)),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(242, 242, 242)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "CheckBox",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 90,
            MinWidth = 80,
            ValueKey = WidgetGalleryNode.KeyCheckBox,
            WidgetFactory = (_, _) => new CheckBoxWidget
            {
                Key = WidgetGalleryNode.KeyCheckBox,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Progress",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = WidgetGalleryNode.KeyProgress,
            WidgetFactory = (_, _) => new ProgressWidget
            {
                Key = WidgetGalleryNode.KeyProgress,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Custom Draw",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = WidgetGalleryNode.KeyCustom,
            WidgetFactory = (_, _) => new CustomDrawWidget
            {
                Key = WidgetGalleryNode.KeyCustom,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Layout",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 220,
            MinWidth = 200,
            ValueKey = WidgetGalleryNode.KeyLayout,
            WidgetFactory = (provider, item) =>
            {
                if (item is WidgetGalleryNode node && node.LayoutFactory is not null)
                {
                    return node.LayoutFactory();
                }

                if (provider?.GetValue(item, WidgetGalleryNode.KeyLayout) is Func<Widget> builder && builder is not null)
                {
                    return builder();
                }

                return null;
            },
        });
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
