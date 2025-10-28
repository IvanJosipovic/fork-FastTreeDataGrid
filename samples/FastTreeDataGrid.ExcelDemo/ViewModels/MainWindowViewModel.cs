using FastTreeDataGrid.ExcelDemo.ViewModels.Pivot;

namespace FastTreeDataGrid.ExcelDemo.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel()
    {
        Pivot = new ExcelPivotViewModel();
    }

    public ExcelPivotViewModel Pivot { get; }
}
