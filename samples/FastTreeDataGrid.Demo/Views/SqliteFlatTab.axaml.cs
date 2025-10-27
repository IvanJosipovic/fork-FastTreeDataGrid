using System;
using System.Threading;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Crud;

namespace FastTreeDataGrid.Demo.Views;

public partial class SqliteFlatTab : UserControl
{
    public SqliteFlatTab()
    {
        InitializeComponent();
        CrudGridEditingHelper.Attach(SqliteFlatGrid);
    }

    private async void OnSqliteFlatCellEditCommitted(object? sender, FastTreeDataGridCellEditCommittedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel shell)
        {
            return;
        }

        var context = shell.SqliteFlat;
        if (context is null || e.Row.Item is not SqliteProductRow row)
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
