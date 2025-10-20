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

    public override void Draw(DrawingContext context)
    {
        if (Text is null)
        {
            return;
        }

        if (_formattedText is null || !string.Equals(_cachedText, Text, StringComparison.Ordinal) || Math.Abs(_cachedEmSize - EmSize) > double.Epsilon)
        {
            _formattedText = new FormattedText(
                Text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                EmSize,
                Foreground);

            _cachedText = Text;
            _cachedEmSize = EmSize;

            _rotateMatrix = Matrix.CreateTranslation(-X, -Y)
                      * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
                      * Matrix.CreateTranslation(X, Y);
        }

        using var rotate = context.PushTransform(_rotateMatrix);
        context.DrawText(_formattedText, new Point(X, Y));
    }

    public override void Invalidate()
    {
        _rotateMatrix = Matrix.CreateTranslation(-X, -Y)
                  * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
                  * Matrix.CreateTranslation(X, Y);

        _formattedText = null;
        _cachedText = null;
        _cachedEmSize = 0;
    }
}
