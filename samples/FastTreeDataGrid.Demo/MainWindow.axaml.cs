using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Controls;
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
            Header = "Toggle",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 110,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyToggle,
            WidgetFactory = (_, item) =>
            {
                var toggle = new ToggleSwitchWidget
                {
                    Key = WidgetGalleryNode.KeyToggle,
                };

                if (item is WidgetGalleryNode node)
                {
                    toggle.Toggled += on =>
                    {
                        if (node.ToggleValue is ToggleSwitchWidgetValue current)
                        {
                            node.ToggleValue = current with { IsOn = on };
                        }
                        else
                        {
                            node.ToggleValue = new ToggleSwitchWidgetValue(on);
                        }
                    };
                }

                return toggle;
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Badge",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyBadge,
            WidgetFactory = (_, _) => new BadgeWidget
            {
                Key = WidgetGalleryNode.KeyBadge,
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

    private void ConfigureFileColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 28;
        grid.SortRequested -= OnFilesSortRequested;
        grid.SortRequested += OnFilesSortRequested;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Name",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = FileSystemNode.KeyName,
            MinWidth = 280,
            IsHierarchy = true,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Type",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = FileSystemNode.KeyType,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Size",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = FileSystemNode.KeySize,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Modified",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 160,
            MinWidth = 140,
            ValueKey = FileSystemNode.KeyModified,
            CanUserSort = true,
        });
    }

    private void ConfigureCountryColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 16;
        grid.RowHeight = 28;
        grid.SortRequested -= OnCountriesSortRequested;
        grid.SortRequested += OnCountriesSortRequested;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Country",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CountryNode.KeyName,
            MinWidth = 320,
            IsHierarchy = true,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Region",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 160,
            ValueKey = CountryNode.KeyRegion,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Population",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyPopulation,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Area",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyArea,
            CanUserSort = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "GDP",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyGdp,
            CanUserSort = true,
        });
    }

    private void OnCountriesSortRequested(object? sender, FastTreeDataGridSortEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var handled = viewModel.Countries.ApplySort(e.Column, e.Direction);

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

    private void OnFilesSortRequested(object? sender, FastTreeDataGridSortEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var handled = viewModel.Files.ApplySort(e.Column, e.Direction);

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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Dispose();
        }
    }
}
