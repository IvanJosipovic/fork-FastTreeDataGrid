using Avalonia;
using Avalonia.Headless.XUnit;
using FastTreeDataGrid.Control.Widgets;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public class MenuWidgetTests
{
    [AvaloniaFact]
    public void MenuItemWidget_SetsAutomationAccessKey()
    {
        var widget = new MenuItemWidget();
        widget.UpdateValue(null, new MenuItemWidgetValue("_Open", GestureText: "Ctrl+O"));

        Assert.Equal("Open", widget.Automation.Name);
        Assert.Equal("Ctrl+O", widget.Automation.CommandLabel);
        Assert.Equal("O", widget.Automation.AccessKey);
    }

    [AvaloniaFact]
    public void ContextMenuWidget_ShowAt_TogglesIsOpen()
    {
        var contextMenu = new ContextMenuWidget();
        contextMenu.UpdateValue(null, new ContextMenuWidgetValue(new[]
        {
            new MenuItemWidgetValue("_Refresh")
        }));

        Assert.False(contextMenu.IsOpen);

        contextMenu.ShowAt(new Rect(new Point(12, 12), new Size(0, 0)));
        Assert.True(contextMenu.IsOpen);

        contextMenu.Close();
        Assert.False(contextMenu.IsOpen);
    }
}
