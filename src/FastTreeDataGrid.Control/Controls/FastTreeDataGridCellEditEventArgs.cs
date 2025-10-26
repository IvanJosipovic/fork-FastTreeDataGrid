using System;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.Control.Controls;

/// <summary>
/// Describes how an edit session was activated.
/// </summary>
public enum FastTreeDataGridEditActivationReason
{
    Programmatic,
    Pointer,
    Keyboard,
    TextInput,
}

/// <summary>
/// Describes how an edit session was committed.
/// </summary>
public enum FastTreeDataGridEditCommitTrigger
{
    Programmatic,
    EnterKey,
    Pointer,
    LostFocus,
    TabNavigation,
}

/// <summary>
/// Describes why an edit session was cancelled.
/// </summary>
public enum FastTreeDataGridEditCancelReason
{
    Programmatic,
    UserCancel,
    ValidationError,
    LostFocus,
    Recycled,
}

/// <summary>
/// Base type for cell editing lifecycle events.
/// </summary>
public class FastTreeDataGridCellEditEventArgs : EventArgs
{
    internal FastTreeDataGridCellEditEventArgs(
        FastTreeDataGrid grid,
        int rowIndex,
        FastTreeDataGridColumn column,
        FastTreeDataGridRow row,
        AvaloniaControl? editor)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        RowIndex = rowIndex;
        Column = column ?? throw new ArgumentNullException(nameof(column));
        Row = row ?? throw new ArgumentNullException(nameof(row));
        Editor = editor;
    }

    /// <summary>
    /// Gets the grid that raised the event.
    /// </summary>
    public FastTreeDataGrid Grid { get; }

    /// <summary>
    /// Gets the row index being edited.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the column being edited.
    /// </summary>
    public FastTreeDataGridColumn Column { get; }

    /// <summary>
    /// Gets the <see cref="FastTreeDataGridRow"/> that provides access to the underlying data item.
    /// </summary>
    public FastTreeDataGridRow Row { get; }

    /// <summary>
    /// Gets the underlying data item.
    /// </summary>
    public object? Item => Row.Item;

    /// <summary>
    /// Gets the value provider associated with the row, if any.
    /// </summary>
    public IFastTreeDataGridValueProvider? ValueProvider => Row.ValueProvider;

    /// <summary>
    /// Gets the editor control that was created for the session.
    /// </summary>
    public AvaloniaControl? Editor { get; }
}

/// <summary>
/// Event args raised before an edit session begins.
/// </summary>
public sealed class FastTreeDataGridCellEditStartingEventArgs : FastTreeDataGridCellEditEventArgs
{
    internal FastTreeDataGridCellEditStartingEventArgs(
        FastTreeDataGrid grid,
        int rowIndex,
        FastTreeDataGridColumn column,
        FastTreeDataGridRow row,
        AvaloniaControl? editor,
        FastTreeDataGridEditActivationReason activationReason,
        string? initialText)
        : base(grid, rowIndex, column, row, editor)
    {
        ActivationReason = activationReason;
        InitialText = initialText;
    }

    /// <summary>
    /// Gets the reason the edit session was activated.
    /// </summary>
    public FastTreeDataGridEditActivationReason ActivationReason { get; }

    /// <summary>
    /// Gets the text that triggered the session when activation was due to user typing.
    /// </summary>
    public string? InitialText { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the edit session should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Event args raised when an edit session is about to commit.
/// </summary>
public sealed class FastTreeDataGridCellEditCommittingEventArgs : FastTreeDataGridCellEditEventArgs
{
    internal FastTreeDataGridCellEditCommittingEventArgs(
        FastTreeDataGrid grid,
        int rowIndex,
        FastTreeDataGridColumn column,
        FastTreeDataGridRow row,
        AvaloniaControl? editor,
        FastTreeDataGridEditCommitTrigger trigger)
        : base(grid, rowIndex, column, row, editor)
    {
        Trigger = trigger;
    }

    /// <summary>
    /// Gets the mechanism that requested the commit.
    /// </summary>
    public FastTreeDataGridEditCommitTrigger Trigger { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the commit should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Event args raised when an edit session is cancelled.
/// </summary>
public sealed class FastTreeDataGridCellEditCanceledEventArgs : FastTreeDataGridCellEditEventArgs
{
    internal FastTreeDataGridCellEditCanceledEventArgs(
        FastTreeDataGrid grid,
        int rowIndex,
        FastTreeDataGridColumn column,
        FastTreeDataGridRow row,
        AvaloniaControl? editor,
        FastTreeDataGridEditCancelReason reason)
        : base(grid, rowIndex, column, row, editor)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason for the cancellation.
    /// </summary>
    public FastTreeDataGridEditCancelReason Reason { get; }
}

/// <summary>
/// Event args raised after an edit session has successfully committed.
/// </summary>
public sealed class FastTreeDataGridCellEditCommittedEventArgs : FastTreeDataGridCellEditEventArgs
{
    internal FastTreeDataGridCellEditCommittedEventArgs(
        FastTreeDataGrid grid,
        int rowIndex,
        FastTreeDataGridColumn column,
        FastTreeDataGridRow row,
        AvaloniaControl? editor,
        FastTreeDataGridEditCommitTrigger trigger)
        : base(grid, rowIndex, column, row, editor)
    {
        Trigger = trigger;
    }

    /// <summary>
    /// Gets the mechanism that requested the commit.
    /// </summary>
    public FastTreeDataGridEditCommitTrigger Trigger { get; }
}
