using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CheckBoxWidget : Widget
{
    private bool? _isChecked;
    private bool _isEnabled = true;

    public double StrokeThickness { get; set; } = 1.5;

    public double Padding { get; set; } = 4;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _isChecked = null;
        _isEnabled = true;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case CheckBoxWidgetValue checkBoxValue:
                _isChecked = checkBoxValue.IsChecked;
                _isEnabled = checkBoxValue.IsEnabled;
                break;
            case bool boolean:
                _isChecked = boolean;
                break;
            case null:
                _isChecked = null;
                break;
        }
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        using var rotation = context.PushTransform(CreateRotationMatrix());

        var size = Math.Min(Bounds.Width, Bounds.Height) - (Padding * 2);
        if (size <= 0)
        {
            return;
        }

        var originX = Bounds.X + (Bounds.Width - size) / 2;
        var originY = Bounds.Y + (Bounds.Height - size) / 2;
        var rect = new Rect(originX, originY, size, size);

        var borderBrush = ResolveBorder();
        var background = ResolveBackground();
        context.DrawRectangle(background, new Pen(borderBrush, StrokeThickness), rect, 2, 2);

        if (_isChecked is true)
        {
            DrawCheckMark(context, rect);
        }
        else if (_isChecked is null)
        {
            DrawIndeterminate(context, rect);
        }
    }

    private ImmutableSolidColorBrush ResolveBorder()
    {
        if (!_isEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(190, 190, 190));
        }

        return Foreground ?? new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60));
    }

    private IBrush ResolveBackground()
    {
        if (!_isEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(235, 235, 235));
        }

        if (_isChecked is true)
        {
            return Foreground ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
        }

        if (_isChecked is null)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(216, 216, 216));
        }

        return Brushes.Transparent;
    }

    private void DrawCheckMark(DrawingContext context, Rect rect)
    {
        var brush = new ImmutableSolidColorBrush(Color.FromRgb(255, 255, 255));
        var pen = new Pen(brush, StrokeThickness, lineJoin: PenLineJoin.Round);
        var start = new Point(rect.X + rect.Width * 0.2, rect.Y + rect.Height * 0.55);
        var middle = new Point(rect.X + rect.Width * 0.45, rect.Y + rect.Height * 0.75);
        var end = new Point(rect.X + rect.Width * 0.8, rect.Y + rect.Height * 0.3);
        context.DrawLine(pen, start, middle);
        context.DrawLine(pen, middle, end);
    }

    private void DrawIndeterminate(DrawingContext context, Rect rect)
    {
        var brush = Foreground ?? new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60));
        var pen = new Pen(brush, StrokeThickness, lineCap: PenLineCap.Round);
        var start = new Point(rect.X + rect.Width * 0.2, rect.Y + rect.Height / 2);
        var end = new Point(rect.Right - rect.Width * 0.2, rect.Y + rect.Height / 2);
        context.DrawLine(pen, start, end);
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
