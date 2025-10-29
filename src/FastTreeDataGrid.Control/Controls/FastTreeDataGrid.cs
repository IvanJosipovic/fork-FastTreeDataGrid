using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Automation.Peers;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.Control.Controls;

public partial class FastTreeDataGrid : TemplatedControl
{
    public static readonly StyledProperty<IFastTreeDataGridSource?> ItemsSourceProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, IFastTreeDataGridSource?>(nameof(ItemsSource));

    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, double>(nameof(RowHeight), 28d);

    public static readonly StyledProperty<double> IndentWidthProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, double>(nameof(IndentWidth), 16d);

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, double>(nameof(HeaderHeight), 32d);

    public static readonly StyledProperty<bool> IsFilterRowVisibleProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, bool>(nameof(IsFilterRowVisible));

    public static readonly DirectProperty<FastTreeDataGrid, AvaloniaList<FastTreeDataGridColumn>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, AvaloniaList<FastTreeDataGridColumn>>(nameof(Columns), o => o.Columns);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, int>(nameof(SelectedIndex), -1);

    public static readonly DirectProperty<FastTreeDataGrid, bool> IsLoadingProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, bool>(nameof(IsLoading), o => o.IsLoading);

    public static readonly DirectProperty<FastTreeDataGrid, double> LoadingProgressProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, double>(nameof(LoadingProgress), o => o.LoadingProgress);

    public static readonly DirectProperty<FastTreeDataGrid, object?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, object?>(nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SetSelectedItem(v));

    public static readonly DirectProperty<FastTreeDataGrid, IFastTreeDataGridRowLayout?> RowLayoutProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, IFastTreeDataGridRowLayout?>(nameof(RowLayout), o => o.RowLayout, (o, v) => o.SetRowLayout(v));

    public static readonly DirectProperty<FastTreeDataGrid, FastTreeDataGridVirtualizationSettings> VirtualizationSettingsProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, FastTreeDataGridVirtualizationSettings>(
            nameof(VirtualizationSettings),
            o => o.VirtualizationSettings,
            (o, v) => o.VirtualizationSettings = v);

    public static readonly DirectProperty<FastTreeDataGrid, FastTreeDataGridRowReorderSettings> RowReorderSettingsProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, FastTreeDataGridRowReorderSettings>(
            nameof(RowReorderSettings),
            o => o.RowReorderSettings,
            (o, v) => o.SetRowReorderSettings(v));

    public static readonly DirectProperty<FastTreeDataGrid, IFastTreeDataGridRowReorderHandler?> RowReorderHandlerProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, IFastTreeDataGridRowReorderHandler?>(
            nameof(RowReorderHandler),
            o => o.RowReorderHandler,
            (o, v) => o.SetExplicitRowReorderHandler(v));

    public static readonly DirectProperty<FastTreeDataGrid, IFastTreeDataGridSelectionModel?> SelectionModelProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, IFastTreeDataGridSelectionModel?>(
            nameof(SelectionModel),
            o => o.SelectionModel,
            (o, v) => o.SetSelectionModel(v));

    public static readonly DirectProperty<FastTreeDataGrid, FastTreeDataGridSelectionMode> SelectionModeProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, FastTreeDataGridSelectionMode>(
            nameof(SelectionMode),
            o => o.SelectionMode,
            (o, v) => o.SelectionMode = v);

    public static readonly DirectProperty<FastTreeDataGrid, Func<FastTreeDataGridRow, string?>?> TypeSearchSelectorProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, Func<FastTreeDataGridRow, string?>?>(
            nameof(TypeSearchSelector),
            o => o.TypeSearchSelector,
            (o, v) => o.TypeSearchSelector = v);

    public static readonly DirectProperty<FastTreeDataGrid, IReadOnlyList<int>> SelectedIndicesProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, IReadOnlyList<int>>(
            nameof(SelectedIndices),
            o => o.SelectedIndices,
            (o, v) => o.SetSelectedIndices(v));

    private readonly AvaloniaList<FastTreeDataGridColumn> _columns = new();
    private readonly List<double> _columnWidths = new();
    private readonly List<double> _columnOffsets = new();
    private readonly List<int> _visibleColumnIndices = new();
    private readonly FastTreeDataGridInlineColumnSource _columnSource;
    private FastTreeDataGridColumnViewportScheduler? _columnScheduler;
    private readonly object _viewportCoordinatorLock = new();
    private bool _pendingColumnReset;
    private readonly List<FastTreeDataGridInvalidationRequest> _pendingColumnInvalidations = new();
    private readonly HashSet<int> _pendingColumnMaterializations = new();
    private bool _horizontalScrollPending;
    private double _pendingHorizontalOffset = double.NaN;
    private long _horizontalScrollStartTimestamp;

    private ScrollViewer? _headerScrollViewer;
    private Border? _headerHost;
    private FastTreeDataGridHeaderPresenter? _headerPresenter;
    private FastTreeDataGridFilterPresenter? _filterPresenter;
    private FastTreeDataGridGroupingBand? _groupingBand;
    private Border? _groupingBandHost;
    private FastTreeDataGridColumnFilterFlyout? _columnFilterFlyout;
    private bool _updatingHeaderFromBody;
    private FastTreeDataGridPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private IFastTreeDataGridSource? _itemsSource;
    private IFastTreeDataVirtualizationProvider? _virtualizationProvider;
    private FastTreeDataGridViewportScheduler? _viewportScheduler;
    private CancellationTokenSource? _providerInitializationCts;
    private FastTreeDataGridVirtualizationSettings _virtualizationSettings = new();
    private bool _templateHandlersAttached;
    private bool _isAttachedToVisualTree;
    private object? _selectedItem;
    private bool _viewportUpdateQueued;
    private bool _columnsDirty = true;
    private bool _autoWidthChanged;
    private readonly List<FastTreeDataGridSortDescription> _sortDescriptions = new();
    private readonly AvaloniaList<FastTreeDataGridGroupDescriptor> _groupDescriptors = new();
    private readonly AvaloniaList<FastTreeDataGridAggregateDescriptor> _aggregateDescriptors = new();
    private readonly FastTreeDataGridGroupingStateStore _groupingStateStore = new();
    private readonly Dictionary<FastTreeDataGridColumn, FastTreeDataGridFilterDescriptor> _activeFilters = new();
    private readonly List<FastTreeDataGridFilterDescriptor> _filterDescriptorCache = new();
    private readonly Dictionary<FastTreeDataGridColumn, string> _filterTextCache = new();
    private IFastTreeDataGridRowLayout? _rowLayout;
    private FastTreeDataGridThrottleDispatcher? _resetThrottle;
    private IFastTreeDataGridSelectionModel _selectionModel = null!;
    private FastTreeDataGridSelectionMode _selectionMode = FastTreeDataGridSelectionMode.Extended;
    private bool _synchronizingSelection;
    private FastTreeDataGridRowReorderSettings _rowReorderSettings = new();
    private IFastTreeDataGridRowReorderHandler? _explicitRowReorderHandler;
    private IFastTreeDataGridRowReorderHandler? _autoRowReorderHandler;
    private readonly RowReorderController _rowReorderController;
    private Func<FastTreeDataGridRow, string?>? _typeSearchSelector;
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private bool IsCellSelection => _selectionModel?.SelectionUnit == FastTreeDataGridSelectionUnit.Cell;
    private IFastTreeDataGridCellSelectionModel? CellSelectionModel => _selectionModel as IFastTreeDataGridCellSelectionModel;
    private string _typeSearchBuffer = string.Empty;
    private DateTime _typeSearchTimestamp = DateTime.MinValue;
    private static readonly TimeSpan s_typeSearchResetInterval = TimeSpan.FromSeconds(1.5);
    private bool _isLoading;
    private double _loadingProgress = double.NaN;
    private bool _rowsLoading;
    private double _rowLoadingProgress = double.NaN;
    private bool _columnsLoading;
    private double _columnLoadingProgress = double.NaN;
    private int _lastGroupPrefetchIndex = -1;
    private DateTime _lastGroupPrefetchTimestamp = DateTime.MinValue;
    private AvaloniaControl? _loadingOverlay;
    private ProgressBar? _loadingProgressBar;
    private TextBlock? _loadingText;
    private ColumnViewportState _currentColumnViewportState = ColumnViewportState.Empty;
    private ColumnViewportDelta _currentColumnViewportDelta = ColumnViewportDelta.Empty;
    private RowViewportState _currentRowViewportState = RowViewportState.Empty;
    private RowViewportDelta _currentRowViewportDelta = RowViewportDelta.Empty;
    private readonly ViewportCoordinator _viewportCoordinator = new();

    static FastTreeDataGrid()
    {
        AffectsMeasure<FastTreeDataGrid>(ItemsSourceProperty, RowHeightProperty, IndentWidthProperty);
        IsFilterRowVisibleProperty.Changed.AddClassHandler<FastTreeDataGrid>((x, e) => x.OnFilterRowVisibilityChanged(e));
    }

    public FastTreeDataGrid()
    {
        _editingController = new EditingController(this);
        _columns.CollectionChanged += OnColumnsChanged;
        _groupDescriptors.CollectionChanged += OnGroupDescriptorsChanged;
        _aggregateDescriptors.CollectionChanged += OnAggregateDescriptorsChanged;
        _groupingStateStore.GroupingStateChanged += OnGroupingStateChanged;
        SetRowLayout(new FastTreeDataGridUniformRowLayout());
        ResetThrottleDispatcher();
        SetSelectionModel(new FastTreeDataGridSelectionModel());
        _rowReorderSettings.SettingsChanged += OnRowReorderSettingsChanged;
        _rowReorderController = new RowReorderController(this);
        _columnSource = new FastTreeDataGridInlineColumnSource(this);
        _columnSource.ResetRequested += OnColumnSourceResetRequested;
        _columnSource.Invalidated += OnColumnSourceInvalidated;
        _columnSource.ColumnMaterialized += OnColumnSourceColumnMaterialized;
        _columnScheduler = new FastTreeDataGridColumnViewportScheduler(_columnSource, _virtualizationSettings);
        _columnScheduler.LoadingStateChanged += OnColumnSchedulerLoadingStateChanged;
    }

    public FastTreeDataGridGroupingLayout GetGroupingLayout()
    {
        var layout = new FastTreeDataGridGroupingLayout();

        foreach (var descriptor in _groupDescriptors)
        {
            var entry = new FastTreeDataGridGroupingLayoutDescriptor
            {
                ColumnKey = GetDescriptorColumnKey(descriptor),
                SortDirection = descriptor.SortDirection,
                IsExpanded = descriptor.IsExpanded,
            };

            foreach (var pair in descriptor.Properties)
            {
                if (pair.Value is string stringValue)
                {
                    entry.Metadata[pair.Key] = stringValue;
                }
            }

            layout.Groups.Add(entry);
        }

        var snapshot = _groupingStateStore.CreateSnapshot();
        foreach (var state in snapshot.Groups)
        {
            layout.ExpansionStates.Add(new FastTreeDataGridGroupingExpansionState
            {
                Path = state.Path,
                IsExpanded = state.IsExpanded,
            });
        }

        return layout;
    }

    public void ApplyGroupingLayout(FastTreeDataGridGroupingLayout layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        if (layout.Version > 1)
        {
            throw new NotSupportedException($"Unsupported grouping layout version: {layout.Version}. Update the control to a newer version.");
        }

        _groupDescriptors.Clear();

        foreach (var entry in layout.Groups)
        {
            if (string.IsNullOrEmpty(entry.ColumnKey))
            {
                continue;
            }

            var column = FindColumnByKey(entry.ColumnKey);
            if (column is null)
            {
                continue;
            }

            var descriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(column);
            descriptor.SortDirection = entry.SortDirection;
            descriptor.IsExpanded = entry.IsExpanded;

            foreach (var pair in entry.Metadata)
            {
                descriptor.Properties[pair.Key] = pair.Value;
            }

            _groupDescriptors.Add(descriptor);

            if (column.SortDirection != entry.SortDirection)
            {
                column.SortDirection = entry.SortDirection;
            }
        }

        _groupingStateStore.Clear();
        foreach (var state in layout.ExpansionStates)
        {
            if (string.IsNullOrEmpty(state.Path))
            {
                continue;
            }

            _groupingStateStore.SetExpanded(state.Path, state.IsExpanded);
        }

        var defaultExpanded = DetermineDefaultGroupExpansion();
        ApplyGroupExpansionLayout(layout.ExpansionStates, defaultExpanded);

        UpdateGroupingBandDescriptors();
        ApplyDataOperationsToProvider();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FastTreeDataGridAutomationPeer(this);

    public IFastTreeDataGridSource? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public double IndentWidth
    {
        get => GetValue(IndentWidthProperty);
        set => SetValue(IndentWidthProperty, value);
    }

    public double HeaderHeight
    {
        get => GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public AvaloniaList<FastTreeDataGridColumn> Columns => _columns;

    public AvaloniaList<FastTreeDataGridGroupDescriptor> GroupDescriptors => _groupDescriptors;

    public AvaloniaList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors => _aggregateDescriptors;

    internal FastTreeDataGridGroupingStateStore GroupingStateStore => _groupingStateStore;

    public bool IsLoading => _isLoading;

    public double LoadingProgress => _loadingProgress;

    public bool IsFilterRowVisible
    {
        get => GetValue(IsFilterRowVisibleProperty);
        set => SetValue(IsFilterRowVisibleProperty, value);
    }

    public void ExpandAllGroups()
    {
        if (_virtualizationProvider is IFastTreeDataGridGroupingController grouping)
        {
            grouping.ExpandAllGroups();
        }
    }

    public void CollapseAllGroups()
    {
        if (_virtualizationProvider is IFastTreeDataGridGroupingController grouping)
        {
            grouping.CollapseAllGroups();
        }
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem => _selectedItem;

    public IFastTreeDataGridRowLayout? RowLayout
    {
        get => _rowLayout;
        set => SetRowLayout(value);
    }

    public FastTreeDataGridVirtualizationSettings VirtualizationSettings
    {
        get => _virtualizationSettings;
        set
        {
            var newValue = value ?? new FastTreeDataGridVirtualizationSettings();
            if (ReferenceEquals(_virtualizationSettings, newValue))
            {
                return;
            }

            SetAndRaise(VirtualizationSettingsProperty, ref _virtualizationSettings, newValue);
            _viewportScheduler?.UpdateSettings(_virtualizationSettings);
            _columnScheduler?.UpdateSettings(_virtualizationSettings);
            ResetThrottleDispatcher();
            UpdateLoadingOverlay();
        }
    }

    internal IFastTreeDataVirtualizationProvider? VirtualizationProvider => _virtualizationProvider;

    public event EventHandler<FastTreeDataGridSortEventArgs>? SortRequested;
    public event EventHandler<FastTreeDataGridSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<FastTreeDataGridTypeSearchEventArgs>? TypeSearchRequested;
    public event EventHandler<FastTreeDataGridRowReorderingEventArgs>? RowReordering;
    public event EventHandler<FastTreeDataGridRowReorderedEventArgs>? RowReordered;

    public IFastTreeDataGridSelectionModel SelectionModel
    {
        get => _selectionModel;
        set => SetSelectionModel(value);
    }

    public FastTreeDataGridSelectionMode SelectionMode
    {
        get => _selectionMode;
        set => SetSelectionMode(value);
    }

    public IReadOnlyList<int> SelectedIndices => _selectedIndices;

    public Func<FastTreeDataGridRow, string?>? TypeSearchSelector
    {
        get => _typeSearchSelector;
        set => SetAndRaise(TypeSearchSelectorProperty, ref _typeSearchSelector, value);
    }

    private void SetSelectedItem(object? value)
    {
        SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        UpdateSelectionIndicators();
    }

    private void SetRowLayout(IFastTreeDataGridRowLayout? layout)
    {
        if (ReferenceEquals(_rowLayout, layout))
        {
            return;
        }

        _rowLayout?.Detach();

        _rowLayout = layout;

        if (_rowLayout is null)
        {
            RequestViewportUpdate();
            return;
        }

        _rowLayout.Attach(this);
        _rowLayout.Bind(_itemsSource);
        _rowLayout.Reset();
        RequestViewportUpdate();
    }

    private void SetSelectionModel(IFastTreeDataGridSelectionModel? model)
    {
        var newModel = model ?? new FastTreeDataGridSelectionModel();

        if (ReferenceEquals(_selectionModel, newModel))
        {
            return;
        }

        var oldModel = _selectionModel;

        if (oldModel is not null)
        {
            oldModel.SelectionChanged -= OnSelectionModelSelectionChanged;
            oldModel.Detach();
        }

        newModel.SelectionMode = _selectionMode;
        newModel.Attach(this);
        newModel.SelectionChanged += OnSelectionModelSelectionChanged;

        _selectionModel = newModel;
        RaisePropertyChanged(SelectionModelProperty, oldModel, newModel);
        _presenter?.SetSelectionUnit(_selectionModel.SelectionUnit);
        UpdateSelectionFromModel();
    }

    private void SetSelectionMode(FastTreeDataGridSelectionMode mode)
    {
        var newValue = Enum.IsDefined(typeof(FastTreeDataGridSelectionMode), mode)
            ? mode
            : FastTreeDataGridSelectionMode.Extended;

        if (_selectionMode == newValue)
        {
            return;
        }

        SetAndRaise(SelectionModeProperty, ref _selectionMode, newValue);
        _selectionModel.SelectionMode = newValue;

        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            _selectionModel.Clear();
        }
    }

    public FastTreeDataGridRowReorderSettings RowReorderSettings => _rowReorderSettings;

    public IFastTreeDataGridRowReorderHandler? RowReorderHandler => _explicitRowReorderHandler ?? _autoRowReorderHandler;

    private void SetRowReorderSettings(FastTreeDataGridRowReorderSettings? settings)
    {
        var newValue = settings ?? new FastTreeDataGridRowReorderSettings();
        if (ReferenceEquals(_rowReorderSettings, newValue))
        {
            return;
        }

        var oldValue = _rowReorderSettings;
        _rowReorderSettings.SettingsChanged -= OnRowReorderSettingsChanged;
        _rowReorderSettings = newValue;
        _rowReorderSettings.SettingsChanged += OnRowReorderSettingsChanged;
        RaisePropertyChanged(RowReorderSettingsProperty, oldValue, newValue);
        OnRowReorderSettingsChanged(_rowReorderSettings, EventArgs.Empty);
    }

    private void SetExplicitRowReorderHandler(IFastTreeDataGridRowReorderHandler? handler)
    {
        if (ReferenceEquals(_explicitRowReorderHandler, handler))
        {
            return;
        }

        var oldResolved = RowReorderHandler;
        _explicitRowReorderHandler = handler;
        RaisePropertyChanged(RowReorderHandlerProperty, oldResolved, RowReorderHandler);
        OnRowReorderHandlerChanged();
    }

    private void OnRowReorderSettingsChanged(object? sender, EventArgs e)
    {
        if (sender is FastTreeDataGridRowReorderSettings settings && !ReferenceEquals(_rowReorderSettings, settings))
        {
            return;
        }

        OnRowReorderSettingsChanged();
    }

    private void OnRowReorderSettingsChanged()
    {
        _rowReorderController.Refresh();
    }

    private void OnRowReorderHandlerChanged()
    {
        _rowReorderController.Refresh();
    }

    private void RefreshAutoRowReorderHandler()
    {
        var resolved = TryResolveRowReorderHandler();
        if (ReferenceEquals(_autoRowReorderHandler, resolved))
        {
            if (_explicitRowReorderHandler is null)
            {
                OnRowReorderHandlerChanged();
            }
            return;
        }

        var oldResolved = RowReorderHandler;
        _autoRowReorderHandler = resolved;

        if (!ReferenceEquals(oldResolved, RowReorderHandler))
        {
            RaisePropertyChanged(RowReorderHandlerProperty, oldResolved, RowReorderHandler);
        }

        OnRowReorderHandlerChanged();
    }

    private IFastTreeDataGridRowReorderHandler? TryResolveRowReorderHandler()
    {
        if (_virtualizationProvider is IFastTreeDataGridRowReorderHandler providerHandler)
        {
            return providerHandler;
        }

        if (_itemsSource is IFastTreeDataGridRowReorderHandler sourceHandler)
        {
            return sourceHandler;
        }

        return null;
    }

    private void SetSelectedIndices(IReadOnlyList<int>? indices)
    {
        if (_selectionModel is null)
        {
            return;
        }

        var normalized = NormalizeSelectionInput(indices);
        if (normalized.Count == 0 && _selectedIndices.Count == 0)
        {
            return;
        }

        if (normalized.Count == _selectedIndices.Count && normalized.SequenceEqual(_selectedIndices))
        {
            return;
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            _selectionModel.Clear();
            return;
        }

        if (normalized.Count == 0)
        {
            _selectionModel.Clear();
            return;
        }

        var primary = normalized[^1];
        var anchor = normalized[0];
        _selectionModel.SetSelection(normalized, primary, anchor);
    }

    private void OnSelectionModelSelectionChanged(object? sender, FastTreeDataGridSelectionChangedEventArgs e)
    {
        if (_synchronizingSelection)
        {
            SetSelectedIndicesInternal(e.SelectedIndices);
            UpdateSelectionIndicators();
            OnSelectionChangedForEditing();
            SelectionChanged?.Invoke(this, e);
            return;
        }

        _synchronizingSelection = true;
        try
        {
            SetCurrentValue(SelectedIndexProperty, e.PrimaryIndex);
            UpdateSelectedItemFromSelection(e.PrimaryIndex);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        SetSelectedIndicesInternal(e.SelectedIndices);
        if (IsCellSelection && CellSelectionModel is { } cellModel && cellModel.PrimaryCell.IsValid)
        {
            SetCurrentColumnForRow(cellModel.PrimaryCell.ColumnIndex);
        }
        UpdateSelectionIndicators();
        OnSelectionChangedForEditing();
        SelectionChanged?.Invoke(this, e);
    }

    private void UpdateSelectionFromModel()
    {
        if (_selectionModel is null)
        {
            return;
        }

        _synchronizingSelection = true;
        try
        {
            SetCurrentValue(SelectedIndexProperty, _selectionModel.PrimaryIndex);
            UpdateSelectedItemFromSelection(_selectionModel.PrimaryIndex);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        SetSelectedIndicesInternal(_selectionModel.SelectedIndices);
        if (IsCellSelection && CellSelectionModel is { } cellModel && cellModel.PrimaryCell.IsValid)
        {
            SetCurrentColumnForRow(cellModel.PrimaryCell.ColumnIndex);
        }
        UpdateSelectionIndicators();
        OnSelectionChangedForEditing();
    }

    private void UpdateSelectedItemFromSelection(int primaryIndex)
    {
        if (_itemsSource is null || primaryIndex < 0 || primaryIndex >= _itemsSource.RowCount || _itemsSource.IsPlaceholder(primaryIndex))
        {
            SetSelectedItem(null);
            return;
        }

        var row = _itemsSource.GetRow(primaryIndex);
        SetSelectedItem(row.Item);
    }

    private void AttachColumnHandlers(FastTreeDataGridColumn column)
    {
        if (column is null)
        {
            return;
        }

        column.PropertyChanged += OnColumnPropertyChanged;
    }

    private void DetachColumnHandlers(FastTreeDataGridColumn column)
    {
        if (column is null)
        {
            return;
        }

        column.PropertyChanged -= OnColumnPropertyChanged;
        _filterTextCache.Remove(column);
    }

    private void OnColumnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        var descriptorDirty = false;

        if (e.Property == FastTreeDataGridColumn.PinnedPositionProperty ||
            e.Property == FastTreeDataGridColumn.SortDirectionProperty ||
            e.Property == FastTreeDataGridColumn.SortOrderProperty ||
            e.Property == FastTreeDataGridColumn.CanUserResizeProperty ||
            e.Property == FastTreeDataGridColumn.CanUserReorderProperty)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
            descriptorDirty = true;
        }
        else if (e.Property == FastTreeDataGridColumn.GroupHeaderTemplateProperty ||
                 e.Property == FastTreeDataGridColumn.GroupHeaderWidgetFactoryProperty ||
                 e.Property == FastTreeDataGridColumn.GroupHeaderControlTemplateProperty ||
                 e.Property == FastTreeDataGridColumn.GroupFooterTemplateProperty ||
                 e.Property == FastTreeDataGridColumn.GroupFooterWidgetFactoryProperty ||
                 e.Property == FastTreeDataGridColumn.GroupFooterControlTemplateProperty)
        {
            RequestViewportUpdate();
        }
        else if (e.Property == FastTreeDataGridColumn.PixelWidthProperty ||
                 e.Property == FastTreeDataGridColumn.MinWidthProperty ||
                 e.Property == FastTreeDataGridColumn.MaxWidthProperty ||
                 e.Property == FastTreeDataGridColumn.SizingModeProperty ||
                 e.Property == FastTreeDataGridColumn.ValueKeyProperty ||
                 e.Property == FastTreeDataGridColumn.HeaderProperty)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
            descriptorDirty = true;
        }

        if (descriptorDirty)
        {
            var columnIndex = sender is FastTreeDataGridColumn c ? _columns.IndexOf(c) : -1;
            _columnSource.RaiseColumnsChanged(columnIndex >= 0 ? columnIndex : null, structuralChange: false);
        }
    }

    private double GetViewportWidth()
    {
        if (_scrollViewer is { } sv && sv.Viewport.Width > 0)
        {
            return sv.Viewport.Width;
        }

        return Bounds.Width > 0 ? Bounds.Width : 0;
    }

    private IFastTreeDataGridRowLayout GetActiveRowLayout()
    {
        if (_rowLayout is null)
        {
            SetRowLayout(new FastTreeDataGridUniformRowLayout());
        }

        return _rowLayout!;
    }

    private void UpdateSelectionFromIndex(int index)
    {
        if (_synchronizingSelection || _selectionModel is null)
        {
            return;
        }

        if (index < 0)
        {
            _selectionModel.Clear();
        }
        else
        {
            _selectionModel.SelectSingle(index);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        DetachTemplateParts(clearReferences: true);

        _headerScrollViewer = e.NameScope.Find<ScrollViewer>("PART_HeaderScrollViewer");
        _headerHost = e.NameScope.Find<Border>("PART_HeaderHost");
        _groupingBandHost = e.NameScope.Find<Border>("PART_GroupingBandHost");
        _groupingBand = e.NameScope.Find<FastTreeDataGridGroupingBand>("PART_GroupingBand");
        _headerPresenter = e.NameScope.Find<FastTreeDataGridHeaderPresenter>("PART_HeaderPresenter");
        _filterPresenter = e.NameScope.Find<FastTreeDataGridFilterPresenter>("PART_FilterPresenter");
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _presenter = e.NameScope.Find<FastTreeDataGridPresenter>("PART_Presenter");
        _loadingOverlay = e.NameScope.Find<AvaloniaControl>("PART_LoadingOverlay");
        _loadingProgressBar = e.NameScope.Find<ProgressBar>("PART_LoadingProgress");
        _loadingText = e.NameScope.Find<TextBlock>("PART_LoadingText");

        if (_headerScrollViewer is not null)
        {
            _headerScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            _headerScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        if (_headerHost is not null)
        {
            _headerHost.ClipToBounds = true;
        }
        if (_presenter is not null)
        {
            _presenter.SetOwner(this);
            _presenter.SetVirtualizationProvider(_virtualizationProvider);
            _presenter.SetSelectionUnit(_selectionModel.SelectionUnit);
        }
        AttachPresenterForEditing(_presenter);

        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
        }

        if (_groupingBand is not null)
        {
            _groupingBand.UpdateDescriptors(_groupDescriptors, GetGroupingLabel);
            _groupingBand.RemoveRequested += OnGroupingBandRemoveRequested;
            _groupingBand.ReorderRequested += OnGroupingBandReorderRequested;
            _groupingBand.ColumnDropRequested += OnGroupingBandColumnDropRequested;
            _groupingBand.KeyboardCommandRequested += OnGroupingBandKeyboardCommandRequested;
        }

        if (_filterPresenter is not null)
        {
            _filterPresenter.FilterHeight = RowHeight;
            _filterPresenter.IsVisible = IsFilterRowVisible;
            _filterPresenter.IsHitTestVisible = IsFilterRowVisible;
            _filterPresenter.FilterChanged += OnFilterPresenterFilterChanged;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        AttachTemplatePartHandlers();
        SynchronizeHeaderScroll();

        _columnsDirty = true;
        RequestViewportUpdate();
        ApplyDataOperationsToProvider();
        UpdateLoadingOverlay();
        UpdateSelectionIndicators();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttachedToVisualTree = true;

        if (_presenter is null || _scrollViewer is null || _headerPresenter is null)
        {
            ApplyTemplate();
        }

        AttachTemplatePartHandlers();

        if (_headerHost is not null)
        {
            _headerHost.ClipToBounds = true;
        }
        _presenter?.SetOwner(this);
        _presenter?.SetSelectionUnit(_selectionModel.SelectionUnit);
        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
        }

        SynchronizeHeaderScroll();

        _columnsDirty = true;
        RequestViewportUpdate();
        UpdateSelectionIndicators();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttachedToVisualTree = false;
        _columnScheduler?.CancelAll();
        DetachTemplateParts(clearReferences: false);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        OnLostFocusForEditing();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            HandleItemsSourceChanged(change.GetOldValue<IFastTreeDataGridSource?>(), change.GetNewValue<IFastTreeDataGridSource?>());
        }
        else if (change.Property == BoundsProperty)
        {
            var oldBounds = change.GetOldValue<Rect>();
            var newBounds = change.GetNewValue<Rect>();
            if (Math.Abs(newBounds.Width - oldBounds.Width) > 0.5)
            {
                _columnsDirty = true;
            }
            RequestViewportUpdate();
        }
        else if (change.Property == HeaderHeightProperty && _headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
            RequestViewportUpdate();
        }
        else if (change.Property == RowHeightProperty)
        {
            _rowLayout?.Reset();
            RequestViewportUpdate();
        }
        else if (change.Property == SelectedIndexProperty)
        {
            UpdateSelectionFromIndex(change.GetNewValue<int>());
        }
    }

    private void HandleItemsSourceChanged(IFastTreeDataGridSource? oldSource, IFastTreeDataGridSource? newSource)
    {
        if (oldSource is not null)
        {
            oldSource.ResetRequested -= OnSourceResetRequested;
        }

        DisposeVirtualizationProvider();
        OnItemsSourceResetForEditing();

        _itemsSource = newSource;
        _rowLayout?.Bind(_itemsSource);
        _rowLayout?.Reset();

        if (newSource is not null)
        {
            newSource.ResetRequested += OnSourceResetRequested;
        }

        ConfigureVirtualizationProvider(newSource);
        RefreshAutoRowReorderHandler();

        ResetTypeSearch();
        SetValue(SelectedIndexProperty, -1);
        RequestViewportUpdate();
    }

    private void OnSourceResetRequested(object? sender, EventArgs e)
    {
        _rowLayout?.Reset();
        ResetTypeSearch();
        OnItemsSourceResetForEditing();
        RequestViewportUpdate();
    }

    private void ConfigureVirtualizationProvider(IFastTreeDataGridSource? source)
    {
        var provider = FastTreeDataGridVirtualizationProviderRegistry.Create(source, _virtualizationSettings);
        if (provider is null)
        {
            _virtualizationProvider = null;
            _presenter?.SetVirtualizationProvider(null);
            RefreshAutoRowReorderHandler();
            return;
        }

        AttachVirtualizationProvider(provider);
    }

    private void AttachVirtualizationProvider(IFastTreeDataVirtualizationProvider provider)
    {
        if (ReferenceEquals(_virtualizationProvider, provider))
        {
            return;
        }

        DisposeVirtualizationProvider();

        _virtualizationProvider = provider;
        _virtualizationProvider.Invalidated += OnVirtualizationProviderInvalidated;
        _virtualizationProvider.RowMaterialized += OnVirtualizationProviderRowMaterialized;
        _virtualizationProvider.CountChanged += OnVirtualizationProviderCountChanged;

        _presenter?.SetVirtualizationProvider(_virtualizationProvider);
        RefreshAutoRowReorderHandler();

        _providerInitializationCts?.Cancel();
        _providerInitializationCts?.Dispose();
        _providerInitializationCts = new CancellationTokenSource();
        _viewportScheduler = new FastTreeDataGridViewportScheduler(_virtualizationProvider, _virtualizationSettings);
        _viewportScheduler.LoadingStateChanged += OnViewportSchedulerLoadingStateChanged;
        ResetThrottleDispatcher();
        var token = _providerInitializationCts.Token;
        _ = InitializeVirtualizationProviderAsync(_virtualizationProvider, token);
        ApplyDataOperationsToProvider();
        UpdateLoadingOverlay();
    }

    private void DisposeVirtualizationProvider()
    {
        _providerInitializationCts?.Cancel();
        _providerInitializationCts?.Dispose();
        _providerInitializationCts = null;

        if (_virtualizationProvider is null)
        {
            _presenter?.SetVirtualizationProvider(null);
            return;
        }

        if (_viewportScheduler is not null)
        {
            _viewportScheduler.LoadingStateChanged -= OnViewportSchedulerLoadingStateChanged;
            _viewportScheduler.Dispose();
            _viewportScheduler = null;
        }
        _resetThrottle?.Dispose();
        _resetThrottle = null;

        _virtualizationProvider.Invalidated -= OnVirtualizationProviderInvalidated;
        _virtualizationProvider.RowMaterialized -= OnVirtualizationProviderRowMaterialized;
        _virtualizationProvider.CountChanged -= OnVirtualizationProviderCountChanged;

        _presenter?.SetVirtualizationProvider(null);

        try
        {
            _virtualizationProvider.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        _virtualizationProvider = null;
        SetAndRaise(IsLoadingProperty, ref _isLoading, false);
        SetAndRaise(LoadingProgressProperty, ref _loadingProgress, double.NaN);
        RefreshAutoRowReorderHandler();
        UpdateLoadingOverlay();
    }

    private async Task InitializeVirtualizationProviderAsync(IFastTreeDataVirtualizationProvider provider, CancellationToken token)
    {
        try
        {
            await provider.InitializeAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        if (token.IsCancellationRequested || !ReferenceEquals(provider, _virtualizationProvider))
        {
            return;
        }

        EnqueueThrottledReset();
    }

    private void OnVirtualizationProviderInvalidated(object? sender, FastTreeDataGridInvalidatedEventArgs e)
    {
        if (!ReferenceEquals(sender, _virtualizationProvider))
        {
            return;
        }

        _viewportScheduler?.CancelAll();
        EnqueueThrottledReset();
    }

    private void OnVirtualizationProviderRowMaterialized(object? sender, FastTreeDataGridRowMaterializedEventArgs e)
    {
        if (!ReferenceEquals(sender, _virtualizationProvider))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!ReferenceEquals(sender, _virtualizationProvider))
            {
                return;
            }

            RequestViewportUpdate();
        }, DispatcherPriority.Render);
    }

    private void OnVirtualizationProviderCountChanged(object? sender, FastTreeDataGridCountChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _virtualizationProvider))
        {
            return;
        }

        _viewportScheduler?.CancelAll();
        EnqueueThrottledReset();
    }

    private void OnViewportSchedulerLoadingStateChanged(object? sender, FastTreeDataGridLoadingStateEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!ReferenceEquals(sender, _viewportScheduler))
            {
                return;
            }
            _rowsLoading = e.IsLoading;
            _rowLoadingProgress = double.IsNaN(e.Progress) ? double.NaN : Math.Clamp(e.Progress, 0d, 1d);
            UpdateLoadingState();
        }, DispatcherPriority.Render);
    }

    private void OnColumnSchedulerLoadingStateChanged(object? sender, FastTreeDataGridLoadingStateEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!ReferenceEquals(sender, _columnScheduler))
            {
                return;
            }

            _columnsLoading = e.IsLoading;
            _columnLoadingProgress = double.IsNaN(e.Progress) ? double.NaN : Math.Clamp(e.Progress, 0d, 1d);
            UpdateLoadingState();
        }, DispatcherPriority.Render);
    }

    private void OnColumnSourceResetRequested(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _columnSource))
        {
            return;
        }

        _columnScheduler?.CancelAll();
        QueueColumnReset();
    }

    private void OnColumnSourceInvalidated(object? sender, FastTreeDataGridInvalidatedEventArgs e)
    {
        if (!ReferenceEquals(sender, _columnSource))
        {
            return;
        }

        if (e.Request.Kind == FastTreeDataGridInvalidationKind.Full)
        {
            _columnScheduler?.CancelAll();
        }

        QueueColumnInvalidation(e.Request);
    }

    private void OnColumnSourceColumnMaterialized(object? sender, FastTreeDataGridColumnMaterializedEventArgs e)
    {
        if (!ReferenceEquals(sender, _columnSource))
        {
            return;
        }

        QueueColumnMaterialized(e.ColumnIndex);
    }

    private void UpdateLoadingState()
    {
        var isLoading = _rowsLoading || _columnsLoading;
        double progress;

        if (!isLoading)
        {
            progress = 1d;
        }
        else
        {
            var values = new List<double>(2);
            if (_rowsLoading && !double.IsNaN(_rowLoadingProgress))
            {
                values.Add(_rowLoadingProgress);
            }

            if (_columnsLoading && !double.IsNaN(_columnLoadingProgress))
            {
                values.Add(_columnLoadingProgress);
            }

            progress = values.Count > 0 ? values.Average() : double.NaN;
        }

        SetAndRaise(IsLoadingProperty, ref _isLoading, isLoading);
        SetAndRaise(LoadingProgressProperty, ref _loadingProgress, progress);
        UpdateLoadingOverlay();
    }

    private void StartHorizontalScrollMeasurement(double newOffset)
    {
        _horizontalScrollPending = true;
        _pendingHorizontalOffset = newOffset;
        _horizontalScrollStartTimestamp = Stopwatch.GetTimestamp();
    }

    private void CompleteHorizontalScrollMeasurement(double currentOffset, double viewportWidth, Stopwatch stopwatch)
    {
        if (!_horizontalScrollPending)
        {
            return;
        }

        if (double.IsNaN(currentOffset) || double.IsInfinity(currentOffset))
        {
            ResetHorizontalScrollMeasurement();
            return;
        }

        const double threshold = 0.001;
        if (!double.IsNaN(_pendingHorizontalOffset) && Math.Abs(currentOffset - _pendingHorizontalOffset) > threshold)
        {
            _pendingHorizontalOffset = currentOffset;
            _horizontalScrollStartTimestamp = Stopwatch.GetTimestamp();
            return;
        }

        var endTicks = Stopwatch.GetTimestamp();
        var startTicks = _horizontalScrollStartTimestamp;
        double latencyMs;

        if (startTicks != 0)
        {
            latencyMs = (endTicks - startTicks) * 1000.0 / Stopwatch.Frequency;
        }
        else
        {
            latencyMs = stopwatch.Elapsed.TotalMilliseconds;
        }

        FastTreeDataGridVirtualizationDiagnostics.HorizontalScrollUpdateDuration.Record(
            latencyMs,
            new KeyValuePair<string, object?>[]
            {
                new("offset", currentOffset),
                new("viewport_width", viewportWidth),
            });

        ResetHorizontalScrollMeasurement();
    }

    private void ResetHorizontalScrollMeasurement()
    {
        _horizontalScrollPending = false;
        _pendingHorizontalOffset = double.NaN;
        _horizontalScrollStartTimestamp = 0;
    }

    private void EnqueueThrottledReset()
    {
        if (_resetThrottle is null)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                FastTreeDataGridVirtualizationDiagnostics.ResetCount.Add(1, CreateControlTags());
                _rowLayout?.Reset();
                RequestViewportUpdate();
                await RestoreSelectionAsync(CancellationToken.None);
            }, _virtualizationSettings.DispatcherPriority.ToAvaloniaPriority());
            return;
        }

        _resetThrottle.Enqueue(async ct =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                FastTreeDataGridVirtualizationDiagnostics.ResetCount.Add(1, CreateControlTags());
                _rowLayout?.Reset();
                RequestViewportUpdate();
            }, _virtualizationSettings.DispatcherPriority.ToAvaloniaPriority());

            await RestoreSelectionAsync(ct).ConfigureAwait(false);
        });
    }

    private async Task RestoreSelectionAsync(CancellationToken token)
    {
        if (SelectedItem is null || _virtualizationProvider is null)
        {
            return;
        }

        var provider = _virtualizationProvider;
        var item = SelectedItem;

        int index;
        try
        {
            index = await provider.LocateRowIndexAsync(item, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return;
        }

        if (index < 0)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (token.IsCancellationRequested || !ReferenceEquals(provider, _virtualizationProvider))
            {
                return;
            }

            SetValue(SelectedIndexProperty, index);
        }, _virtualizationSettings.DispatcherPriority.ToAvaloniaPriority());
    }

    private void ResetThrottleDispatcher()
    {
        _resetThrottle?.Dispose();
        var delay = TimeSpan.FromMilliseconds(_virtualizationSettings.ResetThrottleDelayMilliseconds);
        _resetThrottle = new FastTreeDataGridThrottleDispatcher(delay);
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e is null)
        {
            return;
        }

        var filtersRemoved = false;

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var column in _columns)
            {
                AttachColumnHandlers(column);
            }
        }
        else
        {
            if (e.OldItems is not null)
            {
                foreach (FastTreeDataGridColumn column in e.OldItems)
                {
                    DetachColumnHandlers(column);
                    if (_activeFilters.Remove(column))
                    {
                        filtersRemoved = true;
                    }
                    column.IsFilterActive = false;
                    _filterPresenter?.SetFilterValue(column, string.Empty);

                    var groupingIndex = FindGroupingDescriptorIndex(column);
                    if (groupingIndex >= 0)
                    {
                        _groupDescriptors.RemoveAt(groupingIndex);
                    }
                }
            }

            if (e.NewItems is not null)
            {
                foreach (FastTreeDataGridColumn column in e.NewItems)
                {
                    AttachColumnHandlers(column);
                }
            }
        }

        _columnsDirty = true;
        RequestViewportUpdate();

        UpdateGroupingBandDescriptors();

        if (filtersRemoved)
        {
            ApplyDataOperationsToProvider();
        }

        _columnSource.RaiseColumnsChanged(structuralChange: true);
    }

    private void OnGroupDescriptorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _groupingStateStore.SetDescriptors(_groupDescriptors);
        UpdateGroupingBandDescriptors();
        ApplyDataOperationsToProvider();
    }

    private void OnAggregateDescriptorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyDataOperationsToProvider();
    }

    private void OnGroupingStateChanged(object? sender, FastTreeDataGridGroupingStateChangedEventArgs e)
    {
        if (_virtualizationProvider is IFastTreeDataGridGroupingNotificationSink sink)
        {
            sink.OnGroupingStateChanged(e);
        }

        RequestViewportUpdate();
    }

    private void OnGroupingBandRemoveRequested(int index)
    {
        if ((uint)index >= (uint)_groupDescriptors.Count)
        {
            return;
        }

        _groupDescriptors.RemoveAt(index);
    }

    private void OnGroupingBandReorderRequested(int fromIndex, int insertIndex)
    {
        if ((uint)fromIndex >= (uint)_groupDescriptors.Count)
        {
            return;
        }

        insertIndex = Math.Clamp(insertIndex, 0, _groupDescriptors.Count);
        var descriptor = _groupDescriptors[fromIndex];
        _groupDescriptors.RemoveAt(fromIndex);
        if (insertIndex > fromIndex)
        {
            insertIndex--;
        }
        _groupDescriptors.Insert(insertIndex, descriptor);
    }

    private void OnGroupingBandKeyboardCommandRequested(int index, KeyModifiers modifiers)
    {
        _ = index;
        _ = modifiers;
    }

    private void OnGroupingBandColumnDropRequested(int columnIndex, int insertIndex)
    {
        if ((uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanUserGroup)
        {
            return;
        }

        insertIndex = Math.Clamp(insertIndex, 0, _groupDescriptors.Count);
        var existingIndex = FindGroupingDescriptorIndex(column);
        if (existingIndex >= 0)
        {
            if (insertIndex > existingIndex)
            {
                insertIndex--;
            }

            if (insertIndex == existingIndex || insertIndex == existingIndex + 1)
            {
                return;
            }

            var descriptor = _groupDescriptors[existingIndex];
            _groupDescriptors.RemoveAt(existingIndex);
            _groupDescriptors.Insert(insertIndex, descriptor);
            return;
        }

        var newDescriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(column);
        _groupDescriptors.Insert(insertIndex, newDescriptor);
    }

    private void OnHeaderColumnGroupRequested(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanUserGroup)
        {
            return;
        }

        if (FindGroupingDescriptorIndex(column) >= 0)
        {
            return;
        }

        var descriptor = FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn(column);
        _groupDescriptors.Add(descriptor);
    }

    private void OnHeaderColumnUngroupRequested(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        var index = FindGroupingDescriptorIndex(column);
        if (index >= 0)
        {
            _groupDescriptors.RemoveAt(index);
        }
    }

    private void OnHeaderColumnGroupingCleared()
    {
        if (_groupDescriptors.Count > 0)
        {
            _groupDescriptors.Clear();
        }
    }

    private void UpdateGroupingBandDescriptors()
    {
        if (_groupingBand is null)
        {
            return;
        }

        _groupingBand.UpdateDescriptors(_groupDescriptors, GetGroupingLabel);
    }

    private string GetGroupingLabel(FastTreeDataGridGroupDescriptor descriptor)
    {
        if (descriptor.Properties.TryGetValue("ColumnHeader", out var header) && header is string headerText && !string.IsNullOrWhiteSpace(headerText))
        {
            return headerText;
        }

        var column = FindColumnForDescriptor(descriptor);
        if (column?.Header is string headerString && !string.IsNullOrWhiteSpace(headerString))
        {
            return headerString;
        }

        return descriptor.ColumnKey ?? "Group";
    }

    private FastTreeDataGridColumn? FindColumnForDescriptor(FastTreeDataGridGroupDescriptor descriptor)
    {
        if (descriptor.Properties.TryGetValue("ColumnReference", out var stored) && stored is FastTreeDataGridColumn storedColumn)
        {
            if (_columns.Contains(storedColumn))
            {
                return storedColumn;
            }
        }

        if (!string.IsNullOrEmpty(descriptor.ColumnKey))
        {
            for (var i = 0; i < _columns.Count; i++)
            {
                if (string.Equals(_columns[i].ValueKey, descriptor.ColumnKey, StringComparison.Ordinal))
                {
                    return _columns[i];
                }
            }
        }

        return null;
    }

    private int FindGroupingDescriptorIndex(FastTreeDataGridColumn column)
    {
        for (var i = 0; i < _groupDescriptors.Count; i++)
        {
            var descriptor = _groupDescriptors[i];
            if (descriptor.Properties.TryGetValue("ColumnReference", out var stored) && ReferenceEquals(stored, column))
            {
                return i;
            }

            if (!string.IsNullOrEmpty(descriptor.ColumnKey) && !string.IsNullOrEmpty(column.ValueKey) && string.Equals(descriptor.ColumnKey, column.ValueKey, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private FastTreeDataGridColumn? FindColumnByKey(string columnKey)
    {
        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            if (!string.IsNullOrEmpty(column.ValueKey) && string.Equals(column.ValueKey, columnKey, StringComparison.Ordinal))
            {
                return column;
            }
        }

        return null;
    }

    private static string? GetDescriptorColumnKey(FastTreeDataGridGroupDescriptor descriptor)
    {
        if (!string.IsNullOrEmpty(descriptor.ColumnKey))
        {
            return descriptor.ColumnKey;
        }

        if (descriptor.Properties.TryGetValue("ColumnKey", out var value) && value is string key)
        {
            return key;
        }

        return null;
    }

    private void AttachTemplatePartHandlers()
    {
        if (_templateHandlersAttached)
        {
            return;
        }

        if (_headerScrollViewer is not null)
        {
            _headerScrollViewer.PropertyChanged += OnHeaderScrollViewerPropertyChanged;
            _headerScrollViewer.ScrollChanged += OnHeaderScrollViewerScrollChanged;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
        }

        if (_headerPresenter is not null)
        {
            _headerPresenter.ColumnResizeRequested += OnColumnResizeRequested;
            _headerPresenter.ColumnSortRequested += OnColumnSortRequested;
            _headerPresenter.ColumnReorderRequested += OnColumnReorderRequested;
            _headerPresenter.ColumnPinRequested += OnColumnPinRequested;
            _headerPresenter.ColumnAutoSizeRequested += OnColumnAutoSizeRequested;
            _headerPresenter.AutoSizeAllRequested += OnAutoSizeAllRequested;
            _headerPresenter.ColumnMoveLeftRequested += OnColumnMoveLeftRequested;
            _headerPresenter.ColumnMoveRightRequested += OnColumnMoveRightRequested;
            _headerPresenter.ColumnHideRequested += OnColumnHideRequested;
            _headerPresenter.ExpandAllRequested += OnExpandAllRequested;
            _headerPresenter.CollapseAllRequested += OnCollapseAllRequested;
            _headerPresenter.ColumnFilterRequested += OnColumnFilterRequested;
            _headerPresenter.ColumnFilterCleared += OnColumnFilterCleared;
            _headerPresenter.ColumnGroupRequested += OnHeaderColumnGroupRequested;
            _headerPresenter.ColumnUngroupRequested += OnHeaderColumnUngroupRequested;
            _headerPresenter.ColumnGroupingCleared += OnHeaderColumnGroupingCleared;
        }

        _templateHandlersAttached = true;
    }

    private void DetachTemplatePartHandlers()
    {
        if (!_templateHandlersAttached)
        {
            return;
        }

        if (_headerScrollViewer is not null)
        {
            _headerScrollViewer.PropertyChanged -= OnHeaderScrollViewerPropertyChanged;
            _headerScrollViewer.ScrollChanged -= OnHeaderScrollViewerScrollChanged;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
            _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
        }

        if (_headerPresenter is not null)
        {
            _headerPresenter.ColumnResizeRequested -= OnColumnResizeRequested;
            _headerPresenter.ColumnSortRequested -= OnColumnSortRequested;
            _headerPresenter.ColumnReorderRequested -= OnColumnReorderRequested;
            _headerPresenter.ColumnPinRequested -= OnColumnPinRequested;
            _headerPresenter.ColumnAutoSizeRequested -= OnColumnAutoSizeRequested;
            _headerPresenter.AutoSizeAllRequested -= OnAutoSizeAllRequested;
            _headerPresenter.ColumnMoveLeftRequested -= OnColumnMoveLeftRequested;
            _headerPresenter.ColumnMoveRightRequested -= OnColumnMoveRightRequested;
            _headerPresenter.ColumnHideRequested -= OnColumnHideRequested;
            _headerPresenter.ExpandAllRequested -= OnExpandAllRequested;
            _headerPresenter.CollapseAllRequested -= OnCollapseAllRequested;
            _headerPresenter.ColumnFilterRequested -= OnColumnFilterRequested;
            _headerPresenter.ColumnFilterCleared -= OnColumnFilterCleared;
            _headerPresenter.ColumnGroupRequested -= OnHeaderColumnGroupRequested;
            _headerPresenter.ColumnUngroupRequested -= OnHeaderColumnUngroupRequested;
            _headerPresenter.ColumnGroupingCleared -= OnHeaderColumnGroupingCleared;
        }

        _templateHandlersAttached = false;
    }

    private void DetachTemplateParts(bool clearReferences)
    {
        DetachTemplatePartHandlers();
        _rowReorderController.CancelDrag();

        if (_presenter is not null)
        {
            _presenter.SetVirtualizationProvider(null);
            _presenter.SetOwner(null);
        }
        AttachPresenterForEditing(null);

        if (_groupingBand is not null)
        {
            _groupingBand.RemoveRequested -= OnGroupingBandRemoveRequested;
            _groupingBand.ReorderRequested -= OnGroupingBandReorderRequested;
            _groupingBand.ColumnDropRequested -= OnGroupingBandColumnDropRequested;
            _groupingBand.KeyboardCommandRequested -= OnGroupingBandKeyboardCommandRequested;
            if (clearReferences)
            {
                _groupingBand = null;
            }
        }

        if (_filterPresenter is not null)
        {
            _filterPresenter.FilterChanged -= OnFilterPresenterFilterChanged;
            if (clearReferences)
            {
                _filterPresenter = null;
            }
        }

        if (!clearReferences)
        {
            return;
        }

        _scrollViewer = null;
        _headerScrollViewer = null;
        _headerHost = null;
        _headerPresenter = null;
        _groupingBandHost = null;
        _presenter = null;
        if (clearReferences)
        {
            _filterPresenter = null;
        }
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        if (e.Property == ScrollViewer.ViewportProperty)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
        }
        else if (e.Property == ScrollViewer.OffsetProperty)
        {
            var oldOffset = e.GetOldValue<Vector>();
            var newOffset = e.GetNewValue<Vector>();
            if (Math.Abs(newOffset.X - oldOffset.X) > 0.001)
            {
                StartHorizontalScrollMeasurement(newOffset.X);
            }

            UpdateHeaderScroll(newOffset.X);
            RequestViewportUpdate();
        }
    }

    private void OnColumnResizeRequested(int columnIndex, double delta)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanUserResize)
        {
            return;
        }

        var currentWidth = columnIndex < _columnWidths.Count ? _columnWidths[columnIndex] : column.ActualWidth;
        if (double.IsNaN(currentWidth) || double.IsInfinity(currentWidth) || currentWidth <= 0)
        {
            currentWidth = Math.Max(column.ActualWidth, column.PixelWidth);
        }

        var minWidth = Math.Max(column.MinWidth, 16);
        var maxWidth = double.IsPositiveInfinity(column.MaxWidth) ? double.PositiveInfinity : Math.Max(column.MaxWidth, minWidth);
        var newWidth = currentWidth + delta;
        if (double.IsNaN(newWidth) || double.IsInfinity(newWidth))
        {
            return;
        }

        newWidth = Math.Clamp(newWidth, minWidth, maxWidth);
        if (Math.Abs(newWidth - currentWidth) < 0.5)
        {
            return;
        }

        column.SizingMode = ColumnSizingMode.Pixel;
        column.PixelWidth = newWidth;
        column.CachedAutoWidth = newWidth;
        column.ActualWidth = newWidth;

        RecalculateColumns();
        _columnsDirty = false;

        _headerPresenter?.UpdateWidths(_columnWidths, _scrollViewer?.Offset.X ?? 0, GetViewportWidth());

        SynchronizeHeaderScroll();
        RequestViewportUpdate();
    }

    public void SetSortState(IReadOnlyList<FastTreeDataGridSortDescription> descriptions)
    {
        _sortDescriptions.Clear();

        if (descriptions is not null)
        {
            foreach (var description in descriptions)
            {
                if (description.Direction == FastTreeDataGridSortDirection.None)
                {
                    continue;
                }

                if (!_columns.Contains(description.Column))
                {
                    continue;
                }

                _sortDescriptions.Add(new FastTreeDataGridSortDescription(description.Column, description.Direction, 0));
            }
        }

        ApplySortVisualState(requestUpdate: true);
    }

    public void ClearSortState()
    {
        ClearSortStateInternal(requestUpdate: true);
    }

    private void ClearSortStateInternal(bool requestUpdate)
    {
        if (_sortDescriptions.Count > 0)
        {
            _sortDescriptions.Clear();
        }

        ApplySortVisualState(requestUpdate);
        ApplyDataOperationsToProvider();
    }

    private void OnColumnSortRequested(int columnIndex, FastTreeDataGridSortDirection? explicitDirection, KeyModifiers modifiers)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanUserSort)
        {
            return;
        }

        var allowMultiple = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
        var targetDirection = explicitDirection ?? GetNextSortDirection(column.SortDirection);

        if (!allowMultiple)
        {
            _sortDescriptions.Clear();
            if (targetDirection != FastTreeDataGridSortDirection.None)
            {
                _sortDescriptions.Add(new FastTreeDataGridSortDescription(column, targetDirection, 0));
            }
        }
        else
        {
            var existingIndex = _sortDescriptions.FindIndex(d => d.Column == column);
            if (targetDirection == FastTreeDataGridSortDirection.None)
            {
                if (existingIndex >= 0)
                {
                    _sortDescriptions.RemoveAt(existingIndex);
                }
            }
            else if (existingIndex >= 0)
            {
                _sortDescriptions[existingIndex] = new FastTreeDataGridSortDescription(column, targetDirection, 0);
            }
            else
            {
                _sortDescriptions.Add(new FastTreeDataGridSortDescription(column, targetDirection, 0));
            }
        }

        ApplySortVisualState(requestUpdate: true);

        var resultingDirection = FastTreeDataGridSortDirection.None;
        for (var i = 0; i < _sortDescriptions.Count; i++)
        {
            if (_sortDescriptions[i].Column == column)
            {
                resultingDirection = _sortDescriptions[i].Direction;
                break;
            }
        }

        ApplyDataOperationsToProvider();
        RaiseSortRequested(columnIndex, resultingDirection, modifiers);
    }

    private void ApplyDataOperationsToProvider()
    {
        if (_virtualizationProvider is null)
        {
            return;
        }

        var groupDescriptors = _groupDescriptors.Count == 0 ? Array.Empty<FastTreeDataGridGroupDescriptor>() : _groupDescriptors.ToArray();

        SynchronizeGroupSortWithColumns(groupDescriptors);

        IReadOnlyList<FastTreeDataGridSortDescriptor> sortDescriptors;
        if (_sortDescriptions.Count == 0)
        {
            sortDescriptors = Array.Empty<FastTreeDataGridSortDescriptor>();
        }
        else
        {
            var descriptors = new List<FastTreeDataGridSortDescriptor>(_sortDescriptions.Count);
            foreach (var description in _sortDescriptions)
            {
                var column = description.Column;
                descriptors.Add(new FastTreeDataGridSortDescriptor
                {
                    ColumnKey = column.ValueKey,
                    Direction = description.Direction,
                    RowComparison = column.SortComparison,
                });
            }

            sortDescriptors = descriptors;
        }

        var request = new FastTreeDataGridSortFilterRequest
        {
            SortDescriptors = sortDescriptors,
            FilterDescriptors = BuildFilterDescriptors(),
            GroupDescriptors = groupDescriptors,
            AggregateDescriptors = _aggregateDescriptors.Count == 0 ? Array.Empty<FastTreeDataGridAggregateDescriptor>() : _aggregateDescriptors.ToArray(),
        };

        _ = _virtualizationProvider.ApplySortFilterAsync(request, CancellationToken.None);
    }

    private void SynchronizeGroupSortWithColumns(FastTreeDataGridGroupDescriptor[] groupDescriptors)
    {
        for (var i = 0; i < groupDescriptors.Length; i++)
        {
            var descriptor = groupDescriptors[i];
            if (!descriptor.Properties.TryGetValue("ColumnReference", out var stored) || stored is not FastTreeDataGridColumn column)
            {
                continue;
            }

            descriptor.SortDirection = column.SortDirection is FastTreeDataGridSortDirection.None
                ? FastTreeDataGridSortDirection.Ascending
                : column.SortDirection;

            descriptor.Comparer = column.GroupAdapter?.Comparer;
        }
    }

    private IReadOnlyList<FastTreeDataGridFilterDescriptor> BuildFilterDescriptors()
    {
        if (_activeFilters.Count == 0)
        {
            return Array.Empty<FastTreeDataGridFilterDescriptor>();
        }

        _filterDescriptorCache.Clear();
        foreach (var descriptor in _activeFilters.Values)
        {
            _filterDescriptorCache.Add(descriptor);
        }

        var result = _filterDescriptorCache.ToArray();
        _filterDescriptorCache.Clear();
        return result;
    }

    private void ApplyFilterValue(int columnIndex, string? text, bool updatePresenterText)
    {
        if (_columns.Count == 0 || columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanUserFilter)
        {
            return;
        }

        var displayText = (text ?? string.Empty).Trim();
        var descriptor = CreateFilterDescriptor(column, text);

        if (descriptor is null)
        {
            var removed = _activeFilters.Remove(column);
            column.IsFilterActive = false;
            _filterTextCache.Remove(column);
            if (updatePresenterText)
            {
                _filterPresenter?.SetFilterValue(columnIndex, string.Empty);
            }

            if (removed)
            {
                ApplyDataOperationsToProvider();
            }
        }
        else
        {
            descriptor.ColumnKey ??= column.ValueKey;
            _activeFilters[column] = descriptor;
            column.IsFilterActive = true;
            _filterTextCache[column] = displayText;

            if (updatePresenterText)
            {
                _filterPresenter?.SetFilterValue(columnIndex, displayText);
            }

            ApplyDataOperationsToProvider();
        }
    }

    private string GetFilterText(FastTreeDataGridColumn column)
    {
        if (_filterTextCache.TryGetValue(column, out var cached))
        {
            return cached;
        }

        if (_filterPresenter is not null)
        {
            return _filterPresenter.GetFilterValue(column) ?? string.Empty;
        }

        return string.Empty;
    }

    internal void ApplyFilterFromFlyout(int columnIndex, string? text)
    {
        ApplyFilterValue(columnIndex, text, updatePresenterText: true);
    }

    internal RowLayoutViewport GetAutomationViewport()
    {
        if (_itemsSource is null)
        {
            return RowLayoutViewport.Empty;
        }

        var layout = GetActiveRowLayout();
        var defaultRowHeight = Math.Max(1d, RowHeight);
        var verticalOffset = _scrollViewer?.Offset.Y ?? 0;
        var viewportHeight = _scrollViewer?.Viewport.Height ?? Bounds.Height;
        if (double.IsNaN(viewportHeight) || viewportHeight <= 0)
        {
            viewportHeight = defaultRowHeight;
        }

        return layout.GetVisibleRange(
            verticalOffset,
            viewportHeight,
            defaultRowHeight,
            _itemsSource.RowCount,
            buffer: 1);
    }

    internal bool TryGetRowForAutomation(int rowIndex, out FastTreeDataGridRow row)
    {
        row = default!;
        if (_itemsSource is null || rowIndex < 0 || rowIndex >= _itemsSource.RowCount)
        {
            return false;
        }

        if (_itemsSource.IsPlaceholder(rowIndex))
        {
            return false;
        }

        row = _itemsSource.GetRow(rowIndex);
        return true;
    }

    internal Rect GetRowBoundsForAutomation(int rowIndex)
    {
        if (_itemsSource is null || rowIndex < 0 || rowIndex >= _itemsSource.RowCount)
        {
            return default;
        }

        var layout = GetActiveRowLayout();
        var defaultRowHeight = Math.Max(1d, RowHeight);
        var top = layout.GetRowTop(rowIndex);
        var height = defaultRowHeight;

        if (!_itemsSource.IsPlaceholder(rowIndex))
        {
            var row = _itemsSource.GetRow(rowIndex);
            height = layout.GetRowHeight(rowIndex, row, defaultRowHeight);
        }

        var width = Bounds.Width;
        if (double.IsNaN(width) || width <= 0)
        {
            width = _columnWidths.Count > 0 ? _columnWidths.Sum() : 0;
        }

        return new Rect(0, top, width, height);
    }

    internal Rect GetCellBoundsForAutomation(int rowIndex, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _columnWidths.Count)
        {
            return default;
        }

        var rowBounds = GetRowBoundsForAutomation(rowIndex);
        if (rowBounds.Height <= 0)
        {
            return default;
        }

        var width = _columnWidths[columnIndex];
        var left = columnIndex == 0 ? 0 : _columnOffsets[Math.Min(columnIndex - 1, _columnOffsets.Count - 1)];
        var scrollX = _scrollViewer?.Offset.X ?? 0;

        return new Rect(left - scrollX, rowBounds.Top, width, rowBounds.Height);
    }

    internal string GetAutomationCellText(FastTreeDataGridRow row, FastTreeDataGridColumn column) =>
        GetCellText(row, column);

    internal bool IsRowSelected(int index)
    {
        if (index < 0)
        {
            return false;
        }

        return _selectionModel.SelectedIndices.Contains(index);
    }

    internal void AutomationSelectSingleRow(int index)
    {
        if (index < 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            SetCurrentValue(SelectedIndexProperty, index);
            EnsureRowVisible(index);
        }, DispatcherPriority.Background);
    }

    internal void AutomationAddRowToSelection(int index)
    {
        if (index < 0 || _selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_selectionMode == FastTreeDataGridSelectionMode.Single)
            {
                SetCurrentValue(SelectedIndexProperty, index);
                EnsureRowVisible(index);
                return;
            }

            if (_selectionModel.SelectedIndices.Contains(index))
            {
                return;
            }

            _selectionModel.SelectRange(index, index, keepExisting: true);
            EnsureRowVisible(index);
        }, DispatcherPriority.Background);
    }

    internal void AutomationRemoveRowFromSelection(int index)
    {
        if (index < 0 || _selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_selectionMode == FastTreeDataGridSelectionMode.Single)
            {
                if (_selectionModel.SelectedIndices.Contains(index))
                {
                    _selectionModel.Clear();
                    SetCurrentValue(SelectedIndexProperty, -1);
                }

                return;
            }

            if (_selectionModel.SelectedIndices.Contains(index))
            {
                _selectionModel.Toggle(index);
            }
        }, DispatcherPriority.Background);
    }

    internal void AutomationSetRowExpansion(int rowIndex, bool expand)
    {
        if (_itemsSource is null || rowIndex < 0 || rowIndex >= _itemsSource.RowCount)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_itemsSource is null || rowIndex < 0 || rowIndex >= _itemsSource.RowCount)
            {
                return;
            }

            if (_itemsSource.IsPlaceholder(rowIndex))
            {
                return;
            }

            var row = _itemsSource.GetRow(rowIndex);
            if (!row.HasChildren || row.IsExpanded == expand)
            {
                return;
            }

            ToggleExpansionAt(rowIndex);
        }, DispatcherPriority.Background);
    }

    internal (double HorizontalOffset, double VerticalOffset, double ExtentWidth, double ExtentHeight, double ViewportWidth, double ViewportHeight) GetScrollMetrics()
    {
        if (_scrollViewer is null)
        {
            return (0d, 0d, 0d, 0d, 0d, 0d);
        }

        var extent = _scrollViewer.Extent;
        var viewport = _scrollViewer.Viewport;
        var offset = _scrollViewer.Offset;
        return (offset.X, offset.Y, extent.Width, extent.Height, viewport.Width, viewport.Height);
    }

    internal void AutomationScrollToOffset(double? horizontal, double? vertical)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer is null)
            {
                return;
            }

            var extent = _scrollViewer.Extent;
            var viewport = _scrollViewer.Viewport;
            var current = _scrollViewer.Offset;

            var maxX = Math.Max(0d, extent.Width - viewport.Width);
            var maxY = Math.Max(0d, extent.Height - viewport.Height);

            var targetX = horizontal.HasValue ? Math.Clamp(horizontal.Value, 0d, maxX) : current.X;
            var targetY = vertical.HasValue ? Math.Clamp(vertical.Value, 0d, maxY) : current.Y;

            _scrollViewer.Offset = new Vector(targetX, targetY);
        }, DispatcherPriority.Background);
    }

    private void OnFilterPresenterFilterChanged(int columnIndex, string? text) =>
        ApplyFilterValue(columnIndex, text, updatePresenterText: false);

    private void OnColumnFilterRequested(int columnIndex, ContentControl headerCell)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        _columnFilterFlyout ??= CreateColumnFilterFlyout();

        var currentValue = GetFilterText(column);
        _columnFilterFlyout.Show(headerCell, column, columnIndex, currentValue);
    }

    private FastTreeDataGridColumnFilterFlyout CreateColumnFilterFlyout()
    {
        var flyout = new FastTreeDataGridColumnFilterFlyout();
        flyout.Attach(this);
        return flyout;
    }

    private void OnColumnFilterCleared(int columnIndex) =>
        ApplyFilterValue(columnIndex, string.Empty, updatePresenterText: true);

    private FastTreeDataGridFilterDescriptor? CreateFilterDescriptor(FastTreeDataGridColumn column, string? text)
    {
        var input = text?.Trim();
        if (string.IsNullOrEmpty(input) || !column.CanUserFilter)
        {
            return null;
        }

        if (column.FilterFactory is { } factory)
        {
            return factory(input);
        }

        if (string.IsNullOrEmpty(column.ValueKey))
        {
            return null;
        }

        return new FastTreeDataGridFilterDescriptor
        {
            ColumnKey = column.ValueKey,
            Predicate = row =>
            {
                if (row.ValueProvider is not null)
                {
                    var value = row.ValueProvider.GetValue(row.Item, column.ValueKey);
                    return FilterMatches(value, input);
                }

                return FilterMatches(row.Item, input);
            },
        };
    }

    private static bool FilterMatches(object? value, string text)
    {
        if (value is null)
        {
            return false;
        }

        var comparison = StringComparison.CurrentCultureIgnoreCase;

        if (value is string s)
        {
            return s.IndexOf(text, comparison) >= 0;
        }

        if (value is IFormattable formattable)
        {
            var formatted = formattable.ToString(null, CultureInfo.CurrentCulture);
            return formatted?.IndexOf(text, comparison) >= 0;
        }

        return value.ToString()?.IndexOf(text, comparison) >= 0;
    }

    private void OnFilterRowVisibilityChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_filterPresenter is null)
        {
            return;
        }

        var isVisible = IsFilterRowVisible;
        _filterPresenter.IsVisible = isVisible;
        _filterPresenter.IsHitTestVisible = isVisible;
        if (!isVisible)
        {
            Focus();
        }

        _columnsDirty = true;
        RequestViewportUpdate();
    }

    private static FastTreeDataGridSortDirection GetNextSortDirection(FastTreeDataGridSortDirection current)
    {
        return current switch
        {
            FastTreeDataGridSortDirection.None => FastTreeDataGridSortDirection.Ascending,
            FastTreeDataGridSortDirection.Ascending => FastTreeDataGridSortDirection.Descending,
            _ => FastTreeDataGridSortDirection.None,
        };
    }

    private void ApplySortVisualState(bool requestUpdate)
    {
        foreach (var column in _columns)
        {
            column.SortDirection = FastTreeDataGridSortDirection.None;
            column.SortOrder = 0;
        }

        for (var i = 0; i < _sortDescriptions.Count; i++)
        {
            var description = _sortDescriptions[i];
            var column = description.Column;
            var order = i + 1;
            column.SortDirection = description.Direction;
            column.SortOrder = order;
            _sortDescriptions[i] = new FastTreeDataGridSortDescription(column, description.Direction, order);
        }

        if (requestUpdate)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
        }
    }

    private void RaiseSortRequested(int columnIndex, FastTreeDataGridSortDirection direction, KeyModifiers modifiers)
    {
        if (SortRequested is null || _columns.Count == 0 || columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        var descriptionsSnapshot = _sortDescriptions.Count == 0
            ? Array.Empty<FastTreeDataGridSortDescription>()
            : _sortDescriptions.ToArray();

        var args = new FastTreeDataGridSortEventArgs(column, columnIndex, direction, modifiers, descriptionsSnapshot);
        SortRequested?.Invoke(this, args);
    }

    private void OnColumnReorderRequested(int fromIndex, int insertIndex)
    {
        if (_columns.Count == 0 || (uint)fromIndex >= (uint)_columns.Count)
        {
            return;
        }

        var bounds = GetPinnedGroupBounds(_columns[fromIndex].PinnedPosition);
        if (bounds is null)
        {
            return;
        }

        var target = Math.Clamp(insertIndex, bounds.Value.First, bounds.Value.Last + 1);

        if (target > fromIndex)
        {
            target--;
        }

        target = Math.Clamp(target, bounds.Value.First, bounds.Value.Last);

        MoveColumnTo(fromIndex, target);
    }

    private void OnColumnPinRequested(int columnIndex, FastTreeDataGridPinnedPosition position)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanUserPin || column.PinnedPosition == position)
        {
            return;
        }

        column.PinnedPosition = position;
        NormalizeColumnOrderForPinning();
        _columnsDirty = true;
        RequestViewportUpdate();
    }

    private void OnColumnAutoSizeRequested(int columnIndex, bool includeAllRows)
    {
        AutoSizeColumn(columnIndex, includeAllRows);
    }

    private void OnAutoSizeAllRequested(bool includeAllRows)
    {
        for (var i = 0; i < _columns.Count; i++)
        {
            AutoSizeColumn(i, includeAllRows);
        }
    }

    private void OnExpandAllRequested()
    {
        ExpandAllGroups();
    }

    private void OnCollapseAllRequested()
    {
        CollapseAllGroups();
    }

    private void AutoSizeColumn(int columnIndex, bool includeAllRows)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count || _itemsSource is null)
        {
            return;
        }

        var column = _columns[columnIndex];
        if (!column.CanAutoSize)
        {
            return;
        }

        var measuredWidth = MeasureColumnWidth(columnIndex, includeAllRows);
        if (!double.IsFinite(measuredWidth) || measuredWidth <= 0)
        {
            return;
        }

        column.SizingMode = ColumnSizingMode.Pixel;
        column.PixelWidth = measuredWidth;
        column.CachedAutoWidth = measuredWidth;
        column.ActualWidth = measuredWidth;

        RecalculateColumns();
        _columnsDirty = true;
        _headerPresenter?.UpdateWidths(_columnWidths, _scrollViewer?.Offset.X ?? 0, GetViewportWidth());
        RequestViewportUpdate();
    }

    private void OnColumnMoveLeftRequested(int columnIndex)
    {
        MoveColumn(columnIndex, -1);
    }

    private void OnColumnMoveRightRequested(int columnIndex)
    {
        MoveColumn(columnIndex, 1);
    }

    private void MoveColumn(int columnIndex, int delta)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count || delta == 0)
        {
            return;
        }

        var column = _columns[columnIndex];
        var bounds = GetPinnedGroupBounds(column.PinnedPosition);
        if (bounds is null)
        {
            return;
        }

        var targetIndex = Math.Clamp(columnIndex + delta, bounds.Value.First, bounds.Value.Last);
        MoveColumnTo(columnIndex, targetIndex);
    }

    private void MoveColumnTo(int fromIndex, int targetIndex)
    {
        if (_columns.Count == 0 || (uint)fromIndex >= (uint)_columns.Count)
        {
            return;
        }

        targetIndex = Math.Clamp(targetIndex, 0, _columns.Count - 1);
        if (targetIndex == fromIndex)
        {
            return;
        }

        _columns.Move(fromIndex, targetIndex);

        RecalculateColumns();
        _headerPresenter?.BindColumns(_columns, _columnWidths, _scrollViewer?.Offset.X ?? 0, GetViewportWidth());
        RequestViewportUpdate();
    }

    private void OnColumnHideRequested(int columnIndex)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        _columns.RemoveAt(columnIndex);
        _columnsDirty = true;
        RecalculateColumns();
        _headerPresenter?.BindColumns(_columns, _columnWidths, _scrollViewer?.Offset.X ?? 0, GetViewportWidth());
        RequestViewportUpdate();
    }

    private double MeasureColumnWidth(int columnIndex, bool includeAllRows)
    {
        if (_itemsSource is null || columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return 0;
        }

        var column = _columns[columnIndex];
        var culture = CultureInfo.CurrentCulture;
        var typeface = Typeface.Default;
        var emSize = CalculateCellFontSize(RowHeight);
        var padding = 12d;
        var textBrush = Foreground ?? new SolidColorBrush(Color.FromRgb(33, 33, 33));

        double Measure(string? text)
        {
            var formatted = new FormattedText(text ?? string.Empty, culture, FlowDirection.LeftToRight, typeface, emSize, textBrush)
            {
                MaxTextWidth = double.PositiveInfinity,
                Trimming = TextTrimming.None,
            };

            return formatted.Width;
        }

        var headerText = column.Header?.ToString() ?? string.Empty;
        var max = Measure(headerText) + padding;
        var totalRows = _itemsSource.RowCount;
        if (totalRows == 0)
        {
            return Math.Max(max, column.MinWidth);
        }

        var limit = includeAllRows ? totalRows : Math.Min(totalRows, 200);
        var hierarchyIndex = GetHierarchyColumnIndex();

        for (var i = 0; i < limit; i++)
        {
            var row = _itemsSource.GetRow(i);
            if (_itemsSource.IsPlaceholder(i))
            {
                continue;
            }

            var text = GetCellText(row, column);
            var width = Measure(text) + padding;

            if (columnIndex == hierarchyIndex)
            {
                width += row.Level * IndentWidth + 12;
            }

            if (width > max)
            {
                max = width;
            }
        }

        var minWidth = Math.Max(column.MinWidth, 16);
        var maxWidth = double.IsFinite(column.MaxWidth) && column.MaxWidth > 0 ? column.MaxWidth : double.PositiveInfinity;

        return Math.Clamp(max, minWidth, maxWidth);
    }

    private (int First, int Last)? GetPinnedGroupBounds(FastTreeDataGridPinnedPosition position)
    {
        var first = -1;
        var last = -1;

        for (var i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].PinnedPosition != position)
            {
                continue;
            }

            if (first < 0)
            {
                first = i;
            }

            last = i;
        }

        if (first < 0)
        {
            return position == FastTreeDataGridPinnedPosition.None && _columns.Count > 0
                ? (0, _columns.Count - 1)
                : null;
        }

        return (first, last);
    }

    private static int CoerceInsertionIndex(int insertIndex, (int First, int Last) bounds)
    {
        if (insertIndex < bounds.First)
        {
            return bounds.First;
        }

        if (insertIndex > bounds.Last + 1)
        {
            return bounds.Last + 1;
        }

        return insertIndex;
    }

    private void NormalizeColumnOrderForPinning()
    {
        if (_columns.Count <= 1)
        {
            return;
        }

        var desired = new List<FastTreeDataGridColumn>(_columns.Count);
        foreach (var column in _columns)
        {
            if (column.PinnedPosition == FastTreeDataGridPinnedPosition.Left)
            {
                desired.Add(column);
            }
        }

        foreach (var column in _columns)
        {
            if (column.PinnedPosition == FastTreeDataGridPinnedPosition.None)
            {
                desired.Add(column);
            }
        }

        foreach (var column in _columns)
        {
            if (column.PinnedPosition == FastTreeDataGridPinnedPosition.Right)
            {
                desired.Add(column);
            }
        }

        for (var i = 0; i < desired.Count; i++)
        {
            var column = desired[i];
            var currentIndex = _columns.IndexOf(column);
            if (currentIndex != i && currentIndex >= 0)
            {
                _columns.Move(currentIndex, i);
            }
        }
    }

    private void RequestViewportUpdate()
    {
        if (_viewportUpdateQueued)
        {
            return;
        }

        _viewportUpdateQueued = true;

        Dispatcher.UIThread.Post(() =>
        {
            _viewportUpdateQueued = false;
            UpdateViewport();
        }, DispatcherPriority.Render);
    }

    private readonly struct PendingColumnWork
    {
        public PendingColumnWork(bool reset, FastTreeDataGridInvalidationRequest[] invalidations, int[] materialized)
        {
            Reset = reset;
            Invalidations = invalidations;
            Materialized = materialized;
        }

        public bool Reset { get; }

        public FastTreeDataGridInvalidationRequest[] Invalidations { get; }

        public int[] Materialized { get; }

        public bool HasWork => Reset || Invalidations.Length > 0 || Materialized.Length > 0;

        public static PendingColumnWork Empty { get; } = new(false, Array.Empty<FastTreeDataGridInvalidationRequest>(), Array.Empty<int>());
    }

    internal sealed class RowViewportState
    {
        public static readonly RowViewportState Empty = new(Array.Empty<int>(), Array.Empty<double>(), Array.Empty<bool>(), Array.Empty<FastTreeDataGridRow?>());

        public RowViewportState(int[] indices, double[] heights, bool[] placeholders, FastTreeDataGridRow?[] rows)
        {
            Indices = indices ?? Array.Empty<int>();
            Heights = heights ?? Array.Empty<double>();
            Placeholders = placeholders ?? Array.Empty<bool>();
            Rows = rows ?? Array.Empty<FastTreeDataGridRow?>();
        }

        public int[] Indices { get; }

        public double[] Heights { get; }

        public bool[] Placeholders { get; }

        public FastTreeDataGridRow?[] Rows { get; }
    }

    internal sealed class RowViewportDelta
    {
        public static readonly RowViewportDelta Empty = new(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());

        public RowViewportDelta(int[] enteredIndices, int[] exitedIndices, int[] changedIndices, int[] resizedIndices)
        {
            EnteredIndices = enteredIndices ?? Array.Empty<int>();
            ExitedIndices = exitedIndices ?? Array.Empty<int>();
            ChangedIndices = changedIndices ?? Array.Empty<int>();
            ResizedIndices = resizedIndices ?? Array.Empty<int>();
        }

        public int[] EnteredIndices { get; }

        public int[] ExitedIndices { get; }

        public int[] ChangedIndices { get; }

        public int[] ResizedIndices { get; }

        public bool HasChanges => EnteredIndices.Length > 0 || ExitedIndices.Length > 0 || ChangedIndices.Length > 0 || ResizedIndices.Length > 0;

        public int[] GetAffectedIndices()
        {
            if (!HasChanges)
            {
                return Array.Empty<int>();
            }

            var set = new HashSet<int>();
            foreach (var index in ChangedIndices)
            {
                set.Add(index);
            }

            foreach (var index in ResizedIndices)
            {
                set.Add(index);
            }

            return set.Count == 0 ? Array.Empty<int>() : set.ToArray();
        }

        public static RowViewportDelta Create(RowViewportState previous, RowViewportState current)
        {
            var previousIndices = previous.Indices;
            var currentIndices = current.Indices;

            if (previousIndices.Length == 0 && currentIndices.Length == 0)
            {
                return Empty;
            }

            var entered = new List<int>();
            var exited = new List<int>();
            var changed = new List<int>();
            var resized = new List<int>();

            var previousMap = new Dictionary<int, int>();
            for (var i = 0; i < previousIndices.Length; i++)
            {
                previousMap[previousIndices[i]] = i;
            }

            var currentMap = new Dictionary<int, int>();
            for (var i = 0; i < currentIndices.Length; i++)
            {
                currentMap[currentIndices[i]] = i;
            }

            foreach (var index in currentIndices)
            {
                if (!previousMap.ContainsKey(index))
                {
                    entered.Add(index);
                }
            }

            foreach (var index in previousIndices)
            {
                if (!currentMap.ContainsKey(index))
                {
                    exited.Add(index);
                }
            }

            const double heightEpsilon = 0.5;

            foreach (var index in currentIndices)
            {
                if (!previousMap.TryGetValue(index, out var previousPosition))
                {
                    continue;
                }

                var currentPosition = currentMap[index];
                var previousPlaceholder = previous.Placeholders.Length > previousPosition && previous.Placeholders[previousPosition];
                var currentPlaceholder = current.Placeholders.Length > currentPosition && current.Placeholders[currentPosition];

                if (previousPlaceholder != currentPlaceholder)
                {
                    changed.Add(index);
                    continue;
                }

                var previousRow = previous.Rows.Length > previousPosition ? previous.Rows[previousPosition] : null;
                var currentRow = current.Rows.Length > currentPosition ? current.Rows[currentPosition] : null;
                if (!ReferenceEquals(previousRow, currentRow))
                {
                    changed.Add(index);
                }

                var previousHeight = previous.Heights.Length > previousPosition ? previous.Heights[previousPosition] : 0d;
                var currentHeight = current.Heights.Length > currentPosition ? current.Heights[currentPosition] : 0d;

                if (Math.Abs(previousHeight - currentHeight) > heightEpsilon)
                {
                    resized.Add(index);
                }
            }

            return new RowViewportDelta(entered.ToArray(), exited.ToArray(), changed.ToArray(), resized.ToArray());
        }
    }

    internal sealed class ColumnViewportState
    {
        public static readonly ColumnViewportState Empty = new(
            Array.Empty<int>(),
            Array.Empty<double>(),
            Array.Empty<double>(),
            Array.Empty<FastTreeDataGridColumnDescriptor?>(),
            Array.Empty<bool>());

        public ColumnViewportState(
            int[] indices,
            double[] offsets,
            double[] widths,
            FastTreeDataGridColumnDescriptor?[] descriptors,
            bool[] placeholders)
        {
            Indices = indices ?? Array.Empty<int>();
            Offsets = offsets ?? Array.Empty<double>();
            Widths = widths ?? Array.Empty<double>();
            Descriptors = descriptors ?? Array.Empty<FastTreeDataGridColumnDescriptor?>();
            Placeholders = placeholders ?? Array.Empty<bool>();
        }

        public int[] Indices { get; }

        public double[] Offsets { get; }

        public double[] Widths { get; }

        public FastTreeDataGridColumnDescriptor?[] Descriptors { get; }

        public bool[] Placeholders { get; }
    }

    internal sealed class ColumnViewportDelta
    {
        public static readonly ColumnViewportDelta Empty = new(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());

        public ColumnViewportDelta(int[] enteredIndices, int[] exitedIndices, int[] changedIndices, int[] resizedIndices)
        {
            EnteredIndices = enteredIndices ?? Array.Empty<int>();
            ExitedIndices = exitedIndices ?? Array.Empty<int>();
            ChangedIndices = changedIndices ?? Array.Empty<int>();
            ResizedIndices = resizedIndices ?? Array.Empty<int>();
        }

        public int[] EnteredIndices { get; }

        public int[] ExitedIndices { get; }

        public int[] ChangedIndices { get; }

        public int[] ResizedIndices { get; }

        public bool HasChanges => EnteredIndices.Length > 0 || ExitedIndices.Length > 0 || ChangedIndices.Length > 0 || ResizedIndices.Length > 0;

        public int[] GetAffectedIndices()
        {
            if (!HasChanges)
            {
                return Array.Empty<int>();
            }

            var set = new HashSet<int>();
            foreach (var index in EnteredIndices)
            {
                set.Add(index);
            }

            foreach (var index in ChangedIndices)
            {
                set.Add(index);
            }

            foreach (var index in ResizedIndices)
            {
                set.Add(index);
            }

            return set.Count == 0 ? Array.Empty<int>() : set.ToArray();
        }

        public static ColumnViewportDelta Create(ColumnViewportState previous, ColumnViewportState current)
        {
            var previousIndices = previous.Indices;
            var currentIndices = current.Indices;

            if (previousIndices.Length == 0 && currentIndices.Length == 0)
            {
                return Empty;
            }

            var entered = new List<int>();
            var exited = new List<int>();
            var changed = new List<int>();
            var resized = new List<int>();

            var previousMap = new Dictionary<int, int>();
            for (var i = 0; i < previousIndices.Length; i++)
            {
                previousMap[previousIndices[i]] = i;
            }

            var currentMap = new Dictionary<int, int>();
            for (var i = 0; i < currentIndices.Length; i++)
            {
                currentMap[currentIndices[i]] = i;
            }

            foreach (var index in currentIndices)
            {
                if (!previousMap.ContainsKey(index))
                {
                    entered.Add(index);
                }
            }

            foreach (var index in previousIndices)
            {
                if (!currentMap.ContainsKey(index))
                {
                    exited.Add(index);
                }
            }

            const double widthEpsilon = 0.5;
            const double positionEpsilon = 0.5;

            foreach (var index in currentIndices)
            {
                if (!previousMap.TryGetValue(index, out var previousPosition))
                {
                    continue;
                }

                var currentPosition = currentMap[index];
                var previousPlaceholder = previous.Placeholders.Length > previousPosition && previous.Placeholders[previousPosition];
                var currentPlaceholder = current.Placeholders.Length > currentPosition && current.Placeholders[currentPosition];

                if (previousPlaceholder != currentPlaceholder)
                {
                    changed.Add(index);
                    continue;
                }

                var previousDescriptor = previous.Descriptors.Length > previousPosition ? previous.Descriptors[previousPosition] : null;
                var currentDescriptor = current.Descriptors.Length > currentPosition ? current.Descriptors[currentPosition] : null;

                if (!ReferenceEquals(previousDescriptor, currentDescriptor))
                {
                    var previousKey = previousDescriptor?.Key;
                    var currentKey = currentDescriptor?.Key;
                    if (!string.Equals(previousKey, currentKey, StringComparison.Ordinal))
                    {
                        changed.Add(index);
                        continue;
                    }
                }

                var previousWidth = previous.Widths.Length > previousPosition ? previous.Widths[previousPosition] : 0d;
                var currentWidth = current.Widths.Length > currentPosition ? current.Widths[currentPosition] : 0d;

                if (Math.Abs(previousWidth - currentWidth) > widthEpsilon)
                {
                    resized.Add(index);
                    continue;
                }

                var previousOffset = previous.Offsets.Length > previousPosition ? previous.Offsets[previousPosition] : 0d;
                var currentOffset = current.Offsets.Length > currentPosition ? current.Offsets[currentPosition] : 0d;

                if (Math.Abs(previousOffset - currentOffset) > positionEpsilon)
                {
                    changed.Add(index);
                }
            }

            return new ColumnViewportDelta(entered.ToArray(), exited.ToArray(), changed.ToArray(), resized.ToArray());
        }
    }

    private enum ViewportUpdateMode
    {
        None,
        ColumnPatch,
        RowPatch,
        CombinedPatch,
        Rebuild,
    }

    private readonly struct ViewportDecision
    {
        public ViewportDecision(ViewportUpdateMode mode, RowViewportDelta rowDelta, ColumnViewportDelta columnDelta, PendingColumnWork columnWork)
        {
            Mode = mode;
            RowDelta = rowDelta;
            ColumnDelta = columnDelta;
            ColumnWork = columnWork;
        }

        public ViewportUpdateMode Mode { get; }
        public RowViewportDelta RowDelta { get; }
        public ColumnViewportDelta ColumnDelta { get; }
        public PendingColumnWork ColumnWork { get; }

        public bool HasRowWork => RowDelta.HasChanges;
        public bool HasColumnWork => ColumnWork.HasWork || ColumnDelta.HasChanges;
    }

    private sealed class ViewportCoordinator
    {
        public ViewportDecision Decide(RowViewportDelta rowDelta, ColumnViewportDelta columnDelta, PendingColumnWork columnWork, bool columnsDirty, bool autoWidthChanged)
        {
            if (columnsDirty || autoWidthChanged || columnWork.Reset)
            {
                return new ViewportDecision(ViewportUpdateMode.Rebuild, rowDelta, columnDelta, columnWork);
            }

            var hasRowWork = rowDelta.HasChanges;
            var hasColumnWork = columnWork.HasWork || columnDelta.HasChanges;

            if (!hasRowWork && !hasColumnWork)
            {
                return new ViewportDecision(ViewportUpdateMode.None, rowDelta, columnDelta, columnWork);
            }

            if (rowDelta.EnteredIndices.Length > 0 || rowDelta.ExitedIndices.Length > 0)
            {
                return new ViewportDecision(ViewportUpdateMode.Rebuild, rowDelta, columnDelta, columnWork);
            }

            if (hasRowWork && hasColumnWork)
            {
                return new ViewportDecision(ViewportUpdateMode.CombinedPatch, rowDelta, columnDelta, columnWork);
            }

            if (hasRowWork)
            {
                return new ViewportDecision(ViewportUpdateMode.RowPatch, rowDelta, columnDelta, columnWork);
            }

            return new ViewportDecision(ViewportUpdateMode.ColumnPatch, rowDelta, columnDelta, columnWork);
        }
    }


    private static PendingColumnWork CreatePendingWorkFromViewportDelta(ColumnViewportDelta delta)
    {
        if (delta is null || !delta.HasChanges)
        {
            return PendingColumnWork.Empty;
        }

        var affectedIndices = delta.GetAffectedIndices();
        var invalidations = CreateInvalidationRequestsFromIndices(affectedIndices);
        var materialized = delta.EnteredIndices.Length > 0 ? (int[])delta.EnteredIndices.Clone() : Array.Empty<int>();

        if (invalidations.Length == 0 && materialized.Length == 0)
        {
            return PendingColumnWork.Empty;
        }

        return new PendingColumnWork(reset: false, invalidations, materialized);
    }

    private static FastTreeDataGridInvalidationRequest[] CreateInvalidationRequestsFromIndices(int[] indices)
    {
        if (indices is null || indices.Length == 0)
        {
            return Array.Empty<FastTreeDataGridInvalidationRequest>();
        }

        var buffer = indices.ToArray();
        Array.Sort(buffer);

        var requests = new List<FastTreeDataGridInvalidationRequest>();
        var currentStart = buffer[0];
        var count = 1;

        for (var i = 1; i < buffer.Length; i++)
        {
            var value = buffer[i];
            if (value == currentStart + count)
            {
                count++;
                continue;
            }

            requests.Add(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, currentStart, count));
            currentStart = value;
            count = 1;
        }

        requests.Add(new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, currentStart, count));
        return requests.ToArray();
    }

    private readonly struct CellBuildContext
    {
        public CellBuildContext(double toggleSize, double togglePadding, double cellPadding, CultureInfo culture, Typeface typeface, IBrush textBrush)
        {
            ToggleSize = toggleSize;
            TogglePadding = togglePadding;
            CellPadding = cellPadding;
            Culture = culture;
            Typeface = typeface;
            TextBrush = textBrush;
        }

        public double ToggleSize { get; }
        public double TogglePadding { get; }
        public double CellPadding { get; }
        public CultureInfo Culture { get; }
        public Typeface Typeface { get; }
        public IBrush TextBrush { get; }
    }

    private readonly struct RowBuildInfo
    {
        public RowBuildInfo(
            FastTreeDataGridRow row,
            int rowIndex,
            double top,
            double height,
            bool isSelected,
            bool hasChildren,
            bool isExpanded,
            Rect toggleRect,
            bool isGroup,
            bool isSummary,
            bool isPlaceholder)
        {
            Row = row;
            RowIndex = rowIndex;
            Top = top;
            Height = height;
            IsSelected = isSelected;
            HasChildren = hasChildren;
            IsExpanded = isExpanded;
            ToggleRect = toggleRect;
            IsGroup = isGroup;
            IsSummary = isSummary;
            IsPlaceholder = isPlaceholder;
        }

        public FastTreeDataGridRow Row { get; }
        public int RowIndex { get; }
        public double Top { get; }
        public double Height { get; }
        public bool IsSelected { get; }
        public bool HasChildren { get; }
        public bool IsExpanded { get; }
        public Rect ToggleRect { get; }
        public bool IsGroup { get; }
        public bool IsSummary { get; }
        public bool IsPlaceholder { get; }
    }

    private readonly struct ColumnViewportEntry
    {
        public ColumnViewportEntry(double offset, double width, FastTreeDataGridColumnDescriptor? descriptor, bool isPlaceholder)
        {
            Offset = offset;
            Width = width;
            Descriptor = descriptor;
            IsPlaceholder = isPlaceholder;
        }

        public double Offset { get; }
        public double Width { get; }
        public FastTreeDataGridColumnDescriptor? Descriptor { get; }
        public bool IsPlaceholder { get; }
    }

    private static Dictionary<int, ColumnViewportEntry> CreateColumnViewportEntryMap(ColumnViewportState state)
    {
        if (state.Indices.Length == 0)
        {
            return new Dictionary<int, ColumnViewportEntry>();
        }

        var map = new Dictionary<int, ColumnViewportEntry>(state.Indices.Length);
        for (var i = 0; i < state.Indices.Length; i++)
        {
            var columnIndex = state.Indices[i];
            var offset = state.Offsets.Length > i ? state.Offsets[i] : 0d;
            var width = state.Widths.Length > i ? state.Widths[i] : 0d;
            var descriptor = state.Descriptors.Length > i ? state.Descriptors[i] : null;
            var placeholder = state.Placeholders.Length > i && state.Placeholders[i];
            map[columnIndex] = new ColumnViewportEntry(offset, width, descriptor, placeholder);
        }

        return map;
    }

    private readonly struct CellBuildResult
    {
        public CellBuildResult(FastTreeDataGridPresenter.CellRenderInfo cell, FastTreeDataGridCellValidationState validation, bool autoWidthUpdated)
        {
            Cell = cell;
            Validation = validation;
            AutoWidthUpdated = autoWidthUpdated;
        }

        public FastTreeDataGridPresenter.CellRenderInfo Cell { get; }
        public FastTreeDataGridCellValidationState Validation { get; }
        public bool AutoWidthUpdated { get; }
    }

    private void QueueColumnReset()
    {
        if (!_isAttachedToVisualTree)
        {
            RequestViewportUpdate();
            return;
        }

        lock (_viewportCoordinatorLock)
        {
            _pendingColumnReset = true;
            _pendingColumnInvalidations.Clear();
            _pendingColumnMaterializations.Clear();
        }

        RequestViewportUpdate();
    }

    private void QueueColumnInvalidation(FastTreeDataGridInvalidationRequest request)
    {
        if (!_isAttachedToVisualTree)
        {
            RequestViewportUpdate();
            return;
        }

        var shouldSchedule = false;
        lock (_viewportCoordinatorLock)
        {
            if (_pendingColumnReset)
            {
                shouldSchedule = true;
            }
            else if (request.Kind == FastTreeDataGridInvalidationKind.Full)
            {
                _pendingColumnReset = true;
                _pendingColumnInvalidations.Clear();
                _pendingColumnMaterializations.Clear();
                shouldSchedule = true;
            }
            else
            {
                _pendingColumnInvalidations.Add(request);
                shouldSchedule = true;
            }
        }

        if (shouldSchedule)
        {
            RequestViewportUpdate();
        }
    }

    private void QueueColumnMaterialized(int index)
    {
        if (!_isAttachedToVisualTree)
        {
            RequestViewportUpdate();
            return;
        }

        var shouldSchedule = false;
        lock (_viewportCoordinatorLock)
        {
            if (_pendingColumnReset)
            {
                shouldSchedule = true;
            }
            else
            {
                _pendingColumnMaterializations.Add(index);
                shouldSchedule = true;
            }
        }

        if (shouldSchedule)
        {
            RequestViewportUpdate();
        }
    }

    private PendingColumnWork ConsumePendingColumnWork()
    {
        lock (_viewportCoordinatorLock)
        {
            if (!_pendingColumnReset && _pendingColumnInvalidations.Count == 0 && _pendingColumnMaterializations.Count == 0)
            {
                return PendingColumnWork.Empty;
            }

            var invalidations = _pendingColumnInvalidations.Count > 0
                ? _pendingColumnInvalidations.ToArray()
                : Array.Empty<FastTreeDataGridInvalidationRequest>();

            var materialized = _pendingColumnMaterializations.Count > 0
                ? _pendingColumnMaterializations.ToArray()
                : Array.Empty<int>();

            var work = new PendingColumnWork(_pendingColumnReset, invalidations, materialized);

            _pendingColumnReset = false;
            _pendingColumnInvalidations.Clear();
            _pendingColumnMaterializations.Clear();

            return work;
        }
    }

    private void UpdateViewport()
    {
        var stopwatch = Stopwatch.StartNew();
        var pendingColumnWork = ConsumePendingColumnWork();
        if (!_isAttachedToVisualTree || _scrollViewer is null || _presenter is null || _itemsSource is null)
        {
            ResetHorizontalScrollMeasurement();
            RecordViewportMetrics(stopwatch, 0, 0);
            return;
        }

        if (pendingColumnWork.Reset)
        {
            _columnsDirty = true;
        }

        if (_columns.Count == 0)
        {
            _presenter.UpdateContent(Array.Empty<FastTreeDataGridPresenter.RowRenderInfo>(), 0, 0, Array.Empty<double>());
            ResetHorizontalScrollMeasurement();
            RecordViewportMetrics(stopwatch, 0, 0);
            return;
        }

        var columnsWereDirty = _columnsDirty;
        var autoWidthTriggered = _autoWidthChanged;

        if (_columnsDirty || _autoWidthChanged)
        {
            RecalculateColumns();
            _columnsDirty = false;
            _autoWidthChanged = false;
        }

        var layout = GetActiveRowLayout();
        var defaultRowHeight = Math.Max(1d, RowHeight);
        var totalRows = _itemsSource.RowCount;
        var viewport = _scrollViewer.Viewport;
        var offset = _scrollViewer.Offset;

        _selectionModel?.CoerceSelection(totalRows);
        if (_selectionModel is not null)
        {
            SetSelectedIndicesInternal(_selectionModel.SelectedIndices);
        }

        HashSet<int>? selectionLookup = null;
        if (_selectedIndices.Count > 0)
        {
            selectionLookup = _selectedIndices is IReadOnlyCollection<int> collection
                ? new HashSet<int>(collection)
                : new HashSet<int>(_selectedIndices);
        }

        var viewportHeight = viewport.Height > 0 ? viewport.Height : Bounds.Height;
        var viewportWidth = GetViewportWidth();
        var totalHeight = layout.GetTotalHeight(viewportHeight, defaultRowHeight, totalRows);

        var leftIndices = new List<int>();
        var rightIndices = new List<int>();
        var bodyIndices = new List<int>();

        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            switch (column.PinnedPosition)
            {
                case FastTreeDataGridPinnedPosition.Left:
                    leftIndices.Add(i);
                    break;
                case FastTreeDataGridPinnedPosition.Right:
                    rightIndices.Add(i);
                    break;
                default:
                    bodyIndices.Add(i);
                    break;
            }
        }

        double SumWidths(List<int> indices)
        {
            var total = 0d;
            foreach (var index in indices)
            {
                if (index < _columnWidths.Count && double.IsFinite(_columnWidths[index]))
                {
                    total += Math.Max(0, _columnWidths[index]);
                }
            }

            return total;
        }

        var leftWidth = SumWidths(leftIndices);
        var rightWidth = SumWidths(rightIndices);
        var bodyWidth = SumWidths(bodyIndices);
        var totalContentWidth = leftWidth + bodyWidth + rightWidth;
        var totalWidth = Math.Max(totalContentWidth, viewportWidth);

        var columnPositions = new double[_columns.Count];
        var leftOffset = 0d;
        foreach (var index in leftIndices)
        {
            columnPositions[index] = leftOffset + offset.X;
            leftOffset += _columnWidths[index];
        }

        var bodyOffset = leftWidth;
        foreach (var index in bodyIndices)
        {
            columnPositions[index] = bodyOffset;
            bodyOffset += _columnWidths[index];
        }

        var viewportForRight = viewportWidth > 0 ? viewportWidth : totalContentWidth;
        var baseRight = Math.Max(leftWidth, viewportForRight - rightWidth);
        if (baseRight < leftWidth)
        {
            baseRight = leftWidth;
        }

        var rightOffset = 0d;
        foreach (var index in rightIndices)
        {
            columnPositions[index] = baseRight + rightOffset + offset.X;
            rightOffset += _columnWidths[index];
        }

        _columnOffsets.Clear();
        for (var i = 0; i < _columns.Count; i++)
        {
            _columnOffsets.Add(columnPositions[i] + _columnWidths[i]);
        }

        var viewportLeft = offset.X;
        var viewportRight = viewportLeft + viewportWidth;
        UpdateVisibleColumns(columnPositions, _columnWidths, leftIndices, bodyIndices, rightIndices, viewportLeft, viewportRight);

        var columnPlaceholderStates = new Dictionary<int, bool>(_visibleColumnIndices.Count);
        foreach (var columnIndex in _visibleColumnIndices)
        {
            if ((uint)columnIndex >= (uint)_columns.Count)
            {
                continue;
            }

            var isMaterialized = _columnSource.TryGetMaterializedColumn(columnIndex, out _);
            columnPlaceholderStates[columnIndex] = !isMaterialized;
        }

        bool IsColumnPlaceholder(int columnIndex)
        {
            if (columnPlaceholderStates.TryGetValue(columnIndex, out var placeholder))
            {
                return placeholder;
            }

            var hasDescriptor = _columnSource.TryGetMaterializedColumn(columnIndex, out _);
            placeholder = !hasDescriptor;
            columnPlaceholderStates[columnIndex] = placeholder;
            return placeholder;
        }

        ColumnViewportState viewportSnapshot;
        if (_visibleColumnIndices.Count > 0)
        {
            var count = _visibleColumnIndices.Count;
            var indices = new int[count];
            var offsetsSnapshot = new double[count];
            var widths = new double[count];
            var descriptors = new FastTreeDataGridColumnDescriptor?[count];
            var placeholders = new bool[count];

            for (var i = 0; i < count; i++)
            {
                var columnIndex = _visibleColumnIndices[i];
                indices[i] = columnIndex;
                offsetsSnapshot[i] = columnIndex < columnPositions.Length ? columnPositions[columnIndex] : 0d;
                widths[i] = columnIndex < _columnWidths.Count ? _columnWidths[columnIndex] : 0d;
                var placeholder = IsColumnPlaceholder(columnIndex);
                placeholders[i] = placeholder;
                descriptors[i] = !placeholder && _columnSource.TryGetMaterializedColumn(columnIndex, out var descriptor)
                    ? descriptor
                    : null;
            }

            viewportSnapshot = new ColumnViewportState(indices, offsetsSnapshot, widths, descriptors, placeholders);
        }
        else
        {
            viewportSnapshot = ColumnViewportState.Empty;
        }

        var previousViewportState = _currentColumnViewportState;
        _currentColumnViewportState = viewportSnapshot;

        var viewportDelta = ColumnViewportDelta.Create(previousViewportState, viewportSnapshot);
        _currentColumnViewportDelta = viewportDelta;

        if (!pendingColumnWork.Reset && !pendingColumnWork.HasWork && viewportDelta.HasChanges)
        {
            var deltaWork = CreatePendingWorkFromViewportDelta(viewportDelta);
            if (deltaWork.HasWork)
            {
                lock (_viewportCoordinatorLock)
                {
                    foreach (var request in deltaWork.Invalidations)
                    {
                        _pendingColumnInvalidations.Add(request);
                    }

                    if (deltaWork.Materialized.Length > 0)
                    {
                        foreach (var index in deltaWork.Materialized)
                        {
                            _pendingColumnMaterializations.Add(index);
                        }
                    }
                }

                pendingColumnWork = ConsumePendingColumnWork();
            }
        }

        if (_headerPresenter is not null)
        {
            _headerPresenter.BindColumns(_columns, _columnWidths, offset.X, viewportWidth);
        }

        if (_filterPresenter is not null)
        {
            _filterPresenter.FilterHeight = RowHeight;
            _filterPresenter.BindColumns(_columns, _columnWidths, columnPositions, totalWidth);
            _filterPresenter.IsVisible = IsFilterRowVisible;
            _filterPresenter.IsHitTestVisible = IsFilterRowVisible;
        }

        UpdateHeaderScroll(offset.X);

        var buffer = 2;
        var range = layout.GetVisibleRange(offset.Y, viewportHeight, defaultRowHeight, totalRows, buffer);

        if (range.IsEmpty)
        {
            _presenter.UpdateContent(Array.Empty<FastTreeDataGridPresenter.RowRenderInfo>(), totalWidth, totalHeight, _columnOffsets);
            CompleteHorizontalScrollMeasurement(offset.X, viewportWidth, stopwatch);
            RecordViewportMetrics(stopwatch, 0, 0);
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var typeface = Typeface.Default;
        var textBrush = Foreground ?? new SolidColorBrush(Color.FromRgb(33, 33, 33));
        const double toggleSize = 12;
        const double togglePadding = 4;
        const double cellPadding = 6;
        var hierarchyColumnIndex = GetHierarchyColumnIndex();
        var context = new CellBuildContext(toggleSize, togglePadding, cellPadding, culture, typeface, textBrush);

        var rowCount = Math.Max(0, range.LastIndexExclusive - range.FirstIndex);
        var rowIndices = rowCount > 0 ? new int[rowCount] : Array.Empty<int>();
        var rowHeights = rowCount > 0 ? new double[rowCount] : Array.Empty<double>();
        var rowPlaceholders = rowCount > 0 ? new bool[rowCount] : Array.Empty<bool>();
        var rowRows = rowCount > 0 ? new FastTreeDataGridRow?[rowCount] : Array.Empty<FastTreeDataGridRow?>();
        var rowBuildInfos = new List<RowBuildInfo>(rowCount);

        var toggleColumnStart = hierarchyColumnIndex >= 0 && hierarchyColumnIndex < columnPositions.Length
            ? columnPositions[hierarchyColumnIndex]
            : 0;
        var rowTop = range.FirstRowTop;

        for (var i = 0; i < rowCount; i++)
        {
            var rowIndex = range.FirstIndex + i;
            var isPlaceholder = _virtualizationProvider?.IsPlaceholder(rowIndex) == true;
            var row = _itemsSource.GetRow(rowIndex);
            var rowHeight = layout.GetRowHeight(rowIndex, row, defaultRowHeight);
            var hasChildren = row.HasChildren;
            var toggleRect = hasChildren
                ? new Rect(Math.Max(0, toggleColumnStart + row.Level * IndentWidth + togglePadding), rowTop + (rowHeight - toggleSize) / 2, toggleSize, toggleSize)
                : default;
            var isSelected = selectionLookup?.Contains(rowIndex) ?? false;

            rowBuildInfos.Add(new RowBuildInfo(
                row,
                rowIndex,
                rowTop,
                rowHeight,
                isSelected,
                hasChildren,
                row.IsExpanded,
                toggleRect,
                row.IsGroup,
                row.IsSummary,
                isPlaceholder));

            rowIndices[i] = rowIndex;
            rowHeights[i] = rowHeight;
            rowPlaceholders[i] = isPlaceholder;
            rowRows[i] = row;
            rowTop += rowHeight;
        }

        var newRowState = rowCount > 0
            ? new RowViewportState(rowIndices, rowHeights, rowPlaceholders, rowRows)
            : RowViewportState.Empty;
        var previousRowState = _currentRowViewportState;
        _currentRowViewportState = newRowState;
        var rowDelta = RowViewportDelta.Create(previousRowState, newRowState);
        _currentRowViewportDelta = rowDelta;

        var decision = _viewportCoordinator.Decide(rowDelta, viewportDelta, pendingColumnWork, columnsWereDirty, autoWidthTriggered);
        pendingColumnWork = decision.ColumnWork;

        if (_columnScheduler is not null)
        {
            if (_visibleColumnIndices.Count > 0)
            {
                if (decision.HasColumnWork || decision.Mode == ViewportUpdateMode.Rebuild)
                {
                    var minColumn = int.MaxValue;
                    var maxColumn = int.MinValue;
                    foreach (var index in _visibleColumnIndices)
                    {
                        if (index < minColumn)
                        {
                            minColumn = index;
                        }

                        if (index > maxColumn)
                        {
                            maxColumn = index;
                        }
                    }

                    if (minColumn >= 0 && maxColumn >= minColumn)
                    {
                        var count = maxColumn - minColumn + 1;
                        var radius = Math.Max(0, _virtualizationSettings.PrefetchRadius);
                        _columnScheduler.Request(new FastTreeDataGridColumnViewportRequest(minColumn, count, radius));
                    }
                }
            }
            else
            {
                _columnScheduler.CancelAll();
            }
        }

        if (decision.Mode == ViewportUpdateMode.None)
        {
            CompleteHorizontalScrollMeasurement(offset.X, viewportWidth, stopwatch);
            var visibleRows = _presenter.VisibleRows;
            var placeholderRows = visibleRows.Count(static r => r.IsPlaceholder);
            RecordViewportMetrics(stopwatch, visibleRows.Count, placeholderRows);
            return;
        }

        var patchAutoWidth = false;
        var patchApplied = false;

        switch (decision.Mode)
        {
            case ViewportUpdateMode.ColumnPatch:
                patchApplied = TryApplyColumnPatch(
                    pendingColumnWork,
                    range,
                    layout,
                    totalWidth,
                    totalHeight,
                    columnPositions,
                    _columnWidths,
                    previousViewportState,
                    viewportSnapshot,
                    context,
                    hierarchyColumnIndex,
                    IsColumnPlaceholder,
                    out patchAutoWidth);
                break;
            case ViewportUpdateMode.RowPatch:
                patchApplied = TryApplyRowPatch(rowBuildInfos, rowDelta, layout, totalWidth, totalHeight, columnPositions, _columnWidths, context, hierarchyColumnIndex, IsColumnPlaceholder, out patchAutoWidth);
                break;
            case ViewportUpdateMode.CombinedPatch:
                patchApplied = TryApplyCombinedPatch(
                    pendingColumnWork,
                    rowBuildInfos,
                    rowDelta,
                    range,
                    layout,
                    totalWidth,
                    totalHeight,
                    columnPositions,
                    _columnWidths,
                    previousViewportState,
                    viewportSnapshot,
                    context,
                    hierarchyColumnIndex,
                    IsColumnPlaceholder,
                    out patchAutoWidth);
                break;
        }

        if (patchApplied && decision.Mode != ViewportUpdateMode.Rebuild)
        {
            UpdateSelectionIndicators();
            OnViewportUpdatedForEditing(_presenter.VisibleRows);
            var placeholderPatchedCount = _presenter.VisibleRows.Count(static r => r.IsPlaceholder);
            CompleteHorizontalScrollMeasurement(offset.X, viewportWidth, stopwatch);
            RecordViewportMetrics(stopwatch, _presenter.VisibleRows.Count, placeholderPatchedCount);
            FastTreeDataGridVirtualizationDiagnostics.ColumnPlaceholderDuration.Record(placeholderPatchedCount > 0 ? stopwatch.Elapsed.TotalMilliseconds : 0);

            if (patchAutoWidth)
            {
                _autoWidthChanged = true;
                RequestViewportUpdate();
            }

            return;
        }

        var requestCount = Math.Max(0, range.LastIndexExclusive - range.FirstIndex);
        var prefetchRadius = Math.Max(buffer, 0);
        if (requestCount > 0)
        {
            _viewportScheduler?.Request(new FastTreeDataGridViewportRequest(range.FirstIndex, requestCount, prefetchRadius));
        }

        var rows = new List<FastTreeDataGridPresenter.RowRenderInfo>(rowBuildInfos.Count);
        var autoWidthUpdated = false;
        long rebuiltCells = 0;

        foreach (var info in rowBuildInfos)
        {
            var rowInfo = new FastTreeDataGridPresenter.RowRenderInfo(
                info.Row,
                info.RowIndex,
                info.Top,
                info.Height,
                info.IsSelected,
                info.HasChildren,
                info.IsExpanded,
                info.ToggleRect,
                info.IsGroup,
                info.IsSummary,
                info.IsPlaceholder);

            foreach (var columnIndex in _visibleColumnIndices)
            {
                if ((uint)columnIndex >= (uint)_columns.Count || (uint)columnIndex >= (uint)_columnWidths.Count || columnIndex >= columnPositions.Length)
                {
                    continue;
                }

                var column = _columns[columnIndex];
                var columnPosition = columnPositions[columnIndex];
                var columnWidth = _columnWidths[columnIndex];
                var columnPlaceholder = IsColumnPlaceholder(columnIndex);
                var cellIsPlaceholder = info.IsPlaceholder || columnPlaceholder;

                FastTreeDataGridPresenter.CellRenderInfo? cachedCell = null;
                if (!cellIsPlaceholder && _presenter is not null && _presenter.TryTakeCachedCell(info.RowIndex, columnIndex, out var reusedCell))
                {
                    cachedCell = reusedCell;
                }

                var isCellSelected = false;
                if (IsCellSelection && CellSelectionModel is IFastTreeDataGridCellSelectionModel cellSelectionModel)
                {
                    var cellIndex = new FastTreeDataGridCellIndex(info.RowIndex, columnIndex);
                    isCellSelected = cellSelectionModel.SelectedCells.Contains(cellIndex);
                }

                var result = BuildCell(
                    rowInfo,
                    info.Row,
                    info.RowIndex,
                    column,
                    columnIndex,
                    columnPosition,
                    columnWidth,
                    info.Top,
                    info.Height,
                    info.IsPlaceholder,
                    columnPlaceholder,
                    isCellSelected,
                    hierarchyColumnIndex,
                    context,
                    cachedCell);

                rowInfo.Cells.Add(result.Cell);
                rebuiltCells++;
                autoWidthUpdated |= result.AutoWidthUpdated;
            }

            if (info.IsPlaceholder)
            {
                layout.InvalidateRow(info.RowIndex);
            }

            rowInfo.Validation = ComputeRowValidation(info.Row, rowInfo);

            rows.Add(rowInfo);
        }

        _presenter.UpdateContent(rows, totalWidth, totalHeight, _columnOffsets);
        if (rebuiltCells > 0)
        {
            FastTreeDataGridVirtualizationDiagnostics.CellsRebuilt.Add(rebuiltCells);
        }
        UpdateSelectionIndicators();
        OnViewportUpdatedForEditing(rows);

        var placeholderCount = rows.Count(static r => r.IsPlaceholder);
        CompleteHorizontalScrollMeasurement(offset.X, viewportWidth, stopwatch);
        RecordViewportMetrics(stopwatch, rows.Count, placeholderCount);

        if (autoWidthUpdated)
        {
            _autoWidthChanged = true;
            RequestViewportUpdate();
        }
    }

    private CellBuildResult BuildCell(
        FastTreeDataGridPresenter.RowRenderInfo rowInfo,
        FastTreeDataGridRow row,
        int rowIndex,
        FastTreeDataGridColumn column,
        int columnIndex,
        double columnPosition,
        double columnWidth,
        double rowTop,
        double rowHeight,
        bool rowIsPlaceholder,
        bool columnPlaceholder,
        bool isCellSelected,
        int hierarchyColumnIndex,
        CellBuildContext context,
        FastTreeDataGridPresenter.CellRenderInfo? reusableCell)
    {
        var bounds = new Rect(columnPosition, rowTop, columnWidth, rowHeight);
        double indentOffset = 0d;
        if (columnIndex == hierarchyColumnIndex)
        {
            indentOffset = row.Level * IndentWidth;
            if (rowInfo.HasChildren || rowInfo.IsGroup)
            {
                indentOffset += context.ToggleSize + (context.TogglePadding * 2);
            }
            else if (row.Level > 0)
            {
                indentOffset += context.TogglePadding;
            }

            rowInfo.ToggleBounds ??= bounds;
        }

        var contentWidth = Math.Max(0, columnWidth - indentOffset - (context.CellPadding * 2));
        var contentBounds = new Rect(columnPosition + indentOffset + context.CellPadding, rowTop, contentWidth, rowHeight);

        Widget? widget = reusableCell?.Widget;
        AvaloniaControl? control = reusableCell?.Control;
        FormattedText? formatted = reusableCell?.FormattedText;
        var textOrigin = reusableCell?.TextOrigin ?? new Point(contentBounds.X, contentBounds.Y + (rowHeight / 2));

        var cellIsPlaceholder = rowIsPlaceholder || columnPlaceholder;

        void ReleaseExistingWidget()
        {
            if (reusableCell is not null && (reusableCell.Widget is not null || reusableCell.FormattedText is not null))
            {
                _presenter?.ReleaseCellResources(reusableCell);
            }

            widget = null;
            formatted = null;
            control = null;
        }

        void ReleaseExistingWidgetIfNotOfType<T>() where T : Widget
        {
            if (widget is T)
            {
                return;
            }

            if (widget is not null || formatted is not null)
            {
                ReleaseExistingWidget();
            }
        }

        AvaloniaControl? AcquireControl(FastTreeDataGridColumnControlRole role, Func<AvaloniaControl?> factory)
        {
            if (reusableCell?.Control is { } existingControl)
            {
                var existingRole = FastTreeDataGridColumn.GetControlRole(existingControl);
                if (existingRole == role)
                {
                    return existingControl;
                }

                column.ReturnControl(existingControl);
            }

            return factory();
        }

        double MeasureTextWidgetWidth(FormattedTextWidget textWidget)
        {
            var text = textWidget.Text;
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var typeface = new Typeface(textWidget.FontFamily, textWidget.FontStyle, textWidget.FontWeight, textWidget.FontStretch);
            var brush = textWidget.Foreground ?? context.TextBrush;
            var formattedText = new FormattedText(
                text,
                context.Culture,
                FlowDirection.LeftToRight,
                typeface,
                textWidget.EmSize,
                brush);

            return formattedText.Width;
        }

        if (cellIsPlaceholder)
        {
            if (reusableCell is not null && !reusableCell.IsPlaceholder)
            {
                _presenter?.ReleaseCellResources(reusableCell);
                widget = null;
                formatted = null;
                control = null;
            }

            var placeholderCell = reusableCell ?? new FastTreeDataGridPresenter.CellRenderInfo(
                columnIndex,
                column,
                bounds,
                contentBounds,
                widget: null,
                formattedText: null,
                textOrigin,
                control: null,
                FastTreeDataGridCellValidationState.None,
                isSelected: !IsCellSelection && rowInfo.IsSelected,
                isPlaceholder: true);

            placeholderCell.Update(
                bounds,
                contentBounds,
                widget: null,
                formattedText: null,
                textOrigin,
                control: null,
                FastTreeDataGridCellValidationState.None,
                !IsCellSelection && rowInfo.IsSelected,
                isPlaceholder: true);

            return new CellBuildResult(placeholderCell, FastTreeDataGridCellValidationState.None, autoWidthUpdated: false);
        }

        var hasGroupHeaderCustomization =
            column.GroupHeaderControlTemplate is not null ||
            column.GroupHeaderWidgetFactory is not null ||
            column.GroupHeaderTemplate is not null;

        var hasGroupFooterCustomization =
            column.GroupFooterControlTemplate is not null ||
            column.GroupFooterWidgetFactory is not null ||
            column.GroupFooterTemplate is not null;

        var hasCellCustomization =
            column.CellControlTemplate is not null ||
            column.CellTemplate is not null ||
            column.WidgetFactory is not null;

        if (rowInfo.IsGroup)
        {
            if (hasGroupHeaderCustomization)
            {
                if (column.GroupHeaderControlTemplate is { } headerControlTemplate)
                {
                    ReleaseExistingWidget();
                    control = AcquireControl(FastTreeDataGridColumnControlRole.GroupHeader, () => column.RentGroupHeaderControl() ?? headerControlTemplate.Build(row.Item));
                    if (control is not null)
                    {
                        FastTreeDataGridColumn.SetControlRole(control, FastTreeDataGridColumnControlRole.GroupHeader);
                        if (row.Item is not null)
                        {
                            control.DataContext = row.Item;
                        }

                        if (column.SizingMode == ColumnSizingMode.Auto)
                        {
                            var measureSize = new Size(Math.Max(0, contentBounds.Width), Math.Max(0, contentBounds.Height));
                            control.Measure(measureSize);
                        }
                    }
                }

                if (control is null)
                {
                    if (column.GroupHeaderWidgetFactory is { } headerFactory)
                    {
                        ReleaseExistingWidget();
                        widget = headerFactory(row.ValueProvider, row.Item);
                        formatted = null;
                        textOrigin = default;
                    }
                    else if (column.GroupHeaderTemplate is { } headerTemplate)
                    {
                        ReleaseExistingWidget();
                        widget = headerTemplate.Build();
                        formatted = null;
                        textOrigin = default;
                    }
                }
            }
            else if (columnIndex == hierarchyColumnIndex)
            {
                if (!hasCellCustomization)
                {
                    ReleaseExistingWidgetIfNotOfType<FastTreeDataGridGroupRowPresenter>();
                    var presenterWidget = widget as FastTreeDataGridGroupRowPresenter ?? _presenter?.RentGroupRowPresenter() ?? new FastTreeDataGridGroupRowPresenter();
                    var headerText = ResolveCellText(row, column, context.Culture);
                    var itemCount = row.Item is IFastTreeDataGridGroupMetadata groupMetadata ? groupMetadata.ItemCount : 0;
                    var groupIndent = indentOffset + context.CellPadding;

                    presenterWidget.StyleKey = column.GroupHeaderStyleKey;
                    presenterWidget.Update(headerText, itemCount, groupIndent, context.CellPadding);
                    widget = presenterWidget;
                    formatted = null;
                    textOrigin = default;
                }
            }
        }

        if (rowInfo.IsSummary && widget is null && control is null)
        {
            if (hasGroupFooterCustomization)
            {
                if (column.GroupFooterControlTemplate is { } footerControlTemplate)
                {
                    ReleaseExistingWidget();
                    control = AcquireControl(FastTreeDataGridColumnControlRole.GroupFooter, () => column.RentGroupFooterControl() ?? footerControlTemplate.Build(row.Item));
                    if (control is not null)
                    {
                        FastTreeDataGridColumn.SetControlRole(control, FastTreeDataGridColumnControlRole.GroupFooter);
                        if (row.Item is not null)
                        {
                            control.DataContext = row.Item;
                        }

                        if (column.SizingMode == ColumnSizingMode.Auto)
                        {
                            var measureSize = new Size(Math.Max(0, contentBounds.Width), Math.Max(0, contentBounds.Height));
                            control.Measure(measureSize);
                        }
                    }
                }

                if (control is null)
                {
                    if (column.GroupFooterWidgetFactory is { } footerFactory)
                    {
                        ReleaseExistingWidget();
                        widget = footerFactory(row.ValueProvider, row.Item);
                        formatted = null;
                        textOrigin = default;
                    }
                    else if (column.GroupFooterTemplate is { } footerTemplate)
                    {
                        ReleaseExistingWidget();
                        widget = footerTemplate.Build();
                        formatted = null;
                        textOrigin = default;
                    }
                }
            }
            else if (columnIndex == hierarchyColumnIndex)
            {
                if (!hasCellCustomization)
                {
                    ReleaseExistingWidgetIfNotOfType<FastTreeDataGridGroupSummaryPresenter>();
                    var summaryWidget = widget as FastTreeDataGridGroupSummaryPresenter ?? _presenter?.RentGroupSummaryPresenter() ?? new FastTreeDataGridGroupSummaryPresenter();
                    var labelText = ResolveCellText(row, column, context.Culture);
                    summaryWidget.StyleKey = column.GroupSummaryStyleKey;
                    summaryWidget.Update(labelText, indentOffset + context.CellPadding, context.CellPadding);
                    summaryWidget.Arrange(bounds);
                    widget = summaryWidget;
                    formatted = null;
                    textOrigin = default;
                }
            }
        }

        if (widget is null && control is null)
        {
            if (column.CellControlTemplate is { } controlTemplate)
            {
                ReleaseExistingWidget();
                control = AcquireControl(FastTreeDataGridColumnControlRole.Default, () => column.RentControl() ?? controlTemplate.Build(row.Item));
                if (control is not null)
                {
                    FastTreeDataGridColumn.SetControlRole(control, FastTreeDataGridColumnControlRole.Default);
                    if (row.Item is not null)
                    {
                        control.DataContext = row.Item;
                    }

                    if (column.SizingMode == ColumnSizingMode.Auto)
                    {
                        var measureSize = new Size(Math.Max(0, contentBounds.Width), Math.Max(0, contentBounds.Height));
                        control.Measure(measureSize);
                    }
                }
            }

            if (control is null && column.WidgetFactory is { } factory)
            {
                ReleaseExistingWidget();
                widget = factory(row.ValueProvider, row.Item);
                formatted = null;
                textOrigin = default;
            }
            else if (control is null && column.CellTemplate is { } template)
            {
                ReleaseExistingWidget();
                widget = template.Build();
                formatted = null;
                textOrigin = default;
            }
        }

        if (control is not null)
        {
            if (rowInfo.IsSummary && control is TextBlock textBlock)
            {
                textBlock.FontWeight = FontWeight.SemiBold;
            }
        }
        else if (widget is null)
        {
            ReleaseExistingWidgetIfNotOfType<FormattedTextWidget>();
            var textWidget = widget as FormattedTextWidget ?? column.RentTextWidget();
            textWidget.Key = column.ValueKey;
            textWidget.EmSize = CalculateCellFontSize(rowHeight);
            textWidget.Foreground = GetImmutableBrush(context.TextBrush);
            textWidget.UpdateValue(row.ValueProvider, row.Item);
            textWidget.FontWeight = rowInfo.IsSummary ? FontWeight.SemiBold : FontWeight.Normal;

            textWidget.Arrange(contentBounds);
            textWidget.Invalidate();
            widget = textWidget;
            formatted = null;
            textOrigin = new Point(contentBounds.X, contentBounds.Y);
        }
        else
        {
            // Reuse existing widget where possible; otherwise release and rebuild.
            if (widget is null)
            {
                ReleaseExistingWidget();
            }
            widget.Key ??= column.ValueKey;
            widget.Foreground ??= GetImmutableBrush(context.TextBrush);
            widget.UpdateValue(row.ValueProvider, row.Item);
            var arrangeBounds = widget is FastTreeDataGridGroupRowPresenter
                ? bounds
                : contentBounds;
            widget.Arrange(arrangeBounds);
        }

        var validationState = GetCellValidationState(row, column);

        var initialCellSelected = IsCellSelection ? isCellSelected : rowInfo.IsSelected;
        var cellInfo = reusableCell ?? new FastTreeDataGridPresenter.CellRenderInfo(
            columnIndex,
            column,
            bounds,
            contentBounds,
            widget,
            formatted,
            textOrigin,
            control,
            validationState,
            initialCellSelected,
            isPlaceholder: false);

        cellInfo.Update(bounds, contentBounds, widget, formatted, textOrigin, control, validationState, initialCellSelected, isPlaceholder: false);

        var autoWidthUpdated = false;
        if (column.SizingMode == ColumnSizingMode.Auto)
        {
            double measured = 0d;

            if (control is not null)
            {
                measured = control.DesiredSize.Width + indentOffset + (context.CellPadding * 2);
            }
            else if (formatted is not null)
            {
                measured = formatted.Width + indentOffset + (context.CellPadding * 2);
            }
            else if (widget is FormattedTextWidget textWidgetAuto)
            {
                measured = MeasureTextWidgetWidth(textWidgetAuto) + indentOffset + (context.CellPadding * 2);
            }

            if (measured > 0)
            {
                var adjusted = Math.Clamp(measured, column.MinWidth, column.MaxWidth);
                if (adjusted > column.CachedAutoWidth + 0.5)
                {
                    column.CachedAutoWidth = adjusted;
                    autoWidthUpdated = true;
                }
            }
        }

        return new CellBuildResult(cellInfo, validationState, autoWidthUpdated);
    }

    private FastTreeDataGridRowValidationState ComputeRowValidation(FastTreeDataGridRow row, FastTreeDataGridPresenter.RowRenderInfo rowInfo)
    {
        var errorCount = 0;
        var warningCount = 0;
        string? firstMessage = null;

        foreach (var cell in rowInfo.Cells)
        {
            var state = GetCellValidationState(row, cell.Column);
            if (state.HasError)
            {
                errorCount++;
                firstMessage ??= state.Message;
            }
            else if (state.HasWarning)
            {
                warningCount++;
                firstMessage ??= state.Message;
            }
        }

        return errorCount > 0 || warningCount > 0
            ? new FastTreeDataGridRowValidationState(errorCount, warningCount, firstMessage)
            : FastTreeDataGridRowValidationState.None;
    }

    private void UpdateCellInPlace(
        FastTreeDataGridPresenter.RowRenderInfo rowInfo,
        FastTreeDataGridRow row,
        FastTreeDataGridColumn column,
        int columnIndex,
        double columnPosition,
        double columnWidth,
        CellBuildContext context,
        int hierarchyColumnIndex,
        FastTreeDataGridPresenter.CellRenderInfo cell,
        bool isCellSelected,
        bool cellIsPlaceholder)
    {
        var bounds = new Rect(columnPosition, rowInfo.Top, columnWidth, rowInfo.Height);
        double indentOffset = 0d;

        if (columnIndex == hierarchyColumnIndex)
        {
            indentOffset = row.Level * IndentWidth;
            if (rowInfo.HasChildren || rowInfo.IsGroup)
            {
                indentOffset += context.ToggleSize + (context.TogglePadding * 2);
            }
            else if (row.Level > 0)
            {
                indentOffset += context.TogglePadding;
            }

            rowInfo.ToggleBounds = bounds;
        }

        var contentWidth = Math.Max(0, columnWidth - indentOffset - (context.CellPadding * 2));
        var contentBounds = new Rect(columnPosition + indentOffset + context.CellPadding, rowInfo.Top, contentWidth, rowInfo.Height);
        var textOrigin = new Point(contentBounds.X, rowInfo.Top + (rowInfo.Height / 2));

        cell.Update(
            bounds,
            contentBounds,
            cell.Widget,
            cell.FormattedText,
            textOrigin,
            cell.Control,
            cell.ValidationState,
            isCellSelected,
            cellIsPlaceholder);

        if (cell.Control is { } control)
        {
            var controlSelected = IsCellSelection ? isCellSelected : rowInfo.IsSelected;
            control.SetValue(SelectingItemsControl.IsSelectedProperty, controlSelected);
            _presenter.UpdateControlLayout(cell, contentBounds);
        }

        if (cell.Control is null && cell.Widget is Widget widget)
        {
            widget.Arrange(contentBounds);
            if (widget is FormattedTextWidget formattedTextWidget)
            {
                formattedTextWidget.Invalidate();
            }
        }
    }

    private bool TryApplyRowPatch(
        IReadOnlyList<RowBuildInfo> rowBuildInfos,
        RowViewportDelta rowDelta,
        IFastTreeDataGridRowLayout layout,
        double totalWidth,
        double totalHeight,
        double[] columnPositions,
        IReadOnlyList<double> columnWidths,
        CellBuildContext context,
        int hierarchyColumnIndex,
        Func<int, bool> columnPlaceholderAccessor,
        out bool autoWidthUpdated)
    {
        autoWidthUpdated = false;

        if (_presenter is null || !_isAttachedToVisualTree)
        {
            return false;
        }

        if (_columnsDirty || _autoWidthChanged)
        {
            return false;
        }

        if (rowBuildInfos is null || rowBuildInfos.Count == 0)
        {
            return false;
        }

        var existingRows = _presenter.VisibleRows;
        if (existingRows.Count != rowBuildInfos.Count)
        {
            return false;
        }

        var affectedIndices = rowDelta.GetAffectedIndices();
        if (affectedIndices.Length == 0 && rowDelta.ResizedIndices.Length == 0)
        {
            return true;
        }

        var affectedSet = new HashSet<int>(affectedIndices);
        foreach (var resized in rowDelta.ResizedIndices)
        {
            affectedSet.Add(resized);
        }

        if (affectedSet.Count == 0)
        {
            return true;
        }

        var rowsToAttach = new List<FastTreeDataGridPresenter.RowRenderInfo>(affectedSet.Count);
        var columnSelectionModel = IsCellSelection && CellSelectionModel is IFastTreeDataGridCellSelectionModel selectionModel
            ? selectionModel
            : null;

        long patchedCells = 0;
        var autoWidthChanged = false;

        for (var i = 0; i < rowBuildInfos.Count; i++)
        {
            var info = rowBuildInfos[i];
            if (!affectedSet.Contains(info.RowIndex))
            {
                continue;
            }

            var rowInfo = existingRows[i];
            if (rowInfo.RowIndex != info.RowIndex)
            {
                return false;
            }

            rowInfo.ToggleBounds = null;
            rowInfo.Update(
                info.Row,
                info.Top,
                info.Height,
                info.IsSelected,
                info.HasChildren,
                info.IsExpanded,
                info.ToggleRect,
                info.IsGroup,
                info.IsSummary,
                info.IsPlaceholder);

            foreach (var columnIndex in _visibleColumnIndices)
            {
                if ((uint)columnIndex >= (uint)_columns.Count || (uint)columnIndex >= (uint)columnWidths.Count || columnIndex >= columnPositions.Length)
                {
                    continue;
                }

                var column = _columns[columnIndex];
                var columnPosition = columnPositions[columnIndex];
                var columnWidth = columnWidths[columnIndex];
                var columnPlaceholder = columnPlaceholderAccessor(columnIndex);
                var existingCell = rowInfo.TryGetCell(columnIndex);

                var isCellSelected = false;
                if (columnSelectionModel is not null)
                {
                    var cellIndex = new FastTreeDataGridCellIndex(info.RowIndex, columnIndex);
                    isCellSelected = columnSelectionModel.SelectedCells.Contains(cellIndex);
                }
                else if (!IsCellSelection)
                {
                    isCellSelected = rowInfo.IsSelected;
                }

                var result = BuildCell(
                    rowInfo,
                    info.Row,
                    info.RowIndex,
                    column,
                    columnIndex,
                    columnPosition,
                    columnWidth,
                    info.Top,
                    info.Height,
                    info.IsPlaceholder,
                    columnPlaceholder,
                    isCellSelected,
                    hierarchyColumnIndex,
                    context,
                    existingCell);

                if (existingCell is null)
                {
                    rowInfo.Cells.Add(result.Cell);
                }

                patchedCells++;
                autoWidthChanged |= result.AutoWidthUpdated;
            }

            if (info.IsPlaceholder)
            {
                layout.InvalidateRow(info.RowIndex);
            }

            rowInfo.Validation = ComputeRowValidation(info.Row, rowInfo);
            _presenter.UpdateControlSelectionState(rowInfo);
            rowsToAttach.Add(rowInfo);
        }

        if (rowsToAttach.Count == 0)
        {
            return true;
        }

        _presenter.CommitRowPatch(rowsToAttach, totalWidth, totalHeight, _columnOffsets);

        if (patchedCells > 0)
        {
            FastTreeDataGridVirtualizationDiagnostics.CellsPatched.Add(patchedCells);
        }

        autoWidthUpdated = autoWidthChanged;
        return true;
    }

    private bool TryApplyCombinedPatch(
        PendingColumnWork pendingWork,
        IReadOnlyList<RowBuildInfo> rowBuildInfos,
        RowViewportDelta rowDelta,
        RowLayoutViewport range,
        IFastTreeDataGridRowLayout layout,
        double totalWidth,
        double totalHeight,
        double[] columnPositions,
        IReadOnlyList<double> columnWidths,
        ColumnViewportState previousViewport,
        ColumnViewportState currentViewport,
        CellBuildContext context,
        int hierarchyColumnIndex,
        Func<int, bool> columnPlaceholderAccessor,
        out bool autoWidthUpdated)
    {
        autoWidthUpdated = false;

        var rowPatched = TryApplyRowPatch(
            rowBuildInfos,
            rowDelta,
            layout,
            totalWidth,
            totalHeight,
            columnPositions,
            columnWidths,
            context,
            hierarchyColumnIndex,
            columnPlaceholderAccessor,
            out var rowAutoWidth);

        if (!rowPatched)
        {
            return false;
        }

        var columnPatched = TryApplyColumnPatch(
            pendingWork,
            range,
            layout,
            totalWidth,
            totalHeight,
            columnPositions,
            columnWidths,
            previousViewport,
            currentViewport,
            context,
            hierarchyColumnIndex,
            columnPlaceholderAccessor,
            out var columnAutoWidth);

        if (!columnPatched)
        {
            return false;
        }

        autoWidthUpdated = rowAutoWidth || columnAutoWidth;
        return true;
    }

    private bool TryApplyColumnPatch(
        PendingColumnWork pendingWork,
        RowLayoutViewport range,
        IFastTreeDataGridRowLayout layout,
        double totalWidth,
        double totalHeight,
        double[] columnPositions,
        IReadOnlyList<double> columnWidths,
        ColumnViewportState previousViewport,
        ColumnViewportState currentViewport,
        CellBuildContext context,
        int hierarchyColumnIndex,
        Func<int, bool> columnPlaceholderAccessor,
        out bool autoWidthUpdated)
    {
        autoWidthUpdated = false;

        if (_presenter is null || !_isAttachedToVisualTree)
        {
            return false;
        }

        if (_columnsDirty || _autoWidthChanged)
        {
            return false;
        }

        if (pendingWork.Invalidations.Any(r => r.Kind == FastTreeDataGridInvalidationKind.Full))
        {
            return false;
        }

        if (!pendingWork.HasWork)
        {
            return false;
        }

        var existingRows = _presenter.VisibleRows;
        var expectedRowCount = Math.Max(0, range.LastIndexExclusive - range.FirstIndex);
        if (existingRows.Count != expectedRowCount)
        {
            return false;
        }

        for (var i = 0; i < existingRows.Count; i++)
        {
            var expectedRowIndex = range.FirstIndex + i;
            if (existingRows[i].RowIndex != expectedRowIndex)
            {
                return false;
            }
        }

        var columnsToPatch = CreateColumnIndexSet(pendingWork);
        if (columnsToPatch.Count == 0)
        {
            return false;
        }

        var visibleColumns = new HashSet<int>(_visibleColumnIndices);
        columnsToPatch.IntersectWith(visibleColumns);
        if (columnsToPatch.Count == 0)
        {
            return false;
        }

        var columnsArray = columnsToPatch.ToArray();
        var columnSelectionModel = IsCellSelection && CellSelectionModel is IFastTreeDataGridCellSelectionModel selectionModel
            ? selectionModel
            : null;

        var autoWidthChanged = false;
        long patchedCells = 0;

        var previousEntries = CreateColumnViewportEntryMap(previousViewport);
        var currentEntries = CreateColumnViewportEntryMap(currentViewport);
        const double widthEpsilon = 0.5;

        void UpdateCell(FastTreeDataGridPresenter.RowRenderInfo rowInfo, int columnIndex)
        {
            if ((uint)columnIndex >= (uint)_columns.Count || (uint)columnIndex >= (uint)columnWidths.Count || columnIndex >= columnPositions.Length)
            {
                return;
            }

            var row = rowInfo.Row;
            if (row is null)
            {
                return;
            }

            var column = _columns[columnIndex];
            var existingCell = rowInfo.TryGetCell(columnIndex);
            if (existingCell is null)
            {
                return;
            }

            if (!currentEntries.TryGetValue(columnIndex, out var currentEntry))
            {
                return;
            }

            var hasPreviousEntry = previousEntries.TryGetValue(columnIndex, out var previousEntry);
            var columnPosition = columnPositions[columnIndex];
            var columnWidth = columnWidths[columnIndex];
            var columnPlaceholder = columnPlaceholderAccessor(columnIndex);
            var isCellSelected = false;
            if (columnSelectionModel is not null)
            {
                var cellIndex = new FastTreeDataGridCellIndex(rowInfo.RowIndex, columnIndex);
                isCellSelected = columnSelectionModel.SelectedCells.Contains(cellIndex);
            }

            var cellIsPlaceholder = rowInfo.IsPlaceholder || columnPlaceholder;
            var existingIsPlaceholder = existingCell.IsPlaceholder;

            var placeholderChanged = !hasPreviousEntry || previousEntry.IsPlaceholder != currentEntry.IsPlaceholder;

            var descriptorChanged = !hasPreviousEntry;
            if (!descriptorChanged)
            {
                var previousDescriptor = previousEntry.Descriptor;
                var currentDescriptor = currentEntry.Descriptor;
                if (!ReferenceEquals(previousDescriptor, currentDescriptor))
                {
                    var previousKey = previousDescriptor?.Key;
                    var currentKey = currentDescriptor?.Key;
                    if (!string.Equals(previousKey, currentKey, StringComparison.Ordinal))
                    {
                        descriptorChanged = true;
                    }
                }
            }

            var widthChanged = !hasPreviousEntry || Math.Abs(previousEntry.Width - currentEntry.Width) > widthEpsilon;

            var canReuse =
                hasPreviousEntry &&
                !placeholderChanged &&
                !descriptorChanged &&
                !widthChanged &&
                !rowInfo.IsPlaceholder &&
                !columnPlaceholder &&
                !existingIsPlaceholder;

            if (canReuse)
            {
                UpdateCellInPlace(
                    rowInfo,
                    row,
                    column,
                    columnIndex,
                    columnPosition,
                    columnWidth,
                    context,
                    hierarchyColumnIndex,
                    existingCell,
                    isCellSelected,
                    cellIsPlaceholder);

                patchedCells++;
                return;
            }

            var result = BuildCell(
                rowInfo,
                row,
                rowInfo.RowIndex,
                column,
                columnIndex,
                columnPosition,
                columnWidth,
                rowInfo.Top,
                rowInfo.Height,
                rowInfo.IsPlaceholder,
                columnPlaceholder,
                isCellSelected,
                hierarchyColumnIndex,
                context,
                existingCell);

            autoWidthChanged |= result.AutoWidthUpdated;
            patchedCells++;
        }

        var usedRefresh = _presenter.RefreshColumns(_currentColumnViewportDelta, UpdateCell);
        if (usedRefresh)
        {
            _presenter.FinalizeColumnPatch(totalWidth, totalHeight, _columnOffsets);
        }
        else
        {
            _presenter.ApplyColumnPatch(columnsArray, UpdateCell, totalWidth, totalHeight, _columnOffsets);
        }

        if (patchedCells > 0)
        {
            FastTreeDataGridVirtualizationDiagnostics.CellsPatched.Add(patchedCells);
        }

        foreach (var rowInfo in existingRows)
        {
            var row = rowInfo.Row;
            if (rowInfo.IsPlaceholder)
            {
                layout.InvalidateRow(rowInfo.RowIndex);
            }

            rowInfo.Validation = row is null
                ? FastTreeDataGridRowValidationState.None
                : ComputeRowValidation(row, rowInfo);
        }

        autoWidthUpdated = autoWidthChanged;
        return true;
    }

    private static HashSet<int> CreateColumnIndexSet(PendingColumnWork pendingWork)
    {
        var indices = new HashSet<int>();

        foreach (var request in pendingWork.Invalidations)
        {
            if (request.HasRange)
            {
                var end = request.StartIndex + request.Count;
                for (var i = request.StartIndex; i < end; i++)
                {
                    indices.Add(i);
                }
            }
        }

        foreach (var index in pendingWork.Materialized)
        {
            indices.Add(index);
        }

        return indices;
    }

    private KeyValuePair<string, object?>[] CreateControlTags() => new[]
    {
        new KeyValuePair<string, object?>("control_hash", GetHashCode()),
    };

    private void RecordViewportMetrics(Stopwatch stopwatch, int rowsRendered, int placeholderRows)
    {
        stopwatch.Stop();
        var tags = CreateControlTags();
        FastTreeDataGridVirtualizationDiagnostics.ViewportUpdateDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
        if (rowsRendered > 0)
        {
            FastTreeDataGridVirtualizationDiagnostics.ViewportRowsRendered.Add(rowsRendered, tags);
        }

        if (placeholderRows > 0)
        {
            FastTreeDataGridVirtualizationDiagnostics.PlaceholderRowsRendered.Add(placeholderRows, tags);
        }
    }

    private int GetHierarchyColumnIndex()
    {
        for (var i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].IsHierarchy)
            {
                return i;
            }
        }

        return _columns.Count > 0 ? 0 : -1;
    }

    private static ImmutableSolidColorBrush GetImmutableBrush(IBrush brush)
    {
        return brush switch
        {
            ImmutableSolidColorBrush immutable => immutable,
            SolidColorBrush solid => new ImmutableSolidColorBrush(solid.Color),
            _ => new ImmutableSolidColorBrush(Color.FromRgb(33, 33, 33))
        };
    }

    private const string GroupHeaderValueKey = "FastTreeDataGrid.Group.Header";

    private static string ResolveCellText(FastTreeDataGridRow row, FastTreeDataGridColumn column, CultureInfo culture)
    {
        if (row.ValueProvider is null)
        {
            return row.Item switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, culture),
                _ => row.Item?.ToString() ?? string.Empty,
            };
        }

        var key = column.ValueKey ?? string.Empty;
        var value = row.ValueProvider.GetValue(row.Item, key);

        var text = FormatCellValue(value, culture);

        if (string.IsNullOrEmpty(text) && row.IsGroup)
        {
            var headerValue = row.ValueProvider.GetValue(row.Item, GroupHeaderValueKey);
            text = FormatCellValue(headerValue, culture);
        }

        return text;
    }

    internal void HandlePresenterPointerPressed(FastTreeDataGridPresenter.RowRenderInfo rowInfo, Point pointerPosition, int clickCount, bool toggleHit, PointerPressedEventArgs args)
    {
        if (_itemsSource is null)
        {
            return;
        }

        FastTreeDataGridPresenter.CellRenderInfo? hitCell = null;
        for (var i = 0; i < rowInfo.Cells.Count; i++)
        {
            var cell = rowInfo.Cells[i];
            if (cell.Bounds.Contains(pointerPosition))
            {
                hitCell = cell;
                break;
            }
        }

        if (hitCell is null && rowInfo.Cells.Count > 0)
        {
            hitCell = rowInfo.Cells.FirstOrDefault(c => !c.IsPlaceholder) ?? rowInfo.Cells[0];
        }

        var hitCellIsPlaceholder = hitCell?.IsPlaceholder == true;
        var hitColumn = hitCellIsPlaceholder ? null : hitCell?.Column;
        var hitColumnIndex = hitColumn is not null ? _columns.IndexOf(hitColumn) : -1;
        var hitColumnIsPlaceholder = hitCellIsPlaceholder || (hitColumnIndex >= 0 && IsColumnPlaceholder(hitColumnIndex));
        if (hitColumnIsPlaceholder)
        {
            hitColumn = null;
            hitColumnIndex = -1;
        }
        if (hitColumnIndex >= 0)
        {
            if (!TryHandleEditingPointerPress(rowInfo.RowIndex, hitColumn))
            {
                return;
            }

            SetCurrentColumnForRow(hitColumnIndex);
        }

        var index = rowInfo.RowIndex;
        var normalized = NormalizeKeyModifiers(args.KeyModifiers);
        var hasShift = (normalized & KeyModifiers.Shift) == KeyModifiers.Shift;
        var hasControl = HasControlModifier(normalized);

        if (IsCellSelection && CellSelectionModel is { } cellModel && hitColumnIndex >= 0 && !hitCellIsPlaceholder)
        {
            if (_selectionMode == FastTreeDataGridSelectionMode.None)
            {
                cellModel.Clear();
            }
            else
            {
                var cell = new FastTreeDataGridCellIndex(index, hitColumnIndex);

                if (_selectionMode == FastTreeDataGridSelectionMode.Single)
                {
                    cellModel.SelectCell(cell);
                }
                else if (hasShift && (_selectionMode == FastTreeDataGridSelectionMode.Extended || _selectionMode == FastTreeDataGridSelectionMode.Multiple))
                {
                    var anchorCell = cellModel.AnchorCell.IsValid
                        ? cellModel.AnchorCell
                        : (cellModel.PrimaryCell.IsValid ? cellModel.PrimaryCell : cell);

                    cellModel.SelectCellRange(anchorCell, cell, keepExisting: hasControl);
                }
                else if (hasControl)
                {
                    cellModel.ToggleCell(cell);
                }
                else
                {
                    cellModel.SelectCell(cell);
                }

                if (!hasShift)
                {
                    cellModel.SetCellAnchor(cell);
                }

                _selectionModel.SetAnchor(index);
                EnsureRowVisible(index);
                EnsureColumnVisible(index, hitColumnIndex);
            }
        }
        else
        {
            if (_selectionMode == FastTreeDataGridSelectionMode.None)
            {
                _selectionModel.Clear();
            }
            else
            {
                if (_selectionMode == FastTreeDataGridSelectionMode.Single)
                {
                    _selectionModel.SelectSingle(index);
                }
                else if (hasShift && (_selectionMode == FastTreeDataGridSelectionMode.Extended || _selectionMode == FastTreeDataGridSelectionMode.Multiple))
                {
                    var anchor = _selectionModel.AnchorIndex >= 0
                        ? _selectionModel.AnchorIndex
                        : (_selectionModel.PrimaryIndex >= 0 ? _selectionModel.PrimaryIndex : index);
                    _selectionModel.SelectRange(anchor, index, keepExisting: hasControl);
                }
                else if (hasControl)
                {
                    _selectionModel.Toggle(index);
                }
                else
                {
                    _selectionModel.SelectSingle(index);
                }

                if (!hasShift)
                {
                    _selectionModel.SetAnchor(index);
                }

                EnsureRowVisible(index);
            }
        }

        ResetTypeSearch();

        _rowReorderController.OnPointerPressed(rowInfo, pointerPosition, args, toggleHit);

        var shouldToggle = rowInfo.HasChildren && (toggleHit || clickCount > 1);
        if (shouldToggle)
        {
            ToggleExpansionAt(rowInfo.RowIndex);
            return;
        }

        if (clickCount > 1 && hitColumn is not null && !hitColumn.IsReadOnly && !hitCellIsPlaceholder)
        {
            BeginEdit(FastTreeDataGridEditActivationReason.Pointer, null);
        }
    }

    internal bool HandlePresenterContextMenu(FastTreeDataGridPresenter.RowRenderInfo rowInfo, Point pointerPosition)
    {
        if (_presenter is null)
        {
            return false;
        }

        var row = rowInfo.Row;
        var items = new List<object>();

        bool hasChildren = row.IsGroup || row.HasChildren;
        bool canExpand = hasChildren && !row.IsExpanded;
        bool canCollapse = hasChildren && row.IsExpanded;

        void AddMenuItem(string header, Action action, bool isEnabled)
        {
            var item = new MenuItem
            {
                Header = header,
                IsEnabled = isEnabled,
            };

            if (isEnabled)
            {
                item.Click += (_, _) => Dispatcher.UIThread.Post(action);
            }

            items.Add(item);
        }

        if (hasChildren)
        {
            AddMenuItem("Expand", () => SetGroupExpansionAt(rowInfo.RowIndex, expand: true, recursive: false), canExpand);
            AddMenuItem("Collapse", () => SetGroupExpansionAt(rowInfo.RowIndex, expand: false, recursive: false), canCollapse);
            AddMenuItem("Expand Descendants", () => SetGroupExpansionAt(rowInfo.RowIndex, expand: true, recursive: true), hasChildren && canExpand);
            AddMenuItem("Collapse Descendants", () => SetGroupExpansionAt(rowInfo.RowIndex, expand: false, recursive: true), hasChildren && (canCollapse || row.IsExpanded));
        }

        var hasPrimaryActions = items.OfType<MenuItem>().Any(static mi => mi.IsEnabled);

        if (hasPrimaryActions)
        {
            items.Add(new Separator());
        }

        AddMenuItem("Expand All Groups", ExpandAllGroups, true);
        AddMenuItem("Collapse All Groups", CollapseAllGroups, true);

        if (!items.OfType<MenuItem>().Any(mi => mi.IsEnabled) && !items.OfType<MenuItem>().Any())
        {
            return false;
        }

        var menu = new ContextMenu
        {
            PlacementTarget = _presenter,
            Placement = PlacementMode.Pointer,
            ItemsSource = items,
        };

        menu.Open(_presenter);
        return true;
    }

    internal bool HandlePresenterPointerMoved(FastTreeDataGridPresenter.RowRenderInfo? rowInfo, Point pointerPosition, PointerEventArgs args)
    {
        return _rowReorderController.HandlePointerMoved(rowInfo, pointerPosition, args);
    }

    internal bool HandlePresenterPointerReleased(FastTreeDataGridPresenter.RowRenderInfo? rowInfo, Point pointerPosition, PointerReleasedEventArgs args)
    {
        return _rowReorderController.HandlePointerReleased(rowInfo, pointerPosition, args);
    }

    internal void HandlePresenterPointerCancelled()
    {
        _rowReorderController.CancelDrag();
    }

    internal bool HandlePresenterKeyDown(KeyEventArgs e)
    {
        if (_itemsSource is null)
        {
            return false;
        }

        if (HasActiveEdit)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = CommitEdit(FastTreeDataGridEditCommitTrigger.EnterKey);
                    return true;
                case Key.Tab:
                    {
                        var forward = (e.KeyModifiers & KeyModifiers.Shift) != KeyModifiers.Shift;
                        var committed = CommitEdit(FastTreeDataGridEditCommitTrigger.TabNavigation);
                        if (committed && MoveToAdjacentEditableCell(forward))
                        {
                            BeginEdit(FastTreeDataGridEditActivationReason.Keyboard, null);
                        }

                        e.Handled = true;
                        return true;
                    }
                case Key.Escape:
                    CancelEdit(FastTreeDataGridEditCancelReason.UserCancel);
                    e.Handled = true;
                    return true;
            }
        }
        else
        {
            if (e.Key == Key.F2 || (e.Key == Key.Enter && NormalizeKeyModifiers(e.KeyModifiers) == KeyModifiers.None))
            {
                if (BeginEdit(FastTreeDataGridEditActivationReason.Keyboard, null))
                {
                    e.Handled = true;
                    return true;
                }
            }
        }

        var totalRows = _itemsSource.RowCount;
        if (totalRows <= 0)
        {
            return false;
        }

        var modifiers = NormalizeKeyModifiers(e.KeyModifiers);
        var cellSelection = IsCellSelection && CellSelectionModel is not null;

        if (cellSelection)
        {
            switch (e.Key)
            {
                case Key.Down:
                    ResetTypeSearch();
                    MoveCellSelection(1, 0, modifiers);
                    return true;
                case Key.Up:
                    ResetTypeSearch();
                    MoveCellSelection(-1, 0, modifiers);
                    return true;
                case Key.PageDown:
                    ResetTypeSearch();
                    MoveCellSelection(CalculatePageDelta(), 0, modifiers);
                    return true;
                case Key.PageUp:
                    ResetTypeSearch();
                    MoveCellSelection(-CalculatePageDelta(), 0, modifiers);
                    return true;
                case Key.Home:
                    ResetTypeSearch();
                    MoveCellSelectionTo(0, EnsureCurrentColumnIndex(), modifiers);
                    return true;
                case Key.End:
                    ResetTypeSearch();
                    MoveCellSelectionTo(totalRows - 1, EnsureCurrentColumnIndex(), modifiers);
                    return true;
                case Key.Left:
                    ResetTypeSearch();
                    if ((modifiers & KeyModifiers.Control) == KeyModifiers.Control)
                    {
                        return HandleCollapseKey(modifiers);
                    }

                    MoveCellSelection(0, -1, modifiers);
                    return true;
                case Key.Right:
                    ResetTypeSearch();
                    if ((modifiers & KeyModifiers.Control) == KeyModifiers.Control)
                    {
                        return HandleExpandKey(modifiers);
                    }

                    MoveCellSelection(0, 1, modifiers);
                    return true;
            }
        }

        switch (e.Key)
        {
            case Key.Down:
                ResetTypeSearch();
                NavigateByDelta(1, modifiers);
                return true;
            case Key.Up:
                ResetTypeSearch();
                NavigateByDelta(-1, modifiers);
                return true;
            case Key.PageDown:
                ResetTypeSearch();
                NavigateByDelta(CalculatePageDelta(), modifiers);
                return true;
            case Key.PageUp:
                ResetTypeSearch();
                NavigateByDelta(-CalculatePageDelta(), modifiers);
                return true;
            case Key.Home:
                ResetTypeSearch();
                NavigateToIndex(0, modifiers);
                return true;
            case Key.End:
                ResetTypeSearch();
                NavigateToIndex(totalRows - 1, modifiers);
                return true;
            case Key.Left:
                ResetTypeSearch();
                return HandleCollapseKey(modifiers);
            case Key.Right:
                ResetTypeSearch();
                return HandleExpandKey(modifiers);
            case Key.Add:
            {
                ResetTypeSearch();
                var recursive = (modifiers & KeyModifiers.Control) == KeyModifiers.Control;
                var handled = SetGroupExpansionFromSelection(expand: true, recursive: recursive);
                return handled;
            }
            case Key.Subtract:
            {
                ResetTypeSearch();
                var recursive = (modifiers & KeyModifiers.Control) == KeyModifiers.Control;
                var handled = SetGroupExpansionFromSelection(expand: false, recursive: recursive);
                return handled;
            }
            case Key.Multiply:
                ResetTypeSearch();
                if ((modifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
                {
                    CollapseAllGroups();
                }
                else
                {
                    ExpandAllGroups();
                }
                return true;
            default:
                return false;
        }
    }

    internal bool HandlePresenterTextInput(string text)
    {
        if (_itemsSource is null || string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (!HasActiveEdit)
        {
            var printable = text.Any(static c => !char.IsControl(c));
            if (printable)
            {
                var columnIndex = EnsureCurrentColumnIndex();
                if (columnIndex >= 0 && columnIndex < _columns.Count && !_columns[columnIndex].IsReadOnly)
                {
                    if (BeginEdit(FastTreeDataGridEditActivationReason.TextInput, text))
                    {
                        ResetTypeSearch();
                        return true;
                    }
                }
            }
        }

        var now = DateTime.UtcNow;
        if (now - _typeSearchTimestamp > s_typeSearchResetInterval)
        {
            _typeSearchBuffer = string.Empty;
        }

        _typeSearchTimestamp = now;
        _typeSearchBuffer += text;

        var totalRows = _itemsSource.RowCount;
        var startIndex = _selectionModel.PrimaryIndex;
        var args = new FastTreeDataGridTypeSearchEventArgs(_typeSearchBuffer, startIndex, totalRows);
        TypeSearchRequested?.Invoke(this, args);

        if (args.Handled)
        {
            if (args.TargetIndex >= 0)
            {
                NavigateToIndex(args.TargetIndex, KeyModifiers.None);
            }

            return true;
        }

        var matchIndex = FindTypeSearchMatch(_typeSearchBuffer, startIndex);
        if (matchIndex >= 0)
        {
            NavigateToIndex(matchIndex, KeyModifiers.None);
            return true;
        }

        if (_typeSearchBuffer.Length > 1)
        {
            var fallback = _typeSearchBuffer[^1].ToString();
            matchIndex = FindTypeSearchMatch(fallback, startIndex);
            if (matchIndex >= 0)
            {
                _typeSearchBuffer = fallback;
                NavigateToIndex(matchIndex, KeyModifiers.None);
                return true;
            }
        }

        return false;
    }

    private int CalculatePageDelta()
    {
        var viewportHeight = _scrollViewer is { } sv && sv.Viewport.Height > 0
            ? sv.Viewport.Height
            : Bounds.Height;

        if (double.IsNaN(viewportHeight) || viewportHeight <= 0)
        {
            viewportHeight = RowHeight;
        }

        var rowHeight = Math.Max(1d, RowHeight);
        var result = (int)Math.Floor(viewportHeight / rowHeight);
        return Math.Max(1, result);
    }

    private void NavigateByDelta(int delta, KeyModifiers modifiers)
    {
        if (_itemsSource is null || delta == 0)
        {
            return;
        }

        var target = DetermineRelativeIndex(delta);
        if (target >= 0)
        {
            if (IsCellSelection)
            {
                MoveCellSelectionTo(target, EnsureCurrentColumnIndex(), modifiers);
            }
            else
            {
                ApplyKeyboardSelection(target, modifiers);
            }
        }
    }

    private void NavigateToIndex(int index, KeyModifiers modifiers)
    {
        if (_itemsSource is null)
        {
            return;
        }

        var count = _itemsSource.RowCount;
        if (count <= 0)
        {
            return;
        }

        var target = Math.Clamp(index, 0, count - 1);
        if (IsCellSelection)
        {
            MoveCellSelectionTo(target, EnsureCurrentColumnIndex(), modifiers);
        }
        else
        {
            ApplyKeyboardSelection(target, modifiers);
        }
    }

    private int DetermineRelativeIndex(int delta)
    {
        if (_itemsSource is null)
        {
            return -1;
        }

        var count = _itemsSource.RowCount;
        if (count <= 0)
        {
            return -1;
        }

        var current = _selectionModel.PrimaryIndex;
        if (current < 0)
        {
            return delta > 0 ? 0 : count - 1;
        }

        return Math.Clamp(current + delta, 0, count - 1);
    }

    private void ApplyKeyboardSelection(int targetIndex, KeyModifiers modifiers)
    {
        if (_itemsSource is null || targetIndex < 0)
        {
            return;
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            return;
        }

        var hasShift = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
        var hasControl = HasControlModifier(modifiers);

        if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            _selectionModel.SelectSingle(targetIndex);
        }
        else if (hasShift && (_selectionMode == FastTreeDataGridSelectionMode.Extended || _selectionMode == FastTreeDataGridSelectionMode.Multiple))
        {
            var anchor = _selectionModel.AnchorIndex >= 0
                ? _selectionModel.AnchorIndex
                : (_selectionModel.PrimaryIndex >= 0 ? _selectionModel.PrimaryIndex : targetIndex);

            _selectionModel.SelectRange(anchor, targetIndex, keepExisting: hasControl);
        }
        else
        {
            _selectionModel.SelectSingle(targetIndex);
        }

        if (!hasShift)
        {
            _selectionModel.SetAnchor(targetIndex);
        }

        EnsureRowVisible(targetIndex);
    }

    private void MoveCellSelection(int rowDelta, int columnDelta, KeyModifiers modifiers)
    {
        if (_itemsSource is null || columnDelta == 0 && rowDelta == 0)
        {
            return;
        }

        if (CellSelectionModel is not { } cellModel)
        {
            return;
        }

        var rowCount = _itemsSource.RowCount;
        var columnCount = _columns.Count;
        if (rowCount <= 0 || columnCount <= 0)
        {
            return;
        }

        var current = cellModel.PrimaryCell;
        if (!current.IsValid)
        {
            var rowIndex = _selectionModel.PrimaryIndex >= 0 ? _selectionModel.PrimaryIndex : 0;
            var columnIndex = EnsureCurrentColumnIndex();
            current = new FastTreeDataGridCellIndex(Math.Clamp(rowIndex, 0, rowCount - 1), Math.Clamp(columnIndex, 0, columnCount - 1));
        }

        var targetRow = Math.Clamp(current.RowIndex + rowDelta, 0, Math.Max(0, rowCount - 1));
        var targetColumn = Math.Clamp(current.ColumnIndex + columnDelta, 0, Math.Max(0, columnCount - 1));

        MoveCellSelectionTo(targetRow, targetColumn, modifiers);
    }

    private void MoveCellSelectionTo(int targetRow, int targetColumn, KeyModifiers modifiers)
    {
        if (_itemsSource is null || CellSelectionModel is not { } cellModel)
        {
            return;
        }

        var rowCount = _itemsSource.RowCount;
        var columnCount = _columns.Count;

        if (rowCount <= 0 || columnCount <= 0)
        {
            return;
        }

        targetRow = Math.Clamp(targetRow, 0, rowCount - 1);
        targetColumn = Math.Clamp(targetColumn, 0, columnCount - 1);

        if (IsColumnPlaceholder(targetColumn))
        {
            RequestViewportUpdate();
            return;
        }

        var targetCell = new FastTreeDataGridCellIndex(targetRow, targetColumn);
        var hasShift = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
        var hasControl = HasControlModifier(modifiers);

        if (_selectionMode == FastTreeDataGridSelectionMode.None)
        {
            cellModel.Clear();
        }
        else if (_selectionMode == FastTreeDataGridSelectionMode.Single)
        {
            cellModel.SelectCell(targetCell);
        }
        else if (hasShift && (_selectionMode == FastTreeDataGridSelectionMode.Extended || _selectionMode == FastTreeDataGridSelectionMode.Multiple))
        {
            var anchorCell = cellModel.AnchorCell.IsValid
                ? cellModel.AnchorCell
                : (cellModel.PrimaryCell.IsValid ? cellModel.PrimaryCell : targetCell);

            cellModel.SelectCellRange(anchorCell, targetCell, keepExisting: hasControl);
        }
        else if (hasControl)
        {
            cellModel.ToggleCell(targetCell);
        }
        else
        {
            cellModel.SelectCell(targetCell);
        }

        if (!hasShift)
        {
            cellModel.SetCellAnchor(targetCell);
        }

        _selectionModel.SetAnchor(targetRow);
        SetCurrentColumnForRow(targetColumn);
        EnsureRowVisible(targetRow);
        EnsureColumnVisible(targetRow, targetColumn);
    }

    private bool HandleCollapseKey(KeyModifiers modifiers)
    {
        if (_itemsSource is null)
        {
            return false;
        }

        if (HasControlModifier(modifiers) || (modifiers & KeyModifiers.Alt) == KeyModifiers.Alt)
        {
            return false;
        }

        var index = _selectionModel.PrimaryIndex;
        if (index < 0 || index >= _itemsSource.RowCount || _itemsSource.IsPlaceholder(index))
        {
            return false;
        }

        var row = _itemsSource.GetRow(index);
        if (row.IsGroup)
        {
            if (row.IsExpanded)
            {
                ToggleExpansionAt(index, requestPrefetch: false);
                return true;
            }

            var parentIndex = FindParentIndex(index, row.Level);
            if (parentIndex >= 0)
            {
                NavigateToIndex(parentIndex, KeyModifiers.None);
                return true;
            }

            return false;
        }

        if (row.HasChildren && row.IsExpanded)
        {
            ToggleExpansionAt(index, requestPrefetch: false);
            return true;
        }

        if (row.Level > 0)
        {
            var parentIndex = FindParentIndex(index, row.Level);
            if (parentIndex >= 0)
            {
                NavigateToIndex(parentIndex, KeyModifiers.None);
                return true;
            }
        }

        return false;
    }

    private bool HandleExpandKey(KeyModifiers modifiers)
    {
        if (_itemsSource is null)
        {
            return false;
        }

        if (HasControlModifier(modifiers) || (modifiers & KeyModifiers.Alt) == KeyModifiers.Alt)
        {
            return false;
        }

        var index = _selectionModel.PrimaryIndex;
        if (index < 0)
        {
            index = 0;
        }

        if (index >= _itemsSource.RowCount || _itemsSource.IsPlaceholder(index))
        {
            return false;
        }

        var row = _itemsSource.GetRow(index);
        if (row.IsGroup)
        {
            if (!row.IsExpanded)
            {
                ToggleExpansionAt(index);
                return true;
            }

            var groupChildIndex = index + 1;
            if (groupChildIndex >= _itemsSource.RowCount || _itemsSource.IsPlaceholder(groupChildIndex))
            {
                return false;
            }

            var groupChildRow = _itemsSource.GetRow(groupChildIndex);
            if (groupChildRow.Level > row.Level)
            {
                NavigateToIndex(groupChildIndex, KeyModifiers.None);
                return true;
            }

            return false;
        }

        if (!row.HasChildren)
        {
            return false;
        }

        if (!row.IsExpanded)
        {
            ToggleExpansionAt(index);
            return true;
        }

        var childIndex = index + 1;
        if (childIndex >= _itemsSource.RowCount || _itemsSource.IsPlaceholder(childIndex))
        {
            return false;
        }

        var childRow = _itemsSource.GetRow(childIndex);
        if (childRow.Level > row.Level)
        {
            NavigateToIndex(childIndex, KeyModifiers.None);
            return true;
        }

        return false;
    }

    private bool SetGroupExpansionFromSelection(bool expand, bool recursive)
    {
        if (_itemsSource is null || _selectionModel.PrimaryIndex < 0)
        {
            return false;
        }

        return SetGroupExpansionAt(_selectionModel.PrimaryIndex, expand, recursive);
    }

    private bool SetGroupExpansionAt(int index, bool expand, bool recursive)
    {
        if (_itemsSource is null || index < 0 || index >= _itemsSource.RowCount || _itemsSource.IsPlaceholder(index))
        {
            return false;
        }

        var row = _itemsSource.GetRow(index);
        var changed = false;

        if (row.IsGroup)
        {
            if (recursive && !expand && row.IsExpanded)
            {
                changed |= SetGroupExpansionRecursive(index, row.Level, expand);
            }

            if (row.IsExpanded != expand)
            {
                ToggleExpansionAt(index, requestPrefetch: !recursive && expand);
                changed = true;
                row = _itemsSource.GetRow(index);
            }

            if (recursive && expand)
            {
                changed |= SetGroupExpansionRecursive(index, row.Level, true);
            }

            return changed;
        }

        if (!row.HasChildren)
        {
            return false;
        }

        if (row.IsExpanded != expand)
        {
            ToggleExpansionAt(index, requestPrefetch: !recursive && expand);
            changed = true;
            row = _itemsSource.GetRow(index);
        }

        if (recursive)
        {
            changed |= SetGroupExpansionRecursive(index, row.Level, expand);
        }

        return changed;
    }

    private bool SetGroupExpansionRecursive(int startIndex, int level, bool expand)
    {
        if (_itemsSource is null)
        {
            return false;
        }

        var changed = false;

        for (var i = startIndex + 1; i < _itemsSource.RowCount; i++)
        {
            if (_itemsSource.IsPlaceholder(i))
            {
                continue;
            }

            var row = _itemsSource.GetRow(i);
            if (row.Level <= level)
            {
                break;
            }

            if (!row.IsGroup)
            {
                continue;
            }

            if (expand)
            {
                if (!row.IsExpanded)
                {
                    ToggleExpansionAt(i, requestPrefetch: false);
                    row = _itemsSource.GetRow(i);
                    changed = true;
                }

                changed |= SetGroupExpansionRecursive(i, row.Level, true);
            }
            else
            {
                if (row.IsExpanded)
                {
                    changed |= SetGroupExpansionRecursive(i, row.Level, false);
                    ToggleExpansionAt(i, requestPrefetch: false);
                    changed = true;
                }
            }
        }

        return changed;
    }

    private int FindParentIndex(int startIndex, int currentLevel)
    {
        if (_itemsSource is null || currentLevel <= 0)
        {
            return -1;
        }

        for (var i = startIndex - 1; i >= 0; i--)
        {
            if (_itemsSource.IsPlaceholder(i))
            {
                continue;
            }

            var candidate = _itemsSource.GetRow(i);
            if (candidate.Level < currentLevel)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindTypeSearchMatch(string query, int startIndex)
    {
        if (_itemsSource is null)
        {
            return -1;
        }

        var total = _itemsSource.RowCount;
        if (total <= 0)
        {
            return -1;
        }

        var normalized = query?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return -1;
        }

        if (startIndex < 0 || startIndex >= total)
        {
            startIndex = -1;
        }

        for (var step = 1; step <= total; step++)
        {
            var index = (startIndex + step) % total;
            if (_itemsSource.IsPlaceholder(index))
            {
                continue;
            }

            var row = _itemsSource.GetRow(index);
            var text = GetTypeSearchText(row);
            if (!string.IsNullOrEmpty(text) && text.StartsWith(normalized, StringComparison.CurrentCultureIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private string? GetTypeSearchText(FastTreeDataGridRow row)
    {
        if (_typeSearchSelector is not null)
        {
            return _typeSearchSelector(row);
        }

        var column = GetDefaultSearchColumn();
        if (column is null)
        {
            return row.Item?.ToString();
        }

        return GetCellText(row, column);
    }

    private FastTreeDataGridColumn? GetDefaultSearchColumn()
    {
        if (_columns.Count == 0)
        {
            return null;
        }

        foreach (var column in _columns)
        {
            if (column.IsHierarchy || column.ValueKey is not null)
            {
                return column;
            }
        }

        return _columns[0];
    }

    private void SetSelectedIndicesInternal(IReadOnlyList<int> indices)
    {
        var newValue = indices ?? Array.Empty<int>();
        var oldValue = _selectedIndices;

        if (ReferenceEquals(oldValue, newValue))
        {
            return;
        }

        if (oldValue.Count == newValue.Count && oldValue.SequenceEqual(newValue))
        {
            return;
        }

        _selectedIndices = newValue;
        RaisePropertyChanged(SelectedIndicesProperty, oldValue, newValue);
    }

    private List<int> NormalizeSelectionInput(IReadOnlyList<int>? indices)
    {
        if (indices is null || indices.Count == 0)
        {
            return new List<int>();
        }

        var result = new SortedSet<int>();
        var rowCount = _itemsSource?.RowCount;
        var clamp = rowCount.HasValue && rowCount.Value >= 0;
        var maxIndex = rowCount.GetValueOrDefault();

        foreach (var index in indices)
        {
            if (index < 0)
            {
                continue;
            }

            if (clamp && index >= maxIndex)
            {
                continue;
            }

            if (result.Add(index) && _selectionMode == FastTreeDataGridSelectionMode.Single)
            {
                break;
            }
        }

        if (result.Count == 0)
        {
            return new List<int>();
        }

        if (_selectionMode == FastTreeDataGridSelectionMode.Single && result.Count > 1)
        {
            var retained = result.Max;
            result.Clear();
            result.Add(retained);
        }

        return result.ToList();
    }

    private void ResetTypeSearch()
    {
        _typeSearchBuffer = string.Empty;
        _typeSearchTimestamp = DateTime.MinValue;
    }

    internal void ScrollRowIntoViewForAutomation(int index) => EnsureRowVisible(index);

    private void EnsureRowVisible(int index)
    {
        if (_scrollViewer is null || _itemsSource is null || index < 0)
        {
            return;
        }

        var layout = GetActiveRowLayout();
        var defaultRowHeight = Math.Max(1d, RowHeight);

        var rowTop = layout.GetRowTop(index);
        var rowHeight = defaultRowHeight;
        if (_itemsSource.TryGetMaterializedRow(index, out var row))
        {
            rowHeight = Math.Max(1d, layout.GetRowHeight(index, row, defaultRowHeight));
        }

        var currentOffset = _scrollViewer.Offset;
        var viewportHeight = _scrollViewer.Viewport.Height > 0 ? _scrollViewer.Viewport.Height : Bounds.Height;
        if (double.IsNaN(viewportHeight) || viewportHeight <= 0)
        {
            viewportHeight = defaultRowHeight;
        }

        var viewportTop = currentOffset.Y;
        var viewportBottom = viewportTop + viewportHeight;
        var rowBottom = rowTop + rowHeight;

        double? newOffsetY = null;

        if (rowTop < viewportTop)
        {
            newOffsetY = rowTop;
        }
        else if (rowBottom > viewportBottom)
        {
            newOffsetY = Math.Max(0, rowBottom - viewportHeight);
        }

        if (newOffsetY.HasValue)
        {
            _scrollViewer.Offset = new Vector(currentOffset.X, newOffsetY.Value);
        }
    }

    private bool IsColumnPlaceholder(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_columns.Count)
        {
            return false;
        }

        return _columnSource.SupportsPlaceholders && _columnSource.IsPlaceholder(columnIndex);
    }

    private void EnsureColumnVisible(int rowIndex, int columnIndex)
    {
        if (_scrollViewer is null || _presenter is null || columnIndex < 0 || columnIndex >= _columns.Count || rowIndex < 0)
        {
            return;
        }

        if (!_presenter.TryGetCell(rowIndex, _columns[columnIndex], out _, out var cellInfo) || cellInfo is null)
        {
            return;
        }

        if (cellInfo.IsPlaceholder)
        {
            RequestViewportUpdate();
            return;
        }

        var viewportWidth = _scrollViewer.Viewport.Width > 0 ? _scrollViewer.Viewport.Width : Bounds.Width;
        if (double.IsNaN(viewportWidth) || viewportWidth <= 0)
        {
            return;
        }

        var offset = _scrollViewer.Offset;
        var cellLeft = cellInfo.Bounds.X;
        var cellRight = cellInfo.Bounds.X + cellInfo.Bounds.Width;
        var viewportLeft = offset.X;
        var viewportRight = viewportLeft + viewportWidth;

        double? newOffsetX = null;

        if (cellLeft < viewportLeft)
        {
            newOffsetX = Math.Max(0, cellLeft);
        }
        else if (cellRight > viewportRight)
        {
            newOffsetX = Math.Max(0, cellRight - viewportWidth);
        }

        if (newOffsetX.HasValue && !AreClose(newOffsetX.Value, offset.X))
        {
            _scrollViewer.Offset = new Vector(newOffsetX.Value, offset.Y);
        }
    }

    private void ToggleExpansionAt(int index, bool requestPrefetch = true)
    {
        if (_itemsSource is null || (uint)index >= (uint)_itemsSource.RowCount)
        {
            return;
        }

        if (_itemsSource.IsPlaceholder(index))
        {
            return;
        }

        var row = _itemsSource.GetRow(index);
        var hasGroupPath = TryGetGroupPath(row, out var groupPath);
        var newExpandedState = hasGroupPath ? !row.IsExpanded : false;

        _itemsSource.ToggleExpansion(index);

        if (hasGroupPath)
        {
            _groupingStateStore.SetExpanded(groupPath, newExpandedState);
        }

        if (requestPrefetch)
        {
            TryPrefetchExpandedDescendants(index);
        }
    }

    private static bool TryGetGroupPath(FastTreeDataGridRow row, out string path)
    {
        if (row.Item is IFastTreeDataGridGroupPathProvider provider && !string.IsNullOrEmpty(provider.GroupPath))
        {
            path = provider.GroupPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private bool DetermineDefaultGroupExpansion()
    {
        if (_groupDescriptors.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < _groupDescriptors.Count; i++)
        {
            if (!_groupDescriptors[i].IsExpanded)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyGroupExpansionLayout(IReadOnlyList<FastTreeDataGridGroupingExpansionState> states, bool defaultExpanded)
    {
        if (_virtualizationProvider is IFastTreeDataGridGroupingController grouping)
        {
            grouping.ApplyGroupExpansionLayout(states ?? Array.Empty<FastTreeDataGridGroupingExpansionState>(), defaultExpanded);
        }
    }

    private void TryPrefetchExpandedDescendants(int index)
    {
        if (_viewportScheduler is null || _itemsSource is null)
        {
            return;
        }

        if ((uint)index >= (uint)_itemsSource.RowCount)
        {
            return;
        }

        var row = _itemsSource.GetRow(index);
        if (!row.IsExpanded)
        {
            return;
        }

        var startIndex = index + 1;
        if (startIndex >= _itemsSource.RowCount)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (_lastGroupPrefetchIndex == index && (now - _lastGroupPrefetchTimestamp).TotalMilliseconds < 100)
        {
            return;
        }

        var radius = Math.Max(1, _virtualizationSettings.PrefetchRadius);
        var count = Math.Min(_itemsSource.RowCount - startIndex, radius * 4);
        if (count <= 0)
        {
            return;
        }

        _lastGroupPrefetchIndex = index;
        _lastGroupPrefetchTimestamp = now;

        _viewportScheduler.Request(new FastTreeDataGridViewportRequest(startIndex, count, radius));
    }

    private string GetCellText(FastTreeDataGridRow row, FastTreeDataGridColumn column)
    {
        if (column.ValueKey is { } key && row.ValueProvider is { } provider)
        {
            var value = provider.GetValue(row.Item, key);
            var text = FormatCellValue(value, CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(text) && row.IsGroup)
            {
                var headerValue = provider.GetValue(row.Item, GroupHeaderValueKey);
                text = FormatCellValue(headerValue, CultureInfo.InvariantCulture);
            }

            return text;
        }

        return row.Item?.ToString() ?? string.Empty;
    }

    private static double CalculateCellFontSize(double rowHeight)
    {
        return Math.Clamp(rowHeight - 10, 10, 20);
    }

    private static KeyModifiers NormalizeKeyModifiers(KeyModifiers modifiers)
    {
        if ((modifiers & KeyModifiers.Meta) == KeyModifiers.Meta)
        {
            modifiers |= KeyModifiers.Control;
        }

        return modifiers;
    }

    private static bool HasControlModifier(KeyModifiers modifiers)
    {
        var normalized = NormalizeKeyModifiers(modifiers);
        return (normalized & KeyModifiers.Control) == KeyModifiers.Control;
    }

    private static string FormatCellValue(object? value, CultureInfo culture)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable formattable => formattable.ToString(null, culture),
            _ => value?.ToString() ?? string.Empty,
        };
    }

    private void UpdateSelectionIndicators()
    {
        _presenter?.UpdateSelection(_selectedIndices);

        if (_presenter is null)
        {
            return;
        }

        if (IsCellSelection && CellSelectionModel is { } cellModel)
        {
            _presenter.UpdateCellSelection(cellModel.SelectedCells, cellModel.PrimaryCell);
        }
        else
        {
            _presenter.UpdateCellSelection(Array.Empty<FastTreeDataGridCellIndex>(), FastTreeDataGridCellIndex.Invalid);
        }
    }

    private void RecalculateColumns()
    {
        var availableWidth = _scrollViewer is { } sv && sv.Viewport.Width > 0 ? sv.Viewport.Width : Bounds.Width;
        var widths = ColumnLayoutCalculator.Calculate(_columns, availableWidth);

        _columnWidths.Clear();
        _columnWidths.AddRange(widths);

        _columnOffsets.Clear();
        var offset = 0d;
        for (var i = 0; i < _columnWidths.Count; i++)
        {
            offset += _columnWidths[i];
            _columnOffsets.Add(offset);
        }
    }

    private void SynchronizeHeaderScroll()
    {
        if (_scrollViewer is null)
        {
            UpdateHeaderScroll(0);
            return;
        }

        UpdateHeaderScroll(_scrollViewer.Offset.X);
    }

    private void UpdateHeaderScroll(double horizontalOffset)
    {
        if (_headerScrollViewer is null)
        {
            return;
        }

        if (double.IsNaN(horizontalOffset) || double.IsInfinity(horizontalOffset))
        {
            horizontalOffset = 0;
        }

        var current = _headerScrollViewer.Offset;
        if (!AreClose(current.X, horizontalOffset) || !AreClose(current.Y, 0))
        {
            _updatingHeaderFromBody = true;
            try
            {
                _headerScrollViewer.Offset = new Vector(horizontalOffset, 0);
            }
            finally
            {
                _updatingHeaderFromBody = false;
            }
        }
    }

    private void UpdateVisibleColumns(double[] columnPositions, IReadOnlyList<double> widths, IReadOnlyList<int> leftIndices, IReadOnlyList<int> bodyIndices, IReadOnlyList<int> rightIndices, double viewportLeft, double viewportRight)
    {
        _visibleColumnIndices.Clear();

        void AddColumn(int index)
        {
            if ((uint)index >= (uint)_columns.Count)
            {
                return;
            }

            if (!_visibleColumnIndices.Contains(index))
            {
                _visibleColumnIndices.Add(index);
            }
        }

        foreach (var index in leftIndices)
        {
            AddColumn(index);
        }

        foreach (var index in bodyIndices)
        {
            if ((uint)index >= (uint)widths.Count)
            {
                continue;
            }

            var x = columnPositions[index];
            var width = widths[index];
            var right = x + width;
            if (right >= viewportLeft - 1 && x <= viewportRight + 1)
            {
                AddColumn(index);
            }
        }

        foreach (var index in rightIndices)
        {
            AddColumn(index);
        }

        if (_visibleColumnIndices.Count == 0 && _columns.Count > 0)
        {
            if (bodyIndices.Count > 0)
            {
                AddColumn(bodyIndices[0]);
            }
            else
            {
                AddColumn(0);
            }
        }
    }

    private void UpdateBodyScroll(double horizontalOffset)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        var current = _scrollViewer.Offset;
        if (!AreClose(current.X, horizontalOffset))
        {
            _scrollViewer.Offset = new Vector(horizontalOffset, current.Y);
        }
    }

    private void UpdateLoadingOverlay()
    {
        if (_loadingOverlay is null)
        {
            return;
        }

        if (!_virtualizationSettings.ShowLoadingOverlay)
        {
            _loadingOverlay.IsVisible = false;
            return;
        }

        var isLoading = _isLoading;
        _loadingOverlay.IsVisible = isLoading;

        if (_loadingProgressBar is not null)
        {
            if (double.IsNaN(_loadingProgress))
            {
                _loadingProgressBar.IsIndeterminate = true;
            }
            else
            {
                _loadingProgressBar.IsIndeterminate = false;
                _loadingProgressBar.Maximum = 100;
                _loadingProgressBar.Value = Math.Clamp(_loadingProgress * 100, 0d, 100d);
            }
        }

        if (_loadingText is not null)
        {
            _loadingText.IsVisible = isLoading;
        }
    }

    private void OnHeaderScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty && !_updatingHeaderFromBody)
        {
            var offset = e.GetNewValue<Vector>();
            UpdateBodyScroll(offset.X);

            if (_headerScrollViewer is not null && !AreClose(offset.Y, 0))
            {
                _updatingHeaderFromBody = true;
                try
                {
                    _headerScrollViewer.Offset = new Vector(offset.X, 0);
                }
                finally
                {
                    _updatingHeaderFromBody = false;
                }
            }
        }
    }

    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        if (Math.Abs(e.OffsetDelta.X) > 0.001)
        {
            UpdateHeaderScroll(_scrollViewer.Offset.X);
        }
    }

    private void OnHeaderScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_headerScrollViewer is null || _updatingHeaderFromBody)
        {
            return;
        }

        if (Math.Abs(e.OffsetDelta.X) > 0.001)
        {
            UpdateBodyScroll(_headerScrollViewer.Offset.X);
        }

        if (!AreClose(_headerScrollViewer.Offset.Y, 0))
        {
            _updatingHeaderFromBody = true;
            try
            {
                var x = _headerScrollViewer.Offset.X;
                _headerScrollViewer.Offset = new Vector(x, 0);
            }
            finally
            {
                _updatingHeaderFromBody = false;
            }
        }
    }

    private sealed class RowReorderController
    {
        private readonly FastTreeDataGrid _owner;
        private bool _pending;
        private bool _dragging;
        private IPointer? _pointer;
        private int _pointerId;
        private Point _startPoint;
        private Point _currentPoint;
        private FastTreeDataGridPresenter.RowRenderInfo? _sourceRow;
        private readonly List<int> _activeIndices = new();
        private int[] _sourceIndexSnapshot = Array.Empty<int>();
        private double _dragOffsetWithinRow;
        private double _dragBlockHeight;
        private double _blockTop;
        private double _blockBottom;
        private int _rawInsertIndex;
        private double _indicatorY;
        private FastTreeDataGridPresenter.RowRenderInfo? _targetRow;

        internal RowReorderController(FastTreeDataGrid owner)
        {
            _owner = owner;
        }

        private FastTreeDataGridRowReorderSettings Settings => _owner._rowReorderSettings;

        private IFastTreeDataGridRowReorderHandler? Handler => _owner.RowReorderHandler;

        private FastTreeDataGridPresenter? Presenter => _owner._presenter;

        private bool CanStart => Settings.IsEnabled && Handler is not null && Presenter is not null && !_owner.HasActiveEdit;

        internal void Refresh()
        {
            if (!CanStart)
            {
                CancelDrag();
                return;
            }

            if (_dragging)
            {
                PublishOverlay(new DropTarget(_rawInsertIndex, _targetRow, _indicatorY));
            }
        }

        internal void OnPointerPressed(FastTreeDataGridPresenter.RowRenderInfo rowInfo, Point point, PointerPressedEventArgs args, bool toggleHit)
        {
            CancelDrag();

            if (!CanStart || toggleHit || args.ClickCount > 1 || !IsRowEligible(rowInfo))
            {
                return;
            }

            _pending = true;
            _pointer = args.Pointer;
            _pointerId = args.Pointer.Id;
            _startPoint = point;
            _currentPoint = point;
            _sourceRow = rowInfo;
            _dragOffsetWithinRow = point.Y - rowInfo.Top;
            CaptureSourceIndices(rowInfo.RowIndex);
            _rawInsertIndex = rowInfo.RowIndex;
            _dragBlockHeight = CalculateBlockHeight();
            ComputeBlockBounds();
            _indicatorY = rowInfo.Top;
        }

        internal bool HandlePointerMoved(FastTreeDataGridPresenter.RowRenderInfo? rowInfo, Point point, PointerEventArgs args)
        {
            if (!_pending && !_dragging)
            {
                return false;
            }

            if (_pointer is null || args.Pointer.Id != _pointerId)
            {
                return false;
            }

            _currentPoint = point;
            _ = rowInfo; // intentionally unused

            var element = (Visual?)Presenter ?? _owner;
            var props = args.GetCurrentPoint(element).Properties;
            if (!props.IsLeftButtonPressed)
            {
                CancelDrag();
                return false;
            }

            if (_pending)
            {
                if (!HasExceededThreshold(point))
                {
                    return false;
                }

                if (!BeginDrag())
                {
                    CancelDrag();
                    return false;
                }
            }

            if (_dragging)
            {
                UpdateDrag(point);
                return true;
            }

            return false;
        }

        internal bool HandlePointerReleased(FastTreeDataGridPresenter.RowRenderInfo? rowInfo, Point point, PointerReleasedEventArgs args)
        {
            if ((!_pending && !_dragging) || args.Pointer.Id != _pointerId)
            {
                return false;
            }

            if (_pending)
            {
                CancelDrag();
                return false;
            }

            UpdateDrag(point);
            _ = CommitAsync();
            _ = rowInfo; // intentionally unused
            return true;
        }

        internal void CancelDrag()
        {
            if (_pointer is { } pointer)
            {
                try
                {
                    pointer.Capture(null);
                }
                catch
                {
                    // ignored
                }
            }

            _pending = false;
            _dragging = false;
            _pointer = null;
            _pointerId = 0;
            ClearState();
            Presenter?.UpdateRowReorderOverlay(null);
        }

        private void ClearState()
        {
            _activeIndices.Clear();
            _sourceIndexSnapshot = Array.Empty<int>();
            _sourceRow = null;
            _targetRow = null;
            _dragBlockHeight = 0;
            _blockTop = 0;
            _blockBottom = 0;
            _rawInsertIndex = 0;
            _indicatorY = 0;
        }

        private bool BeginDrag()
        {
            var presenter = Presenter;
            if (!CanStart || presenter is null || _pointer is null)
            {
                return false;
            }

            try
            {
                _pointer.Capture(presenter);
            }
            catch
            {
                return false;
            }

            _pending = false;
            _dragging = true;
            return true;
        }

        private bool HasExceededThreshold(Point point)
        {
            var threshold = Settings.ActivationThreshold;
            if (threshold <= 0)
            {
                return true;
            }

            return Math.Abs(point.X - _startPoint.X) >= threshold || Math.Abs(point.Y - _startPoint.Y) >= threshold;
        }

        private void CaptureSourceIndices(int primaryIndex)
        {
            _activeIndices.Clear();

            if (ShouldUseSelection(primaryIndex))
            {
                var selection = _owner._selectionModel?.SelectedIndices;
                if (selection is not null)
                {
                    for (var i = 0; i < selection.Count; i++)
                    {
                        var index = selection[i];
                        if (index >= 0)
                        {
                            _activeIndices.Add(index);
                        }
                    }
                }
            }

            if (_activeIndices.Count == 0)
            {
                _activeIndices.Add(primaryIndex);
            }

            _activeIndices.Sort();
            _sourceIndexSnapshot = _activeIndices.ToArray();
        }

        private bool ShouldUseSelection(int primaryIndex)
        {
            if (!Settings.UseSelection || _owner._selectionModel is null)
            {
                return false;
            }

            var selection = _owner._selectionModel.SelectedIndices;
            if (selection.Count <= 1)
            {
                return false;
            }

            for (var i = 0; i < selection.Count; i++)
            {
                if (selection[i] == primaryIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private double CalculateBlockHeight()
        {
            if (_sourceIndexSnapshot.Length == 0)
            {
                return _sourceRow?.Height ?? _owner.RowHeight;
            }

            var presenter = Presenter;
            double height = 0;

            if (presenter is null)
            {
                var fallback = _sourceRow?.Height ?? _owner.RowHeight;
                height = fallback * _sourceIndexSnapshot.Length;
            }
            else
            {
                for (var i = 0; i < _sourceIndexSnapshot.Length; i++)
                {
                    var row = presenter.TryGetRow(_sourceIndexSnapshot[i]);
                    height += row?.Height ?? (_sourceRow?.Height ?? _owner.RowHeight);
                }
            }

            return Math.Max(height, _sourceRow?.Height ?? _owner.RowHeight);
        }

        private void ComputeBlockBounds()
        {
            var presenter = Presenter;
            if (presenter is null)
            {
                var top = _sourceRow?.Top ?? 0;
                _blockTop = top;
                _blockBottom = top + _dragBlockHeight;
                return;
            }

            double topBound = double.MaxValue;
            double bottomBound = double.MinValue;

            for (var i = 0; i < _sourceIndexSnapshot.Length; i++)
            {
                var row = presenter.TryGetRow(_sourceIndexSnapshot[i]);
                if (row is null)
                {
                    continue;
                }

                topBound = Math.Min(topBound, row.Top);
                bottomBound = Math.Max(bottomBound, row.Top + row.Height);
            }

            if (double.IsInfinity(topBound) || topBound == double.MaxValue)
            {
                topBound = _sourceRow?.Top ?? 0;
            }

            if (double.IsInfinity(bottomBound) || bottomBound == double.MinValue)
            {
                bottomBound = topBound + _dragBlockHeight;
            }

            _blockTop = topBound;
            _blockBottom = bottomBound;
        }

        private void UpdateDrag(Point point)
        {
            ComputeBlockBounds();
            var target = CalculateDropTarget(point);
            _rawInsertIndex = target.InsertIndex;
            _targetRow = target.TargetRow;
            _indicatorY = target.IndicatorY;
            PublishOverlay(target);
        }

        private DropTarget CalculateDropTarget(Point point)
        {
            var presenter = Presenter;
            if (presenter is null || presenter.VisibleRows.Count == 0)
            {
                return new DropTarget(0, null, point.Y);
            }

            var rows = presenter.VisibleRows;
            var indicatorY = rows[^1].Top + rows[^1].Height;
            var insertIndex = rows[^1].RowIndex + 1;
            FastTreeDataGridPresenter.RowRenderInfo? targetRow = rows[^1];

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (point.Y < row.Top)
                {
                    insertIndex = row.RowIndex;
                    indicatorY = row.Top;
                    targetRow = row;
                    break;
                }

                var rowBottom = row.Top + row.Height;
                if (point.Y <= rowBottom)
                {
                    var before = point.Y < row.Top + row.Height / 2;
                    insertIndex = before ? row.RowIndex : row.RowIndex + 1;
                    indicatorY = before ? row.Top : rowBottom;
                    targetRow = before ? row : (i + 1 < rows.Count ? rows[i + 1] : row);
                    break;
                }
            }

            var minIndex = _sourceIndexSnapshot.Length > 0 ? _sourceIndexSnapshot[0] : 0;
            var maxExclusive = _sourceIndexSnapshot.Length > 0 ? _sourceIndexSnapshot[^1] + 1 : minIndex;

            if (insertIndex >= minIndex && insertIndex <= maxExclusive)
            {
                if (point.Y < _blockTop)
                {
                    insertIndex = minIndex;
                    indicatorY = _blockTop;
                    targetRow = presenter.TryGetRow(insertIndex) ?? targetRow;
                }
                else
                {
                    insertIndex = maxExclusive;
                    indicatorY = _blockBottom;
                    targetRow = presenter.TryGetRow(insertIndex) ?? targetRow;
                }
            }

            return new DropTarget(insertIndex, targetRow, indicatorY);
        }

        private void PublishOverlay(DropTarget target)
        {
            var presenter = Presenter;
            if (presenter is null)
            {
                return;
            }

            var width = presenter.Bounds.Width;
            if (!double.IsFinite(width) || width <= 0)
            {
                width = presenter.Width;
            }

            if (!double.IsFinite(width) || width <= 0)
            {
                width = _owner.Bounds.Width;
            }

            width = Math.Max(0, width);

            var height = presenter.Bounds.Height;
            if (!double.IsFinite(height) || height <= 0)
            {
                height = presenter.Height;
            }

            if (!double.IsFinite(height) || height <= 0)
            {
                height = _owner.Bounds.Height;
            }

            height = Math.Max(0, height);

            var previewTop = Math.Clamp(_currentPoint.Y - _dragOffsetWithinRow, 0, Math.Max(0, height - _dragBlockHeight));
            var dragPreviewRect = new Rect(0, previewTop, width, _dragBlockHeight);
            var indicatorY = Math.Clamp(target.IndicatorY, 0, height);

            Rect targetRect = default;
            var showTarget = target.TargetRow is not null;
            if (target.TargetRow is { } targetRow)
            {
                targetRect = new Rect(0, targetRow.Top, width, targetRow.Height);
            }

            var highlightOpacity = Math.Clamp(Settings.DragPreviewOpacity * 0.5, 0, 1);

            var overlay = new FastTreeDataGridPresenter.RowReorderOverlayState(
                Settings.ShowDropIndicator,
                indicatorY,
                Settings.DropIndicatorThickness,
                Settings.DropIndicatorBrush,
                Settings.ShowDragPreview,
                dragPreviewRect,
                Settings.DragPreviewBrush,
                Settings.DragPreviewOpacity,
                Settings.DragPreviewCornerRadius,
                showTarget,
                targetRect,
                Settings.DragPreviewBrush,
                highlightOpacity,
                Settings.DragPreviewCornerRadius);

            presenter.UpdateRowReorderOverlay(overlay);
        }

        private int AdjustInsertIndex(int insertIndex)
        {
            var adjusted = insertIndex;
            for (var i = 0; i < _sourceIndexSnapshot.Length; i++)
            {
                if (_sourceIndexSnapshot[i] < adjusted)
                {
                    adjusted--;
                }
            }

            return Math.Max(0, adjusted);
        }

        private async Task CommitAsync()
        {
            var handler = Handler;
            if (handler is null)
            {
                CancelDrag();
                return;
            }

            var indices = (int[])_sourceIndexSnapshot.Clone();
            var insertIndex = AdjustInsertIndex(_rawInsertIndex);

            Presenter?.UpdateRowReorderOverlay(null);

            if (_pointer is { } pointer)
            {
                try
                {
                    pointer.Capture(null);
                }
                catch
                {
                    // ignored
                }
            }

            _pointer = null;
            _pointerId = 0;
            _pending = false;
            _dragging = false;

            var request = new FastTreeDataGridRowReorderRequest(indices, insertIndex)
            {
                Source = _owner._itemsSource,
                Context = Settings
            };

            if (!handler.CanReorder(request))
            {
                ClearState();
                return;
            }

            var reorderingArgs = new FastTreeDataGridRowReorderingEventArgs(request);
            _owner.RowReordering?.Invoke(_owner, reorderingArgs);
            if (reorderingArgs.Cancel)
            {
                ClearState();
                return;
            }

            FastTreeDataGridRowReorderResult result;
            try
            {
                result = await handler.ReorderAsync(request, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ClearState();
                return;
            }

            _owner.RowReordered?.Invoke(_owner, new FastTreeDataGridRowReorderedEventArgs(request, result));
            ClearState();
        }

        private bool IsRowEligible(FastTreeDataGridPresenter.RowRenderInfo row)
        {
            if (row is null || row.IsPlaceholder || row.IsSummary)
            {
                return false;
            }

            if (row.IsGroup && !Settings.AllowGroupReorder)
            {
                return false;
            }

            return true;
        }

        private readonly struct DropTarget
        {
            public DropTarget(int insertIndex, FastTreeDataGridPresenter.RowRenderInfo? targetRow, double indicatorY)
            {
                InsertIndex = insertIndex;
                TargetRow = targetRow;
                IndicatorY = indicatorY;
            }

            public int InsertIndex { get; }

            public FastTreeDataGridPresenter.RowRenderInfo? TargetRow { get; }

            public double IndicatorY { get; }
        }
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.25;
    }
}
