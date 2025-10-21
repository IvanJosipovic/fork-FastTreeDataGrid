using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using ButtonVariantPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.ButtonVariantPalette;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ButtonWidget : Widget
{
    private string _text = string.Empty;
    private bool _isPrimary;
    private bool _isPressedSource;
    private bool _isPointerPressed;
    private ImmutableSolidColorBrush? _background;
    private ImmutableSolidColorBrush? _borderBrush;
    private double? _fontSize;

    private FormattedText? _formattedText;
    private string? _cachedText;
    private double _cachedFontSize;
    private ImmutableSolidColorBrush? _cachedForeground;

    public ButtonWidget()
    {
        IsInteractive = true;
    }

    public event Action<ButtonWidget>? Clicked;

    public ImmutableSolidColorBrush? Background { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double? FontSize { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        var palette = WidgetFluentPalette.Current.Button;

        _text = string.Empty;
        _isPrimary = false;
        _isPressedSource = false;
        _isPointerPressed = false;
        var enabled = true;
        _background = Background;
        _borderBrush = BorderBrush;
        _fontSize = FontSize;
        CornerRadius = default;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case ButtonWidgetValue buttonValue:
                    _text = buttonValue.Text;
                    _isPrimary = buttonValue.IsPrimary;
                    _isPressedSource = buttonValue.IsPressed;
                    enabled = buttonValue.IsEnabled;
                    _background = buttonValue.Background ?? Background;
                    _borderBrush = buttonValue.BorderBrush ?? BorderBrush;
                    _fontSize = buttonValue.FontSize ?? FontSize;
                    if (buttonValue.CornerRadius.HasValue)
                    {
                        CornerRadius = new CornerRadius(buttonValue.CornerRadius.Value);
                    }
                    break;
                case string text:
                    _text = text;
                    break;
            }
        }

        InvalidateFormattedText();
        IsEnabled = enabled;
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        InvalidateFormattedText();
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        var palette = WidgetFluentPalette.Current.Button;
        var variant = _isPrimary ? palette.Accent : palette.Standard;
        var background = ResolveBackground(variant);
        var borderBrush = ResolveBorder(variant);
        var borderThickness = palette.BorderThickness;
        var cornerRadius = GetUniformCornerRadius(CornerRadius == default ? palette.CornerRadius : CornerRadius);

        using var rotation = context.PushTransform(CreateRotationMatrix());
        var pen = borderBrush is null || borderThickness <= 0
            ? null
            : new Pen(borderBrush, borderThickness);

        context.DrawRectangle(background, pen, Bounds, cornerRadius, cornerRadius);

        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        var formatted = GetOrCreateText(variant);
        if (formatted is null)
        {
            return;
        }

        var contentRect = Deflate(Bounds, palette.Padding);
        var originX = contentRect.X + Math.Max(0, (contentRect.Width - formatted.Width) / 2);
        var originY = contentRect.Y + Math.Max(0, (contentRect.Height - formatted.Height) / 2);

        context.DrawText(formatted, new Point(originX, originY));
    }

    private FormattedText? GetOrCreateText(ButtonVariantPalette variant)
    {
        var foreground = ResolveForeground(variant);

        var fontSize = _fontSize ?? Math.Max(12, Bounds.Height * 0.45);

        if (_formattedText is not null
            && string.Equals(_cachedText, _text, StringComparison.Ordinal)
            && Math.Abs(_cachedFontSize - fontSize) <= double.Epsilon
            && BrushEquals(_cachedForeground, foreground))
        {
            return _formattedText;
        }

        if (string.IsNullOrEmpty(_text))
        {
            _formattedText = null;
            _cachedText = null;
            _cachedFontSize = 0;
            return null;
        }

        _formattedText = new FormattedText(
            _text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            foreground);

        _cachedText = _text;
        _cachedFontSize = fontSize;
        _cachedForeground = foreground;
        return _formattedText;
    }

    private void InvalidateFormattedText()
    {
        _formattedText = null;
        _cachedText = null;
        _cachedFontSize = 0;
        _cachedForeground = null;
    }

    private ImmutableSolidColorBrush ResolveForeground(ButtonVariantPalette variant)
    {
        if (Foreground is not null)
        {
            return Foreground;
        }

        var brush = variant.Foreground.Get(VisualState)
                    ?? variant.Foreground.Normal
                    ?? variant.Foreground.Disabled;

        if (brush is not null)
        {
            return brush;
        }

        return _isPrimary
            ? new ImmutableSolidColorBrush(Colors.White)
            : new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40));
    }

    private ImmutableSolidColorBrush ResolveBackground(ButtonVariantPalette variant)
    {
        if (!IsEnabled)
        {
            var disabled = variant.Background.Get(WidgetVisualState.Disabled) ?? variant.Background.Normal;
            return disabled ?? new ImmutableSolidColorBrush(Color.FromRgb(230, 230, 230));
        }

        if (_background is not null)
        {
            return AdjustForPressed(_background);
        }

        var brush = variant.Background.Get(VisualState) ?? variant.Background.Normal;
        if (brush is not null)
        {
            return brush;
        }

        var baseColor = _isPrimary ? Color.FromRgb(49, 130, 206) : Color.FromRgb(242, 242, 242);
        return new ImmutableSolidColorBrush(baseColor);
    }

    private ImmutableSolidColorBrush? ResolveBorder(ButtonVariantPalette variant)
    {
        if (!IsEnabled)
        {
            return variant.Border.Get(WidgetVisualState.Disabled) ?? variant.Border.Normal;
        }

        if (_borderBrush is not null)
        {
            return AdjustForPressed(_borderBrush);
        }

        var brush = variant.Border.Get(VisualState) ?? variant.Border.Normal;
        if (brush is not null)
        {
            return brush;
        }

        return _isPrimary
            ? new ImmutableSolidColorBrush(Color.FromRgb(36, 98, 156))
            : new ImmutableSolidColorBrush(Color.FromRgb(205, 205, 205));
    }

    private static Rect Deflate(Rect rect, Thickness thickness)
    {
        if (thickness == default)
        {
            return rect;
        }

        var left = Math.Max(0, thickness.Left);
        var top = Math.Max(0, thickness.Top);
        var right = Math.Max(0, thickness.Right);
        var bottom = Math.Max(0, thickness.Bottom);

        var width = Math.Max(0, rect.Width - left - right);
        var height = Math.Max(0, rect.Height - top - bottom);

        return new Rect(rect.X + left, rect.Y + top, width, height);
    }

    private static double GetUniformCornerRadius(CornerRadius cornerRadius)
    {
        if (cornerRadius == default)
        {
            return 0;
        }

        if (Math.Abs(cornerRadius.TopLeft - cornerRadius.TopRight) < double.Epsilon
            && Math.Abs(cornerRadius.TopLeft - cornerRadius.BottomLeft) < double.Epsilon
            && Math.Abs(cornerRadius.TopLeft - cornerRadius.BottomRight) < double.Epsilon)
        {
            return cornerRadius.TopLeft;
        }

        return (cornerRadius.TopLeft + cornerRadius.TopRight + cornerRadius.BottomLeft + cornerRadius.BottomRight) / 4;
    }

    private ImmutableSolidColorBrush AdjustForPressed(ImmutableSolidColorBrush brush)
    {
        if (IsPressedVisual)
        {
            var color = brush.Color;
            byte Reduce(byte channel) => (byte)Math.Max(0, channel - 20);
            return new ImmutableSolidColorBrush(Color.FromRgb(Reduce(color.R), Reduce(color.G), Reduce(color.B)));
        }

        return brush;
    }

    private static bool BrushEquals(ImmutableSolidColorBrush? cached, ImmutableSolidColorBrush current)
    {
        return cached is not null && cached.Color.Equals(current.Color);
    }

    public void SetText(string text)
    {
        _text = text;
        InvalidateFormattedText();
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Pressed:
                _isPointerPressed = true;
                break;
            case WidgetPointerEventKind.Released:
                if (_isPointerPressed && IsWithinBounds(e.Position))
                {
                    Clicked?.Invoke(this);
                }
                _isPointerPressed = false;
                break;
            case WidgetPointerEventKind.Cancelled:
                _isPointerPressed = false;
                break;
        }

        return handled || IsInteractive;
    }

    private bool IsPressedVisual => _isPointerPressed || _isPressedSource;

    private bool IsWithinBounds(Point position)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(position);
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
