using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ButtonWidget : Widget
{
    private string _text = string.Empty;
    private bool _isPrimary;
    private bool _isPressed;
    private bool _isEnabled = true;
    private ImmutableSolidColorBrush? _background;
    private ImmutableSolidColorBrush? _borderBrush;
    private double _cornerRadius = 4;
    private double? _fontSize;

    private FormattedText? _formattedText;
    private string? _cachedText;
    private double _cachedFontSize;

    public double CornerRadius { get; set; } = 4;

    public ImmutableSolidColorBrush? Background { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double? FontSize { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _text = string.Empty;
        _isPrimary = false;
        _isPressed = false;
        _isEnabled = true;
        _background = Background;
        _borderBrush = BorderBrush;
        _cornerRadius = CornerRadius;
        _fontSize = FontSize;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case ButtonWidgetValue buttonValue:
                    _text = buttonValue.Text;
                    _isPrimary = buttonValue.IsPrimary;
                    _isPressed = buttonValue.IsPressed;
                    _isEnabled = buttonValue.IsEnabled;
                    _background = buttonValue.Background ?? Background;
                    _borderBrush = buttonValue.BorderBrush ?? BorderBrush;
                    _cornerRadius = buttonValue.CornerRadius;
                    _fontSize = buttonValue.FontSize ?? FontSize;
                    break;
                case string text:
                    _text = text;
                    break;
            }
        }

        InvalidateFormattedText();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        InvalidateFormattedText();
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        var background = ResolveBackground();
        var borderBrush = _borderBrush ?? ResolveBorder();
        var corner = _cornerRadius;

        using var rotation = context.PushTransform(CreateRotationMatrix());
        context.DrawRectangle(background, borderBrush is null ? null : new Pen(borderBrush, 1), Bounds, corner, corner);

        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        var formatted = GetOrCreateText();
        if (formatted is null)
        {
            return;
        }

        var originX = Bounds.X + Math.Max(0, (Bounds.Width - formatted.Width) / 2);
        var originY = Bounds.Y + Math.Max(0, (Bounds.Height - formatted.Height) / 2);

        context.DrawText(formatted, new Point(originX, originY));
    }

    private FormattedText? GetOrCreateText()
    {
        var fontSize = _fontSize ?? Math.Max(12, Bounds.Height * 0.5);

        if (_formattedText is not null && string.Equals(_cachedText, _text, StringComparison.Ordinal) && Math.Abs(_cachedFontSize - fontSize) <= double.Epsilon)
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
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            ResolveForeground());

        _cachedText = _text;
        _cachedFontSize = fontSize;
        return _formattedText;
    }

    private void InvalidateFormattedText()
    {
        _formattedText = null;
        _cachedText = null;
        _cachedFontSize = 0;
    }

    private ImmutableSolidColorBrush ResolveForeground()
    {
        if (!_isEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(180, 180, 180));
        }

        return Foreground ?? new ImmutableSolidColorBrush(_isPrimary ? Color.FromRgb(255, 255, 255) : Color.FromRgb(40, 40, 40));
    }

    private ImmutableSolidColorBrush? ResolveBackground()
    {
        if (!_isEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(230, 230, 230));
        }

        if (_background is not null)
        {
            return AdjustForPressed(_background);
        }

        var baseColor = _isPrimary ? Color.FromRgb(49, 130, 206) : Color.FromRgb(242, 242, 242);
        return AdjustForPressed(new ImmutableSolidColorBrush(baseColor));
    }

    private ImmutableSolidColorBrush? ResolveBorder()
    {
        if (!_isEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(200, 200, 200));
        }

        if (_borderBrush is not null)
        {
            return AdjustForPressed(_borderBrush);
        }

        var baseColor = _isPrimary ? Color.FromRgb(36, 98, 156) : Color.FromRgb(205, 205, 205);
        return AdjustForPressed(new ImmutableSolidColorBrush(baseColor));
    }

    private ImmutableSolidColorBrush AdjustForPressed(ImmutableSolidColorBrush brush)
    {
        if (!_isPressed)
        {
            return brush;
        }

        var color = brush.Color;
        byte Reduce(byte channel) => (byte)Math.Max(0, channel - 20);
        return new ImmutableSolidColorBrush(Color.FromRgb(Reduce(color.R), Reduce(color.G), Reduce(color.B)));
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
