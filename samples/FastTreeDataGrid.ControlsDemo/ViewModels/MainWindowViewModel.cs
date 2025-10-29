using System;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        DataGridPage = new DataGridPageViewModel();
        DataGridAdapterPage = new DataGridAdapterPageViewModel();
        ListBoxPage = new ListBoxPageViewModel();
        ListBoxAdapterPage = new ListBoxAdapterPageViewModel();
        TreeViewPage = new TreeViewPageViewModel();
        ItemsControlPage = new ItemsControlPageViewModel();
    }

    public DataGridPageViewModel DataGridPage { get; }

    public DataGridAdapterPageViewModel DataGridAdapterPage { get; }

    public ListBoxPageViewModel ListBoxPage { get; }

    public ListBoxAdapterPageViewModel ListBoxAdapterPage { get; }

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
