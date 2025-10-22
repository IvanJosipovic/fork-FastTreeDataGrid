using System;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class FilesTab : UserControl
{
    public FilesTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("FilesGrid") is { } grid)
        {
            ConfigureFileColumns(grid);
        }
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
