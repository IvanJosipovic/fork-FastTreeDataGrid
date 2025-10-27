using System.Globalization;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class CountriesTab : UserControl
{
    public CountriesTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("CountriesGrid") is { } grid)
        {
            ConfigureCountryColumns(grid);
        }
    }

    private void ConfigureCountryColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 16;
        grid.RowHeight = 28;
        grid.IsFilterRowVisible = true;
        grid.SortRequested -= OnCountriesSortRequested;
        grid.SortRequested += OnCountriesSortRequested;

        grid.AggregateDescriptors.Clear();
        grid.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
        {
            Label = "Total",
            Placement = FastTreeDataGridAggregatePlacement.GroupAndGrid,
        });
        grid.AggregateDescriptors.Add(CreateSumDescriptor(CountryNode.KeyPopulation));
        grid.AggregateDescriptors.Add(CreateSumDescriptor(CountryNode.KeyArea));
        grid.AggregateDescriptors.Add(CreateSumDescriptor(CountryNode.KeyGdp));

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Country",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CountryNode.KeyName,
            IsHierarchy = true,
            CanUserSort = true,
            FilterPlaceholder = "Filter country",
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Region",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CountryNode.KeyRegion,
            CanUserSort = true,
            FilterPlaceholder = "Filter region",
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Population",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = CountryNode.KeyPopulation,
            CanUserSort = true,
            FilterPlaceholder = "Filter population",
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Area",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CountryNode.KeyArea,
            CanUserSort = true,
            FilterPlaceholder = "Filter area",
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "GDP",
            SizingMode = ColumnSizingMode.Star,
            ValueKey = CountryNode.KeyGdp,
            CanUserSort = true,
            FilterPlaceholder = "Filter GDP",
        });
    }

    private void OnCountriesSortRequested(object? sender, FastTreeDataGridSortEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var handled = viewModel.Countries.ApplySort(e.Descriptions);

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

    private static FastTreeDataGridAggregateDescriptor CreateSumDescriptor(string columnKey)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = columnKey,
            Placement = FastTreeDataGridAggregatePlacement.GroupAndGrid,
            Aggregator = rows =>
            {
                long sum = 0;
                foreach (var row in rows)
                {
                    if (row.Item is not CountryNode node || node.IsGroup)
                    {
                        continue;
                    }

                    sum += columnKey switch
                    {
                        CountryNode.KeyPopulation => node.Population ?? 0,
                        CountryNode.KeyArea => node.Area ?? 0,
                        CountryNode.KeyGdp => node.Gdp ?? 0,
                        _ => 0,
                    };
                }

                return sum;
            },
            Formatter = value => value is long l ? l.ToString("N0", CultureInfo.CurrentCulture) : value?.ToString(),
        };
    }

    private void CountriesGrid_OnRowReordering(object? sender, FastTreeDataGridRowReorderingEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var countries = vm.Countries;
        if (countries.ContainsGroups(e.Request.SourceIndices))
        {
            e.Cancel = true;
            countries.NotifyReorderCancelled();
        }
        else
        {
            countries.ResetReorderStatus();
        }
    }

    private void CountriesGrid_OnRowReordered(object? sender, FastTreeDataGridRowReorderedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (e.Result.Success)
        {
            vm.Countries.NotifyReorderCompleted(e.Result.NewIndices);
        }
        else
        {
            vm.Countries.NotifyReorderCancelled();
        }
    }
}
