using Avalonia.Controls.Templates;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Models;

/// <summary>
/// Provides a hook for determining the edit template used by a <see cref="FastTreeDataGridColumn"/>.
/// </summary>
public interface IFastTreeDataGridEditTemplateSelector
{
    /// <summary>
    /// Returns a template that should be used for editing the supplied item in the provided column.
    /// </summary>
    /// <param name="item">The data item for the row being edited.</param>
    /// <param name="column">The owning column.</param>
    /// <returns>An <see cref="IDataTemplate"/> to build the editor, or <c>null</c> to fall back to the column defaults.</returns>
    IDataTemplate? SelectTemplate(object? item, FastTreeDataGridColumn column);
}
