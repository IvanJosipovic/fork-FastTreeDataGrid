using System;
using Avalonia.Controls;
using FastTreeDataGrid.Demo.ViewModels;

namespace FastTreeDataGrid.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Dispose();
        }
    }
}
