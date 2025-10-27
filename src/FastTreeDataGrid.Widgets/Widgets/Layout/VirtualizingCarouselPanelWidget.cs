using Avalonia.Layout;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class VirtualizingCarouselPanelWidget : VirtualizingPanelWidget
{
    public VirtualizingCarouselPanelWidget()
    {
        SetOrientation(Orientation.Horizontal);
    }

    public double ItemWidth
    {
        get => ItemExtent;
        set => ItemExtent = value;
    }
}
