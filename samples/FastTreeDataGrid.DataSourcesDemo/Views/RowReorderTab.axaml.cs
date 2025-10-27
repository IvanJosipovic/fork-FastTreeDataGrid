using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.DataSourcesDemo.ViewModels;

namespace FastTreeDataGrid.DataSourcesDemo.Views;

public partial class RowReorderTab : UserControl
{
    public RowReorderTab()
    {
        InitializeComponent();
    }

    private void RoadmapGrid_OnRowReordering(object? sender, FastTreeDataGridRowReorderingEventArgs e)
    {
        if (DataContext is not RowReorderSamplesViewModel vm)
        {
            return;
        }

        if (vm.ContainsLocked(e.Request.SourceIndices))
        {
            e.Cancel = true;
            vm.NotifyCancelled();
        }
    }

    private void RoadmapGrid_OnRowReordered(object? sender, FastTreeDataGridRowReorderedEventArgs e)
    {
        if (DataContext is not RowReorderSamplesViewModel vm)
        {
            return;
        }

        if (e.Result.Success)
        {
            vm.NotifyCommitted(e.Result.NewIndices);
        }
        else
        {
            vm.NotifyCancelled();
        }
    }
}
