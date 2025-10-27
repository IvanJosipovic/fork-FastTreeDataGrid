namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Represents a factory that can materialize <see cref="Widget"/> instances, typically from XAML templates.
/// </summary>
public interface IWidgetTemplate
{
    /// <summary>
    /// Materializes a new <see cref="Widget"/> instance.
    /// </summary>
    /// <returns>A newly created <see cref="Widget"/> or <c>null</c>.</returns>
    Widget? Build();
}
