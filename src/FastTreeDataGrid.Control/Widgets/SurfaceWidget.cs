using System.Collections.Generic;
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
}
