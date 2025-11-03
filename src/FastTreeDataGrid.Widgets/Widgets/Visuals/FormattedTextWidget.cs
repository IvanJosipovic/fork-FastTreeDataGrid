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

        if (TextDecorations is { } decorations)
        {
            formatted.SetTextDecorations(decorations);
        }

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

    protected override Size MeasureCore(Size available)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return default;
        }

        return MeasureFormattedText(available.Width, available.Height);
    }

    public override double GetAutoWidth(double availableHeight)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return 0;
        }

        var metrics = MeasureFormattedText(double.PositiveInfinity, availableHeight);
        return metrics.Width;
    }

    public override double GetAutoHeight(double availableWidth)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return 0;
        }

        var metrics = MeasureFormattedText(availableWidth, double.PositiveInfinity);
        return metrics.Height;
    }

    private Size MeasureFormattedText(double availableWidth, double availableHeight)
    {
        var emSize = GetEffectiveEmSize();
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        var formatted = new FormattedText(
            Text ?? string.Empty,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            emSize,
            Foreground);

        if (TextDecorations is { } decorations)
        {
            formatted.SetTextDecorations(decorations);
        }

        formatted.MaxTextWidth = double.IsFinite(availableWidth) && availableWidth > 0
            ? availableWidth
            : double.PositiveInfinity;
        formatted.MaxTextHeight = double.IsFinite(availableHeight) && availableHeight > 0
            ? availableHeight
            : double.PositiveInfinity;
        formatted.Trimming = Trimming;
        formatted.TextAlignment = TextAlignment;

        return new Size(
            Math.Min(formatted.WidthIncludingTrailingWhitespace, formatted.MaxTextWidth),
            Math.Min(formatted.Height, formatted.MaxTextHeight));
    }

    protected virtual Point GetTextOrigin(FormattedText formatted)
    {
        var originY = Bounds.Y;
        return new Point(Bounds.X, originY);
    }

    protected virtual void DrawFormattedText(DrawingContext context, FormattedText formatted, Point origin)
    {
        context.DrawText(formatted, origin);
    }
}
