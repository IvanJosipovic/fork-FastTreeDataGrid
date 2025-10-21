using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ToggleSwitchWidget : Widget
{
    private bool _isOn;
    private bool _isPointerPressed;
    private ImmutableSolidColorBrush? _onBrush;
    private ImmutableSolidColorBrush? _offBrush;
    private ImmutableSolidColorBrush? _thumbBrush;

    public ToggleSwitchWidget()
    {
        IsInteractive = true;
    }

    public event Action<bool>? Toggled;

    public bool IsOn => _isOn;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
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

    public override void Draw(DrawingContext context)
    {
        var trackRect = Bounds;
        if (trackRect.Width <= 0 || trackRect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);

        var trackHeight = Math.Max(18, Math.Min(trackRect.Height, 28));
        var trackWidth = Math.Max(trackHeight * 1.6, trackRect.Width);
        var offsetY = trackRect.Y + (trackRect.Height - trackHeight) / 2;
        var offsetX = trackRect.X + (trackRect.Width - trackWidth) / 2;
        var drawRect = new Rect(offsetX, offsetY, trackWidth, trackHeight);
        var radius = trackHeight / 2;

        var onBrush = _onBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
        var offBrush = _offBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(189, 189, 189));
        var thumbBrush = _thumbBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(255, 255, 255));

        var trackBrush = _isOn ? onBrush : offBrush;
        context.DrawRectangle(trackBrush, null, drawRect, radius, radius);

        var thumbDiameter = trackHeight - 4;
        var thumbRadius = thumbDiameter / 2;
        var thumbY = drawRect.Y + 2;
        var thumbX = _isOn
            ? drawRect.Right - thumbDiameter - 2
            : drawRect.X + 2;
        var thumbRect = new Rect(thumbX, thumbY, thumbDiameter, thumbDiameter);

        context.DrawEllipse(thumbBrush, null, thumbRect.Center, thumbRadius, thumbRadius);
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsInteractive)
        {
            return handled;
        }

        if (!IsEnabled)
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
                    ToggleState();
                }
                _isPointerPressed = false;
                break;
            case WidgetPointerEventKind.Cancelled:
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

        _isOn = value;
        RefreshStyle();

        if (raise)
        {
            Toggled?.Invoke(_isOn);
        }
    }

    private bool IsWithinBounds(Point point)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(point);
    }
}
