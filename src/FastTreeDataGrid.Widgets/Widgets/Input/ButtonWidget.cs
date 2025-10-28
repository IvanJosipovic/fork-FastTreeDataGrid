using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using ButtonPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.ButtonPalette;
using ButtonVariantPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.ButtonVariantPalette;

namespace FastTreeDataGrid.Control.Widgets;

public class ButtonWidget : ButtonWidgetBase
{
    private const double MinimumFontSize = 12;

    private static readonly Thickness DefaultPadding = new(8, 5, 8, 6);

    private string _text = string.Empty;
    private ButtonWidgetVariant _variant = ButtonWidgetVariant.Standard;
    private ButtonWidgetVariant _appliedVariant = ButtonWidgetVariant.Standard;
    private ButtonWidgetVariant? _dataVariant;
    private bool _isPressedSource;
    private ImmutableSolidColorBrush? _background;
    private ImmutableSolidColorBrush? _borderBrush;
    private double? _fontSize;
    private BorderWidget? _borderPart;
    private AccessTextWidget? _contentPart;
    private WidgetTypography? _typography;

    static ButtonWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(ButtonWidget),
                    state,
                    (widget, _) =>
                    {
                        if (widget is ButtonWidget button)
                        {
                            button.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public ImmutableSolidColorBrush? Background { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double? FontSize { get; set; }

    public ButtonWidgetVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            if (_dataVariant is null)
            {
                SetAppliedVariant(_variant);
            }
        }
    }

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
        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case ButtonWidgetValue buttonValue:
                    ApplyButtonValue(buttonValue);
                    return;
                case string textValue:
                    ApplyTextValue(textValue);
                    return;
            }
        }

        switch (item)
        {
            case ButtonWidgetValue directValue:
                ApplyButtonValue(directValue);
                return;
            case string textItem:
                ApplyTextValue(textItem);
                return;
        }

        base.UpdateValue(provider, item);
    }

    internal void ApplyButtonValue(ButtonWidgetValue value)
    {
        var palette = WidgetFluentPalette.Current.Button;

        _text = value.Text ?? string.Empty;
        _dataVariant = value.Variant ?? (value.IsPrimary ? ButtonWidgetVariant.Accent : null);
        _isPressedSource = value.IsPressed;
        _appliedVariant = _dataVariant ?? _variant;
        SetAutomationSettings(value.Automation);
        _background = Background;
        _borderBrush = BorderBrush;
        _fontSize = FontSize;
        CornerRadius = default;
        _typography = value.Typography;

        var enabled = value.IsEnabled;
        var command = value.Command ?? Command;
        var commandParameter = value.CommandParameter ?? CommandParameter;

        if (value.Background is not null)
        {
            _background = value.Background;
        }

        if (value.BorderBrush is not null)
        {
            _borderBrush = value.BorderBrush;
        }

        if (value.FontSize.HasValue)
        {
            _fontSize = value.FontSize.Value;
        }

        if (value.CornerRadius.HasValue)
        {
            CornerRadius = new CornerRadius(value.CornerRadius.Value);
        }

        if (value.CommandSettings is { } commandSettings)
        {
            enabled = commandSettings.IsEnabled;
            command = commandSettings.Command ?? command;
            commandParameter = commandSettings.CommandParameter ?? commandParameter;
        }

        if (_typography is not null && _typography.FontSize.HasValue)
        {
            _fontSize = _typography.FontSize.Value;
        }

        var cornerRadius = CornerRadius == default ? palette.CornerRadius : CornerRadius;
        Command = command;
        CommandParameter = commandParameter;
        SetIsEnabledFromData(enabled);
        ApplyTemplateValues(palette, cornerRadius);
        SetText(_text);

        if (!_fontSize.HasValue)
        {
            UpdateDynamicFontSize();
        }
    }

    private void ApplyTextValue(string text)
    {
        _text = text ?? string.Empty;
        SetAutomationSettings(null);
        _dataVariant = null;
        SetAppliedVariant(_variant);
        SetText(_text);
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
        UpdateAutomationFromText(_text ?? string.Empty, _contentPart);
    }

    public void SetShowAccessKey(bool show)
    {
        if (_contentPart is null && !IsTemplateApplied)
        {
            ApplyTemplate();
        }

        if (_contentPart is not null)
        {
            _contentPart.ShowAccessKey = show;
        }
    }

    protected override void OnPointerPressed(in WidgetPointerEvent e)
    {
        base.OnPointerPressed(e);
        RefreshTemplateAppearance();
    }

    protected override void OnPointerReleased(bool executedClick, in WidgetPointerEvent e)
    {
        base.OnPointerReleased(executedClick, e);
        RefreshTemplateAppearance();
    }

    protected override void OnPointerCancelled()
    {
        base.OnPointerCancelled();
        RefreshTemplateAppearance();
    }

    private void SetAppliedVariant(ButtonWidgetVariant variant)
    {
        if (_appliedVariant == variant)
        {
            return;
        }

        _appliedVariant = variant;

        if (IsTemplateApplied)
        {
            RefreshTemplateAppearance();
        }
    }

    private void ApplyTemplateValues(ButtonPalette palette, CornerRadius cornerRadius)
    {
        var variant = palette.GetVariant(_appliedVariant);
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
            ApplyTypography(content);
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
        UpdateAutomationFromText(_text ?? string.Empty, _contentPart);

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

        var brush = variant.Foreground.Get(VisualState);
        if (brush is not null)
        {
            return brush;
        }

        var fallback = ButtonVariantPalette.CreateFallback(_appliedVariant);
        return fallback.Foreground.Get(VisualState) ?? fallback.Foreground.Normal!;
    }

    private ImmutableSolidColorBrush ResolveBackground(ButtonVariantPalette variant)
    {
        ButtonVariantPalette? fallbackPalette = null;

        if (!IsEnabled)
        {
            var disabled = variant.Background.Get(WidgetVisualState.Disabled);
            if (disabled is not null)
            {
                return disabled;
            }

            fallbackPalette ??= ButtonVariantPalette.CreateFallback(_appliedVariant);
            var fallbackBackground = fallbackPalette.Background;
            return fallbackBackground.Get(WidgetVisualState.Disabled) ?? fallbackBackground.Normal!;
        }

        if (_background is not null)
        {
            return AdjustForPressed(_background);
        }

        var brush = variant.Background.Get(VisualState);
        if (brush is not null)
        {
            return brush;
        }

        fallbackPalette ??= ButtonVariantPalette.CreateFallback(_appliedVariant);
        var fallbackActive = fallbackPalette.Background;
        return fallbackActive.Get(VisualState) ?? fallbackActive.Normal!;
    }

    private ImmutableSolidColorBrush ResolveBorder(ButtonVariantPalette variant)
    {
        ButtonVariantPalette? fallbackPalette = null;

        if (!IsEnabled)
        {
            var disabled = variant.Border.Get(WidgetVisualState.Disabled);
            if (disabled is not null)
            {
                return disabled;
            }

            fallbackPalette ??= ButtonVariantPalette.CreateFallback(_appliedVariant);
            var fallbackBorder = fallbackPalette.Border;
            return fallbackBorder.Get(WidgetVisualState.Disabled) ?? fallbackBorder.Normal!;
        }

        if (_borderBrush is not null)
        {
            return AdjustForPressed(_borderBrush);
        }

        var brush = variant.Border.Get(VisualState);
        if (brush is not null)
        {
            return brush;
        }

        fallbackPalette ??= ButtonVariantPalette.CreateFallback(_appliedVariant);
        var fallbackActive = fallbackPalette.Border;
        return fallbackActive.Get(VisualState) ?? fallbackActive.Normal!;
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

    private void ApplyTypography(AccessTextWidget content)
    {
        if (_typography is null)
        {
            return;
        }

        if (_typography.FontFamily is { } family && content.FontFamily != family)
        {
            content.FontFamily = family;
        }

        if (_typography.FontWeight.HasValue && content.FontWeight != _typography.FontWeight.Value)
        {
            content.FontWeight = _typography.FontWeight.Value;
        }

        if (_typography.FontStyle.HasValue && content.FontStyle != _typography.FontStyle.Value)
        {
            content.FontStyle = _typography.FontStyle.Value;
        }

        if (_typography.FontStretch.HasValue && content.FontStretch != _typography.FontStretch.Value)
        {
            content.FontStretch = _typography.FontStretch.Value;
        }
    }

}
