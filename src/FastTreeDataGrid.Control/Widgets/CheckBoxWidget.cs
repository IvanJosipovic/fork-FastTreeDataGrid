using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

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

    public double StrokeThickness { get; set; } = 1.5;

    public double Padding { get; set; } = 4;

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

        if (_value is true)
        {
            DrawCheckMark(context, rect);
        }
        else if (_value is null)
        {
            DrawIndeterminate(context, rect);
        }
    }

    private ImmutableSolidColorBrush ResolveBorder()
    {
        if (!IsEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(190, 190, 190));
        }

        return Foreground ?? new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60));
    }

    private IBrush ResolveBackground()
    {
        if (!IsEnabled)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(235, 235, 235));
        }

        if (_value is true)
        {
            return Foreground ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
        }

        if (_value is null)
        {
            return new ImmutableSolidColorBrush(Color.FromRgb(216, 216, 216));
        }

        return Brushes.Transparent;
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
                _value = _sourceValue;
                RefreshStyle();
                break;
        }

        return true;
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
