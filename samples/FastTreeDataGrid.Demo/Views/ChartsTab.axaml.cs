using Avalonia;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Demo.ViewModels.Charts;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Demo.Views;

public partial class ChartsTab : UserControl
{
    public ChartsTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("ChartsGrid") is { } grid)
        {
            ConfigureChartColumns(grid);
        }
    }

    private static void ConfigureChartColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 48;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Sample",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 220,
            MinWidth = 180,
            ValueKey = ChartSampleNode.KeyTitle,
            CanUserSort = false,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Description",
            SizingMode = ColumnSizingMode.Star,
            MinWidth = 260,
            ValueKey = ChartSampleNode.KeyDescription,
            CanUserSort = false,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Chart",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 320,
            MinWidth = 240,
            ValueKey = ChartSampleNode.KeyChart,
            CanUserSort = false,
            WidgetFactory = (_, _) => new ChartWidget
            {
                Key = ChartSampleNode.KeyChart,
                StrokeThickness = 1.6,
                Margin = new Thickness(6, 8, 6, 8),
            },
        });
    }
}
