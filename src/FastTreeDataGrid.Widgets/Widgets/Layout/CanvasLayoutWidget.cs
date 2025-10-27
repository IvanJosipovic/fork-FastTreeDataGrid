using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CanvasLayoutWidget : PanelLayoutWidget
{
    private readonly Dictionary<Widget, CanvasPlacement> _placements = new();
    private readonly CanvasLayoutAdapter _adapter;

    public CanvasLayoutWidget()
    {
        _adapter = new CanvasLayoutAdapter(_placements);
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override bool SupportsSpacing => false;

    public void SetLeft(Widget child, double? value) => UpdatePlacement(child, placement => placement.With(left: value, clearLeft: value is null));
    public double? GetLeft(Widget child) => GetPlacement(child).Left;

    public void SetTop(Widget child, double? value) => UpdatePlacement(child, placement => placement.With(top: value, clearTop: value is null));
    public double? GetTop(Widget child) => GetPlacement(child).Top;

    public void SetRight(Widget child, double? value) => UpdatePlacement(child, placement => placement.With(right: value, clearRight: value is null));
    public double? GetRight(Widget child) => GetPlacement(child).Right;

    public void SetBottom(Widget child, double? value) => UpdatePlacement(child, placement => placement.With(bottom: value, clearBottom: value is null));
    public double? GetBottom(Widget child) => GetPlacement(child).Bottom;

    public void SetWidth(Widget child, double? value) => UpdatePlacement(child, placement => placement.With(width: value, clearWidth: value is null));
    public double? GetWidth(Widget child) => GetPlacement(child).Width;

    public void SetHeight(Widget child, double? value) => UpdatePlacement(child, placement => placement.With(height: value, clearHeight: value is null));
    public double? GetHeight(Widget child) => GetPlacement(child).Height;

    private CanvasPlacement GetPlacement(Widget child)
    {
        return _placements.TryGetValue(child, out var placement) ? placement : default;
    }

    private void UpdatePlacement(Widget child, System.Func<CanvasPlacement, CanvasPlacement> updater)
    {
        var current = GetPlacement(child);
        var updated = updater(current);
        _placements[child] = updated;
    }

    protected override PanelLayoutContext CreateContext(Rect bounds, Rect innerBounds)
    {
        return new PanelLayoutContext(bounds, innerBounds, 0, Padding, null);
    }
}
