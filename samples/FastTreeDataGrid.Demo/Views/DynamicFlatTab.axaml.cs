using System;
using System.Threading;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crud;

namespace FastTreeDataGrid.Demo.Views;

public partial class DynamicFlatTab : UserControl
{
    public DynamicFlatTab()
    {
        InitializeComponent();
        CrudGridEditingHelper.Attach(DynamicFlatGrid);
    }

    private async void OnDynamicFlatCellEditCommitted(object? sender, FastTreeDataGridCellEditCommittedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel shell)
        {
            return;
        }

        var context = shell.DynamicFlat;
        if (context is null || e.Row.Item is not DynamicFlatRow row)
        {
            return;
        }

        try
        {
            await context.CommitEditAsync(row, CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
