using System;
using Avalonia;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

public class GlyphRunWidget : TextWidget
{
    private GlyphRun? _glyphRun;
    private Matrix _rotateMatrix;
    private Matrix _translateMatrix;
    private string? _cachedText;
    private double _cachedEmSize;

    private GlyphRun? CreateGlyphRun(string text, Typeface typeface, double fontSize)
    {
        var glyphTypeface = typeface.GlyphTypeface ?? Typeface.Default.GlyphTypeface;
        if (glyphTypeface is null)
        {
            return null;
        }

        var glyphIndices = new ushort[text.Length];

        for (var i = 0; i < text.Length; i++)
        {
            glyphIndices[i] = glyphTypeface.GetGlyph(text[i]);
        }

        return new GlyphRun(glyphTypeface, fontSize, text.ToCharArray(), glyphIndices);
    }

    public override void Draw(DrawingContext context)
    {
        if (Text is null || Foreground is null)
        {
            return;
        }

        if (_glyphRun is null || !string.Equals(_cachedText, Text, StringComparison.Ordinal) || Math.Abs(_cachedEmSize - EmSize) > double.Epsilon)
        {
            _glyphRun = CreateGlyphRun(Text, Typeface.Default, EmSize);
            if (_glyphRun is null)
            {
                return;
            }
            _translateMatrix = Matrix.CreateTranslation(X, Y);
            _rotateMatrix = Matrix.CreateTranslation(-X, -Y)
                                  * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
                                  * Matrix.CreateTranslation(X, Y);
            _cachedText = Text;
            _cachedEmSize = EmSize;
        }

        using var translate = context.PushTransform(_translateMatrix);
        using var rotate = context.PushTransform(_rotateMatrix);
        context.DrawGlyphRun(Foreground, _glyphRun);
    }

    public override void Invalidate()
    {
        _translateMatrix = Matrix.CreateTranslation(X, Y);
        _rotateMatrix = Matrix.CreateTranslation(-X, -Y)
                              * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
                              * Matrix.CreateTranslation(X, Y);

        _glyphRun = null;
        _cachedText = null;
    }
}
