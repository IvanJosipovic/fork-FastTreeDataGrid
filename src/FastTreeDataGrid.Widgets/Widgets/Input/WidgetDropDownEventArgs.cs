using System;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class WidgetDropDownEventArgs : EventArgs
{
    public WidgetDropDownEventArgs(Widget source, Widget? content, Rect anchor)
    {
        Source = source;
        Content = content;
        Anchor = anchor;
    }

    public Widget Source { get; }

    public Widget? Content { get; }

    public Rect Anchor { get; }

    public bool Handled { get; set; }
}
