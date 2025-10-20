using System.Collections.Concurrent;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

public class SurfaceWidget : Widget
{
    public ConcurrentQueue<Widget> Children { get; set; } = new();

    public override void Draw(DrawingContext context)
    {
        foreach (var child in Children)
        {
            child.Draw(context);
        }
    }
}
