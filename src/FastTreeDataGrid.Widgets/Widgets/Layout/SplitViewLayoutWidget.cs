namespace FastTreeDataGrid.Control.Widgets;

public sealed class SplitViewLayoutWidget : PanelLayoutWidget
{
    public const string PaneStyleKey = "SplitViewPane";
    public const string ContentStyleKey = "SplitViewContent";

    private readonly SplitViewLayoutAdapter _adapter = new();

    protected override IPanelLayoutAdapter Adapter => _adapter;

    public double CompactPaneLength
    {
        get => _adapter.CompactPaneLength;
        set => _adapter.CompactPaneLength = value;
    }

    public double OpenPaneLength
    {
        get => _adapter.OpenPaneLength;
        set => _adapter.OpenPaneLength = value;
    }

    public bool IsPaneOpen
    {
        get => _adapter.IsPaneOpen;
        set => _adapter.IsPaneOpen = value;
    }

    public SplitViewPanePlacement PanePlacement
    {
        get => _adapter.PanePlacement;
        set => _adapter.PanePlacement = value;
    }

    protected override bool SupportsSpacing => false;
}
