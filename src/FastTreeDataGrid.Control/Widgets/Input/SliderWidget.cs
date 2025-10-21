using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class SliderWidget : TemplatedWidget
{
    private double _minimum = 0;
    private double _maximum = 1;
    private double _value;
    private bool _isDragging;
    private ImmutableSolidColorBrush? _trackBrush;
    private ImmutableSolidColorBrush? _fillBrush;
    private ImmutableSolidColorBrush? _thumbBrush;
    private TrackWidget? _trackPart;
    private ThumbWidget? _thumbPart;

    static SliderWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(SliderWidget),
                    state,
                    widget =>
                    {
                        if (widget is SliderWidget slider)
                        {
                            slider.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public SliderWidget()
    {
        IsInteractive = true;
    }

    public event EventHandler<WidgetValueChangedEventArgs<double>>? ValueChanged;

    public double Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }
            Value = _value;
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_maximum < _minimum)
            {
                _minimum = _maximum;
            }
            Value = _value;
        }
    }

    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (Math.Abs(_value - clamped) < double.Epsilon)
            {
                return;
            }

            var previous = _value;
            _value = clamped;
            ValueChanged?.Invoke(this, new WidgetValueChangedEventArgs<double>(this, previous, _value));
            RefreshStyle();
        }
    }

    public ImmutableSolidColorBrush? TrackBrush
    {
        get => _trackBrush;
        set => _trackBrush = value;
    }

    public ImmutableSolidColorBrush? FillBrush
    {
        get => _fillBrush;
        set => _fillBrush = value;
    }

    public ImmutableSolidColorBrush? ThumbBrush
    {
        get => _thumbBrush;
        set => _thumbBrush = value;
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

        var min = _minimum;
        var max = _maximum;
        var value = _value;
        var enabled = IsEnabled;
        _trackBrush = TrackBrush;
        _fillBrush = FillBrush;
        _thumbBrush = ThumbBrush;

        if (provider is not null && Key is not null)
        {
            var data = provider.GetValue(item, Key);
            switch (data)
            {
                case SliderWidgetValue sliderValue:
                    min = sliderValue.Minimum;
                    max = sliderValue.Maximum;
                    value = sliderValue.Value;
                    enabled = sliderValue.IsEnabled;
                    _trackBrush = sliderValue.TrackBrush ?? TrackBrush;
                    _fillBrush = sliderValue.FillBrush ?? FillBrush;
                    _thumbBrush = sliderValue.ThumbBrush ?? ThumbBrush;
                    break;
                case double numeric:
                    value = numeric;
                    break;
            }
        }

        _minimum = min;
        _maximum = Math.Max(max, _minimum);
        IsEnabled = enabled;
        Value = value;
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (!IsTemplateApplied)
        {
            return;
        }

        var track = _trackPart;
        var thumb = _thumbPart;
        if (track is null || thumb is null)
        {
            return;
        }

        var trackHeight = CalculateTrackHeight(bounds);
        var trackY = bounds.Y + (bounds.Height - trackHeight) / 2;
        var trackRect = new Rect(bounds.X, trackY, bounds.Width, Math.Max(0, trackHeight));
        var percent = GetValuePercent();
        track.Arrange(trackRect);
        track.CornerRadius = new CornerRadius(Math.Max(0, trackRect.Height / 2));
        track.IndicatorValue = percent;

        var thumbDiameter = Math.Max(0, CalculateThumbDiameter(trackRect.Height));
        thumbDiameter = Math.Min(thumbDiameter, bounds.Width);

        var thumbRange = Math.Max(0, bounds.Width - thumbDiameter);
        var thumbX = bounds.X + (thumbRange * percent);
        var thumbY = bounds.Y + (bounds.Height - thumbDiameter) / 2;
        thumb.Arrange(new Rect(thumbX, thumbY, thumbDiameter, thumbDiameter));
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
                _isDragging = true;
                UpdateValueFromPoint(e.Position);
                break;
            case WidgetPointerEventKind.Moved:
                if (_isDragging)
                {
                    UpdateValueFromPoint(e.Position);
                }
                break;
            case WidgetPointerEventKind.Released:
                if (_isDragging)
                {
                    UpdateValueFromPoint(e.Position);
                }
                _isDragging = false;
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isDragging = false;
                break;
        }

        return true;
    }

    private void RefreshTemplateAppearance()
    {
        if (!IsTemplateApplied && !ApplyTemplate())
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.Slider;
        var percent = GetValuePercent();

        if (_trackPart is not null)
        {
            var trackBrush = _trackBrush ?? palette.TrackFill.Get(VisualState) ?? palette.TrackFill.Normal
                             ?? new ImmutableSolidColorBrush(Color.FromRgb(220, 220, 220));
            var fillBrush = _fillBrush ?? palette.ValueFill.Get(VisualState) ?? palette.ValueFill.Normal
                            ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
            _trackPart.BackgroundBrush = trackBrush;
            _trackPart.IndicatorBrush = fillBrush;
            _trackPart.BorderBrush = null;
            _trackPart.BorderThickness = 0;
            _trackPart.IndicatorValue = percent;
        }

        if (_thumbPart is not null)
        {
            var thumbFill = _thumbBrush ?? palette.ThumbFill.Get(VisualState) ?? palette.ThumbFill.Normal
                            ?? new ImmutableSolidColorBrush(Colors.White);
            var thumbBorder = palette.ThumbBorder.Get(VisualState) ?? palette.ThumbBorder.Normal
                              ?? new ImmutableSolidColorBrush(Color.FromRgb(200, 200, 200));
            _thumbPart.FillBrush = thumbFill;
            _thumbPart.BorderBrush = thumbBorder;
            _thumbPart.BorderThickness = 1;
        }
    }

    private double CalculateTrackHeight(Rect bounds)
    {
        if (bounds.Height <= 0)
        {
            return 0;
        }

        var height = Math.Min(6, bounds.Height / 3);
        return Math.Max(1, height);
    }

    private static double CalculateThumbDiameter(double trackHeight)
    {
        if (trackHeight <= 0)
        {
            return 14;
        }

        return Math.Max(trackHeight * 2.5, 14);
    }

    private double GetValuePercent()
    {
        if (_maximum <= _minimum)
        {
            return 0;
        }

        var percent = (_value - _minimum) / (_maximum - _minimum);
        return Math.Clamp(percent, 0, 1);
    }

    private void UpdateValueFromPoint(Point localPoint)
    {
        var width = Math.Max(1, Bounds.Width);
        var x = Math.Clamp(localPoint.X, 0, width);
        var percent = x / width;
        var newValue = _minimum + ((_maximum - _minimum) * percent);
        Value = newValue;
    }
}
