using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public readonly record struct VirtualizingWidgetViewport(Size ViewportSize, Point Offset);

public interface IVirtualizingWidgetHost
{
    void UpdateViewport(in VirtualizingWidgetViewport viewport);
}
