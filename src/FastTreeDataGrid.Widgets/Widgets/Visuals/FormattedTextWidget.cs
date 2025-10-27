using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

public class FormattedTextWidget : TextWidget
{
    private FormattedText? _formattedText;
    private string? _cachedText;
    private double _cachedEmSize;
    private double _cachedMaxWidth;

    public override void Draw(DrawingContext context)
    {
        var formatted = EnsureFormattedText();
        if (formatted is null)
        {
            return;
        }

        using var clip = PushClip(context);
        using var transform = PushRenderTransform(context);
        var origin = GetTextOrigin(formatted);
        DrawFormattedText(context, formatted, origin);
    }

    public override void Invalidate()
    {
        _formattedText = null;
        _cachedText = null;
        _cachedEmSize = 0;
        _cachedMaxWidth = double.NaN;
    }

    protected FormattedText? EnsureFormattedText()
    {
        if (Text is null)
        {
            _formattedText = null;
            _cachedText = null;
            return null;
        }

        var emSize = GetEffectiveEmSize();
        var requiresRebuild = _formattedText is null
            || !string.Equals(_cachedText, Text, StringComparison.Ordinal)
            || Math.Abs(_cachedEmSize - emSize) > double.Epsilon;

        if (requiresRebuild)
        {
            _formattedText = CreateFormattedText(emSize);
            _cachedText = Text;
            _cachedEmSize = emSize;
        }
        else
        {
            UpdateFormattedMetrics(_formattedText!);
        }

        return _formattedText;
    }

    protected FormattedText CreateFormattedText(double emSize)
    {
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        var formatted = new FormattedText(
            Text ?? string.Empty,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            emSize,
            Foreground);

        UpdateFormattedMetrics(formatted);
        _cachedMaxWidth = formatted.MaxTextWidth;
        return formatted;
    }

    protected void UpdateFormattedMetrics(FormattedText formatted)
    {
        var availableWidth = Bounds.Width;
        var maxWidth = double.IsFinite(availableWidth) && availableWidth > 0
            ? availableWidth
            : double.PositiveInfinity;

        if (!double.IsNaN(_cachedMaxWidth) && Math.Abs(_cachedMaxWidth - maxWidth) <= double.Epsilon && formatted.Trimming == Trimming && formatted.TextAlignment == TextAlignment)
        {
            return;
        }

        formatted.MaxTextWidth = maxWidth;
        formatted.MaxTextHeight = double.IsFinite(Bounds.Height) && Bounds.Height > 0
            ? Bounds.Height
            : double.PositiveInfinity;
        formatted.Trimming = Trimming;
        formatted.TextAlignment = TextAlignment;

        _cachedMaxWidth = maxWidth;
    }

    protected virtual Point GetTextOrigin(FormattedText formatted)
    {
        var textHeight = formatted.Height;
        var originY = Bounds.Y + Math.Max(0, (Bounds.Height - textHeight) / 2);
        return new Point(Bounds.X, originY);
    }

    protected virtual void DrawFormattedText(DrawingContext context, FormattedText formatted, Point origin)
    {
        context.DrawText(formatted, origin);
    }
}
