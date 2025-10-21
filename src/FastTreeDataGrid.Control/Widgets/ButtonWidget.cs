using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using ButtonPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.ButtonPalette;
using ButtonVariantPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.ButtonVariantPalette;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ButtonWidget : TemplatedWidget
{
    private const double MinimumFontSize = 12;

    private static readonly Thickness DefaultPadding = new(8, 5, 8, 6);

    private string _text = string.Empty;
    private bool _isPrimary;
    private bool _isPressedSource;
    private bool _isPointerPressed;
    private ImmutableSolidColorBrush? _background;
    private ImmutableSolidColorBrush? _borderBrush;
    private double? _fontSize;

    private BorderWidget? _borderPart;
    private AccessTextWidget? _contentPart;

    static ButtonWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(ButtonWidget),
                    state,
                    widget =>
                    {
                        if (widget is ButtonWidget button)
                        {
                            button.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public ButtonWidget()
    {
        IsInteractive = true;
    }

    public event Action<ButtonWidget>? Clicked;

    public ImmutableSolidColorBrush? Background { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double? FontSize { get; set; }

    protected override Widget? CreateDefaultTemplate()
    {
        var content = new AccessTextWidget
        {
            TextAlignment = TextAlignment.Center,
            Trimming = TextTrimming.CharacterEllipsis,
            ClipToBounds = false
        };

        var border = new BorderWidget
        {
            Padding = DefaultPadding,
            BorderThickness = new Thickness(1),
            Child = content
        };

        return border;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        _borderPart = templateRoot as BorderWidget;
        _contentPart = _borderPart?.Child as AccessTextWidget;

        if (_contentPart is not null)
        {
            _contentPart.TextAlignment = TextAlignment.Center;
            _contentPart.Trimming = TextTrimming.CharacterEllipsis;
        }

        RefreshTemplateAppearance();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

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

        var cornerRadius = CornerRadius == default ? palette.CornerRadius : CornerRadius;
        IsEnabled = enabled;
        ApplyTemplateValues(palette, cornerRadius);
        SetText(_text);

        if (!_fontSize.HasValue)
        {
            UpdateDynamicFontSize();
        }
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        UpdateDynamicFontSize();
    }

    public void SetText(string? text)
    {
        _text = text ?? string.Empty;

        if (_contentPart is null && !IsTemplateApplied)
        {
            ApplyTemplate();
        }

        _contentPart?.SetAccessText(_text);
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
            case WidgetPointerEventKind.CaptureLost:
                _isPointerPressed = false;
                break;
        }

        RefreshTemplateAppearance();
        return handled || IsInteractive;
    }

    private void ApplyTemplateValues(ButtonPalette palette, CornerRadius cornerRadius)
    {
        var variant = _isPrimary ? palette.Accent : palette.Standard;
        var border = _borderPart;
        if (border is not null)
        {
            border.Padding = palette.Padding;
            border.BorderThickness = new Thickness(palette.BorderThickness);
            border.CornerRadius = cornerRadius;
            border.Background = ResolveBackground(variant);
            border.BorderBrush = ResolveBorder(variant);
            border.IsEnabled = IsEnabled;
        }

        var content = _contentPart;
        if (content is not null)
        {
            content.Foreground = ResolveForeground(variant);
            if (_fontSize.HasValue)
            {
                var fontSize = Math.Max(1, _fontSize.Value);
                if (Math.Abs(content.EmSize - fontSize) > double.Epsilon)
                {
                    content.EmSize = fontSize;
                    content.Invalidate();
                }
            }
        }
    }

    private void RefreshTemplateAppearance()
    {
        if (!IsTemplateApplied)
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.Button;
        var cornerRadius = CornerRadius == default ? palette.CornerRadius : CornerRadius;
        ApplyTemplateValues(palette, cornerRadius);
        _contentPart?.SetAccessText(_text);

        if (!_fontSize.HasValue)
        {
            UpdateDynamicFontSize();
        }
    }

    private void UpdateDynamicFontSize()
    {
        var content = _contentPart;
        if (content is null || _fontSize.HasValue)
        {
            return;
        }

        var height = Bounds.Height;
        if (!double.IsFinite(height) || height <= 0)
        {
            return;
        }

        var fontSize = Math.Max(MinimumFontSize, height * 0.45);
        if (Math.Abs(content.EmSize - fontSize) > double.Epsilon)
        {
            content.EmSize = fontSize;
            content.Invalidate();
        }
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

    private ImmutableSolidColorBrush ResolveBorder(ButtonVariantPalette variant)
    {
        if (!IsEnabled)
        {
            return variant.Border.Get(WidgetVisualState.Disabled) ?? variant.Border.Normal
                   ?? new ImmutableSolidColorBrush(Color.FromRgb(210, 210, 210));
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

    private ImmutableSolidColorBrush AdjustForPressed(ImmutableSolidColorBrush brush)
    {
        if (IsPressedVisual)
        {
            var color = brush.Color;
            if (color.A == 0)
            {
                return brush;
            }

            byte Reduce(byte channel) => (byte)Math.Max(0, channel - 20);
            return new ImmutableSolidColorBrush(Color.FromArgb(color.A, Reduce(color.R), Reduce(color.G), Reduce(color.B)));
        }

        return brush;
    }

    private bool IsPressedVisual => VisualState == WidgetVisualState.Pressed || _isPressedSource;

    private bool IsWithinBounds(Point position)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(position);
    }
}
