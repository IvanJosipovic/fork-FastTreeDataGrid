namespace FastTreeDataGrid.Control.Widgets;

public sealed class LayoutTransformLayoutWidget : PanelLayoutWidget
{
    private readonly LayoutTransformLayoutAdapter _adapter = new();

    public double ScaleX { get; set; } = 1;

    public double ScaleY { get; set; } = 1;

    public double Angle { get; set; } = 0;

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override bool SupportsSpacing => false;

    protected override PanelLayoutContext CreateContext(Avalonia.Rect bounds, Avalonia.Rect innerBounds)
    {
        var options = new LayoutTransformOptions(ScaleX, ScaleY, Angle);
        return new PanelLayoutContext(bounds, innerBounds, 0, Padding, options);
    }
}
