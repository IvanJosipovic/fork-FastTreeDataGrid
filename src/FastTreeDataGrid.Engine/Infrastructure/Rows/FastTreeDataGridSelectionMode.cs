namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Specifies how the <see cref="Controls.FastTreeDataGrid"/> handles selection.
/// </summary>
public enum FastTreeDataGridSelectionMode
{
    /// <summary>
    /// Selection is disabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Only a single item can be selected at a time.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be selected; modifier keys are ignored.
    /// </summary>
    Multiple,

    /// <summary>
    /// Multiple items can be selected; Shift/Ctrl modifiers extend or toggle the selection.
    /// </summary>
    Extended,
}
