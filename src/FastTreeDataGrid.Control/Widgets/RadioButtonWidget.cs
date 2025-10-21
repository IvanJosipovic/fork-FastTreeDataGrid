using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

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
        using var rotation = context.PushTransform(CreateRotationMatrix());

        var palette = WidgetFluentPalette.Current.RadioButton;
        var controlCorner = WidgetFluentPalette.Current.ControlCornerRadius.TopLeft;

        var background = palette.Background.Get(VisualState);
        var border = palette.Border.Get(VisualState);
        if (background is not null || border is not null)
        {
            var pen = border is null ? null : new Pen(border, palette.BorderThickness);
            context.DrawRectangle(background, pen, bounds, controlCorner, controlCorner);
        }

        var diameter = Math.Min(bounds.Width, bounds.Height);
        var radius = diameter / 2;
        var center = bounds.Center;

        var strokeState = _isChecked ? palette.CheckedEllipseStroke : palette.OuterEllipseStroke;
        var fillState = _isChecked ? palette.CheckedEllipseFill : palette.OuterEllipseFill;

        var strokeBrush = _borderBrush ?? strokeState.Get(VisualState) ?? strokeState.Normal;
        var fillBrush = fillState.Get(VisualState) ?? fillState.Normal;
        var outerPen = strokeBrush is null ? null : new Pen(strokeBrush, Math.Max(1, palette.BorderThickness));
        context.DrawEllipse(fillBrush ?? Brushes.Transparent, outerPen, center, radius, radius);

        if (_isChecked)
        {
            var glyphBrush = Foreground
                              ?? (_fillBrush ?? palette.GlyphFill.Get(VisualState) ?? palette.GlyphFill.Normal)
                              ?? new ImmutableSolidColorBrush(Colors.White);
            var innerRadius = Math.Max(2, radius - 4);
            context.DrawEllipse(glyphBrush, null, center, innerRadius, innerRadius);
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

    private Matrix CreateRotationMatrix()
    {
        if (Math.Abs(Rotation) <= double.Epsilon)
        {
            return Matrix.Identity;
        }

        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        return Matrix.CreateTranslation(-centerX, -centerY)
               * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
               * Matrix.CreateTranslation(centerX, centerY);
    }
}
