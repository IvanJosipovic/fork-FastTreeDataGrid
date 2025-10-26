using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class CountriesControlsTab : UserControl
{
    public CountriesControlsTab()
    {
        InitializeComponent();
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
}
