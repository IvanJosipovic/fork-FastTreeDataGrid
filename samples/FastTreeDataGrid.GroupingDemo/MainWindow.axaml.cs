using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.GroupingDemo.Adapters;
using FastTreeDataGrid.GroupingDemo.ViewModels;

namespace FastTreeDataGrid.GroupingDemo;

public partial class MainWindow : Window
{
    private bool _gridInitialized;
    private readonly List<FastTreeDataGridColumn> _configuredColumns = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ConfigureGridIfNeeded();
    }

    private void ConfigureGridIfNeeded()
    {
        if (_gridInitialized || DataContext is not MainWindowViewModel vm || GroupingGrid is null)
        {
            return;
        }

        _gridInitialized = true;

        ConfigureGrid(vm);

        if (vm.Showcase.SelectedPreset is GroupPreset preset)
        {
            ApplyPreset(preset, vm);
        }
    }

    private void ConfigureGrid(MainWindowViewModel vm)
    {
        _configuredColumns.Clear();
        GroupingGrid.Columns.Clear();
        GroupingGrid.AggregateDescriptors.Clear();
        GroupingGrid.GroupDescriptors.Clear();

        foreach (var descriptor in vm.Showcase.AggregateDescriptors)
        {
            if (string.IsNullOrEmpty(descriptor.ColumnKey))
            {
                continue;
            }

            var formatter = descriptor.Formatter ?? (value => value?.ToString() ?? string.Empty);
            GroupingGrid.AggregateDescriptors.Add(SalesRecord.CreateSumDescriptor(descriptor.ColumnKey!, descriptor.Placement, formatter));
        }

        AddColumn(CreateTextColumn("Region", SalesRecord.KeyRegion, ColumnSizingMode.Star));
        AddColumn(CreateTextColumn("Category", SalesRecord.KeyCategory, ColumnSizingMode.Star));
        AddColumn(CreateTextColumn("Subcategory", SalesRecord.KeySubcategory, ColumnSizingMode.Star));
        AddColumn(CreateProductColumn());
        AddColumn(CreateQuarterColumn());
        AddColumn(CreateUnitsColumn());
        AddColumn(CreateRevenueColumn());

        void AddColumn(FastTreeDataGridColumn column)
        {
            _configuredColumns.Add(column);
            GroupingGrid.Columns.Add(column);
        }
    }

    private static FastTreeDataGridColumn CreateTextColumn(string header, string valueKey, ColumnSizingMode sizingMode)
    {
        return new FastTreeDataGridColumn
        {
            Header = header,
            ValueKey = valueKey,
            SizingMode = sizingMode,
            CanUserSort = true,
            CanUserFilter = true,
            CanUserGroup = true,
            GroupAdapter = new FastTreeDataGridValueGroupAdapter(valueKey),
            FilterPlaceholder = $"Filter {header.ToLowerInvariant()}",
        };
    }

    private static FastTreeDataGridColumn CreateQuarterColumn()
    {
        return new FastTreeDataGridColumn
        {
            Header = "Quarter",
            ValueKey = SalesRecord.KeyQuarter,
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            CanUserSort = true,
            CanUserFilter = true,
            CanUserGroup = true,
            GroupAdapter = new FastTreeDataGridValueGroupAdapter(SalesRecord.KeyQuarter),
            FilterPlaceholder = "Filter quarter",
        };
    }

    private static FastTreeDataGridColumn CreateUnitsColumn()
    {
        var column = new FastTreeDataGridColumn
        {
            Header = "Units Sold",
            ValueKey = SalesRecord.KeyUnitsSold,
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            CanUserSort = true,
            CanUserFilter = true,
            CanUserGroup = true,
            FilterPlaceholder = "Filter units",
            GroupAdapter = new FastTreeDataGridValueGroupAdapter(SalesRecord.KeyUnitsSold),
        };

        column.AggregateDescriptors.Add(SalesRecord.CreateSumDescriptor(SalesRecord.KeyUnitsSold, FastTreeDataGridAggregatePlacement.GroupAndGrid, value => value?.ToString() ?? string.Empty));
        return column;
    }

    private static FastTreeDataGridColumn CreateProductColumn()
    {
        return new FastTreeDataGridColumn
        {
            Header = "Product",
            ValueKey = SalesRecord.KeyProduct,
            SizingMode = ColumnSizingMode.Star,
            CanUserSort = true,
            CanUserFilter = true,
            CanUserGroup = true,
            FilterPlaceholder = "Filter products",
            GroupAdapter = new FastTreeDataGridValueGroupAdapter(SalesRecord.KeyProduct),
            WidgetFactory = (provider, item) =>
            {
                var badge = new BadgeWidget
                {
                    Padding = 6,
                    CornerRadius = new Avalonia.CornerRadius(10),
                    BackgroundBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(0xFFE0ECFF),
                    Foreground = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(0xFF1F3B7A),
                };

                if (provider is not null)
                {
                    badge.Key = SalesRecord.KeyProduct;
                }

                return badge;
            },
        };
    }

    private static FastTreeDataGridColumn CreateRevenueColumn()
    {
        var column = new FastTreeDataGridColumn
        {
            Header = "Revenue",
            ValueKey = SalesRecord.KeyRevenue,
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 150,
            CanUserSort = true,
            CanUserFilter = true,
            CanUserGroup = true,
            FilterPlaceholder = "Filter revenue",
            GroupAdapter = new RevenueBucketGroupAdapter(),
        };

        column.AggregateDescriptors.Add(SalesRecord.CreateSumDescriptor(SalesRecord.KeyRevenue, FastTreeDataGridAggregatePlacement.GroupAndGrid, SalesRecord.FormatCurrency));
        return column;
    }

    private void ApplyPreset(GroupPreset preset, MainWindowViewModel vm)
    {
        if (GroupingGrid is null)
        {
            return;
        }

        vm.Showcase.GroupDescriptors.Clear();
        GroupingGrid.GroupDescriptors.Clear();

        foreach (var level in preset.Levels)
        {
            var column = _configuredColumns.FirstOrDefault(c => string.Equals(c.ValueKey, level.ColumnKey, StringComparison.Ordinal));
            if (column is null)
            {
                continue;
            }

            var descriptor = new FastTreeDataGridGroupDescriptor
            {
                ColumnKey = column.ValueKey,
                Adapter = column.GroupAdapter ?? new FastTreeDataGridValueGroupAdapter(column.ValueKey),
                SortDirection = level.Direction,
                Comparer = column.GroupAdapter?.Comparer,
                IsExpanded = true,
            };

            descriptor.Properties["ColumnHeader"] = column.Header?.ToString();
            descriptor.Properties["ColumnReference"] = column;

            foreach (var aggregate in column.AggregateDescriptors)
            {
                descriptor.AggregateDescriptors.Add(CloneAggregate(aggregate));
            }

            vm.Showcase.GroupDescriptors.Add(descriptor);
            GroupingGrid.GroupDescriptors.Add(descriptor);
        }
    }

    private static FastTreeDataGridAggregateDescriptor CloneAggregate(FastTreeDataGridAggregateDescriptor source)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = source.ColumnKey,
            Placement = source.Placement,
            Aggregator = source.Aggregator,
            Formatter = source.Formatter,
            Label = source.Label,
            Provider = source.Provider,
        };
    }

    private void OnApplyPresetClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.Showcase.SelectedPreset is not GroupPreset preset)
        {
            return;
        }

        ApplyPreset(preset, vm);
    }

    private void OnClearGroupingClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || GroupingGrid is null)
        {
            return;
        }

        vm.Showcase.GroupDescriptors.Clear();
        GroupingGrid.GroupDescriptors.Clear();
    }

    private void OnSaveLayoutClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || GroupingGrid is null)
        {
            return;
        }

        var layout = GroupingGrid.GetGroupingLayout();
        vm.Showcase.SaveLayout(layout);
    }

    private void OnRestoreLayoutClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || GroupingGrid is null)
        {
            return;
        }

        var layout = vm.Showcase.GetSavedLayout();
        if (layout is null)
        {
            return;
        }

        GroupingGrid.ApplyGroupingLayout(layout);

        vm.Showcase.GroupDescriptors.Clear();
        foreach (var descriptor in GroupingGrid.GroupDescriptors)
        {
            vm.Showcase.GroupDescriptors.Add(descriptor);
        }
    }

    private void OnClearSavedLayoutClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.Showcase.ClearSavedLayout();
    }
}
