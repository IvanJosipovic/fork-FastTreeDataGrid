using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.ExcelLikeDemo.ViewModels;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;
using Avalonia.Platform.Storage;

namespace FastTreeDataGrid.ExcelLikeDemo;

public partial class MainWindow : Window
{
    private readonly FastTreeDataGridCellSelectionModel _cellSelectionModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        Grid.SelectionModel = _cellSelectionModel;
        Grid.SelectionMode = FastTreeDataGridSelectionMode.Extended;
        _cellSelectionModel.SelectionChanged += OnSelectionChanged;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            ApplyWorksheet(vm.ActiveWorksheet);
        }
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private GridControl Grid => WorksheetGrid ?? throw new InvalidOperationException("Worksheet grid not initialized.");

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel vm)
        {
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.ActiveWorksheet), StringComparison.Ordinal))
        {
            ApplyWorksheet(vm.ActiveWorksheet);
        }
    }

    private void ApplyWorksheet(WorksheetViewModel? worksheet)
    {
        Grid.Columns.Clear();

        if (worksheet is null)
        {
            Grid.ItemsSource = null;
            return;
        }

        foreach (var column in worksheet.Columns)
        {
            Grid.Columns.Add(column);
        }

        Grid.VirtualizationSettings = worksheet.VirtualizationSettings;
        Grid.ItemsSource = worksheet.Source;

        // Reset selection to the first data cell once the grid updates.
        Dispatcher.UIThread.Post(() =>
        {
            _cellSelectionModel.Clear();
            if (worksheet.RowCount > 0)
            {
                _cellSelectionModel.SelectSingle(0);
            }
        });
    }

    private void OnSelectionChanged(object? sender, FastTreeDataGridSelectionChangedEventArgs e)
    {
        if (ViewModel is null || ViewModel.ActiveWorksheet is null)
        {
            return;
        }

        var primary = e.PrimaryCell;
        if (!primary.IsValid)
        {
            ViewModel.UpdateActiveCell(-1, -1);
            return;
        }

        var descriptors = ViewModel.ActiveWorksheet.ColumnDescriptors;
        if (primary.ColumnIndex < 0 || primary.ColumnIndex >= descriptors.Count)
        {
            return;
        }

        var descriptor = descriptors[primary.ColumnIndex];
        if (descriptor.IsRowHeader)
        {
            var firstDataDescriptor = descriptors.FirstOrDefault(static d => !d.IsRowHeader);
            if (firstDataDescriptor is null)
            {
                return;
            }

            ViewModel.UpdateActiveCell(primary.RowIndex, firstDataDescriptor.ColumnIndex ?? 0);
            return;
        }

        var columnIndex = descriptor.ColumnIndex ?? 0;
        ViewModel.UpdateActiveCell(primary.RowIndex, columnIndex);
    }

    private async void OnOpenWorkbookClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || StorageProvider is null)
        {
            return;
        }

        var options = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Open Excel Workbook",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Excel Workbook (*.xlsx)")
                {
                    Patterns = new[] { "*.xlsx" },
                    AppleUniformTypeIdentifiers = new[] { "org.openxmlformats.spreadsheetml.sheet" },
                    MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                },
                FilePickerFileTypes.All,
            },
        };

        var files = await StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            return;
        }

        var file = files[0];
        var path = await EnsureLocalPathAsync(file);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        await ViewModel.LoadWorkbookAsync(path);
    }

    private void OnLoadSampleClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.LoadSampleWorkbook();
    }

    private static async Task<string?> EnsureLocalPathAsync(IStorageFile file)
    {
        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrEmpty(localPath))
        {
            return localPath;
        }

        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".xlsx";
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"FastTreeDataGrid_{Guid.NewGuid():N}{extension}");
        await using var source = await file.OpenReadAsync();
        await using var destination = File.Create(tempPath);
        await source.CopyToAsync(destination);
        return tempPath;
    }
}
