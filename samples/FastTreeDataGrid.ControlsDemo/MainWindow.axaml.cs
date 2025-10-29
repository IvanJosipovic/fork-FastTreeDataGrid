using System;
using Avalonia.Controls;
using FastTreeDataGrid.ControlsDemo.ViewModels;

namespace FastTreeDataGrid.ControlsDemo;

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
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
