using System;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        DataGridPage = new DataGridPageViewModel();
        ListBoxPage = new ListBoxPageViewModel();
        TreeViewPage = new TreeViewPageViewModel();
        ItemsControlPage = new ItemsControlPageViewModel();
    }

    public DataGridPageViewModel DataGridPage { get; }

    public ListBoxPageViewModel ListBoxPage { get; }

    public TreeViewPageViewModel TreeViewPage { get; }

    public ItemsControlPageViewModel ItemsControlPage { get; }

    public void Dispose()
    {
        ItemsControlPage.Dispose();
        TreeViewPage.Dispose();
        ListBoxPage.Dispose();
        DataGridPage.Dispose();
    }
}
