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
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Control.Controls;

public class FastTreeDataGrid : TemplatedControl
{
    public static readonly StyledProperty<IFastTreeDataGridSource?> ItemsSourceProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, IFastTreeDataGridSource?>(nameof(ItemsSource));

    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, double>(nameof(RowHeight), 28d);

    public static readonly StyledProperty<double> IndentWidthProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, double>(nameof(IndentWidth), 16d);

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, double>(nameof(HeaderHeight), 32d);

    public static readonly DirectProperty<FastTreeDataGrid, AvaloniaList<FastTreeDataGridColumn>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, AvaloniaList<FastTreeDataGridColumn>>(nameof(Columns), o => o.Columns);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<FastTreeDataGrid, int>(nameof(SelectedIndex), -1);

    public static readonly DirectProperty<FastTreeDataGrid, object?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, object?>(nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SetSelectedItem(v));

    public static readonly DirectProperty<FastTreeDataGrid, IFastTreeDataGridRowLayout?> RowLayoutProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, IFastTreeDataGridRowLayout?>(nameof(RowLayout), o => o.RowLayout, (o, v) => o.SetRowLayout(v));

    public static readonly DirectProperty<FastTreeDataGrid, FastTreeDataGridVirtualizationSettings> VirtualizationSettingsProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGrid, FastTreeDataGridVirtualizationSettings>(
            nameof(VirtualizationSettings),
            o => o.VirtualizationSettings,
            (o, v) => o.VirtualizationSettings = v);

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

    private ScrollViewer? _headerScrollViewer;
    private Border? _headerHost;
    private FastTreeDataGridHeaderPresenter? _headerPresenter;
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
    private int? _sortedColumnIndex;
    private FastTreeDataGridSortDirection _sortedDirection = FastTreeDataGridSortDirection.None;
    private IFastTreeDataGridRowLayout? _rowLayout;
    private FastTreeDataGridThrottleDispatcher? _resetThrottle;
    private IFastTreeDataGridSelectionModel _selectionModel = null!;
    private FastTreeDataGridSelectionMode _selectionMode = FastTreeDataGridSelectionMode.Extended;
    private bool _synchronizingSelection;
    private Func<FastTreeDataGridRow, string?>? _typeSearchSelector;
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private string _typeSearchBuffer = string.Empty;
    private DateTime _typeSearchTimestamp = DateTime.MinValue;
    private static readonly TimeSpan s_typeSearchResetInterval = TimeSpan.FromSeconds(1.5);

    static FastTreeDataGrid()
    {
        AffectsMeasure<FastTreeDataGrid>(ItemsSourceProperty, RowHeightProperty, IndentWidthProperty);
    }

    public FastTreeDataGrid()
    {
        _columns.CollectionChanged += OnColumnsChanged;
        SetRowLayout(new FastTreeDataGridUniformRowLayout());
        ResetThrottleDispatcher();
        SetSelectionModel(new FastTreeDataGridSelectionModel());
    }

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
        }
    }

    internal IFastTreeDataVirtualizationProvider? VirtualizationProvider => _virtualizationProvider;

    public event EventHandler<FastTreeDataGridSortEventArgs>? SortRequested;
    public event EventHandler<FastTreeDataGridSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<FastTreeDataGridTypeSearchEventArgs>? TypeSearchRequested;

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
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _presenter = e.NameScope.Find<FastTreeDataGridPresenter>("PART_Presenter");

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

        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
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
        ApplySortStateToProvider();
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

        _itemsSource = newSource;
        _rowLayout?.Bind(_itemsSource);
        _rowLayout?.Reset();

        if (newSource is not null)
        {
            newSource.ResetRequested += OnSourceResetRequested;
        }

        ConfigureVirtualizationProvider(newSource);

        ResetTypeSearch();
        SetValue(SelectedIndexProperty, -1);
        RequestViewportUpdate();
    }

    private void OnSourceResetRequested(object? sender, EventArgs e)
    {
        _rowLayout?.Reset();
        ResetTypeSearch();
        RequestViewportUpdate();
    }

    private void ConfigureVirtualizationProvider(IFastTreeDataGridSource? source)
    {
        var provider = FastTreeDataGridVirtualizationProviderRegistry.Create(source, _virtualizationSettings);
        if (provider is null)
        {
            _virtualizationProvider = null;
            _presenter?.SetVirtualizationProvider(null);
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

        _providerInitializationCts?.Cancel();
        _providerInitializationCts?.Dispose();
        _providerInitializationCts = new CancellationTokenSource();
        _viewportScheduler = new FastTreeDataGridViewportScheduler(_virtualizationProvider, _virtualizationSettings);
        ResetThrottleDispatcher();
        var token = _providerInitializationCts.Token;
        _ = InitializeVirtualizationProviderAsync(_virtualizationProvider, token);
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

        _viewportScheduler?.Dispose();
        _viewportScheduler = null;
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
        _columnsDirty = true;
        if (_sortedColumnIndex.HasValue && (_sortedColumnIndex.Value < 0 || _sortedColumnIndex.Value >= _columns.Count))
        {
            ClearSortStateInternal(requestUpdate: false);
        }
        RequestViewportUpdate();
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
        }

        _templateHandlersAttached = false;
    }

    private void DetachTemplateParts(bool clearReferences)
    {
        DetachTemplatePartHandlers();

        if (_presenter is not null)
        {
            _presenter.SetVirtualizationProvider(null);
            _presenter.SetOwner(null);
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

        _headerPresenter?.UpdateWidths(_columnWidths);

        SynchronizeHeaderScroll();
        RequestViewportUpdate();
    }

    public void SetSortState(int columnIndex, FastTreeDataGridSortDirection direction)
    {
        if (_columns.Count == 0 || (uint)columnIndex >= (uint)_columns.Count)
        {
            return;
        }

        _sortedColumnIndex = direction == FastTreeDataGridSortDirection.None ? null : columnIndex;
        _sortedDirection = direction;

        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            var target = i == columnIndex ? direction : FastTreeDataGridSortDirection.None;
            if (column.SortDirection != target)
            {
                column.SortDirection = target;
            }
        }

        _columnsDirty = true;
        RequestViewportUpdate();
    }

    public void ClearSortState()
    {
        ClearSortStateInternal(requestUpdate: true);
    }

    private void ClearSortStateInternal(bool requestUpdate)
    {
        _sortedColumnIndex = null;
        _sortedDirection = FastTreeDataGridSortDirection.None;

        foreach (var column in _columns)
        {
            if (column.SortDirection != FastTreeDataGridSortDirection.None)
            {
                column.SortDirection = FastTreeDataGridSortDirection.None;
            }
        }

        if (requestUpdate)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
        }

        ApplySortStateToProvider();
    }

    private void OnColumnSortRequested(int columnIndex)
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

        var newDirection = FastTreeDataGridSortDirection.Ascending;
        if (_sortedColumnIndex == columnIndex)
        {
            newDirection = _sortedDirection switch
            {
                FastTreeDataGridSortDirection.None => FastTreeDataGridSortDirection.Ascending,
                FastTreeDataGridSortDirection.Ascending => FastTreeDataGridSortDirection.Descending,
                FastTreeDataGridSortDirection.Descending => FastTreeDataGridSortDirection.None,
                _ => FastTreeDataGridSortDirection.None,
            };
        }

        if (newDirection == FastTreeDataGridSortDirection.None)
        {
            ClearSortStateInternal(requestUpdate: false);
        }
        else
        {
            _sortedColumnIndex = columnIndex;
            _sortedDirection = newDirection;

            for (var i = 0; i < _columns.Count; i++)
            {
                var currentColumn = _columns[i];
                var targetDirection = i == columnIndex ? newDirection : FastTreeDataGridSortDirection.None;
                if (currentColumn.SortDirection != targetDirection)
                {
                    currentColumn.SortDirection = targetDirection;
                }
            }
        }

        if (newDirection == FastTreeDataGridSortDirection.None)
        {
            _sortedColumnIndex = null;
            _sortedDirection = FastTreeDataGridSortDirection.None;
        }

        _columnsDirty = true;
        RequestViewportUpdate();
        ApplySortStateToProvider();
        SortRequested?.Invoke(this, new FastTreeDataGridSortEventArgs(column, columnIndex, newDirection));
    }

    private void ApplySortStateToProvider()
    {
        if (_virtualizationProvider is null)
        {
            return;
        }

        var descriptors = new List<FastTreeDataGridSortDescriptor>();

        if (_sortedColumnIndex is int index && index >= 0 && index < _columns.Count && _sortedDirection != FastTreeDataGridSortDirection.None)
        {
            var column = _columns[index];
            descriptors.Add(new FastTreeDataGridSortDescriptor
            {
                ColumnKey = column.ValueKey ?? index.ToString(CultureInfo.InvariantCulture),
                Direction = _sortedDirection,
                RowComparison = column.SortComparison,
            });
        }

        var request = new FastTreeDataGridSortFilterRequest
        {
            SortDescriptors = descriptors,
            FilterDescriptors = Array.Empty<FastTreeDataGridFilterDescriptor>(),
        };

        _ = _virtualizationProvider.ApplySortFilterAsync(request, CancellationToken.None);
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
        var viewportWidth = viewport.Width > 0 ? viewport.Width : Bounds.Width;
        var totalHeight = layout.GetTotalHeight(viewportHeight, defaultRowHeight, totalRows);
        var totalWidth = Math.Max(_columnWidths.Sum(), viewportWidth);

        if (_headerPresenter is not null)
        {
            _headerPresenter.BindColumns(_columns, _columnWidths);
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
        var toggleColumnStart = hierarchyColumnIndex <= 0 || hierarchyColumnIndex - 1 >= _columnOffsets.Count
            ? 0
            : _columnOffsets[hierarchyColumnIndex - 1];
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
                isPlaceholder);

            var x = 0d;
            for (var columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
            {
                var column = _columns[columnIndex];
                var width = _columnWidths[columnIndex];
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
                FormattedText? formatted = null;
                Point textOrigin = new(contentBounds.X, contentBounds.Y + (rowHeight / 2));

                if (!isPlaceholder)
                {
                    if (column.WidgetFactory is { } factory)
                    {
                        widget = factory(row.ValueProvider, row.Item);
                    }
                    else if (column.CellTemplate is { } template)
                    {
                        widget = template.Build();
                    }
                }

                if (widget is null)
                {
                    var textWidget = new FormattedTextWidget
                    {
                        Key = column.ValueKey,
                        EmSize = CalculateCellFontSize(rowHeight),
                        Foreground = GetImmutableBrush(textBrush),
                    };
                    textWidget.UpdateValue(row.ValueProvider, row.Item);

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

                rowInfo.Cells.Add(new FastTreeDataGridPresenter.CellRenderInfo(bounds, widget, formatted, textOrigin));

                if (column.SizingMode == ColumnSizingMode.Auto && formatted is not null)
                {
                    var measured = formatted.Width + indentOffset + (cellPadding * 2);
                    var adjusted = Math.Clamp(measured, column.MinWidth, column.MaxWidth);
                    if (adjusted > column.CachedAutoWidth + 0.5)
                    {
                        column.CachedAutoWidth = adjusted;
                        autoWidthUpdated = true;
                    }
                }

                x += width;
            }

            if (isPlaceholder)
            {
                layout.InvalidateRow(rowIndex);
            }

            rows.Add(rowInfo);
            rowTop += rowHeight;
        }

        _presenter.UpdateContent(rows, totalWidth, totalHeight, _columnOffsets);
        _presenter.UpdateSelection(selectedIndices);

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

    internal void HandlePresenterPointerPressed(FastTreeDataGridPresenter.RowRenderInfo rowInfo, Point pointerPosition, int clickCount, bool toggleHit, KeyModifiers modifiers)
    {
        if (_itemsSource is null)
        {
            return;
        }

        var index = rowInfo.RowIndex;
        var normalized = NormalizeKeyModifiers(modifiers);
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

        var shouldToggle = rowInfo.HasChildren && (toggleHit || clickCount > 1);
        if (shouldToggle)
        {
            _itemsSource.ToggleExpansion(rowInfo.RowIndex);
        }
    }

    internal bool HandlePresenterKeyDown(KeyEventArgs e)
    {
        if (_itemsSource is null)
        {
            return false;
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

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.25;
    }
}
