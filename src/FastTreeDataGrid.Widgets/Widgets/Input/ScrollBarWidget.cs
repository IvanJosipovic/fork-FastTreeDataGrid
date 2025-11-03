using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ScrollBarWidget : TemplatedWidget
{
    private Orientation _orientation = Orientation.Vertical;
    private double _minimum;
    private double _maximum = 1;
    private double _value;
    private double _viewportSize;
    private double _smallChange = 1;
    private double _largeChange = double.NaN;
    private ImmutableSolidColorBrush? _trackBrushOverride;
    private ImmutableSolidColorBrush? _trackBorderBrushOverride;
    private ImmutableSolidColorBrush? _thumbBrushOverride;
    private ImmutableSolidColorBrush? _thumbBorderBrushOverride;
    private TrackWidget? _trackPart;
    private ThumbWidget? _thumbPart;
    private Rect _trackRect;
    private Rect _thumbRect;
    private bool _isDragging;
    private double _dragOffset;

    static ScrollBarWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(ScrollBarWidget),
                    state,
                    (widget, _) =>
                    {
                        if (widget is ScrollBarWidget scrollBar)
                        {
                            scrollBar.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public ScrollBarWidget()
    {
        IsInteractive = true;
        ClipToBounds = true;
    }

    public event EventHandler<WidgetValueChangedEventArgs<double>>? ValueChanged;

    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value)
            {
                return;
            }

            _orientation = value;
            RefreshStyle();
        }
    }

    public double Minimum
    {
        get => _minimum;
        set
        {
            if (Math.Abs(_minimum - value) <= double.Epsilon)
            {
                return;
            }

            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            SetValueInternal(_value, raise: false);
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            if (Math.Abs(_maximum - value) <= double.Epsilon)
            {
                return;
            }

            _maximum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            SetValueInternal(_value, raise: false);
        }
    }

    public double ViewportSize
    {
        get => _viewportSize;
        set
        {
            var clamped = Math.Max(0, value);
            if (Math.Abs(_viewportSize - clamped) <= double.Epsilon)
            {
                return;
            }

            _viewportSize = clamped;
            RefreshStyle();
        }
    }

    public double SmallChange
    {
        get => _smallChange;
        set => _smallChange = Math.Max(0, value);
    }

    public double LargeChange
    {
        get => _largeChange;
        set => _largeChange = value;
    }

    public double Value
    {
        get => _value;
        set => SetValueInternal(value, raise: true);
    }

    protected override Widget? CreateDefaultTemplate()
    {
        var track = new TrackWidget();
        var thumb = new ThumbWidget();

        var root = new SurfaceWidget();
        root.Children.Add(track);
        root.Children.Add(thumb);

        _trackPart = track;
        _thumbPart = thumb;

        return root;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        RefreshTemplateAppearance();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        var minimum = _minimum;
        var maximum = _maximum;
        var value = _value;
        var viewport = _viewportSize;
        var orientation = _orientation;
        var enabled = IsEnabled;
        var smallChange = _smallChange;
        var largeChange = _largeChange;

        _trackBrushOverride = null;
        _trackBorderBrushOverride = null;
        _thumbBrushOverride = null;
        _thumbBorderBrushOverride = null;

        if (provider is not null && Key is not null)
        {
            var data = provider.GetValue(item, Key);
            switch (data)
            {
                case ScrollBarWidgetValue scrollValue:
                    minimum = scrollValue.Minimum;
                    maximum = scrollValue.Maximum;
                    value = scrollValue.Value;
                    viewport = scrollValue.ViewportSize;
                    orientation = scrollValue.Orientation;
                    if (scrollValue.SmallChange.HasValue)
                    {
                        smallChange = Math.Max(0, scrollValue.SmallChange.Value);
                    }

                    if (scrollValue.LargeChange.HasValue)
                    {
                        largeChange = scrollValue.LargeChange.Value;
                    }

                    enabled = scrollValue.IsEnabled;
                    _trackBrushOverride = scrollValue.TrackBrush;
                    _trackBorderBrushOverride = scrollValue.TrackBorderBrush;
                    _thumbBrushOverride = scrollValue.ThumbBrush;
                    _thumbBorderBrushOverride = scrollValue.ThumbBorderBrush;
                    if (scrollValue.Interaction is { } interaction)
                    {
                        enabled = interaction.IsEnabled;
                    }
                    break;
                case double numeric:
                    value = numeric;
                    break;
            }
        }

        _minimum = minimum;
        _maximum = Math.Max(maximum, minimum);
        _viewportSize = Math.Max(0, viewport);
        _orientation = orientation;
        _smallChange = smallChange;
        _largeChange = largeChange;
        IsEnabled = enabled;
        SetValueInternal(value, raise: false);
        RefreshStyle();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (!IsTemplateApplied)
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.ScrollBar;
        var thickness = Math.Max(1, Math.Min(_orientation == Orientation.Horizontal ? bounds.Height : bounds.Width, palette.Thickness));

        var trackLocal = _orientation == Orientation.Horizontal
            ? new Rect(0, Math.Max(0, (bounds.Height - thickness) / 2), bounds.Width, thickness)
            : new Rect(Math.Max(0, (bounds.Width - thickness) / 2), 0, thickness, bounds.Height);
        _trackRect = trackLocal;

        if (_trackPart is not null)
        {
            _trackPart.Arrange(ToAbsolute(bounds, trackLocal));
            _trackPart.CornerRadius = new CornerRadius(thickness / 2);
        }

        var thumbLocal = CalculateThumbRect(trackLocal, palette);
        _thumbRect = thumbLocal;

        if (_thumbPart is not null)
        {
            _thumbPart.Arrange(ToAbsolute(bounds, thumbLocal));
        }
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsInteractive || !IsEnabled)
        {
            return handled;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Pressed:
                if (IsPointInThumb(e.Position))
                {
                    _isDragging = true;
                    _dragOffset = _orientation == Orientation.Horizontal
                        ? e.Position.X - _thumbRect.X
                        : e.Position.Y - _thumbRect.Y;
                }
                else
                {
                    _isDragging = true;
                    _dragOffset = _orientation == Orientation.Horizontal ? _thumbRect.Width / 2 : _thumbRect.Height / 2;
                    UpdateValueFromPoint(e.Position);
                }
                handled = true;
                break;
            case WidgetPointerEventKind.Moved:
                if (_isDragging)
                {
                    UpdateValueFromPoint(e.Position);
                    handled = true;
                }
                break;
            case WidgetPointerEventKind.Released:
                if (_isDragging)
                {
                    UpdateValueFromPoint(e.Position);
                    handled = true;
                }
                _isDragging = false;
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isDragging = false;
                break;
        }

        return handled || IsInteractive;
    }

    private void UpdateValueFromPoint(Point point)
    {
        var trackLength = _orientation == Orientation.Horizontal ? _trackRect.Width : _trackRect.Height;
        var thumbLength = _orientation == Orientation.Horizontal ? _thumbRect.Width : _thumbRect.Height;
        var range = Math.Max(0, _maximum - _minimum);
        if (trackLength <= 0 || range <= 0)
        {
            SetValueInternal(_minimum, raise: true);
            return;
        }

        var axisStart = _orientation == Orientation.Horizontal ? _trackRect.X : _trackRect.Y;
        var local = _orientation == Orientation.Horizontal ? point.X : point.Y;
        var offset = Math.Clamp(local - axisStart - _dragOffset, 0, Math.Max(0, trackLength - thumbLength));
        var denominator = Math.Max(double.Epsilon, trackLength - thumbLength);
        var percent = denominator <= 0 ? 0 : offset / denominator;
        var newValue = _minimum + (range * percent);
        SetValueInternal(newValue, raise: true);
    }

    private Rect CalculateThumbRect(Rect trackRect, WidgetFluentPalette.ScrollBarPalette palette)
    {
        var axisLength = _orientation == Orientation.Horizontal ? trackRect.Width : trackRect.Height;
        if (axisLength <= 0)
        {
            return _orientation == Orientation.Horizontal
                ? new Rect(trackRect.X, trackRect.Y, 0, trackRect.Height)
                : new Rect(trackRect.X, trackRect.Y, trackRect.Width, 0);
        }

        var range = Math.Max(0, _maximum - _minimum);
        var viewport = Math.Max(0, _viewportSize);
        double thumbLength;

        if (range <= 0 || viewport >= range)
        {
            thumbLength = axisLength;
        }
        else if (viewport <= 0)
        {
            thumbLength = palette.MinimumThumbLength;
        }
        else
        {
            thumbLength = Math.Max(palette.MinimumThumbLength, axisLength * (viewport / (range + viewport)));
        }

        if (axisLength < palette.MinimumThumbLength)
        {
            thumbLength = axisLength;
        }
        else
        {
            thumbLength = Math.Clamp(thumbLength, palette.MinimumThumbLength, axisLength);
        }
        var percent = GetValuePercent(range);
        var travel = Math.Max(0, axisLength - thumbLength);
        var offset = travel * percent;

        if (_orientation == Orientation.Horizontal)
        {
            return new Rect(trackRect.X + offset, trackRect.Y, thumbLength, trackRect.Height);
        }

        return new Rect(trackRect.X, trackRect.Y + offset, trackRect.Width, thumbLength);
    }

    private double GetValuePercent(double range)
    {
        if (range <= 0)
        {
            return 0;
        }

        var percent = (_value - _minimum) / range;
        return Math.Clamp(percent, 0, 1);
    }

    private bool IsPointInThumb(Point point)
    {
        return _thumbRect.Contains(point);
    }

    private static Rect ToAbsolute(Rect bounds, Rect local)
    {
        return new Rect(bounds.X + local.X, bounds.Y + local.Y, local.Width, local.Height);
    }

    private void RefreshTemplateAppearance()
    {
        if (!IsTemplateApplied && !ApplyTemplate())
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.ScrollBar;

        if (_trackPart is not null)
        {
            var trackBrush = _trackBrushOverride
                             ?? palette.TrackFill.Get(VisualState)
                             ?? palette.TrackFill.Normal
                             ?? new ImmutableSolidColorBrush(Colors.Transparent);
            var trackBorder = _trackBorderBrushOverride
                              ?? palette.TrackStroke.Get(VisualState)
                              ?? palette.TrackStroke.Normal;

            _trackPart.BackgroundBrush = trackBrush;
            _trackPart.IndicatorBrush = null;
            _trackPart.IndicatorValue = 0;
            _trackPart.BorderBrush = trackBorder;
            _trackPart.BorderThickness = trackBorder is null ? 0 : palette.TrackBorderThickness;
        }

        if (_thumbPart is not null)
        {
            var thumbFill = _thumbBrushOverride
                             ?? palette.ThumbFill.Get(VisualState)
                             ?? palette.ThumbFill.Normal
                             ?? new ImmutableSolidColorBrush(Color.FromRgb(0x9C, 0x9C, 0x9C));
            var thumbBorder = _thumbBorderBrushOverride
                              ?? palette.ThumbBorder.Get(VisualState)
                              ?? palette.ThumbBorder.Normal;

            _thumbPart.FillBrush = thumbFill;
            _thumbPart.BorderBrush = thumbBorder;
            _thumbPart.BorderThickness = thumbBorder is null ? 0 : 1;
        }
    }

    private void SetValueInternal(double value, bool raise)
    {
        var clamped = Clamp(value);
        if (Math.Abs(_value - clamped) <= double.Epsilon)
        {
            return;
        }

        var previous = _value;
        _value = clamped;

        if (raise)
        {
            ValueChanged?.Invoke(this, new WidgetValueChangedEventArgs<double>(this, previous, _value));
        }

        RefreshStyle();
    }

    private double Clamp(double value)
    {
        if (_maximum <= _minimum)
        {
            return _minimum;
        }

        var clamped = Math.Clamp(value, _minimum, _maximum);
        return clamped;
    }
}
