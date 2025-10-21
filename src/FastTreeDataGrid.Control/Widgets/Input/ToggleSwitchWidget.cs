using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ToggleSwitchWidget : TemplatedWidget
{
    private bool _isOn;
    private bool _isPointerPressed;
    private ImmutableSolidColorBrush? _onBrush;
    private ImmutableSolidColorBrush? _offBrush;
    private ImmutableSolidColorBrush? _thumbBrush;
    private BorderWidget? _containerPart;
    private TrackWidget? _trackPart;
    private ThumbWidget? _thumbPart;

    static ToggleSwitchWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(ToggleSwitchWidget),
                    state,
                    widget =>
                    {
                        if (widget is ToggleSwitchWidget toggle)
                        {
                            toggle.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public ToggleSwitchWidget()
    {
        IsInteractive = true;
    }

    public event EventHandler<WidgetEventArgs>? Click;

    public event EventHandler<WidgetEventArgs>? Checked;

    public event EventHandler<WidgetEventArgs>? Unchecked;

    public event EventHandler<WidgetValueChangedEventArgs<bool>>? Toggled;

    public bool IsOn => _isOn;

    protected override Widget? CreateDefaultTemplate()
    {
        var container = new BorderWidget
        {
            ClipToBounds = true
        };

        var track = new TrackWidget();
        var thumb = new ThumbWidget();

        var root = new SurfaceWidget();
        root.Children.Add(container);
        root.Children.Add(track);
        root.Children.Add(thumb);

        _containerPart = container;
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

        var on = _isOn;
        var enabled = IsEnabled;
        _onBrush = null;
        _offBrush = null;
        _thumbBrush = null;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case ToggleSwitchWidgetValue toggleValue:
                    on = toggleValue.IsOn;
                    enabled = toggleValue.IsEnabled;
                    _onBrush = toggleValue.OnBrush;
                    _offBrush = toggleValue.OffBrush;
                    _thumbBrush = toggleValue.ThumbBrush;
                    break;
                case bool boolean:
                    on = boolean;
                    break;
            }
        }

        _isPointerPressed = false;
        SetState(on, raise: false);
        IsEnabled = enabled;
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (!IsTemplateApplied)
        {
            return;
        }

        _containerPart?.Arrange(bounds);

        var track = _trackPart;
        var thumb = _thumbPart;
        if (track is null || thumb is null)
        {
            return;
        }

        var layout = CalculateLayout(bounds);
        track.Arrange(layout.trackRect);
        track.CornerRadius = new CornerRadius(layout.trackRect.Height / 2);
        track.IndicatorValue = 1;
        thumb.Arrange(layout.thumbRect);
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
                _isPointerPressed = true;
                break;
            case WidgetPointerEventKind.Released:
                if (_isPointerPressed && IsWithinBounds(e.Position))
                {
                    OnClick();
                }
                _isPointerPressed = false;
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isPointerPressed = false;
                break;
        }

        return true;
    }

    public void SetState(bool value) => SetState(value, raise: true);

    private void ToggleState()
    {
        SetState(!_isOn);
    }

    private void SetState(bool value, bool raise)
    {
        if (_isOn == value)
        {
            return;
        }

        var oldValue = _isOn;
        _isOn = value;
        RefreshStyle();

        if (raise)
        {
            RaiseToggleEvents(oldValue, _isOn);
        }
    }

    private void OnClick()
    {
        if (!IsEnabled)
        {
            return;
        }

        ToggleState();
        Click?.Invoke(this, new WidgetEventArgs(this));
    }

    private void RaiseToggleEvents(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        var args = new WidgetValueChangedEventArgs<bool>(this, oldValue, newValue);
        Toggled?.Invoke(this, args);

        if (newValue)
        {
            Checked?.Invoke(this, new WidgetEventArgs(this));
        }
        else
        {
            Unchecked?.Invoke(this, new WidgetEventArgs(this));
        }
    }

    private (Rect trackRect, Rect thumbRect) CalculateLayout(Rect bounds)
    {
        var trackHeight = Math.Max(18, Math.Min(bounds.Height, 28));
        var trackWidth = Math.Max(trackHeight * 1.6, bounds.Width);
        var offsetY = bounds.Y + (bounds.Height - trackHeight) / 2;
        var offsetX = bounds.X + (bounds.Width - trackWidth) / 2;
        var trackRect = new Rect(offsetX, offsetY, trackWidth, trackHeight);

        var thumbDiameter = Math.Max(0, trackHeight - 4);
        var thumbX = _isOn ? trackRect.Right - thumbDiameter - 2 : trackRect.X + 2;
        var thumbY = trackRect.Y + 2;
        var thumbRect = new Rect(thumbX, thumbY, thumbDiameter, thumbDiameter);
        return (trackRect, thumbRect);
    }

    private void RefreshTemplateAppearance()
    {
        if (!IsTemplateApplied && !ApplyTemplate())
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.ToggleSwitch;
        var statePalette = _isOn ? palette.On : palette.Off;

        if (_containerPart is not null)
        {
            _containerPart.Background = palette.ContainerBackground.Get(VisualState);
            _containerPart.BorderBrush = null;
            _containerPart.BorderThickness = default;
        }

        if (_trackPart is not null)
        {
            var fill = (_isOn ? _onBrush : _offBrush) ?? statePalette.TrackFill.Get(VisualState) ?? statePalette.TrackFill.Normal
                       ?? new ImmutableSolidColorBrush(Color.FromRgb(189, 189, 189));
            var stroke = statePalette.TrackStroke.Get(VisualState) ?? statePalette.TrackStroke.Normal;
            var thickness = _isOn ? palette.OnStrokeThickness : palette.OuterStrokeThickness;

            _trackPart.BackgroundBrush = fill;
            _trackPart.IndicatorBrush = null;
            _trackPart.BorderBrush = stroke;
            _trackPart.BorderThickness = thickness;
            _trackPart.IndicatorValue = 1;
        }

        if (_thumbPart is not null)
        {
            var thumbFill = _thumbBrush ?? statePalette.KnobFill.Get(VisualState) ?? statePalette.KnobFill.Normal
                            ?? new ImmutableSolidColorBrush(Colors.White);
            _thumbPart.FillBrush = thumbFill;
            _thumbPart.BorderBrush = null;
            _thumbPart.BorderThickness = 0;
        }
    }

    private bool IsWithinBounds(Point point)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(point);
    }

}
