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

        var emSize = GetEffectiveEmSize();
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        if (_glyphRun is null || !string.Equals(_cachedText, Text, StringComparison.Ordinal) || Math.Abs(_cachedEmSize - emSize) > double.Epsilon)
        {
            _glyphRun = CreateGlyphRun(Text, typeface, emSize) ??
                        CreateGlyphRun(Text, Typeface.Default, emSize);
            if (_glyphRun is null)
            {
                return;
            }
            _cachedText = Text;
            _cachedEmSize = emSize;
        }

        var originY = Bounds.Y + Math.Max(0, (Bounds.Height - emSize) / 2);
        var translate = Matrix.CreateTranslation(Bounds.X, originY);

        using var rotation = PushRenderTransform(context);
        using var translation = context.PushTransform(translate);
        context.DrawGlyphRun(Foreground, _glyphRun);
    }

    public override void Invalidate()
    {
        _glyphRun = null;
        _cachedText = null;
    }
}
