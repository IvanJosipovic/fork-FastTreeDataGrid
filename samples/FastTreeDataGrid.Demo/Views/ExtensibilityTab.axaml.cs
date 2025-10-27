using System;
using System.Threading;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Demo.ViewModels;
using FastTreeDataGrid.Demo.ViewModels.Extensibility;

namespace FastTreeDataGrid.Demo.Views;

public partial class ExtensibilityTab : UserControl
{
    public ExtensibilityTab()
    {
        InitializeComponent();
    }

    private async void OnCellEditCommitted(object? sender, FastTreeDataGridCellEditCommittedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel shell)
        {
            return;
        }

        var context = shell.Extensibility;
        if (context is null || e.Row.Item is not InventoryRowValueProvider provider)
        {
            return;
        }

        try
        {
            await context.CommitEditAsync(provider, CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
