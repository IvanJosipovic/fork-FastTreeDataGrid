using Avalonia.Controls;
using FastTreeDataGrid.DataSourcesDemo.ViewModels;

namespace FastTreeDataGrid.DataSourcesDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Dispose();
        }
    }
}
