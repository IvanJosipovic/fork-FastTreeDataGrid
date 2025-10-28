using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Widgets;
using AvaloniaControl = Avalonia.Controls.Control;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Models;

internal enum FastTreeDataGridColumnControlRole
{
    Default,
    GroupHeader,
    GroupFooter,
}

public class FastTreeDataGridColumn : AvaloniaObject
{
    internal static readonly AttachedProperty<FastTreeDataGridColumnControlRole> ControlRoleProperty =
        AvaloniaProperty.RegisterAttached<FastTreeDataGridColumn, AvaloniaObject, FastTreeDataGridColumnControlRole>(
            "ControlRole",
            FastTreeDataGridColumnControlRole.Default);

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

    public static readonly StyledProperty<bool> CanUserFilterProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanUserFilter), true);

    public static readonly StyledProperty<string?> FilterPlaceholderProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, string?>(nameof(FilterPlaceholder), "Filter");

    public static readonly StyledProperty<Func<string?, FastTreeDataGridFilterDescriptor?>?> FilterFactoryProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Func<string?, FastTreeDataGridFilterDescriptor?>?>(nameof(FilterFactory));

    public static readonly StyledProperty<FastTreeDataGridPinnedPosition> PinnedPositionProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, FastTreeDataGridPinnedPosition>(nameof(PinnedPosition), FastTreeDataGridPinnedPosition.None);

    public static readonly StyledProperty<Func<IFastTreeDataGridValueProvider?, object?, Widget?>?> WidgetFactoryProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Func<IFastTreeDataGridValueProvider?, object?, Widget?>?>(nameof(WidgetFactory));

    public static readonly StyledProperty<IWidgetTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IWidgetTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<IDataTemplate?> CellControlTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IDataTemplate?>(nameof(CellControlTemplate));

    public static readonly StyledProperty<Func<IFastTreeDataGridValueProvider?, object?, Widget?>?> GroupHeaderWidgetFactoryProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Func<IFastTreeDataGridValueProvider?, object?, Widget?>?>(nameof(GroupHeaderWidgetFactory));

    public static readonly StyledProperty<IWidgetTemplate?> GroupHeaderTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IWidgetTemplate?>(nameof(GroupHeaderTemplate));

    public static readonly StyledProperty<IDataTemplate?> GroupHeaderControlTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IDataTemplate?>(nameof(GroupHeaderControlTemplate));

    public static readonly StyledProperty<Func<IFastTreeDataGridValueProvider?, object?, Widget?>?> GroupFooterWidgetFactoryProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Func<IFastTreeDataGridValueProvider?, object?, Widget?>?>(nameof(GroupFooterWidgetFactory));

    public static readonly StyledProperty<IWidgetTemplate?> GroupFooterTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IWidgetTemplate?>(nameof(GroupFooterTemplate));

    public static readonly StyledProperty<IDataTemplate?> GroupFooterControlTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IDataTemplate?>(nameof(GroupFooterControlTemplate));

    public static readonly StyledProperty<string?> GroupHeaderStyleKeyProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, string?>(nameof(GroupHeaderStyleKey));

    public static readonly StyledProperty<string?> GroupSummaryStyleKeyProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, string?>(nameof(GroupSummaryStyleKey));

    public static readonly StyledProperty<IDataTemplate?> EditTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IDataTemplate?>(nameof(EditTemplate));

    public static readonly StyledProperty<IFastTreeDataGridEditTemplateSelector?> EditTemplateSelectorProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IFastTreeDataGridEditTemplateSelector?>(nameof(EditTemplateSelector));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<bool> CanUserGroupProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(CanUserGroup), true);

    public static readonly StyledProperty<IFastTreeDataGridGroupAdapter?> GroupAdapterProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, IFastTreeDataGridGroupAdapter?>(nameof(GroupAdapter));

    public static readonly StyledProperty<string?> ValidationKeyProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, string?>(nameof(ValidationKey));

    public static readonly StyledProperty<Comparison<FastTreeDataGridRow>?> SortComparisonProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, Comparison<FastTreeDataGridRow>?>(nameof(SortComparison));

    public static readonly DirectProperty<FastTreeDataGridColumn, int> SortOrderProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGridColumn, int>(nameof(SortOrder), o => o.SortOrder, (o, v) => o.SortOrder = v);

    public static readonly StyledProperty<bool> IsFilterActiveProperty =
        AvaloniaProperty.Register<FastTreeDataGridColumn, bool>(nameof(IsFilterActive));

    private int _sortOrder;
    private readonly Dictionary<FastTreeDataGridColumnControlRole, Stack<AvaloniaControl>> _controlPools = new();
    private readonly Stack<FormattedTextWidget> _textWidgetPool = new();
    private readonly AvaloniaList<FastTreeDataGridAggregateDescriptor> _aggregateDescriptors = new();

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

    public Func<IFastTreeDataGridValueProvider?, object?, Widget?>? GroupHeaderWidgetFactory
    {
        get => GetValue(GroupHeaderWidgetFactoryProperty);
        set => SetValue(GroupHeaderWidgetFactoryProperty, value);
    }

    public IWidgetTemplate? GroupHeaderTemplate
    {
        get => GetValue(GroupHeaderTemplateProperty);
        set => SetValue(GroupHeaderTemplateProperty, value);
    }

    public IDataTemplate? GroupHeaderControlTemplate
    {
        get => GetValue(GroupHeaderControlTemplateProperty);
        set => SetValue(GroupHeaderControlTemplateProperty, value);
    }

    public Func<IFastTreeDataGridValueProvider?, object?, Widget?>? GroupFooterWidgetFactory
    {
        get => GetValue(GroupFooterWidgetFactoryProperty);
        set => SetValue(GroupFooterWidgetFactoryProperty, value);
    }

    public IWidgetTemplate? GroupFooterTemplate
    {
        get => GetValue(GroupFooterTemplateProperty);
        set => SetValue(GroupFooterTemplateProperty, value);
    }

    public IDataTemplate? GroupFooterControlTemplate
    {
        get => GetValue(GroupFooterControlTemplateProperty);
        set => SetValue(GroupFooterControlTemplateProperty, value);
    }

    public string? GroupHeaderStyleKey
    {
        get => GetValue(GroupHeaderStyleKeyProperty);
        set => SetValue(GroupHeaderStyleKeyProperty, value);
    }

    public string? GroupSummaryStyleKey
    {
        get => GetValue(GroupSummaryStyleKeyProperty);
        set => SetValue(GroupSummaryStyleKeyProperty, value);
    }

    public IDataTemplate? EditTemplate
    {
        get => GetValue(EditTemplateProperty);
        set => SetValue(EditTemplateProperty, value);
    }

    public IFastTreeDataGridEditTemplateSelector? EditTemplateSelector
    {
        get => GetValue(EditTemplateSelectorProperty);
        set => SetValue(EditTemplateSelectorProperty, value);
    }

    public bool IsHierarchy
    {
        get => GetValue(IsHierarchyProperty);
        set => SetValue(IsHierarchyProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool CanUserGroup
    {
        get => GetValue(CanUserGroupProperty);
        set => SetValue(CanUserGroupProperty, value);
    }

    public IFastTreeDataGridGroupAdapter? GroupAdapter
    {
        get => GetValue(GroupAdapterProperty);
        set => SetValue(GroupAdapterProperty, value);
    }

    public AvaloniaList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors => _aggregateDescriptors;

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

    public bool CanUserFilter
    {
        get => GetValue(CanUserFilterProperty);
        set => SetValue(CanUserFilterProperty, value);
    }

    public string? FilterPlaceholder
    {
        get => GetValue(FilterPlaceholderProperty);
        set => SetValue(FilterPlaceholderProperty, value);
    }

    public Func<string?, FastTreeDataGridFilterDescriptor?>? FilterFactory
    {
        get => GetValue(FilterFactoryProperty);
        set => SetValue(FilterFactoryProperty, value);
    }

    public FastTreeDataGridPinnedPosition PinnedPosition
    {
        get => GetValue(PinnedPositionProperty);
        set => SetValue(PinnedPositionProperty, value);
    }

    public string? ValidationKey
    {
        get => GetValue(ValidationKeyProperty);
        set => SetValue(ValidationKeyProperty, value);
    }

    public int SortOrder
    {
        get => _sortOrder;
        set => SetAndRaise(SortOrderProperty, ref _sortOrder, value);
    }

    public bool IsFilterActive
    {
        get => GetValue(IsFilterActiveProperty);
        internal set => SetValue(IsFilterActiveProperty, value);
    }

    public Comparison<FastTreeDataGridRow>? SortComparison
    {
        get => GetValue(SortComparisonProperty);
        set => SetValue(SortComparisonProperty, value);
    }

    public double ActualWidth { get; internal set; }

    internal double CachedAutoWidth { get; set; } = 120d;

    internal AvaloniaControl? RentControl() => RentControl(FastTreeDataGridColumnControlRole.Default);

    internal AvaloniaControl? RentControl(FastTreeDataGridColumnControlRole role)
    {
        if (_controlPools.TryGetValue(role, out var pool) && pool.Count > 0)
        {
            var control = pool.Pop();
            SetControlRole(control, role);
            return control;
        }

        return null;
    }

    internal void ReturnControl(AvaloniaControl control)
    {
        if (control is null)
        {
            return;
        }

        var role = GetControlRole(control);
        ReturnControl(control, role);
    }

    internal void ReturnControl(AvaloniaControl control, FastTreeDataGridColumnControlRole role)
    {
        if (control is null)
        {
            return;
        }

        control.DataContext = null;
        SetControlRole(control, role);

        if (!_controlPools.TryGetValue(role, out var pool))
        {
            pool = new Stack<AvaloniaControl>();
            _controlPools[role] = pool;
        }

        pool.Push(control);
    }

    internal FormattedTextWidget RentTextWidget()
    {
        return _textWidgetPool.Count > 0 ? _textWidgetPool.Pop() : new FormattedTextWidget();
    }

    internal void ReturnTextWidget(FormattedTextWidget? widget)
    {
        if (widget is null)
        {
            return;
        }

        widget.SetText(string.Empty);
        widget.Key = null;
        widget.Foreground = null;
        widget.StyleKey = null;
        widget.Margin = default;
        widget.CornerRadius = default;
        _textWidgetPool.Push(widget);
    }

    internal AvaloniaControl? RentGroupHeaderControl() => RentControl(FastTreeDataGridColumnControlRole.GroupHeader);

    internal AvaloniaControl? RentGroupFooterControl() => RentControl(FastTreeDataGridColumnControlRole.GroupFooter);

    internal static FastTreeDataGridColumnControlRole GetControlRole(AvaloniaObject target)
    {
        return target.GetValue(ControlRoleProperty);
    }

    internal static void SetControlRole(AvaloniaObject target, FastTreeDataGridColumnControlRole role)
    {
        target.SetValue(ControlRoleProperty, role);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CellControlTemplateProperty)
        {
            ClearControlPool(FastTreeDataGridColumnControlRole.Default);
        }
        else if (change.Property == GroupHeaderControlTemplateProperty)
        {
            ClearControlPool(FastTreeDataGridColumnControlRole.GroupHeader);
        }
        else if (change.Property == GroupFooterControlTemplateProperty)
        {
            ClearControlPool(FastTreeDataGridColumnControlRole.GroupFooter);
        }
    }

    private void ClearControlPool(FastTreeDataGridColumnControlRole role)
    {
        if (!_controlPools.TryGetValue(role, out var pool))
        {
            return;
        }

        while (pool.Count > 0)
        {
            var control = pool.Pop();
            control.DataContext = null;
        }
    }
}
