using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

public class FormattedTextWidget : TextWidget
{
    private FormattedText? _formattedText;
    private Matrix _rotateMatrix;
    private string? _cachedText;
    private double _cachedEmSize;
    private double _cachedMaxWidth;

    public override void Draw(DrawingContext context)
    {
        if (Text is null)
        {
            return;
        }

        using var clip = PushClip(context);

        if (_formattedText is null || !string.Equals(_cachedText, Text, StringComparison.Ordinal) || Math.Abs(_cachedEmSize - EmSize) > double.Epsilon)
        {
            _formattedText = CreateFormattedText();
        }
        else
        {
            UpdateFormattedMetrics(_formattedText);
        }

        _cachedText = Text;
        _cachedEmSize = EmSize;

        UpdateTransform();

        using var rotate = context.PushTransform(_rotateMatrix);
        var textHeight = _formattedText.Height;
        var originY = Bounds.Y + Math.Max(0, (Bounds.Height - textHeight) / 2);
        context.DrawText(_formattedText, new Point(Bounds.X, originY));
    }

    public override void Invalidate()
    {
        _formattedText = null;
        _cachedText = null;
        _cachedEmSize = 0;
        _cachedMaxWidth = double.NaN;
        UpdateTransform();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        _rotateMatrix = Matrix.CreateTranslation(-centerX, -centerY)
            * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
            * Matrix.CreateTranslation(centerX, centerY);
    }

    private FormattedText CreateFormattedText()
    {
        var formatted = new FormattedText(
            Text ?? string.Empty,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            EmSize,
            Foreground);

        UpdateFormattedMetrics(formatted);
        return formatted;
    }

    private void UpdateFormattedMetrics(FormattedText formatted)
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
}
