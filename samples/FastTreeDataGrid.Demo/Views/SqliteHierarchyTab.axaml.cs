using System;
using System.Threading;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crud;

namespace FastTreeDataGrid.Demo.Views;

public partial class SqliteHierarchyTab : UserControl
{
    public SqliteHierarchyTab()
    {
        InitializeComponent();
        CrudGridEditingHelper.Attach(HierarchyGrid);
    }

    private async void OnHierarchyCellEditCommitted(object? sender, FastTreeDataGridCellEditCommittedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel shell)
        {
            return;
        }

        var context = shell.SqliteHierarchy;
        if (context is null || e.Row.Item is not SqliteCrudNode node)
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
