using System;
using Avalonia;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

public class GlyphRunWidget : TextWidget
{
    private GlyphRun? _glyphRun;
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

        using var clip = PushClip(context);

        if (_glyphRun is null || !string.Equals(_cachedText, Text, StringComparison.Ordinal) || Math.Abs(_cachedEmSize - EmSize) > double.Epsilon)
        {
            _glyphRun = CreateGlyphRun(Text, Typeface.Default, EmSize);
            if (_glyphRun is null)
            {
                return;
            }
            _cachedText = Text;
            _cachedEmSize = EmSize;
        }

        var originY = Bounds.Y + Math.Max(0, (Bounds.Height - EmSize) / 2);
        var translate = Matrix.CreateTranslation(Bounds.X, originY);
        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        var rotate = Matrix.CreateTranslation(-centerX, -centerY)
                     * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
                     * Matrix.CreateTranslation(centerX, centerY);

        using var rotation = context.PushTransform(rotate);
        using var translation = context.PushTransform(translate);
        context.DrawGlyphRun(Foreground, _glyphRun);
    }

    public override void Invalidate()
    {
        _glyphRun = null;
        _cachedText = null;
    }
}
