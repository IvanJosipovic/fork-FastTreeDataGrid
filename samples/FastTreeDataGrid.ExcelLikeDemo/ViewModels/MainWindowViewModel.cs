using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.ExcelLikeDemo.Models;
using FastTreeDataGrid.ExcelLikeDemo.Services;

namespace FastTreeDataGrid.ExcelLikeDemo.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ExcelWorkbookLoader _loader = new();
    private WorkbookModel? _workbook;
    private WorksheetViewModel? _activeWorksheet;
    private WorksheetCellModel? _activeCell;
    private string _formulaText = string.Empty;
    private string _activeCellReference = "A1";
    private string _statusMessage = "Ready";
    private string _workbookTitle = "Workbook";
    private bool _isBusy;

    public MainWindowViewModel()
    {
        Worksheets = new ObservableCollection<WorksheetViewModel>();
        ApplyFormulaCommand = new RelayCommand(_ => ApplyFormula(), _ => ActiveCell is not null);
        ClearCellCommand = new RelayCommand(_ => ClearActiveCell(), _ => ActiveCell is not null);

        // Seed with sample data to showcase virtualization immediately.
        SetWorkbook(ExcelSampleWorkbookFactory.CreateSampleWorkbook());
        StatusMessage = "Sample workbook generated.";
    }

    public ObservableCollection<WorksheetViewModel> Worksheets { get; }

    public WorksheetViewModel? ActiveWorksheet
    {
        get => _activeWorksheet;
        set
        {
            if (SetProperty(ref _activeWorksheet, value))
            {
                OnActiveWorksheetChanged();
            }
        }
    }

    public WorksheetCellModel? ActiveCell
    {
        get => _activeCell;
        private set
        {
            if (SetProperty(ref _activeCell, value))
            {
                FormulaText = value?.EditText ?? string.Empty;
                ActiveCellReference = value?.Address ?? "—";
                ApplyFormulaCommand.RaiseCanExecuteChanged();
                ClearCellCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FormulaText
    {
        get => _formulaText;
        set => SetProperty(ref _formulaText, value);
    }

    public string ActiveCellReference
    {
        get => _activeCellReference;
        private set => SetProperty(ref _activeCellReference, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string WorkbookTitle
    {
        get => _workbookTitle;
        private set => SetProperty(ref _workbookTitle, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public RelayCommand ApplyFormulaCommand { get; }

    public RelayCommand ClearCellCommand { get; }

    public bool HasWorkbook => Worksheets.Count > 0;

    public async Task LoadWorkbookAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Loading workbook...";
            var workbook = await _loader.LoadAsync(filePath, cancellationToken);
            SetWorkbook(workbook);
            StatusMessage = $"Loaded '{workbook.Name}' ({workbook.Worksheets.Count} worksheet(s)).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load workbook: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SetWorkbook(WorkbookModel workbook)
    {
        _workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
        WorkbookTitle = workbook.Name;

        Worksheets.Clear();
        foreach (var sheet in workbook.Worksheets)
        {
            Worksheets.Add(new WorksheetViewModel(sheet));
        }

        ActiveWorksheet = Worksheets.FirstOrDefault();
        StatusMessage = $"Workbook '{workbook.Name}' ready.";
    }

    public void UpdateActiveCell(int rowIndex, int columnIndex)
    {
        if (ActiveWorksheet is null)
        {
            ActiveCell = null;
            return;
        }

        if ((uint)rowIndex >= (uint)ActiveWorksheet.RowCount)
        {
            ActiveCell = null;
            return;
        }

        var cell = ActiveWorksheet.TryGetCell(rowIndex, columnIndex);
        ActiveCell = cell ?? ActiveWorksheet.EnsureCell(rowIndex, columnIndex);
    }

    private void OnActiveWorksheetChanged()
    {
        if (ActiveWorksheet is null)
        {
            ActiveCell = null;
            return;
        }

        ActiveCell = ActiveWorksheet.TryGetCell(0, 0);
        StatusMessage = $"Worksheet '{ActiveWorksheet.Name}' ready.";
    }

    private void ApplyFormula()
    {
        if (ActiveCell is null)
        {
            return;
        }

        ActiveCell.EditText = FormulaText;
        StatusMessage = $"Cell {ActiveCell.Address} updated.";
    }

    private void ClearActiveCell()
    {
        if (ActiveCell is null)
        {
            return;
        }

        ActiveCell.EditText = string.Empty;
        FormulaText = string.Empty;
        StatusMessage = $"Cell {ActiveCell.Address} cleared.";
    }

    public void LoadSampleWorkbook()
    {
        SetWorkbook(ExcelSampleWorkbookFactory.CreateSampleWorkbook());
        StatusMessage = "Sample workbook generated.";
    }
}
