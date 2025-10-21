using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class RadioButtonWidget : Widget
{
    private bool _isChecked;
    private bool _isPointerPressed;
    private ImmutableSolidColorBrush? _fillBrush;
    private ImmutableSolidColorBrush? _borderBrush;

    public RadioButtonWidget()
    {
        IsInteractive = true;
    }

    public event Action<bool>? CheckedChanged;

    public bool IsChecked => _isChecked;

    public string? Group { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        var isChecked = _isChecked;
        var enabled = IsEnabled;
        _fillBrush = null;
        _borderBrush = null;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case RadioButtonWidgetValue radioValue:
                    isChecked = radioValue.IsChecked;
                    enabled = radioValue.IsEnabled;
                    break;
                case bool boolean:
                    isChecked = boolean;
                    break;
            }
        }

        _isPointerPressed = false;
        SetChecked(isChecked, raise: false);
        IsEnabled = enabled;
    }

    public override void Draw(DrawingContext context)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        var diameter = Math.Min(bounds.Width, bounds.Height);
        var radius = diameter / 2;
        var center = bounds.Center;

        var borderBrush = _borderBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96));
        var fillBrush = _fillBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
        var background = IsEnabled ? Brushes.Transparent : new ImmutableSolidColorBrush(Color.FromRgb(235, 235, 235));

        context.DrawEllipse(background, new Pen(borderBrush, 1.5), center, radius, radius);

        if (_isChecked)
        {
            var innerRadius = Math.Max(2, radius - 4);
            context.DrawEllipse(fillBrush, null, center, innerRadius, innerRadius);
        }
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
                    SetChecked(true);
                }
                _isPointerPressed = false;
                break;
            case WidgetPointerEventKind.Cancelled:
                _isPointerPressed = false;
                break;
        }

        return true;
    }

    public void SetChecked(bool value) => SetChecked(value, raise: true);

    private void SetChecked(bool value, bool raise)
    {
        if (_isChecked == value)
        {
            return;
        }

        _isChecked = value;
        RefreshStyle();

        if (raise)
        {
            CheckedChanged?.Invoke(_isChecked);
        }
    }

    private bool IsWithinBounds(Point point)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(point);
    }
}
