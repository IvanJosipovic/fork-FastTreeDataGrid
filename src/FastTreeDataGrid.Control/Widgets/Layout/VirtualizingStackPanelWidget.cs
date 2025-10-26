using Avalonia.Layout;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class VirtualizingStackPanelWidget : VirtualizingPanelWidget
{
    public VirtualizingStackPanelWidget()
    {
        SetOrientation(Orientation.Vertical);
    }

    public double ItemHeight
    {
        get => ItemExtent;
        set => ItemExtent = value;
    }
}
