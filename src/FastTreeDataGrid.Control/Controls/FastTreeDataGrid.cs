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

    static FastTreeDataGrid()
    {
        AffectsMeasure<FastTreeDataGrid>(ItemsSourceProperty, RowHeightProperty, IndentWidthProperty);
    }

    public FastTreeDataGrid()
    {
        _columns.CollectionChanged += OnColumnsChanged;
        SetRowLayout(new FastTreeDataGridUniformRowLayout());
        ResetThrottleDispatcher();
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
        if (_itemsSource is null || index < 0 || index >= _itemsSource.RowCount)
        {
            SetSelectedItem(null);
        }
        else
        {
            var row = _itemsSource.GetRow(index);
            SetSelectedItem(row.Item);
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

        SetValue(SelectedIndexProperty, -1);
        RequestViewportUpdate();
    }

    private void OnSourceResetRequested(object? sender, EventArgs e)
    {
        _rowLayout?.Reset();
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
                rowIndex == SelectedIndex,
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

                    if (!isPlaceholder)
                    {
                        textWidget.UpdateValue(row.ValueProvider, row.Item);
                    }

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

    internal void HandlePresenterPointerPressed(FastTreeDataGridPresenter.RowRenderInfo rowInfo, Point pointerPosition, int clickCount, bool toggleHit)
    {
        if (_itemsSource is null)
        {
            return;
        }

        SelectedIndex = rowInfo.RowIndex;

        var shouldToggle = rowInfo.HasChildren && (toggleHit || clickCount > 1);
        if (shouldToggle)
        {
            _itemsSource.ToggleExpansion(rowInfo.RowIndex);
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

    private void UpdateRowSelectionIndicators()
    {
        _presenter?.UpdateSelection(SelectedIndex);
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
