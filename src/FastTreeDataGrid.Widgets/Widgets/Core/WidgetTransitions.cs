using System;

namespace FastTreeDataGrid.Control.Widgets;

public enum WidgetTransitionKind
{
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
}

public readonly record struct WidgetTransitionDescriptor(
    WidgetTransitionKind Kind,
    TimeSpan Duration,
    double SlideOffset = 32)
{
    public static WidgetTransitionDescriptor Fade(TimeSpan duration) =>
        new(WidgetTransitionKind.Fade, duration);

    public static WidgetTransitionDescriptor Slide(WidgetTransitionKind kind, TimeSpan duration, double offset = 32) =>
        new(kind, duration, offset);
}
