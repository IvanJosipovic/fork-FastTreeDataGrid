using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ViewboxLayoutWidget : PanelLayoutWidget
{
    private readonly ViewboxLayoutAdapter _adapter = new();

    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public StretchDirection StretchDirection { get; set; } = StretchDirection.Both;

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override bool SupportsSpacing => false;

    protected override PanelLayoutContext CreateContext(Avalonia.Rect bounds, Avalonia.Rect innerBounds)
    {
        var options = new ViewboxLayoutOptions(Stretch, StretchDirection);
        return new PanelLayoutContext(bounds, innerBounds, 0, Padding, options);
    }
}
