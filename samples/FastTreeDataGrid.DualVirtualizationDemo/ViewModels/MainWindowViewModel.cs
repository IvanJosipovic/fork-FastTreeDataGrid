namespace FastTreeDataGrid.DualVirtualizationDemo.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel()
    {
        DualVirtualization = new DualVirtualizationViewModel();
    }

    public DualVirtualizationViewModel DualVirtualization { get; }
}
