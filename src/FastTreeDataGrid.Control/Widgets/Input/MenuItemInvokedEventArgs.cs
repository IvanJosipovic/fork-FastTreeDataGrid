using System;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class MenuItemInvokedEventArgs : WidgetEventArgs
{
    public MenuItemInvokedEventArgs(MenuItemWidget source, object? dataItem, MenuItemWidgetValue? value)
        : base(source)
    {
        DataItem = dataItem;
        Value = value;
    }

    public object? DataItem { get; }

    public MenuItemWidgetValue? Value { get; }

    public bool Handled { get; set; }
}
