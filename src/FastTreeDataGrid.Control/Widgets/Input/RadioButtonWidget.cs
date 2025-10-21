using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class RadioButtonWidget : TemplatedWidget
{
    private const double InnerInset = 4;
    private static readonly EllipseGeometry CircleGeometry = new(new Rect(0, 0, 1, 1));

    private bool _isChecked;
    private bool _isPointerPressed;
    private BorderWidget? _containerPart;
    private GeometryWidget? _outerCirclePart;
    private GeometryWidget? _innerCirclePart;

    static RadioButtonWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(RadioButtonWidget),
                    state,
                    widget =>
                    {
                        if (widget is RadioButtonWidget radio)
                        {
                            radio.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public RadioButtonWidget()
    {
        IsInteractive = true;
    }

    public event EventHandler<WidgetEventArgs>? Click;

    public event EventHandler<WidgetEventArgs>? Checked;

    public event EventHandler<WidgetEventArgs>? Unchecked;

    public event EventHandler<WidgetValueChangedEventArgs<bool>>? IsCheckedChanged;

    public bool IsChecked => _isChecked;

    public string? Group { get; set; }

    protected override Widget? CreateDefaultTemplate()
    {
        var container = new BorderWidget
        {
            ClipToBounds = true
        };

        var outer = new GeometryWidget
        {
            ClipToBounds = false,
            Stretch = Stretch.Uniform,
            Padding = 0
        };

        var inner = new GeometryWidget
        {
            ClipToBounds = false,
            Stretch = Stretch.Uniform,
            Padding = 0
        };

        var root = new SurfaceWidget();
        root.Children.Add(container);
        root.Children.Add(outer);
        root.Children.Add(inner);

        _containerPart = container;
        _outerCirclePart = outer;
        _innerCirclePart = inner;

        return root;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        RefreshTemplateAppearance();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        var isChecked = _isChecked;
        var enabled = IsEnabled;

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

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (!IsTemplateApplied)
        {
            return;
        }

        _containerPart?.Arrange(bounds);

        var palette = WidgetFluentPalette.Current.RadioButton;
        var padded = Deflate(bounds, palette.Padding);
        var diameter = Math.Min(padded.Width, padded.Height);
        if (diameter <= 0)
        {
            _outerCirclePart?.Arrange(new Rect(padded.X, padded.Y, 0, 0));
            _innerCirclePart?.Arrange(new Rect(padded.X, padded.Y, 0, 0));
            return;
        }

        var circleRect = new Rect(
            padded.X + (padded.Width - diameter) / 2,
            padded.Y + (padded.Height - diameter) / 2,
            diameter,
            diameter);
        _outerCirclePart?.Arrange(circleRect);

        var innerDiameter = Math.Max(0, diameter - (InnerInset * 2));
        if (innerDiameter > 0)
        {
            var innerRect = new Rect(
                circleRect.X + (circleRect.Width - innerDiameter) / 2,
                circleRect.Y + (circleRect.Height - innerDiameter) / 2,
                innerDiameter,
                innerDiameter);
            _innerCirclePart?.Arrange(innerRect);
        }
        else
        {
            _innerCirclePart?.Arrange(new Rect(circleRect.X, circleRect.Y, 0, 0));
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

    public void SetChecked(bool value) => SetChecked(value, raise: true);

    private void SetChecked(bool value, bool raise)
    {
        var previous = _isChecked;
        if (_isChecked == value)
        {
            if (raise)
            {
                Click?.Invoke(this, new WidgetEventArgs(this));
            }
            return;
        }

        _isChecked = value;
        RefreshStyle();

        if (raise)
        {
            RaiseCheckedEvents(previous, _isChecked);
            Click?.Invoke(this, new WidgetEventArgs(this));
        }
    }

    private void OnClick()
    {
        if (!IsEnabled)
        {
            return;
        }

        SetChecked(true);
    }

    private void RaiseCheckedEvents(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        IsCheckedChanged?.Invoke(this, new WidgetValueChangedEventArgs<bool>(this, oldValue, newValue));

        if (newValue)
        {
            Checked?.Invoke(this, new WidgetEventArgs(this));
        }
        else
        {
            Unchecked?.Invoke(this, new WidgetEventArgs(this));
        }
    }

    private void RefreshTemplateAppearance()
    {
        if (!IsTemplateApplied && !ApplyTemplate())
        {
            return;
        }

        var paletteData = WidgetFluentPalette.Current;
        var palette = paletteData.RadioButton;

        if (_containerPart is not null)
        {
            var background = palette.Background.Get(VisualState);
            var border = palette.Border.Get(VisualState);
            _containerPart.Background = background;
            _containerPart.BorderBrush = border;
            var thickness = palette.BorderThickness;
            _containerPart.BorderThickness = thickness > 0 ? new Thickness(thickness) : default;
            _containerPart.CornerRadius = paletteData.ControlCornerRadius;
        }

        if (_outerCirclePart is not null)
        {
            var outerFillState = _isChecked ? palette.CheckedEllipseFill : palette.OuterEllipseFill;
            var outerStrokeState = _isChecked ? palette.CheckedEllipseStroke : palette.OuterEllipseStroke;

            var fill = outerFillState.Get(VisualState) ?? outerFillState.Normal;
            var strokeBrush = outerStrokeState.Get(VisualState) ?? outerStrokeState.Normal;
            Pen? strokePen = strokeBrush is null ? null : new Pen(strokeBrush, Math.Max(1, palette.BorderThickness));

            _outerCirclePart.SetGeometry(CircleGeometry, Stretch.Uniform, fill, strokePen, 0);
        }

        if (_innerCirclePart is not null)
        {
            if (_isChecked)
            {
                var glyphFill = Foreground
                                ?? (palette.GlyphFill.Get(VisualState) ?? palette.GlyphFill.Normal)
                                ?? new ImmutableSolidColorBrush(Colors.White);
                _innerCirclePart.SetGeometry(CircleGeometry, Stretch.Uniform, glyphFill, null, 0);
            }
            else
            {
                _innerCirclePart.SetGeometry(null);
            }
        }
    }

    private bool IsWithinBounds(Point point)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(point);
    }

    private static Rect Deflate(Rect rect, Thickness padding)
    {
        if (padding == default)
        {
            return rect;
        }

        var left = Math.Max(0, padding.Left);
        var top = Math.Max(0, padding.Top);
        var right = Math.Max(0, padding.Right);
        var bottom = Math.Max(0, padding.Bottom);

        var width = Math.Max(0, rect.Width - left - right);
        var height = Math.Max(0, rect.Height - top - bottom);
        return new Rect(rect.X + left, rect.Y + top, width, height);
    }
}
