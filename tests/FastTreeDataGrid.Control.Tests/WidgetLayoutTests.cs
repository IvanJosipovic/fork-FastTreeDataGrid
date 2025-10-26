using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Widgets;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

[Collection("Avalonia")]
public class WidgetLayoutTests
{
[Fact]
public void ExpanderWidget_TogglesOnPointerRelease()
{
    var expander = new ExpanderWidget();
    expander.Arrange(new Rect(0, 0, 180, 120));

        Assert.True(expander.IsExpanded);

        expander.HandlePointerEvent(new WidgetPointerEvent(WidgetPointerEventKind.Released, new Point(10, 10), null));
        Assert.False(expander.IsExpanded);

        expander.HandlePointerEvent(new WidgetPointerEvent(WidgetPointerEventKind.Released, new Point(10, 10), null));
        Assert.True(expander.IsExpanded);
    }

    [Fact]
    public void ScrollViewerWidget_NotifiesVirtualizingHost()
    {
        var scrollViewer = new ScrollViewerWidget
        {
            Padding = new Thickness(4),
        };

        var host = new TestVirtualizingHost();
        scrollViewer.Children.Add(host);

        scrollViewer.Arrange(new Rect(0, 0, 200, 100));
        Assert.True(host.LastViewport.HasValue);
        var initial = host.LastViewport!.Value;
        Assert.Equal(new Size(192, 92), initial.ViewportSize);
        Assert.Equal(new Point(0, 0), initial.Offset);

        scrollViewer.VerticalOffset = 24;
        Assert.True(host.LastViewport.HasValue);
        var updated = host.LastViewport!.Value;
        Assert.Equal(new Point(0, 24), updated.Offset);
        Assert.Equal(initial.ViewportSize, updated.ViewportSize);
    }

    private sealed class TestVirtualizingHost : Widget, IVirtualizingWidgetHost
    {
        public VirtualizingWidgetViewport? LastViewport { get; private set; }

        public override void Draw(DrawingContext context)
        {
        }

        public void UpdateViewport(in VirtualizingWidgetViewport viewport)
    {
        LastViewport = viewport;
    }
}
}
