using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
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

    private readonly AvaloniaList<FastTreeDataGridColumn> _columns = new();
    private readonly List<double> _columnWidths = new();
    private readonly List<double> _columnOffsets = new();

    private FastTreeDataGridHeaderPresenter? _headerPresenter;
    private FastTreeDataGridPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private IFastTreeDataGridSource? _itemsSource;
    private object? _selectedItem;
    private bool _viewportUpdateQueued;
    private bool _columnsDirty = true;
    private bool _autoWidthChanged;
    private int? _sortedColumnIndex;
    private FastTreeDataGridSortDirection _sortedDirection = FastTreeDataGridSortDirection.None;

    static FastTreeDataGrid()
    {
        AffectsMeasure<FastTreeDataGrid>(ItemsSourceProperty, RowHeightProperty, IndentWidthProperty);
    }

    public FastTreeDataGrid()
    {
        _columns.CollectionChanged += OnColumnsChanged;
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

    public event EventHandler<FastTreeDataGridSortEventArgs>? SortRequested;

    private void SetSelectedItem(object? value)
    {
        SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        UpdateRowSelectionIndicators();
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

        DetachTemplateParts();

        _headerPresenter = e.NameScope.Find<FastTreeDataGridHeaderPresenter>("PART_HeaderPresenter");
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _presenter = e.NameScope.Find<FastTreeDataGridPresenter>("PART_Presenter");

        if (_presenter is not null)
        {
            _presenter.SetOwner(this);
        }

        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
            _headerPresenter.ColumnResizeRequested += OnColumnResizeRequested;
            _headerPresenter.ColumnSortRequested += OnColumnSortRequested;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
        }

        _columnsDirty = true;
        RequestViewportUpdate();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_presenter is null || _scrollViewer is null || _headerPresenter is null)
        {
            ApplyTemplate();
        }

        _presenter?.SetOwner(this);
        if (_headerPresenter is not null)
        {
            _headerPresenter.HeaderHeight = HeaderHeight;
        }

        _columnsDirty = true;
        RequestViewportUpdate();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachTemplateParts();
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

        _itemsSource = newSource;

        if (newSource is not null)
        {
            newSource.ResetRequested += OnSourceResetRequested;
        }

        SetValue(SelectedIndexProperty, -1);
        RequestViewportUpdate();
    }

    private void OnSourceResetRequested(object? sender, EventArgs e)
    {
        RequestViewportUpdate();
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

    private void DetachTemplateParts()
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
            _scrollViewer = null;
        }

        if (_headerPresenter is not null)
        {
            _headerPresenter.ColumnResizeRequested -= OnColumnResizeRequested;
            _headerPresenter.ColumnSortRequested -= OnColumnSortRequested;
            _headerPresenter = null;
        }
        _presenter = null;
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ViewportProperty)
        {
            _columnsDirty = true;
            RequestViewportUpdate();
        }
        else if (e.Property == ScrollViewer.OffsetProperty)
        {
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

        if (columnIndex < _columnWidths.Count)
        {
            _columnWidths[columnIndex] = newWidth;
        }

        _columnOffsets.Clear();
        var cumulative = 0d;
        for (var i = 0; i < _columnWidths.Count; i++)
        {
            cumulative += _columnWidths[i];
            _columnOffsets.Add(cumulative);
        }

        _columnsDirty = false;
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
        SortRequested?.Invoke(this, new FastTreeDataGridSortEventArgs(column, columnIndex, newDirection));
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
        if (_scrollViewer is null || _presenter is null || _itemsSource is null)
        {
            return;
        }

        if (_columns.Count == 0)
        {
            _presenter.UpdateContent(Array.Empty<FastTreeDataGridPresenter.RowRenderInfo>(), 0, 0, Array.Empty<double>());
            return;
        }

        if (_columnsDirty || _autoWidthChanged)
        {
            RecalculateColumns();
            _columnsDirty = false;
            _autoWidthChanged = false;
        }

        var rowHeight = Math.Max(1d, RowHeight);
        var totalRows = _itemsSource.RowCount;
        var viewport = _scrollViewer.Viewport;
        var offset = _scrollViewer.Offset;

        var viewportHeight = viewport.Height > 0 ? viewport.Height : Bounds.Height;
        var viewportWidth = viewport.Width > 0 ? viewport.Width : Bounds.Width;
        var totalHeight = Math.Max(totalRows * rowHeight, viewportHeight);
        var totalWidth = Math.Max(_columnWidths.Sum(), viewportWidth);

        if (_headerPresenter is not null)
        {
            _headerPresenter.BindColumns(_columns, _columnWidths);
            _headerPresenter.RenderTransform = new TranslateTransform(-offset.X, 0);
        }

        var buffer = 2;
        var firstIndex = totalRows == 0 ? 0 : Math.Clamp((int)Math.Floor(offset.Y / rowHeight), 0, Math.Max(0, totalRows - 1));
        var visibleCount = (int)Math.Ceiling(viewportHeight / rowHeight) + buffer;
        var lastIndexExclusive = Math.Min(totalRows, firstIndex + visibleCount);

        var rows = new List<FastTreeDataGridPresenter.RowRenderInfo>(Math.Max(0, lastIndexExclusive - firstIndex));
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

        for (var rowIndex = firstIndex; rowIndex < lastIndexExclusive; rowIndex++)
        {
            var row = _itemsSource.GetRow(rowIndex);
            var top = rowIndex * rowHeight;
            var hasChildren = row.HasChildren;
            var toggleRect = hasChildren
                ? new Rect(Math.Max(0, toggleColumnStart + row.Level * IndentWidth + togglePadding), top + (rowHeight - toggleSize) / 2, toggleSize, toggleSize)
                : default;
            var isGroup = row.IsGroup;

            var rowInfo = new FastTreeDataGridPresenter.RowRenderInfo(
                row,
                rowIndex,
                top,
                rowHeight,
                rowIndex == SelectedIndex,
                hasChildren,
                row.IsExpanded,
                toggleRect,
                isGroup);

            var x = 0d;
            for (var columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
            {
                var column = _columns[columnIndex];
                var width = _columnWidths[columnIndex];
                var bounds = new Rect(x, top, width, rowHeight);
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
                var contentBounds = new Rect(x + indentOffset + cellPadding, top, contentWidth, rowHeight);

                Widget? widget = null;
                if (column.WidgetFactory is { } factory)
                {
                    widget = factory(row.ValueProvider, row.Item);
                }

                FormattedText? formatted = null;
                Point textOrigin = new(contentBounds.X, contentBounds.Y + (rowHeight / 2));

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

            rows.Add(rowInfo);
        }

        _presenter.UpdateContent(rows, totalWidth, totalHeight, _columnOffsets);

        if (autoWidthUpdated)
        {
            _autoWidthChanged = true;
            RequestViewportUpdate();
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

        var shouldToggle = rowInfo.HasChildren && (toggleHit || rowInfo.IsGroup || clickCount > 1);
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
}
