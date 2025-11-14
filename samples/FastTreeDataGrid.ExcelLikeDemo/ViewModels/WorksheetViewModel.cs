using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.ExcelLikeDemo.Models;

namespace FastTreeDataGrid.ExcelLikeDemo.ViewModels;

public sealed class WorksheetViewModel : ObservableObject
{
    private readonly WorksheetModel _worksheet;
    private readonly WorksheetVirtualizationSource _source;
    private readonly ReadOnlyCollection<WorksheetColumnDescriptor> _columnDescriptors;
    private readonly ReadOnlyCollection<FastTreeDataGridColumn> _columns;
    private readonly FastTreeDataGridVirtualizationSettings _virtualizationSettings;

    public WorksheetViewModel(WorksheetModel worksheet)
    {
        _worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));

        _columnDescriptors = BuildDescriptors(worksheet);
        _columns = BuildColumns(_columnDescriptors);
        _source = new WorksheetVirtualizationSource(worksheet, _columnDescriptors);
        _virtualizationSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 256,
            PrefetchRadius = 2,
            MaxPages = 64,
            MaxConcurrentLoads = 4,
            ResetThrottleDelayMilliseconds = 80,
        };
    }

    public string Name => _worksheet.Name;

    public IFastTreeDataGridSource Source => _source;

    public IReadOnlyList<WorksheetColumnDescriptor> ColumnDescriptors => _columnDescriptors;

    public IReadOnlyList<FastTreeDataGridColumn> Columns => _columns;

    public FastTreeDataGridVirtualizationSettings VirtualizationSettings => _virtualizationSettings;

    public int RowCount => _worksheet.RowCount;

    public int ColumnCount => _worksheet.ColumnCount;

    public WorksheetCellModel? TryGetCell(int rowIndex, int columnIndex)
    {
        if ((uint)rowIndex >= (uint)_worksheet.RowCount)
        {
            return null;
        }

        var row = _worksheet.Rows[rowIndex];
        return row.TryGetCell(columnIndex);
    }

    public WorksheetCellModel EnsureCell(int rowIndex, int columnIndex)
    {
        if ((uint)rowIndex >= (uint)_worksheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        var row = _worksheet.Rows[rowIndex];
        return row.GetOrAddCell(columnIndex);
    }

    private static ReadOnlyCollection<WorksheetColumnDescriptor> BuildDescriptors(WorksheetModel worksheet)
    {
        var descriptors = new List<WorksheetColumnDescriptor>(worksheet.ColumnCount + 1)
        {
            WorksheetColumnDescriptor.CreateRowHeader()
        };

        for (var columnIndex = 0; columnIndex < worksheet.ColumnCount; columnIndex++)
        {
            var header = columnIndex < worksheet.ColumnHeaders.Count
                ? worksheet.ColumnHeaders[columnIndex]
                : ExcelAddressHelper.ToColumnName(columnIndex);
            descriptors.Add(WorksheetColumnDescriptor.CreateDataColumn(columnIndex, header));
        }

        return new ReadOnlyCollection<WorksheetColumnDescriptor>(descriptors);
    }

    private static ReadOnlyCollection<FastTreeDataGridColumn> BuildColumns(IReadOnlyList<WorksheetColumnDescriptor> descriptors)
    {
        var columns = new List<FastTreeDataGridColumn>(descriptors.Count);

        foreach (var descriptor in descriptors)
        {
            columns.Add(CreateColumn(descriptor));
        }

        return new ReadOnlyCollection<FastTreeDataGridColumn>(columns);
    }

    private static FastTreeDataGridColumn CreateColumn(WorksheetColumnDescriptor descriptor)
    {
        var column = new FastTreeDataGridColumn
        {
            Header = descriptor.Header,
            ValueKey = descriptor.ValueKey,
            SizingMode = descriptor.SizingMode,
            PixelWidth = descriptor.PixelWidth,
            MinWidth = descriptor.MinWidth,
            StarValue = descriptor.StarValue,
            CanUserSort = !descriptor.IsRowHeader,
            CanUserResize = true,
            CanUserFilter = !descriptor.IsRowHeader,
            IsReadOnly = descriptor.IsRowHeader,
            PinnedPosition = descriptor.IsRowHeader ? FastTreeDataGridPinnedPosition.Left : FastTreeDataGridPinnedPosition.None,
        };

        column.WidgetFactory = (provider, item) =>
        {
            var widget = new FormattedTextWidget
            {
                Key = descriptor.ValueKey,
                EmSize = descriptor.IsRowHeader ? 12 : 13,
                TextAlignment = descriptor.IsRowHeader ? TextAlignment.Right : TextAlignment.Left,
                Trimming = TextTrimming.CharacterEllipsis,
                Margin = descriptor.IsRowHeader ? new Thickness(6, 0, 8, 0) : new Thickness(8, 0, 8, 0),
            };

            return widget;
        };

        if (!descriptor.IsRowHeader && descriptor.ColumnIndex is int columnIndex)
        {
            column.EditTemplate = CreateEditTemplate(descriptor, columnIndex);
        }

        return column;
    }

    private static IDataTemplate CreateEditTemplate(WorksheetColumnDescriptor descriptor, int columnIndex)
    {
        return new FuncDataTemplate<WorksheetRowValueProvider>((provider, scope) =>
        {
            if (provider is null)
            {
                return null;
            }

            var cell = provider.GetOrCreateCell(columnIndex);
            var textBox = new TextBox
            {
                MinWidth = descriptor.MinWidth,
                Padding = new Thickness(8, 4, 8, 4),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
            };

            textBox.Bind(TextBox.TextProperty, new Binding(nameof(WorksheetCellModel.EditText))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = cell,
            });

            textBox.AttachedToVisualTree += (_, _) => provider.NotifyEditBegan(columnIndex);
            textBox.DetachedFromVisualTree += (_, _) => provider.NotifyEditEnded(columnIndex);
            return textBox;
        });
    }
}

public sealed class WorksheetColumnDescriptor
{
    private WorksheetColumnDescriptor(
        string header,
        string valueKey,
        bool isRowHeader,
        int? columnIndex,
        ColumnSizingMode sizingMode,
        double pixelWidth,
        double minWidth,
        double starValue)
    {
        Header = header;
        ValueKey = valueKey;
        IsRowHeader = isRowHeader;
        ColumnIndex = columnIndex;
        SizingMode = sizingMode;
        PixelWidth = pixelWidth;
        MinWidth = minWidth;
        StarValue = starValue;
    }

    public string Header { get; }

    public string ValueKey { get; }

    public bool IsRowHeader { get; }

    public int? ColumnIndex { get; }

    public ColumnSizingMode SizingMode { get; }

    public double PixelWidth { get; }

    public double MinWidth { get; }

    public double StarValue { get; }

    public static WorksheetColumnDescriptor CreateRowHeader()
    {
        return new WorksheetColumnDescriptor(
            header: "#",
            valueKey: WorksheetColumnKeys.RowHeader,
            isRowHeader: true,
            columnIndex: null,
            sizingMode: ColumnSizingMode.Pixel,
            pixelWidth: 56,
            minWidth: 48,
            starValue: 1);
    }

    public static WorksheetColumnDescriptor CreateDataColumn(int columnIndex, string header)
    {
        return new WorksheetColumnDescriptor(
            header,
            WorksheetColumnKeys.CreateColumnKey(columnIndex),
            isRowHeader: false,
            columnIndex,
            sizingMode: ColumnSizingMode.Pixel,
            pixelWidth: 120,
            minWidth: 80,
            starValue: 1);
    }
}

public static class WorksheetColumnKeys
{
    private const string ColumnPrefix = "Column:";

    public const string RowHeader = "__RowHeader";

    public static string CreateColumnKey(int columnIndex) => $"{ColumnPrefix}{columnIndex}";

    public static bool TryGetColumnIndex(string key, out int columnIndex)
    {
        if (key.StartsWith(ColumnPrefix, StringComparison.Ordinal))
        {
            var span = key.AsSpan(ColumnPrefix.Length);
            if (int.TryParse(span, out columnIndex))
            {
                return true;
            }
        }

        columnIndex = -1;
        return false;
    }
}

internal sealed class WorksheetVirtualizationSource : IFastTreeDataGridSource
{
    private readonly WorksheetModel _worksheet;
    private readonly IReadOnlyList<WorksheetColumnDescriptor> _descriptors;
    private readonly FastTreeDataGridRow?[] _rowCache;
    private readonly object _cacheLock = new();

    public WorksheetVirtualizationSource(WorksheetModel worksheet, IReadOnlyList<WorksheetColumnDescriptor> descriptors)
    {
        _worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _rowCache = new FastTreeDataGridRow[_worksheet.RowCount];
    }

    public event EventHandler? ResetRequested;

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;

    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

    public int RowCount => _worksheet.RowCount;

    public bool SupportsPlaceholders => false;

    public ValueTask<int> GetRowCountAsync(System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(RowCount);
    }

    public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var end = Math.Min(request.StartIndex + request.Count, RowCount);
        var rows = new List<FastTreeDataGridRow>(end - request.StartIndex);

        for (var index = request.StartIndex; index < end; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = GetOrCreateRow(index);
            rows.Add(row);
        }

        return ValueTask.FromResult(new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null));
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, System.Threading.CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, System.Threading.CancellationToken cancellationToken)
    {
        if (request.Kind == FastTreeDataGridInvalidationKind.Full)
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        if ((uint)index >= (uint)RowCount)
        {
            row = default!;
            return false;
        }

        row = GetOrCreateRow(index);
        return true;
    }

    public bool IsPlaceholder(int index) => false;

    public FastTreeDataGridRow GetRow(int index) => GetOrCreateRow(index);

    public void ToggleExpansion(int index)
    {
        _ = index;
    }

    private FastTreeDataGridRow GetOrCreateRow(int index)
    {
        var cached = System.Threading.Volatile.Read(ref _rowCache[index]);
        if (cached is not null)
        {
            return cached;
        }

        lock (_cacheLock)
        {
            cached = _rowCache[index];
            if (cached is not null)
            {
                return cached;
            }

            var rowModel = _worksheet.Rows[index];
            var provider = new WorksheetRowValueProvider(rowModel, _descriptors, index, InvalidateRow);
            var row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
            _rowCache[index] = row;
            RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, row));
            return row;
        }
    }

    private void InvalidateRow(int rowIndex)
    {
        var request = new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, rowIndex, 1);
        Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(request));
    }
}

public sealed class WorksheetRowValueProvider : IFastTreeDataGridValueProvider, IFastTreeDataGridRowHeightAware
{
    private readonly WorksheetRowModel _row;
    private readonly IReadOnlyList<WorksheetColumnDescriptor> _descriptors;
    private readonly int _rowIndex;
    private readonly Action<int> _invalidateRow;
    private readonly Dictionary<string, WorksheetCellModel> _cellsByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<WorksheetCellModel, string> _cellKeyLookup = new();

    public WorksheetRowValueProvider(
        WorksheetRowModel row,
        IReadOnlyList<WorksheetColumnDescriptor> descriptors,
        int rowIndex,
        Action<int> invalidateRowCallback)
    {
        _row = row ?? throw new ArgumentNullException(nameof(row));
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _rowIndex = rowIndex;
        _invalidateRow = invalidateRowCallback ?? throw new ArgumentNullException(nameof(invalidateRowCallback));

        foreach (var cell in row.Cells)
        {
            RegisterCell(cell.Key, cell.Value);
        }
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public object? GetValue(object? item, string key)
    {
        if (string.Equals(key, WorksheetColumnKeys.RowHeader, StringComparison.Ordinal))
        {
            return _row.Index + 1;
        }

        if (WorksheetColumnKeys.TryGetColumnIndex(key, out var columnIndex))
        {
            var cell = GetOrCreateCell(columnIndex);
            return cell.DisplayOrEmpty;
        }

        return null;
    }

    public WorksheetCellModel GetOrCreateCell(int columnIndex)
    {
        var key = WorksheetColumnKeys.CreateColumnKey(columnIndex);
        if (_cellsByKey.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var cell = _row.GetOrAddCell(columnIndex);
        RegisterCell(columnIndex, cell);
        return cell;
    }

    public IReadOnlyDictionary<string, WorksheetCellModel> Cells => _cellsByKey;

    public void NotifyEditBegan(int columnIndex)
    {
        _ = columnIndex;
    }

    public void NotifyEditEnded(int columnIndex)
    {
        _invalidateRow(_rowIndex);
    }

    public double GetRowHeight(double defaultRowHeight)
    {
        if (_row.ExplicitHeight.HasValue)
        {
            return Math.Max(1d, _row.ExplicitHeight.Value);
        }

        var maxLines = 1;
        foreach (var descriptor in _descriptors)
        {
            if (descriptor.IsRowHeader || descriptor.ColumnIndex is null)
            {
                continue;
            }

            var cell = GetOrCreateCell(descriptor.ColumnIndex.Value);
            var text = cell.DisplayText ?? cell.FormulaText ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var estimatedLines = EstimateLines(text, descriptor.MinWidth);
            if (estimatedLines > maxLines)
            {
                maxLines = estimatedLines;
            }
        }

        var cappedLines = Math.Clamp(maxLines, 1, 8);
        return Math.Max(1d, defaultRowHeight * cappedLines);
    }

    private void RegisterCell(int columnIndex, WorksheetCellModel cell)
    {
        var key = WorksheetColumnKeys.CreateColumnKey(columnIndex);
        if (_cellsByKey.TryGetValue(key, out var existing))
        {
            existing.PropertyChanged -= OnCellPropertyChanged;
        }

        _cellsByKey[key] = cell;
        _cellKeyLookup[cell] = key;
        cell.PropertyChanged += OnCellPropertyChanged;
    }

    private void OnCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not WorksheetCellModel cell)
        {
            return;
        }

        if (!_cellKeyLookup.TryGetValue(cell, out var key))
        {
            return;
        }

        if (e.PropertyName is nameof(WorksheetCellModel.DisplayText) or nameof(WorksheetCellModel.EditText))
        {
            ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(cell, key));
            _invalidateRow(_rowIndex);
        }
    }

    private static int EstimateLines(string text, double minColumnWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        var averageCharsPerLine = Math.Max(8, (int)(minColumnWidth / 6));
        var lines = (int)Math.Ceiling(text.Length / (double)averageCharsPerLine);
        return Math.Max(1, lines);
    }
}
