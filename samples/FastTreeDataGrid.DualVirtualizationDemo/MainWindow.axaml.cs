using System.Collections.Generic;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.DualVirtualizationDemo.ViewModels;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.DualVirtualizationDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        ConfigureGridColumns(viewModel.DualVirtualization.ColumnDefinitions);
    }

    private void ConfigureGridColumns(IReadOnlyList<MatrixVirtualizationColumnDefinition> definitions)
    {
        if (this.FindControl<GridControl>("MatrixGrid") is not { } grid)
        {
            return;
        }

        grid.Columns.Clear();
        foreach (var definition in definitions)
        {
            var column = new FastTreeDataGridColumn
            {
                Header = definition.Header,
                ValueKey = definition.ValueKey,
                SizingMode = definition.SizingMode,
                PixelWidth = definition.PixelWidth,
                MinWidth = definition.MinWidth,
                MaxWidth = definition.MaxWidth,
                PinnedPosition = definition.PinnedPosition,
            };

            grid.Columns.Add(column);
        }
    }
}
