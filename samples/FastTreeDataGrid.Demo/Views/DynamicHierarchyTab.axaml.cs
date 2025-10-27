using System;
using System.Threading;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crud;

namespace FastTreeDataGrid.Demo.Views;

public partial class DynamicHierarchyTab : UserControl
{
    public DynamicHierarchyTab()
    {
        InitializeComponent();
        CrudGridEditingHelper.Attach(DynamicHierarchyGrid);
    }

    private async void OnDynamicHierarchyCellEditCommitted(object? sender, FastTreeDataGridCellEditCommittedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel shell)
        {
            return;
        }

        var context = shell.DynamicHierarchy;
        if (context is null || e.Row.Item is not DynamicCrudNode node)
        {
            return;
        }

        try
        {
            await context.CommitEditAsync(node, CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
