using System;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Wraps a <see cref="Func{TResult}"/> as an <see cref="IWidgetTemplate"/>.
/// </summary>
public sealed class FuncWidgetTemplate : IWidgetTemplate
{
    public FuncWidgetTemplate(Func<Widget?> factory)
    {
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Gets the factory delegate.
    /// </summary>
    public Func<Widget?> Factory { get; }

    /// <inheritdoc />
    public Widget? Build() => Factory();
}
