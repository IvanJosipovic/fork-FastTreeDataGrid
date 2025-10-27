using System.Globalization;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class CountriesControlsTab : UserControl
{
    public CountriesControlsTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("CountriesGrid") is { } grid)
        {
            grid.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
            {
                Label = "Total",
                Placement = FastTreeDataGridAggregatePlacement.GroupAndGrid,
            });

            grid.AggregateDescriptors.Add(CreateSumDescriptor(CountryNode.KeyPopulation));
            grid.AggregateDescriptors.Add(CreateSumDescriptor(CountryNode.KeyArea));
            grid.AggregateDescriptors.Add(CreateSumDescriptor(CountryNode.KeyGdp));
        }
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

    private void OnCountriesRowReordering(object? sender, FastTreeDataGridRowReorderingEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var countries = vm.Countries;
        if (countries.HasMixedSelection(e.Request.SourceIndices))
        {
            e.Cancel = true;
            countries.NotifyReorderCancelled();
        }
        else
        {
            countries.ResetReorderStatus();
        }
    }

    private void OnCountriesRowReordered(object? sender, FastTreeDataGridRowReorderedEventArgs e)
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
