using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public readonly record struct PanelLayoutContext(
    Rect Bounds,
    Rect InnerBounds,
    double Spacing,
    Thickness Padding,
    object? CustomData = null);

public interface IPanelLayoutAdapter
{
    void Arrange(IList<Widget> children, in PanelLayoutContext context);
}

public abstract class PanelLayoutWidget : SurfaceWidget
{
    static PanelLayoutWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(PanelLayoutWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not PanelLayoutWidget panel)
                {
                    return;
                }

                var layout = theme.Palette.Layout;

                if (panel.Padding == default)
                {
                    panel.Padding = layout.ContentPadding;
                }

                if (panel.SupportsSpacing && panel.Spacing <= 0)
                {
                    panel.Spacing = layout.DefaultSpacing;
                }
            }));
    }

    public Thickness Padding { get; set; } = new Thickness(0);

    public double Spacing { get; set; }

    protected virtual bool SupportsSpacing => true;

    protected abstract IPanelLayoutAdapter Adapter { get; }

    protected virtual PanelLayoutContext CreateContext(Rect bounds, Rect innerBounds)
    {
        return new PanelLayoutContext(bounds, innerBounds, Spacing, Padding, null);
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (Children.Count == 0)
        {
            return;
        }

        var inner = bounds.Deflate(Padding);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        var context = CreateContext(bounds, inner);
        Adapter.Arrange(Children, context);
    }
}
