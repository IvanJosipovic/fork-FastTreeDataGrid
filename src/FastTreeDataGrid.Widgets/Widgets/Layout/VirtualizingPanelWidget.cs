using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class VirtualizingPanelWidget : SurfaceWidget, IVirtualizingWidgetHost
{
    private readonly Dictionary<int, Widget> _realized = new();
    private IFastTreeDataGridSource? _itemsSource;
    private IFastTreeDataVirtualizationProvider? _provider;
    private FastTreeDataGridViewportScheduler? _viewportScheduler;
    private FastTreeDataGridVirtualizationSettings _virtualizationSettings = new();
    private int _totalCount;
    private double _viewportOffset;
    private double _viewportLength;
    private int _activeStart;
    private int _activeEnd;
    private bool _pendingViewportRequest;

    private double _itemExtent = 48;
    private double _spacing;
    private Thickness _padding;
    private int _bufferItemCount = 2;

    static VirtualizingPanelWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(VirtualizingPanelWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not VirtualizingPanelWidget panel)
                {
                    return;
                }

                var layout = theme.Palette.Layout;

                if (panel.Padding == default)
                {
                    panel.Padding = layout.ContentPadding;
                }

                if (panel.Spacing <= 0)
                {
                    panel.Spacing = layout.DefaultSpacing;
                }
            }));
    }

    protected VirtualizingPanelWidget()
    {
        Orientation = Orientation.Vertical;
    }

    protected IReadOnlyDictionary<int, Widget> RealizedWidgets => _realized;

    protected IFastTreeDataGridSource? ItemsSourceCore => _itemsSource;

    protected IFastTreeDataVirtualizationProvider? VirtualizationProvider => _provider;

    protected int TotalItemCount => _totalCount;

    protected bool TryGetRow(int index, out FastTreeDataGridRow row)
    {
        if (_provider?.TryGetMaterializedRow(index, out row) == true)
        {
            return true;
        }

        if (_itemsSource is not null)
        {
            if (_itemsSource.TryGetMaterializedRow(index, out row))
            {
                return true;
            }

            if (index >= 0 && index < _itemsSource.RowCount)
            {
                try
                {
                    row = _itemsSource.GetRow(index);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        row = default!;
        return false;
    }

    protected void ToggleExpansionAt(int index)
    {
        if (index < 0)
        {
            return;
        }

        _itemsSource?.ToggleExpansion(index);
    }

    public Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ItemFactory { get; set; }

    public IWidgetTemplate? ItemTemplate { get; set; }

    public Orientation Orientation { get; protected set; }

    public IFastTreeDataGridSource? ItemsSource
    {
        get => _itemsSource;
        set => SetItemsSource(value);
    }

    public FastTreeDataGridVirtualizationSettings VirtualizationSettings
    {
        get => _virtualizationSettings;
        set
        {
            _virtualizationSettings = value ?? new FastTreeDataGridVirtualizationSettings();
            _viewportScheduler?.UpdateSettings(_virtualizationSettings);
            RequestViewportUpdate();
        }
    }

    public double ItemExtent
    {
        get => _itemExtent;
        set
        {
            var coerced = Math.Max(1, value);
            if (Math.Abs(_itemExtent - coerced) <= double.Epsilon)
            {
                return;
            }

            _itemExtent = coerced;
            UpdateDesiredExtent();
            RequestViewportUpdate();
        }
    }

    public double Spacing
    {
        get => _spacing;
        set
        {
            var coerced = Math.Max(0, value);
            if (Math.Abs(_spacing - coerced) <= double.Epsilon)
            {
                return;
            }

            _spacing = coerced;
            UpdateDesiredExtent();
            RequestViewportUpdate();
        }
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value)
            {
                return;
            }

            _padding = value;
            UpdateDesiredExtent();
            RequestViewportUpdate();
        }
    }

    public int BufferItemCount
    {
        get => _bufferItemCount;
        set
        {
            var coerced = Math.Max(0, value);
            if (_bufferItemCount == coerced)
            {
                return;
            }

            _bufferItemCount = coerced;
            RequestViewportUpdate();
        }
    }

    public double CrossAxisItemLength { get; set; } = double.NaN;

    public void UpdateViewport(in VirtualizingWidgetViewport viewport)
    {
        var offset = Orientation == Orientation.Vertical ? viewport.Offset.Y : viewport.Offset.X;
        var length = Orientation == Orientation.Vertical ? viewport.ViewportSize.Height : viewport.ViewportSize.Width;
        offset = Math.Max(0, offset);
        length = Math.Max(0, length);

        if (Math.Abs(_viewportOffset - offset) <= double.Epsilon && Math.Abs(_viewportLength - length) <= double.Epsilon)
        {
            return;
        }

        _viewportOffset = offset;
        _viewportLength = length;
        RequestViewportUpdate();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var inner = bounds.Deflate(Padding);
        var axisOffset = Orientation == Orientation.Vertical ? inner.Y : inner.X;
        var crossStart = Orientation == Orientation.Vertical ? inner.X : inner.Y;
        var crossSize = Orientation == Orientation.Vertical ? inner.Width : inner.Height;

        if (!double.IsNaN(CrossAxisItemLength) && CrossAxisItemLength > 0)
        {
            crossSize = Math.Min(crossSize, CrossAxisItemLength);
        }

        var itemExtent = ItemExtent;
        var spacing = Spacing;

        foreach (var pair in _realized.OrderBy(static p => p.Key))
        {
            var index = pair.Key;
            var widget = pair.Value;
            var position = axisOffset + index * (itemExtent + spacing);

            Rect rect;
            if (Orientation == Orientation.Vertical)
            {
                rect = new Rect(
                    crossStart,
                    position,
                    crossSize,
                    itemExtent);
            }
            else
            {
                rect = new Rect(
                    position,
                    crossStart,
                    itemExtent,
                    crossSize);
            }

            widget.Arrange(rect);
        }
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);
        base.Draw(context);
    }

    protected virtual Widget CreateFallbackWidget(FastTreeDataGridRow row)
    {
        var body = WidgetFluentPalette.Current.Text.Typography.Body;
        var widget = new FormattedTextWidget
        {
            EmSize = body.FontSize > 0 ? body.FontSize : 14,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        if (body.FontFamily is not null)
        {
            widget.FontFamily = body.FontFamily;
        }

        widget.FontWeight = body.FontWeight;

        widget.SetText(row.Item?.ToString() ?? string.Empty);
        return widget;
    }

    protected void SetOrientation(Orientation orientation)
    {
        if (Orientation == orientation)
        {
            return;
        }

        Orientation = orientation;
        UpdateDesiredExtent();
        RequestViewportUpdate();
    }

    private void SetItemsSource(IFastTreeDataGridSource? source)
    {
        if (ReferenceEquals(_itemsSource, source))
        {
            return;
        }

        DisposeProvider();
        _itemsSource = source;
        ClearRealized();

        if (source is null)
        {
            UpdateTotalCount(0);
            return;
        }

        AttachProvider(source);
    }

    private void AttachProvider(IFastTreeDataGridSource source)
    {
        _provider = FastTreeDataGridVirtualizationProviderRegistry.Create(source, _virtualizationSettings);

        if (_provider is null)
        {
            UpdateTotalCount(source.RowCount);
            RequestViewportUpdate();
            return;
        }

        _provider.RowMaterialized += OnProviderRowMaterialized;
        _provider.Invalidated += OnProviderInvalidated;
        _provider.CountChanged += OnProviderCountChanged;

        _viewportScheduler = new FastTreeDataGridViewportScheduler(_provider, _virtualizationSettings);

        _ = InitializeProviderAsync(_provider);
    }

    private async Task InitializeProviderAsync(IFastTreeDataVirtualizationProvider provider)
    {
        try
        {
            await provider.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            var count = await provider.GetRowCountAsync(CancellationToken.None).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateTotalCount(count);
                RequestViewportUpdate();
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateTotalCount(_itemsSource?.RowCount ?? 0);
                RequestViewportUpdate();
            });
        }
    }

    private void OnProviderCountChanged(object? sender, FastTreeDataGridCountChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateTotalCount(e.NewCount);
            RequestViewportUpdate();
        });
    }

    private void OnProviderInvalidated(object? sender, FastTreeDataGridInvalidatedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Request.Kind == FastTreeDataGridInvalidationKind.Full)
            {
                ClearRealized();
            }
            else if (e.Request.HasRange)
            {
                RemoveRealizedRange(e.Request.StartIndex, e.Request.Count);
            }

            RequestViewportUpdate();
        });
    }

    private void OnProviderRowMaterialized(object? sender, FastTreeDataGridRowMaterializedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_provider is null)
            {
                return;
            }

            if (e.Index < _activeStart || e.Index >= _activeEnd)
            {
                return;
            }

            if (_realized.ContainsKey(e.Index))
            {
                return;
            }

            RealizeRow(e.Index, e.Row);
            ScheduleArrange();
        });
    }

    protected void RequestViewportUpdate()
    {
        if (_pendingViewportRequest)
        {
            return;
        }

        _pendingViewportRequest = true;

        Dispatcher.UIThread.Post(() =>
        {
            _pendingViewportRequest = false;

            if (_provider is null || _viewportLength <= 0 || _totalCount <= 0)
            {
                TrimRealizedRange(0, 0);
                ScheduleArrange();
                return;
            }

            var axisPaddingStart = Orientation == Orientation.Vertical ? Padding.Top : Padding.Left;
            var totalAxisPadding = Orientation == Orientation.Vertical
                ? Padding.Top + Padding.Bottom
                : Padding.Left + Padding.Right;

            var contentOffset = Math.Max(0, _viewportOffset - axisPaddingStart);
            var extentPerItem = ItemExtent + Spacing;
            var firstIndex = extentPerItem <= 0
                ? 0
                : Math.Max(0, (int)Math.Floor(contentOffset / extentPerItem));
            var visibleItems = extentPerItem <= 0
                ? _totalCount
                : Math.Max(1, (int)Math.Ceiling((_viewportLength + Spacing + axisPaddingStart + totalAxisPadding) / extentPerItem));

            var visibleStart = Math.Max(0, firstIndex);
            var visibleEnd = Math.Min(_totalCount, visibleStart + visibleItems);
            var buffer = BufferItemCount;
            var materializeStart = Math.Max(0, visibleStart - buffer);
            var materializeEnd = Math.Min(_totalCount, visibleEnd + buffer);

            _activeStart = materializeStart;
            _activeEnd = materializeEnd;

            TrimRealizedRange(materializeStart, materializeEnd);
            MaterializeRange(materializeStart, materializeEnd);

            var requestStart = materializeStart;
            var requestCount = Math.Max(0, materializeEnd - materializeStart);

            if (requestCount > 0)
            {
                _viewportScheduler?.Request(new FastTreeDataGridViewportRequest(requestStart, requestCount, buffer));
            }

            ScheduleArrange();
        });
    }

    private void TrimRealizedRange(int startInclusive, int endExclusive)
    {
        if (_realized.Count == 0)
        {
            return;
        }

        var toRemove = _realized.Keys.Where(index => index < startInclusive || index >= endExclusive).ToList();

        if (toRemove.Count == 0)
        {
            return;
        }

        foreach (var index in toRemove)
        {
            _realized.Remove(index);
        }

        RebuildChildren();
    }

    private void MaterializeRange(int startInclusive, int endExclusive)
    {
        if (_provider is null)
        {
            return;
        }

        var realized = false;
        for (var index = startInclusive; index < endExclusive; index++)
        {
            if (_realized.ContainsKey(index))
            {
                continue;
            }

            if (!_provider.TryGetMaterializedRow(index, out var row))
            {
                continue;
            }

            RealizeRow(index, row);
            realized = true;
        }

        if (realized)
        {
            ScheduleArrange();
        }
    }

    private void RealizeRow(int index, FastTreeDataGridRow row)
    {
        var widget = CreateWidget(row);
        if (widget is null)
        {
            return;
        }

        _realized[index] = widget;
        RebuildChildren();
    }

    private Widget? CreateWidget(FastTreeDataGridRow row)
    {
        Widget? widget = null;

        if (ItemFactory is { } factory)
        {
            widget = factory(row.ValueProvider, row.Item);
        }
        else if (ItemTemplate is { } template)
        {
            widget = template.Build();
        }

        widget ??= CreateFallbackWidget(row);

        if (widget is null)
        {
            return null;
        }

        widget.DesiredWidth = Orientation == Orientation.Vertical
            ? (double.IsNaN(CrossAxisItemLength) ? double.NaN : CrossAxisItemLength)
            : ItemExtent;

        widget.DesiredHeight = Orientation == Orientation.Vertical
            ? ItemExtent
            : (double.IsNaN(CrossAxisItemLength) ? double.NaN : CrossAxisItemLength);

        widget.UpdateValue(row.ValueProvider, row.Item);
        PrepareWidget(widget, row);
        return widget;
    }

    protected virtual void PrepareWidget(Widget widget, FastTreeDataGridRow row)
    {
        _ = widget;
        _ = row;
    }

    private void RebuildChildren()
    {
        Children.Clear();
        foreach (var widget in _realized.OrderBy(static pair => pair.Key).Select(static pair => pair.Value))
        {
            Children.Add(widget);
        }
    }

    private void RemoveRealizedRange(int startIndex, int count)
    {
        if (count <= 0 || _realized.Count == 0)
        {
            return;
        }

        var endExclusive = startIndex + count;
        var toRemove = _realized.Keys.Where(index => index >= startIndex && index < endExclusive).ToList();
        foreach (var index in toRemove)
        {
            _realized.Remove(index);
        }

        if (toRemove.Count > 0)
        {
            RebuildChildren();
        }
    }

    protected void ClearRealized()
    {
        if (_realized.Count == 0)
        {
            return;
        }

        _realized.Clear();
        Children.Clear();
        ScheduleArrange();
    }

    private void UpdateTotalCount(int count)
    {
        var coerced = Math.Max(0, count);
        if (_totalCount == coerced)
        {
            return;
        }

        _totalCount = coerced;
        UpdateDesiredExtent();

        if (_totalCount == 0)
        {
            ClearRealized();
        }
    }

    private void UpdateDesiredExtent()
    {
        var totalExtent = _totalCount * ItemExtent;
        if (_totalCount > 1)
        {
            totalExtent += (_totalCount - 1) * Spacing;
        }

        var axisPadding = Orientation == Orientation.Vertical
            ? Padding.Top + Padding.Bottom
            : Padding.Left + Padding.Right;

        totalExtent += axisPadding;

        if (Orientation == Orientation.Vertical)
        {
            DesiredHeight = totalExtent;
            DesiredWidth = double.IsNaN(CrossAxisItemLength) ? double.NaN : CrossAxisItemLength + Padding.Left + Padding.Right;
        }
        else
        {
            DesiredWidth = totalExtent;
            DesiredHeight = double.IsNaN(CrossAxisItemLength) ? double.NaN : CrossAxisItemLength + Padding.Top + Padding.Bottom;
        }
    }

    private void DisposeProvider()
    {
        _viewportScheduler?.Dispose();
        _viewportScheduler = null;

        if (_provider is null)
        {
            return;
        }

        _provider.RowMaterialized -= OnProviderRowMaterialized;
        _provider.Invalidated -= OnProviderInvalidated;
        _provider.CountChanged -= OnProviderCountChanged;

        var provider = _provider;
        _provider = null;

        _ = DisposeProviderAsync(provider);
    }

    private static async Task DisposeProviderAsync(IFastTreeDataVirtualizationProvider provider)
    {
        try
        {
            await provider.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            provider.Dispose();
        }
    }

    private void ScheduleArrange()
    {
        if (Bounds.Width <= 0 && Bounds.Height <= 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () => Arrange(Bounds),
            DispatcherPriority.Render);
    }
}
