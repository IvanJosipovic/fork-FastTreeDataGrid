using Avalonia;
using Avalonia.Layout;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class StackLayoutWidget : PanelLayoutWidget
{
    private readonly StackLayoutAdapter _adapter = new();

    public Orientation Orientation
    {
        get => _adapter.Orientation;
        set => _adapter.Orientation = value;
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override PanelLayoutContext CreateContext(Avalonia.Rect bounds, Avalonia.Rect innerBounds)
    {
        return new PanelLayoutContext(bounds, innerBounds, Spacing, Padding, Orientation);
    }

    protected override Size MeasureCore(Size available)
    {
        var padding = Padding;
        var spacing = Math.Max(0, Spacing);
        var innerWidth = Math.Max(0, available.Width - padding.Left - padding.Right);
        var innerHeight = Math.Max(0, available.Height - padding.Top - padding.Bottom);

        double width = 0;
        double height = 0;

        if (Orientation == Orientation.Vertical)
        {
            var childAvailable = new Size(innerWidth, double.PositiveInfinity);
            for (var i = 0; i < Children.Count; i++)
            {
                var childSize = Children[i].Measure(childAvailable);
                width = Math.Max(width, childSize.Width);
                height += childSize.Height;
                if (i < Children.Count - 1)
                {
                    height += spacing;
                }
            }
        }
        else
        {
            var childAvailable = new Size(double.PositiveInfinity, innerHeight);
            for (var i = 0; i < Children.Count; i++)
            {
                var childSize = Children[i].Measure(childAvailable);
                width += childSize.Width;
                if (i < Children.Count - 1)
                {
                    width += spacing;
                }

                height = Math.Max(height, childSize.Height);
            }
        }

        width += padding.Left + padding.Right;
        height += padding.Top + padding.Bottom;

        return new Size(width, height);
    }
}
