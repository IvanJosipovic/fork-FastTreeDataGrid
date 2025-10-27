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

    private ScrollViewer? _headerScrollViewer;
    private Border? _headerHost;
    private FastTreeDataGridHeaderPresenter? _headerPresenter;
    private FastTreeDataGridFilterPresenter? _filterPresenter;
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
    private string _typeSearchBuffer = string.Empty;
    private DateTime _typeSearchTimestamp = DateTime.MinValue;
    private static readonly TimeSpan s_typeSearchResetInterval = TimeSpan.FromSeconds(1.5);
    private bool _isLoading;
    private double _loadingProgress = double.NaN;
    private AvaloniaControl? _loadingOverlay;
    private ProgressBar? _loadingProgressBar;
    private TextBlock? _loadingText;

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
        SetRowLayout(new FastTreeDataGridUniformRowLayout());
        ResetThrottleDispatcher();
        SetSelectionModel(new FastTreeDataGridSelectionModel());
        _rowReorderSettings.SettingsChanged += OnRowReorderSettingsChanged;
        _rowReorderController = new RowReorderController(this);
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
        UpdateRowSelectionIndicators();
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
        UpdateSelectionFromModel();
        SetSelectedIndicesInternal(_selectionModel.SelectedIndices);
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
            UpdateRowSelectionIndicators();
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
        UpdateRowSelectionIndicators();
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
        UpdateRowSelectionIndicators();
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
        if (e.Property == FastTreeDataGridColumn.PinnedPositionProperty ||
            e.Property == FastTreeDataGridColumn.SortDirectionProperty ||
            e.Property == FastTreeDataGridColumn.SortOrderProperty ||
            e.Property == FastTreeDataGridColumn.CanUserResizeProperty ||
            e.Property == FastTreeDataGridColumn.CanUserReorderProperty)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
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
        }
        AttachPresenterForEditing(_presenter);

        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
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
        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
        }

        SynchronizeHeaderScroll();

        _columnsDirty = true;
        RequestViewportUpdate();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttachedToVisualTree = false;
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

            var progress = double.IsNaN(e.Progress) ? double.NaN : Math.Clamp(e.Progress, 0d, 1d);
            SetAndRaise(IsLoadingProperty, ref _isLoading, e.IsLoading);
            SetAndRaise(LoadingProgressProperty, ref _loadingProgress, progress);
            UpdateLoadingOverlay();
        }, DispatcherPriority.Render);
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
            }, _virtualizationSettings.DispatcherPriority);
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
            }, _virtualizationSettings.DispatcherPriority);

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
        }, _virtualizationSettings.DispatcherPriority);
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

        if (filtersRemoved)
        {
            ApplyDataOperationsToProvider();
        }
    }

    private void OnGroupDescriptorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyDataOperationsToProvider();
    }

    private void OnAggregateDescriptorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyDataOperationsToProvider();
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
            UpdateHeaderScroll(_scrollViewer.Offset.X);
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
            GroupDescriptors = _groupDescriptors.Count == 0 ? Array.Empty<FastTreeDataGridGroupDescriptor>() : _groupDescriptors.ToArray(),
            AggregateDescriptors = _aggregateDescriptors.Count == 0 ? Array.Empty<FastTreeDataGridAggregateDescriptor>() : _aggregateDescriptors.ToArray(),
        };

        _ = _virtualizationProvider.ApplySortFilterAsync(request, CancellationToken.None);
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

            _itemsSource.ToggleExpansion(rowIndex);
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

    private void UpdateViewport()
    {
        var stopwatch = Stopwatch.StartNew();
        if (!_isAttachedToVisualTree || _scrollViewer is null || _presenter is null || _itemsSource is null)
        {
            RecordViewportMetrics(stopwatch, 0, 0);
            return;
        }

        if (_columns.Count == 0)
        {
            _presenter.UpdateContent(Array.Empty<FastTreeDataGridPresenter.RowRenderInfo>(), 0, 0, Array.Empty<double>());
            RecordViewportMetrics(stopwatch, 0, 0);
            return;
        }

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

        var selectedIndices = _selectedIndices;
        HashSet<int>? selectionLookup = null;
        if (selectedIndices.Count > 0)
        {
            selectionLookup = selectedIndices is IReadOnlyCollection<int> collection
                ? new HashSet<int>(collection)
                : new HashSet<int>(selectedIndices);
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
            RecordViewportMetrics(stopwatch, 0, 0);
            return;
        }

        var requestCount = Math.Max(0, range.LastIndexExclusive - range.FirstIndex);
        var prefetchRadius = Math.Max(buffer, 0);
        if (requestCount > 0)
        {
            _viewportScheduler?.Request(new FastTreeDataGridViewportRequest(range.FirstIndex, requestCount, prefetchRadius));
        }

        var rows = new List<FastTreeDataGridPresenter.RowRenderInfo>(Math.Max(0, range.LastIndexExclusive - range.FirstIndex));
        var culture = CultureInfo.CurrentCulture;
        var typeface = Typeface.Default;
        var textBrush = Foreground ?? new SolidColorBrush(Color.FromRgb(33, 33, 33));
        const double toggleSize = 12;
        const double togglePadding = 4;
        const double cellPadding = 6;
        var hierarchyColumnIndex = GetHierarchyColumnIndex();
        var toggleColumnStart = hierarchyColumnIndex >= 0 && hierarchyColumnIndex < columnPositions.Length
            ? columnPositions[hierarchyColumnIndex]
            : 0;
        var autoWidthUpdated = false;
        var rowTop = range.FirstRowTop;

        for (var rowIndex = range.FirstIndex; rowIndex < range.LastIndexExclusive; rowIndex++)
        {
            var isPlaceholder = _virtualizationProvider?.IsPlaceholder(rowIndex) == true;
            var row = _itemsSource.GetRow(rowIndex);
            var rowHeight = layout.GetRowHeight(rowIndex, row, defaultRowHeight);
            var hasChildren = row.HasChildren;
            var toggleRect = hasChildren
                ? new Rect(Math.Max(0, toggleColumnStart + row.Level * IndentWidth + togglePadding), rowTop + (rowHeight - toggleSize) / 2, toggleSize, toggleSize)
                : default;
            var isGroup = row.IsGroup;

            var rowInfo = new FastTreeDataGridPresenter.RowRenderInfo(
                row,
                rowIndex,
                rowTop,
                rowHeight,
                selectionLookup?.Contains(rowIndex) ?? false,
                hasChildren,
                row.IsExpanded,
                toggleRect,
                isGroup,
                row.IsSummary,
                isPlaceholder);

            var rowErrorCount = 0;
            var rowWarningCount = 0;
            string? rowFirstMessage = null;

            foreach (var columnIndex in _visibleColumnIndices)
            {
                var column = _columns[columnIndex];
                var width = _columnWidths[columnIndex];
                var x = columnPositions[columnIndex];
                var bounds = new Rect(x, rowTop, width, rowHeight);
                double indentOffset = 0d;
                if (columnIndex == hierarchyColumnIndex)
                {
                    indentOffset = row.Level * IndentWidth + toggleSize + (togglePadding * 2);
                    if (rowInfo.ToggleBounds is null)
                    {
                        rowInfo.ToggleBounds = bounds;
                    }
                }

                var contentWidth = Math.Max(0, width - indentOffset - (cellPadding * 2));
                var contentBounds = new Rect(x + indentOffset + cellPadding, rowTop, contentWidth, rowHeight);

                Widget? widget = null;
                AvaloniaControl? control = null;
                FormattedText? formatted = null;
                Point textOrigin = new(contentBounds.X, contentBounds.Y + (rowHeight / 2));

                if (!isPlaceholder)
                {
                    if (column.CellControlTemplate is { } controlTemplate)
                    {
                        control = column.RentControl() ?? controlTemplate.Build(row.Item);
                        if (control is not null)
                        {
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
                        widget = factory(row.ValueProvider, row.Item);
                    }
                    else if (control is null && column.CellTemplate is { } template)
                    {
                        widget = template.Build();
                    }
                }

                if (control is not null)
                {
                    if (rowInfo.IsSummary && control is TextBlock textBlock)
                    {
                        textBlock.FontWeight = FontWeight.SemiBold;
                    }

                    // Intentionally left blank: controls are measured/arranged by the presenter.
                }
                else if (widget is null)
                {
                    var textWidget = column.RentTextWidget();
                    textWidget.Key = column.ValueKey;
                    textWidget.EmSize = CalculateCellFontSize(rowHeight);
                    textWidget.Foreground = GetImmutableBrush(textBrush);
                    textWidget.UpdateValue(row.ValueProvider, row.Item);
                    textWidget.FontWeight = rowInfo.IsSummary ? FontWeight.SemiBold : FontWeight.Normal;

                    textWidget.Arrange(contentBounds);
                    textWidget.Invalidate();
                    widget = textWidget;

                    var text = textWidget.Text ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                    {
                        formatted = new FormattedText(
                            text,
                            culture,
                            FlowDirection.LeftToRight,
                            typeface,
                            textWidget.EmSize,
                            textBrush)
                        {
                            MaxTextWidth = contentWidth,
                            Trimming = TextTrimming.CharacterEllipsis,
                        };
                        if (rowInfo.IsSummary)
                        {
                            formatted.SetFontWeight(FontWeight.SemiBold);
                        }

                        textOrigin = new Point(
                            contentBounds.X,
                            contentBounds.Y + Math.Max(0, (rowHeight - formatted.Height) / 2));
                    }
                }
                else
                {
                    widget.Key ??= column.ValueKey;
                    widget.Foreground ??= GetImmutableBrush(textBrush);
                    widget.UpdateValue(row.ValueProvider, row.Item);
                    widget.Arrange(contentBounds);
                }

                var validationState = GetCellValidationState(row, column);
                if (validationState.HasError)
                {
                    rowErrorCount++;
                    rowFirstMessage ??= validationState.Message;
                }
                else if (validationState.HasWarning)
                {
                    rowWarningCount++;
                    rowFirstMessage ??= validationState.Message;
                }

                rowInfo.Cells.Add(new FastTreeDataGridPresenter.CellRenderInfo(column, bounds, contentBounds, widget, formatted, textOrigin, control, validationState));

                if (column.SizingMode == ColumnSizingMode.Auto)
                {
                    double measured = 0d;

                    if (control is not null)
                    {
                        measured = control.DesiredSize.Width + indentOffset + (cellPadding * 2);
                    }
                    else if (formatted is not null)
                    {
                        measured = formatted.Width + indentOffset + (cellPadding * 2);
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
            }

            if (isPlaceholder)
            {
                layout.InvalidateRow(rowIndex);
            }

            rowInfo.Validation = rowErrorCount > 0 || rowWarningCount > 0
                ? new FastTreeDataGridRowValidationState(rowErrorCount, rowWarningCount, rowFirstMessage)
                : FastTreeDataGridRowValidationState.None;

            rows.Add(rowInfo);
            rowTop += rowHeight;
        }

        _presenter.UpdateContent(rows, totalWidth, totalHeight, _columnOffsets);
        _presenter.UpdateSelection(selectedIndices);
        OnViewportUpdatedForEditing(rows);

        var placeholderCount = rows.Count(static r => r.IsPlaceholder);
        RecordViewportMetrics(stopwatch, rows.Count, placeholderCount);

        if (autoWidthUpdated)
        {
            _autoWidthChanged = true;
            RequestViewportUpdate();
        }
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

    internal void HandlePresenterPointerPressed(FastTreeDataGridPresenter.RowRenderInfo rowInfo, Point pointerPosition, int clickCount, bool toggleHit, PointerPressedEventArgs args)
    {
        if (_itemsSource is null)
        {
            return;
        }

        FastTreeDataGridColumn? hitColumn = null;
        for (var i = 0; i < rowInfo.Cells.Count; i++)
        {
            var cell = rowInfo.Cells[i];
            if (cell.Bounds.Contains(pointerPosition))
            {
                hitColumn = cell.Column;
                break;
            }
        }

        if (hitColumn is null && rowInfo.Cells.Count > 0)
        {
            hitColumn = rowInfo.Cells[0].Column;
        }

        var hitColumnIndex = hitColumn is not null ? _columns.IndexOf(hitColumn) : -1;
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

        ResetTypeSearch();

        _rowReorderController.OnPointerPressed(rowInfo, pointerPosition, args, toggleHit);

        var shouldToggle = rowInfo.HasChildren && (toggleHit || clickCount > 1);
        if (shouldToggle)
        {
            _itemsSource.ToggleExpansion(rowInfo.RowIndex);
            return;
        }

        if (clickCount > 1 && hitColumn is not null && !hitColumn.IsReadOnly)
        {
            BeginEdit(FastTreeDataGridEditActivationReason.Pointer, null);
        }
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
            ApplyKeyboardSelection(target, modifiers);
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
        ApplyKeyboardSelection(target, modifiers);
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
        if (row.HasChildren && row.IsExpanded)
        {
            _itemsSource.ToggleExpansion(index);
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
        if (!row.HasChildren)
        {
            return false;
        }

        if (!row.IsExpanded)
        {
            _itemsSource.ToggleExpansion(index);
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

    private string GetCellText(FastTreeDataGridRow row, FastTreeDataGridColumn column)
    {
        if (column.ValueKey is { } key && row.ValueProvider is { } provider)
        {
            var value = provider.GetValue(row.Item, key);
            return value switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            };
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

    private void UpdateRowSelectionIndicators()
    {
        _presenter?.UpdateSelection(_selectedIndices);
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
