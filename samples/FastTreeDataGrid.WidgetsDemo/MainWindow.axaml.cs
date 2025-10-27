using Avalonia.Controls;
using FastTreeDataGrid.WidgetsDemo.ViewModels;

namespace FastTreeDataGrid.WidgetsDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
