using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Templates;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Control.Models;

public class FastTreeDataGridColumn : AvaloniaObject
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, object?>(nameof(Header));

    public static readonly StyledProperty<ColumnSizingMode> SizingModeProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, ColumnSizingMode>(nameof(SizingMode), ColumnSizingMode.Auto);

    public static readonly StyledProperty<double> PixelWidthProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, double>(nameof(PixelWidth), 120d, coerce: (_, value) => Math.Max(0d, value));

    public static readonly StyledProperty<double> StarValueProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, double>(nameof(StarValue), 1d, coerce: (_, value) => Math.Max(0d, value));

    public static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, double>(nameof(MinWidth), 40d, coerce: (_, value) => Math.Max(0d, value));

    public static readonly StyledProperty<double> MaxWidthProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, double>(nameof(MaxWidth), double.PositiveInfinity, coerce: (_, value) => value <= 0 ? double.PositiveInfinity : value);

    public static readonly StyledProperty<string?> ValueKeyProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, string?>(nameof(ValueKey));

    public static readonly StyledProperty<bool> IsHierarchyProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(IsHierarchy));

    public static readonly StyledProperty<bool> CanUserResizeProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanUserResize), true);

    public static readonly StyledProperty<bool> CanUserSortProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanUserSort));

    public static readonly StyledProperty<FastTreeDataGridSortDirection> SortDirectionProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, FastTreeDataGridSortDirection>(nameof(SortDirection), FastTreeDataGridSortDirection.None);

    public static readonly StyledProperty<bool> CanUserReorderProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanUserReorder), true);

    public static readonly StyledProperty<bool> CanUserPinProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanUserPin), true);

    public static readonly StyledProperty<bool> CanAutoSizeProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanAutoSize), true);

    public static readonly StyledProperty<FastTreeDataGridPinnedPosition> PinnedPositionProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, FastTreeDataGridPinnedPosition>(nameof(PinnedPosition), FastTreeDataGridPinnedPosition.None);

    public static readonly StyledProperty<Func<IFastTreeDataGridValueProvider?, object?, Widget?>?> WidgetFactoryProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Func<IFastTreeDataGridValueProvider?, object?, Widget?>?>(nameof(WidgetFactory));

    public static readonly StyledProperty<IWidgetTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IWidgetTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<IDataTemplate?> CellControlTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IDataTemplate?>(nameof(CellControlTemplate));

    public static readonly StyledProperty<Comparison<FastTreeDataGridRow>?> SortComparisonProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Comparison<FastTreeDataGridRow>?>(nameof(SortComparison));

    public static readonly DirectProperty<FastTreeDataGridColumn, int> SortOrderProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGridColumn, int>(nameof(SortOrder), o => o.SortOrder, (o, v) => o.SortOrder = v);

    private int _sortOrder;

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public ColumnSizingMode SizingMode
    {
        get => GetValue(SizingModeProperty);
        set => SetValue(SizingModeProperty, value);
    }

    public double PixelWidth
    {
        get => GetValue(PixelWidthProperty);
        set => SetValue(PixelWidthProperty, value);
    }

    public double StarValue
    {
        get => GetValue(StarValueProperty);
        set => SetValue(StarValueProperty, value);
    }

    public double MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    public double MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    public string? ValueKey
    {
        get => GetValue(ValueKeyProperty);
        set => SetValue(ValueKeyProperty, value);
    }

    public Func<IFastTreeDataGridValueProvider?, object?, Widget?>? WidgetFactory
    {
        get => GetValue(WidgetFactoryProperty);
        set => SetValue(WidgetFactoryProperty, value);
    }

    public IWidgetTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public IDataTemplate? CellControlTemplate
    {
        get => GetValue(CellControlTemplateProperty);
        set => SetValue(CellControlTemplateProperty, value);
    }

    public bool IsHierarchy
    {
        get => GetValue(IsHierarchyProperty);
        set => SetValue(IsHierarchyProperty, value);
    }

    public bool CanUserResize
    {
        get => GetValue(CanUserResizeProperty);
        set => SetValue(CanUserResizeProperty, value);
    }

    public bool CanUserSort
    {
        get => GetValue(CanUserSortProperty);
        set => SetValue(CanUserSortProperty, value);
    }

    public FastTreeDataGridSortDirection SortDirection
    {
        get => GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    public bool CanUserReorder
    {
        get => GetValue(CanUserReorderProperty);
        set => SetValue(CanUserReorderProperty, value);
    }

    public bool CanUserPin
    {
        get => GetValue(CanUserPinProperty);
        set => SetValue(CanUserPinProperty, value);
    }

    public bool CanAutoSize
    {
        get => GetValue(CanAutoSizeProperty);
        set => SetValue(CanAutoSizeProperty, value);
    }

    public FastTreeDataGridPinnedPosition PinnedPosition
    {
        get => GetValue(PinnedPositionProperty);
        set => SetValue(PinnedPositionProperty, value);
    }

    public int SortOrder
    {
        get => _sortOrder;
        set => SetAndRaise(SortOrderProperty, ref _sortOrder, value);
    }

    public Comparison<FastTreeDataGridRow>? SortComparison
    {
        get => GetValue(SortComparisonProperty);
        set => SetValue(SortComparisonProperty, value);
    }

    public double ActualWidth { get; internal set; }

    internal double CachedAutoWidth { get; set; } = 120d;
}
