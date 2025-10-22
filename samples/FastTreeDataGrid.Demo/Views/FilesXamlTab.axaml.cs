using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class FilesXamlTab : UserControl
{
    public FilesXamlTab()
    {
        InitializeComponent();
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
}
