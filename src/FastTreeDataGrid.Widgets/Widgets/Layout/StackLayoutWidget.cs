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
}
