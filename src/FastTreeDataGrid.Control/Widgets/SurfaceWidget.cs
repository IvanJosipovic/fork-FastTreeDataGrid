using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class SurfaceWidget : Widget
{
    public IList<Widget> Children { get; } = new List<Widget>();

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        foreach (var child in Children)
        {
            child.Draw(context);
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);
        foreach (var child in Children)
        {
            child.UpdateValue(provider, item);
        }
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            var child = Children[i];
            if (!child.SupportsPointerInput)
            {
                continue;
            }

            var bounds = child.Bounds;
            if (!bounds.Contains(e.Position))
            {
                continue;
            }

            var local = new Point(e.Position.X - bounds.X, e.Position.Y - bounds.Y);
            var forwarded = new WidgetPointerEvent(e.Kind, local, e.Args);
            if (child.HandlePointerEvent(forwarded))
            {
                handled = true;
                break;
            }
        }

        return handled;
    }

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        var handled = base.HandleKeyboardEvent(e);
        foreach (var child in Children)
        {
            if (!child.SupportsKeyboardInput)
            {
                continue;
            }

            if (child.HandleKeyboardEvent(e))
            {
                handled = true;
            }
        }

        return handled;
    }
}
