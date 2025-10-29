using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.ExcelDemo.Models;
using FastTreeDataGrid.ExcelDemo.Services;
using FastTreeDataGrid.ExcelDemo.ViewModels.Grid;

namespace FastTreeDataGrid.ExcelDemo.ViewModels.Pivot;

public sealed class ExcelPivotViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    private readonly IReadOnlyList<SalesRecord> _records;
    private readonly IReadOnlyList<MeasureOption> _measures;
    private readonly IReadOnlyList<FormulaDefinition> _formulas;
    private readonly PivotEngine _pivotEngine;
    private readonly PowerFxFormulaEvaluator _formulaEvaluator;
    private readonly List<ExcelColumnDescriptor> _activeDescriptors = new();

    private DimensionOption _selectedRowDimension;
    private DimensionOption _selectedColumnDimension;

    private bool _includeSales = true;
    private bool _includeCost = true;
    private bool _includeUnits = true;
    private bool _includeProfit = true;
    private bool _includeMargin = true;
    private bool _includeAveragePrice = true;
    private bool _powerFxEnabled = true;

    private PivotResult? _currentResult;
    private IReadOnlyList<FastTreeDataGridColumn> _columns = Array.Empty<FastTreeDataGridColumn>();
    private IFastTreeDataGridSource? _source;
    private int _rowCount;
    private int _columnCount;

    public ExcelPivotViewModel()
    {
        _records = SalesDataGenerator.Create(10_000);

        _measures = new List<MeasureOption>
        {
            new("Sales", "Revenue", record => record.Sales, value => value.ToString("C0", DisplayCulture), "Sum of revenue"),
            new("Cost", "Cost", record => record.Cost, value => value.ToString("C0", DisplayCulture), "Sum of cost"),
            new("Units", "Units", record => record.Units, value => value.ToString("N0", CultureInfo.InvariantCulture), "Sum of units"),
        };

        _formulas = new List<FormulaDefinition>
        {
            new("Profit", "Profit", "Sales - Cost", value => value.ToString("C0", DisplayCulture), "Revenue minus cost using Power Fx"),
            new("Margin", "Margin %", "If(Sales = 0, Blank(), (Sales - Cost) / Sales)", value => value.ToString("P1", CultureInfo.InvariantCulture), "Margin percentage"),
            new("AveragePrice", "Avg Price", "If(Units = 0, Blank(), Sales / Units)", value => value.ToString("C2", DisplayCulture), "Average selling price"),
        };

        _formulaEvaluator = new PowerFxFormulaEvaluator(_measures, _formulas);
        _pivotEngine = new PivotEngine(_records, _measures, _formulas);

        RowDimensions = new ReadOnlyCollection<DimensionOption>(new List<DimensionOption>
        {
            new("region", "Region", r => r.Region, "Aggregate by geographic region"),
            new("country", "Country", r => r.Country, "Aggregate by country"),
            new("segment", "Segment", r => r.Segment, "Aggregate by market segment"),
            new("salesperson", "Salesperson", r => r.Salesperson, "Aggregate by owner"),
            new("product", "Product", r => r.Product, "Aggregate by SKU"),
            new("region-product", "Region · Product", r => $"{r.Region} · {r.Product}", "Composite key for region and product"),
            new("transaction", "Transaction", r => $"TX-{r.TransactionId:D5}", "Show individual transactions with no aggregation"),
        });

        ColumnDimensions = new ReadOnlyCollection<DimensionOption>(new List<DimensionOption>
        {
            new("year", "Year", r => r.Date.Year.ToString(CultureInfo.InvariantCulture), "Pivot columns by fiscal year"),
            new("quarter", "Quarter", r => $"{r.Date.Year} Q{((r.Date.Month - 1) / 3) + 1}", "Pivot columns by quarter"),
            new("month", "Month", r => r.Date.ToString("yyyy MMM", CultureInfo.InvariantCulture), "Pivot columns by month"),
            new("segment", "Segment", r => r.Segment, "Pivot columns by market segment"),
            new("salesperson", "Salesperson", r => r.Salesperson, "Pivot columns by owner"),
            new("region", "Region", r => r.Region, "Pivot columns by region"),
        });

        VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 256,
            PrefetchRadius = 3,
            MaxPages = 64,
            MaxConcurrentLoads = 6,
            ResetThrottleDelayMilliseconds = 80,
        };

        _selectedRowDimension = RowDimensions[^1];
        _selectedColumnDimension = ColumnDimensions[0];

        RebuildPivot();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? ColumnsChanged;

    public IReadOnlyList<DimensionOption> RowDimensions { get; }

    public IReadOnlyList<DimensionOption> ColumnDimensions { get; }

    public FastTreeDataGridVirtualizationSettings VirtualizationSettings { get; }

    public DimensionOption SelectedRowDimension
    {
        get => _selectedRowDimension;
        set
        {
            if (value is null || ReferenceEquals(_selectedRowDimension, value))
            {
                return;
            }

            _selectedRowDimension = value;
            OnPropertyChanged();
            RebuildPivot();
        }
    }

    public DimensionOption SelectedColumnDimension
    {
        get => _selectedColumnDimension;
        set
        {
            if (value is null || ReferenceEquals(_selectedColumnDimension, value))
            {
                return;
            }

            _selectedColumnDimension = value;
            OnPropertyChanged();
            RebuildPivot();
        }
    }

    public bool IncludeSales
    {
        get => _includeSales;
        set
        {
            if (SetProperty(ref _includeSales, value))
            {
                RebuildColumns();
            }
        }
    }

    public bool IncludeCost
    {
        get => _includeCost;
        set
        {
            if (SetProperty(ref _includeCost, value))
            {
                RebuildColumns();
            }
        }
    }

    public bool IncludeUnits
    {
        get => _includeUnits;
        set
        {
            if (SetProperty(ref _includeUnits, value))
            {
                RebuildColumns();
            }
        }
    }

    public bool IncludeProfit
    {
        get => _includeProfit;
        set
        {
            if (SetProperty(ref _includeProfit, value))
            {
                RebuildColumns();
            }
        }
    }

    public bool IncludeMargin
    {
        get => _includeMargin;
        set
        {
            if (SetProperty(ref _includeMargin, value))
            {
                RebuildColumns();
            }
        }
    }

    public bool IncludeAveragePrice
    {
        get => _includeAveragePrice;
        set
        {
            if (SetProperty(ref _includeAveragePrice, value))
            {
                RebuildColumns();
            }
        }
    }

    public IFastTreeDataGridSource? Source
    {
        get => _source;
        private set => SetProperty(ref _source, value);
    }

    public IReadOnlyList<FastTreeDataGridColumn> Columns
    {
        get => _columns;
        private set => SetProperty(ref _columns, value);
    }

    public int RowCount
    {
        get => _rowCount;
        private set => SetProperty(ref _rowCount, value);
    }

    public int ColumnCount
    {
        get => _columnCount;
        private set => SetProperty(ref _columnCount, value);
    }

    public bool PowerFxEnabled
    {
        get => _powerFxEnabled;
        set
        {
            if (SetProperty(ref _powerFxEnabled, value) && Source is ExcelVirtualizationSource excelSource)
            {
                excelSource.SetPowerFxEnabled(value);
            }
        }
    }

    private void RebuildPivot()
    {
        _currentResult = _pivotEngine.Build(_selectedRowDimension, _selectedColumnDimension);
        RebuildColumns();
    }

    private void RebuildColumns()
    {
        if (_currentResult is null)
        {
            return;
        }

        var descriptors = BuildDescriptors(_currentResult);
        _activeDescriptors.Clear();
        _activeDescriptors.AddRange(descriptors);

        var columnList = new List<FastTreeDataGridColumn>(_activeDescriptors.Count);
        foreach (var descriptor in _activeDescriptors)
        {
            columnList.Add(CreateColumn(descriptor));
        }

        Columns = new ReadOnlyCollection<FastTreeDataGridColumn>(columnList);
        Source = new ExcelVirtualizationSource(_currentResult, new ReadOnlyCollection<ExcelColumnDescriptor>(_activeDescriptors.ToList()), _formulaEvaluator, _powerFxEnabled);
        RowCount = _currentResult.RowCount;
        ColumnCount = _activeDescriptors.Count;

        ColumnsChanged?.Invoke(this, EventArgs.Empty);
    }

    private List<ExcelColumnDescriptor> BuildDescriptors(PivotResult result)
    {
        var descriptors = new List<ExcelColumnDescriptor>();

        descriptors.Add(new ExcelColumnDescriptor
        {
            Header = "#",
            ValueKey = ExcelPivotColumns.RowIndex,
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 70,
            MinWidth = 60,
            Alignment = TextAlignment.Right,
            IsNumeric = true,
            Role = ExcelColumnRole.RowIndex,
        });

        descriptors.Add(new ExcelColumnDescriptor
        {
            Header = _selectedRowDimension.DisplayName,
            ValueKey = ExcelPivotColumns.RowLabel,
            SizingMode = ColumnSizingMode.Star,
            MinWidth = 200,
            StarValue = 2,
            Alignment = TextAlignment.Left,
            Role = ExcelColumnRole.RowHeader,
        });

        var activeMeasures = GetActiveMeasures();
        var activeFormulas = GetActiveFormulas();

        for (var columnIndex = 0; columnIndex < result.ColumnCount; columnIndex++)
        {
            var columnHeader = result.ColumnLabels[columnIndex];

            foreach (var (measure, measureIndex) in activeMeasures)
            {
                descriptors.Add(new ExcelColumnDescriptor
                {
                    Header = $"{columnHeader} · {measure.DisplayName}",
                    ValueKey = ExcelPivotColumns.BuildMeasureCellKey(columnIndex, measure.Key),
                    SizingMode = ColumnSizingMode.Pixel,
                    PixelWidth = 140,
                    MinWidth = 120,
                    Alignment = TextAlignment.Right,
                    IsNumeric = true,
                    Role = ExcelColumnRole.MeasureCell,
                    ColumnIndex = columnIndex,
                    Measure = measure,
                    MeasureIndex = measureIndex,
                });
            }

            foreach (var (formula, formulaIndex) in activeFormulas)
            {
                descriptors.Add(new ExcelColumnDescriptor
                {
                    Header = $"{columnHeader} · {formula.DisplayName}",
                    ValueKey = ExcelPivotColumns.BuildFormulaCellKey(columnIndex, formula.Key),
                    SizingMode = ColumnSizingMode.Pixel,
                    PixelWidth = 150,
                    MinWidth = 130,
                    Alignment = TextAlignment.Right,
                    IsNumeric = true,
                    Role = ExcelColumnRole.FormulaCell,
                    ColumnIndex = columnIndex,
                    Formula = formula,
                    FormulaIndex = formulaIndex,
                });
            }
        }

        foreach (var (measure, measureIndex) in activeMeasures)
        {
            descriptors.Add(new ExcelColumnDescriptor
            {
                Header = $"Total · {measure.DisplayName}",
                ValueKey = ExcelPivotColumns.BuildRowTotalKey(measure.Key),
                SizingMode = ColumnSizingMode.Pixel,
                PixelWidth = 160,
                MinWidth = 140,
                Alignment = TextAlignment.Right,
                IsNumeric = true,
                Role = ExcelColumnRole.MeasureRowTotal,
                Measure = measure,
                MeasureIndex = measureIndex,
            });
        }

        foreach (var (formula, formulaIndex) in activeFormulas)
        {
            descriptors.Add(new ExcelColumnDescriptor
            {
                Header = $"Total · {formula.DisplayName}",
                ValueKey = ExcelPivotColumns.BuildFormulaRowTotalKey(formula.Key),
                SizingMode = ColumnSizingMode.Pixel,
                PixelWidth = 170,
                MinWidth = 150,
                Alignment = TextAlignment.Right,
                IsNumeric = true,
                Role = ExcelColumnRole.FormulaRowTotal,
                Formula = formula,
                FormulaIndex = formulaIndex,
            });
        }

        return descriptors;
    }

    private List<(MeasureOption Measure, int Index)> GetActiveMeasures()
    {
        var result = new List<(MeasureOption, int)>();
        for (var i = 0; i < _measures.Count; i++)
        {
            var measure = _measures[i];
            var include = measure.Key switch
            {
                "Sales" => _includeSales,
                "Cost" => _includeCost,
                "Units" => _includeUnits,
                _ => true,
            };

            if (include)
            {
                result.Add((measure, i));
            }
        }

        if (result.Count == 0)
        {
            _includeSales = true;
            OnPropertyChanged(nameof(IncludeSales));
            result.Add((_measures[0], 0));
        }

        return result;
    }

    private List<(FormulaDefinition Formula, int Index)> GetActiveFormulas()
    {
        var result = new List<(FormulaDefinition, int)>();
        for (var i = 0; i < _formulas.Count; i++)
        {
            var formula = _formulas[i];
            var include = formula.Key switch
            {
                "Profit" => _includeProfit,
                "Margin" => _includeMargin,
                "AveragePrice" => _includeAveragePrice,
                _ => true,
            };

            if (include)
            {
                result.Add((formula, i));
            }
        }

        return result;
    }

    private static FastTreeDataGridColumn CreateColumn(ExcelColumnDescriptor descriptor)
    {
        var column = new FastTreeDataGridColumn
        {
            Header = descriptor.Header,
            ValueKey = descriptor.ValueKey,
            SizingMode = descriptor.SizingMode,
            PixelWidth = descriptor.PixelWidth,
            MinWidth = descriptor.MinWidth,
            StarValue = descriptor.StarValue,
            CanUserSort = descriptor.Role is not ExcelColumnRole.RowIndex,
            CanUserFilter = descriptor.Role is ExcelColumnRole.MeasureCell or ExcelColumnRole.FormulaCell,
            CanUserGroup = false,
        };

        column.WidgetFactory = (_, _) => CreateCellWidget(descriptor);
        return column;
    }

    private static Widget CreateCellWidget(ExcelColumnDescriptor descriptor)
    {
        var background = descriptor.Role switch
        {
            ExcelColumnRole.RowHeader => new ImmutableSolidColorBrush(0xFFF8FBFF),
            ExcelColumnRole.MeasureRowTotal or ExcelColumnRole.FormulaRowTotal => new ImmutableSolidColorBrush(0xFFEFF4FF),
            ExcelColumnRole.FormulaCell => new ImmutableSolidColorBrush(0xFFFFF9EB),
            _ => new ImmutableSolidColorBrush(0xFFFFFFFF),
        };

        var textForeground = descriptor.Role == ExcelColumnRole.RowHeader
            ? new ImmutableSolidColorBrush(0xFF1F2933)
            : new ImmutableSolidColorBrush(0xFF202D3A);

        var widget = new FormattedTextWidget
        {
            Key = descriptor.ValueKey,
            EmSize = descriptor.Role == ExcelColumnRole.RowHeader ? 13 : 12,
            TextAlignment = descriptor.Alignment,
            Foreground = textForeground,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        var border = new BorderWidget
        {
            Padding = descriptor.Role == ExcelColumnRole.RowHeader ? new Thickness(12, 4, 12, 4) : new Thickness(10, 4, 10, 4),
            BorderBrush = new ImmutableSolidColorBrush(0xFFE1E8F0),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Background = background,
            Child = widget,
        };

        return border;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
