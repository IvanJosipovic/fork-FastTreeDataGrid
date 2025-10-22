using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
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
}
