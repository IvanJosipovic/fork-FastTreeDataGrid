using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CustomDrawWidget : Widget
{
    private Action<DrawingContext, Rect>? _draw;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _draw = null;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case CustomDrawWidgetValue custom:
                _draw = custom.Draw;
                break;
            case Action<DrawingContext, Rect> action:
                _draw = action;
                break;
        }
    }

    public override void Draw(DrawingContext context)
    {
        if (_draw is null)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = context.PushTransform(CreateRotationMatrix());
        _draw.Invoke(context, Bounds);
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
