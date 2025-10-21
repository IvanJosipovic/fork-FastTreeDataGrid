using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using CheckBoxPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.CheckBoxPalette;
using CheckBoxValuePalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.CheckBoxValuePalette;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CheckBoxWidget : Widget
{
    private bool? _value;
    private bool? _sourceValue;

    public CheckBoxWidget()
    {
        IsInteractive = true;
    }

    public event Action<bool?>? Toggled;

    public double StrokeThickness { get; set; } = double.NaN;

    public double Padding { get; set; } = double.NaN;

    public bool? Value => _value;

    public void SetValue(bool? value)
    {
        _value = value;
        _sourceValue = value;
        RefreshStyle();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _value = null;
        _sourceValue = null;
        var enabled = true;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case CheckBoxWidgetValue checkBoxValue:
                _value = checkBoxValue.IsChecked;
                _sourceValue = _value;
                enabled = checkBoxValue.IsEnabled;
                break;
            case bool boolean:
                _value = boolean;
                _sourceValue = _value;
                break;
            case null:
                _value = null;
                _sourceValue = null;
                break;
        }

        IsEnabled = enabled;
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        using var rotation = context.PushTransform(CreateRotationMatrix());
        var palette = WidgetFluentPalette.Current.CheckBox;
        var valuePalette = GetValuePalette(palette);

        var outerRect = Bounds;
        var outerBackground = GetBrushForState(valuePalette.Background, VisualState)
                              ?? new ImmutableSolidColorBrush(Colors.Transparent);
        var outerBorderBrush = GetBrushForState(valuePalette.Border, VisualState);
        var outerPen = outerBorderBrush is null ? null : new Pen(outerBorderBrush, palette.StrokeThickness);
        var outerCorner = palette.CornerRadius.TopLeft;
        context.DrawRectangle(outerBackground, outerPen, outerRect, outerCorner, outerCorner);

        var effectivePadding = palette.Padding;
        if (!double.IsNaN(Padding) && Padding > 0)
        {
            effectivePadding = new Thickness(
                effectivePadding.Left + Padding,
                effectivePadding.Top + Padding,
                effectivePadding.Right + Padding,
                effectivePadding.Bottom + Padding);
        }

        var contentRect = Deflate(outerRect, effectivePadding);
        var boxSize = Math.Min(Math.Min(contentRect.Width, contentRect.Height), palette.BoxSize);
        if (boxSize <= 0)
        {
            return;
        }

        var boxRect = new Rect(
            contentRect.X + (contentRect.Width - boxSize) / 2,
            contentRect.Y + (contentRect.Height - boxSize) / 2,
            boxSize,
            boxSize);

        var strokeThickness = double.IsNaN(StrokeThickness) ? palette.StrokeThickness : StrokeThickness;
        var boxFill = GetBrushForState(valuePalette.BoxFill, VisualState) ?? new ImmutableSolidColorBrush(Colors.Transparent);
        var boxStroke = GetBrushForState(valuePalette.BoxStroke, VisualState);
        var boxPen = boxStroke is null ? null : new Pen(boxStroke, strokeThickness);

        context.DrawRectangle(boxFill, boxPen, boxRect, outerCorner, outerCorner);

        DrawGlyph(context, valuePalette, boxRect, palette.BoxSize);
    }

    private CheckBoxValuePalette GetValuePalette(CheckBoxPalette palette)
    {
        return _value switch
        {
            true => palette.Checked,
            null => palette.Indeterminate,
            _ => palette.Unchecked,
        };
    }

    private static ImmutableSolidColorBrush? GetBrushForState(WidgetFluentPalette.BrushState state, WidgetVisualState visualState)
    {
        return state.Get(visualState) ?? state.Normal;
    }

    private void DrawGlyph(DrawingContext context, CheckBoxValuePalette palette, Rect boxRect, double baseBoxSize)
    {
        if (palette.GlyphGeometry is null)
        {
            return;
        }

        var glyphBrush = Foreground ?? GetBrushForState(palette.Glyph, VisualState);
        if (glyphBrush is null)
        {
            glyphBrush = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40));
        }

        var geometry = palette.GlyphGeometry;
        var bounds = geometry.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = baseBoxSize <= 0 ? 1 : boxRect.Width / baseBoxSize;
        var desiredWidth = palette.GlyphWidth > 0 ? palette.GlyphWidth * scale : boxRect.Width * 0.6;
        var glyphScale = desiredWidth / bounds.Width;
        var desiredHeight = bounds.Height * glyphScale;

        var offsetX = boxRect.X + (boxRect.Width - desiredWidth) / 2;
        var offsetY = boxRect.Y + (boxRect.Height - desiredHeight) / 2;

        var transform = Matrix.CreateTranslation(-bounds.X, -bounds.Y)
                       * Matrix.CreateScale(glyphScale, glyphScale)
                       * Matrix.CreateTranslation(offsetX, offsetY);

        using var glyphTransform = context.PushTransform(transform);
        context.DrawGeometry(glyphBrush, null, geometry);
    }

    private static Rect Deflate(Rect rect, Thickness padding)
    {
        if (padding == default)
        {
            return rect;
        }

        var left = Math.Max(0, padding.Left);
        var top = Math.Max(0, padding.Top);
        var right = Math.Max(0, padding.Right);
        var bottom = Math.Max(0, padding.Bottom);

        var width = Math.Max(0, rect.Width - left - right);
        var height = Math.Max(0, rect.Height - top - bottom);
        return new Rect(rect.X + left, rect.Y + top, width, height);
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsEnabled)
        {
            return handled;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Released:
                if (IsWithinBounds(e.Position))
                {
                    ToggleValue();
                }
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _value = _sourceValue;
                RefreshStyle();
                break;
        }

        return true;
    }


    private void ToggleValue()
    {
        _value = _value switch
        {
            null => true,
            true => false,
            false => true,
        };

        _sourceValue = _value;
        Toggled?.Invoke(_value);
        RefreshStyle();
    }

    private bool IsWithinBounds(Point position)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(position);
    }

    private Matrix CreateRotationMatrix()
    {
        if (Math.Abs(Rotation) <= double.Epsilon)
        {
            return Matrix.Identity;
        }

        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        return Matrix.CreateTranslation(-centerX, -centerY)
               * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
               * Matrix.CreateTranslation(centerX, centerY);
    }
}
