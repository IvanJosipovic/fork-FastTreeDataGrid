using System;

namespace FastTreeDataGrid.Control.Widgets;

public class WidgetEventArgs : EventArgs
{
    public WidgetEventArgs(Widget source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Widget Source { get; }
}

public sealed class WidgetValueChangedEventArgs<T> : WidgetEventArgs
{
    public WidgetValueChangedEventArgs(Widget source, T oldValue, T newValue)
        : base(source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T OldValue { get; }

    public T NewValue { get; }
}
