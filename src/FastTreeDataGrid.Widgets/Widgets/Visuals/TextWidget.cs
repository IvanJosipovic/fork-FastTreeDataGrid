using System;
using System.Globalization;
using Avalonia.Media;
using FastTreeDataGrid.Control.Theming;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class TextWidget : Widget
{
    private const double DefaultEmSize = 12;

    static TextWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(TextWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not TextWidget text)
                {
                    return;
                }

                var palette = theme.Palette.Text;
                var body = palette.Typography.Body;

                if (text.Foreground is null)
                {
                    text.Foreground = palette.Foreground.Get(WidgetVisualState.Normal);
                }

                if (Math.Abs(text.EmSize - DefaultEmSize) <= double.Epsilon && !double.IsNaN(body.FontSize) && body.FontSize > 0)
                {
                    text.EmSize = body.FontSize;
                }

                if (Equals(text.FontFamily, FontFamily.Default) && body.FontFamily is not null)
                {
                    text.FontFamily = body.FontFamily;
                }

                if (text.FontWeight == FontWeight.Normal && body.FontWeight != FontWeight.Normal)
                {
                    text.FontWeight = body.FontWeight;
                }
            }));
    }

    public string? Text { get; protected set; }

    public double EmSize { get; set; } = DefaultEmSize;

    private FontFamily _fontFamily = FontFamily.Default;
    private FontWeight _fontWeight = FontWeight.Normal;
    private FontStyle _fontStyle = FontStyle.Normal;
    private FontStretch _fontStretch = FontStretch.Normal;

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set
        {
            var resolved = value ?? FontFamily.Default;
            if (_fontFamily == resolved)
            {
                return;
            }

            _fontFamily = resolved;
            Invalidate();
        }
    }

    public FontWeight FontWeight
    {
        get => _fontWeight;
        set
        {
            if (_fontWeight == value)
            {
                return;
            }

            _fontWeight = value;
            Invalidate();
        }
    }

    public FontStyle FontStyle
    {
        get => _fontStyle;
        set
        {
            if (_fontStyle == value)
            {
                return;
            }

            _fontStyle = value;
            Invalidate();
        }
    }

    public FontStretch FontStretch
    {
        get => _fontStretch;
        set
        {
            if (_fontStretch == value)
            {
                return;
            }

            _fontStretch = value;
            Invalidate();
        }
    }

    protected double GetEffectiveEmSize(double fallback = DefaultEmSize)
    {
        var size = EmSize;
        if (!double.IsFinite(size) || size <= 0)
        {
            return fallback > 0 ? fallback : DefaultEmSize;
        }

        return size;
    }

    public void SetText(string? text)
    {
        Text = text ?? string.Empty;
        Invalidate();
    }

    public TextTrimming Trimming { get; set; } = TextTrimming.None;

    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (provider is null || Key is null)
        {
            Text = item?.ToString() ?? string.Empty;
            return;
        }

        var value = provider.GetValue(item, Key);

        Text = value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    public abstract void Invalidate();
}
