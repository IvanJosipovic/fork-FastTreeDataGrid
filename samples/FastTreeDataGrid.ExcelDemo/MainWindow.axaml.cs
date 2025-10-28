using System;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.ExcelDemo.ViewModels;
using FastTreeDataGrid.ExcelDemo.ViewModels.Pivot;

namespace FastTreeDataGrid.ExcelDemo;

public partial class MainWindow : Window
{
    private ExcelPivotViewModel? _pivot;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_pivot is not null)
        {
            _pivot.ColumnsChanged -= OnColumnsChanged;
        }

        _pivot = (DataContext as MainWindowViewModel)?.Pivot;
        if (_pivot is not null)
        {
            if (PivotGrid is not null && PivotGrid.SelectionModel is not FastTreeDataGridCellSelectionModel)
            {
                PivotGrid.SelectionModel = new FastTreeDataGridCellSelectionModel();
            }

            _pivot.ColumnsChanged += OnColumnsChanged;
            ApplyColumns();
        }
    }

    private void OnColumnsChanged(object? sender, EventArgs e) => ApplyColumns();

    private void ApplyColumns()
    {
        if (PivotGrid is null || _pivot is null)
        {
            return;
        }

        PivotGrid.Columns.Clear();
        foreach (var column in _pivot.Columns)
        {
            PivotGrid.Columns.Add(column);
        }

        if (PivotGrid.SelectionModel is IFastTreeDataGridCellSelectionModel cellSelection && PivotGrid.Columns.Count > 0)
        {
            cellSelection.SelectCell(new FastTreeDataGridCellIndex(0, 0));
        }
    }
}
