using Avalonia;
using Avalonia.Input;

namespace FastTreeDataGrid.Control.Widgets;

public enum WidgetPointerEventKind
{
    Entered,
    Exited,
    Moved,
    Pressed,
    Released,
    Cancelled,
}

public readonly record struct WidgetPointerEvent(
    WidgetPointerEventKind Kind,
    Point Position,
    PointerEventArgs? Args);

public enum WidgetKeyboardEventKind
{
    KeyDown,
    KeyUp,
}

public readonly record struct WidgetKeyboardEvent(
    WidgetKeyboardEventKind Kind,
    KeyEventArgs Args);

public enum WidgetVisualState
{
    Normal,
    PointerOver,
    Pressed,
    Disabled,
    Focused,
}
