using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Models;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.Control.Controls;

public partial class FastTreeDataGrid
{
    private readonly EditingController _editingController;
    private int _currentColumnIndex = -1;

    /// <summary>
    /// Raised before a cell enters edit mode.
    /// </summary>
    public event EventHandler<FastTreeDataGridCellEditStartingEventArgs>? CellEditStarting;

    /// <summary>
    /// Raised when a cell edit operation is about to commit.
    /// </summary>
    public event EventHandler<FastTreeDataGridCellEditCommittingEventArgs>? CellEditCommitting;

    /// <summary>
    /// Raised after a cell edit operation has committed.
    /// </summary>
    public event EventHandler<FastTreeDataGridCellEditCommittedEventArgs>? CellEditCommitted;

    /// <summary>
    /// Raised when a cell edit operation is cancelled.
    /// </summary>
    public event EventHandler<FastTreeDataGridCellEditCanceledEventArgs>? CellEditCanceled;

    internal bool HasActiveEdit => _editingController.HasActiveSession;

    internal EditingSessionSnapshot? ActiveEdit => _editingController.GetSnapshot();

    public bool IsEditing => HasActiveEdit;

    internal FastTreeDataGridPresenter? Presenter => _presenter;

    public bool BeginEdit() => BeginEdit(FastTreeDataGridEditActivationReason.Programmatic, null);

    internal bool BeginEdit(FastTreeDataGridEditActivationReason reason, string? initialText)
    {
        if (_columns.Count == 0 || _itemsSource is null)
        {
            return false;
        }

        var rowIndex = _selectionModel.PrimaryIndex;
        if (rowIndex < 0)
        {
            return false;
        }

        var columnIndex = EnsureCurrentColumnIndex();
        if (columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return false;
        }

        var column = _columns[columnIndex];
        ResetTypeSearch();
        return _editingController.BeginEdit(rowIndex, column, reason, initialText);
    }

    public bool CommitEdit() => CommitEdit(FastTreeDataGridEditCommitTrigger.Programmatic);

    internal bool CommitEdit(FastTreeDataGridEditCommitTrigger trigger)
    {
        return _editingController.CommitEdit(trigger);
    }

    public void CancelEdit() => CancelEdit(FastTreeDataGridEditCancelReason.Programmatic);

    internal void CancelEdit(FastTreeDataGridEditCancelReason reason)
    {
        _editingController.CancelEdit(reason);
    }

    internal void SetCurrentColumnForRow(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count)
        {
            return;
        }

        _currentColumnIndex = columnIndex;
    }

    internal void AttachPresenterForEditing(FastTreeDataGridPresenter? presenter)
    {
        _editingController.AttachPresenter(presenter);
    }

    internal void OnViewportUpdatedForEditing(IReadOnlyList<FastTreeDataGridPresenter.RowRenderInfo> rows)
    {
        _editingController.OnViewportUpdated(rows);
    }

    internal void OnSelectionChangedForEditing()
    {
        if (!_editingController.HasActiveSession)
        {
            return;
        }

        var active = _editingController.GetSnapshot();
        if (active is null)
        {
            return;
        }

        var snapshot = active.Value;

        if (!_selectionModel.SelectedIndices.Contains(snapshot.RowIndex))
        {
            CancelEdit(FastTreeDataGridEditCancelReason.Programmatic);
        }
    }

    internal void OnItemsSourceResetForEditing()
    {
        CancelEdit(FastTreeDataGridEditCancelReason.Recycled);
        _currentColumnIndex = -1;
    }

    internal void EnsureActiveEditCommitted()
    {
        if (_editingController.HasActiveSession)
        {
            _editingController.CommitEdit(FastTreeDataGridEditCommitTrigger.Programmatic);
        }
    }

    internal bool TryHandleEditingPointerPress(int rowIndex, FastTreeDataGridColumn column)
    {
        if (!_editingController.HasActiveSession)
        {
            return true;
        }

        var active = _editingController.GetSnapshot();
        if (active is null)
        {
            return true;
        }

        var snapshot = active.Value;

        if (snapshot.RowIndex == rowIndex && ReferenceEquals(snapshot.Column, column))
        {
            return true;
        }

        return _editingController.CommitEdit(FastTreeDataGridEditCommitTrigger.Pointer);
    }

    internal void OnLostFocusForEditing()
    {
        if (_editingController.HasActiveSession)
        {
            _editingController.CommitEdit(FastTreeDataGridEditCommitTrigger.LostFocus);
        }
    }

    internal int EnsureCurrentColumnIndex()
    {
        if (_currentColumnIndex >= 0 && _currentColumnIndex < _columns.Count)
        {
            return _currentColumnIndex;
        }

        _currentColumnIndex = _columns.Count > 0 ? 0 : -1;
        return _currentColumnIndex;
    }

    private void RaiseCellEditStarting(FastTreeDataGridCellEditStartingEventArgs args) =>
        CellEditStarting?.Invoke(this, args);

    private void RaiseCellEditCommitting(FastTreeDataGridCellEditCommittingEventArgs args) =>
        CellEditCommitting?.Invoke(this, args);

    private void RaiseCellEditCommitted(FastTreeDataGridCellEditCommittedEventArgs args) =>
        CellEditCommitted?.Invoke(this, args);

    private void RaiseCellEditCanceled(FastTreeDataGridCellEditCanceledEventArgs args) =>
        CellEditCanceled?.Invoke(this, args);

    private bool MoveToAdjacentEditableCell(bool forward)
    {
        if (_columns.Count == 0 || _itemsSource is null)
        {
            return false;
        }

        var rowCount = _itemsSource.RowCount;
        if (rowCount <= 0)
        {
            return false;
        }

        var rowIndex = _selectionModel.PrimaryIndex;
        if (rowIndex < 0)
        {
            rowIndex = forward ? 0 : rowCount - 1;
            _selectionModel.SelectSingle(rowIndex);
            EnsureRowVisible(rowIndex);
        }

        var columnIndex = EnsureCurrentColumnIndex();
        var totalCells = Math.Max(1, _columns.Count) * Math.Max(1, rowCount);
        var visited = 0;

        while (visited < totalCells)
        {
            columnIndex += forward ? 1 : -1;

            if (columnIndex >= 0 && columnIndex < _columns.Count)
            {
                if (!_columns[columnIndex].IsReadOnly)
                {
                    SetCurrentColumnForRow(columnIndex);
                    return true;
                }
            }
            else
            {
                rowIndex += forward ? 1 : -1;
                if (rowIndex < 0 || rowIndex >= rowCount)
                {
                    return false;
                }

                _selectionModel.SelectSingle(rowIndex);
                EnsureRowVisible(rowIndex);
                columnIndex = forward ? -1 : _columns.Count;
            }

            visited++;
        }

        return false;
    }

    private FastTreeDataGridCellValidationState GetCellValidationState(FastTreeDataGridRow row, FastTreeDataGridColumn column)
    {
        if (row is null)
        {
            return FastTreeDataGridCellValidationState.None;
        }

        var item = row.Item;
        if (item is null)
        {
            return FastTreeDataGridCellValidationState.None;
        }

        var key = column.ValidationKey ?? column.ValueKey;
        string? message = null;

        if (item is IDataErrorInfo dataErrorInfo)
        {
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    var cellError = dataErrorInfo[key!];
                    if (!string.IsNullOrWhiteSpace(cellError))
                    {
                        return new FastTreeDataGridCellValidationState(FastTreeDataGridValidationLevel.Error, cellError);
                    }
                }
                catch (Exception)
                {
                    // Some IDataErrorInfo implementations throw for unknown keys. Ignore.
                }
            }

            var rowError = dataErrorInfo.Error;
            if (!string.IsNullOrWhiteSpace(rowError))
            {
                return new FastTreeDataGridCellValidationState(FastTreeDataGridValidationLevel.Error, rowError);
            }
        }

        if (item is INotifyDataErrorInfo notify && notify.HasErrors)
        {
            if (!string.IsNullOrEmpty(key))
            {
                message = ExtractFirstValidationMessage(notify.GetErrors(key));
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return new FastTreeDataGridCellValidationState(FastTreeDataGridValidationLevel.Error, message);
                }
            }

            message = ExtractFirstValidationMessage(notify.GetErrors(string.Empty) ?? notify.GetErrors(null));
            if (!string.IsNullOrWhiteSpace(message))
            {
                return new FastTreeDataGridCellValidationState(FastTreeDataGridValidationLevel.Error, message);
            }
        }

        return FastTreeDataGridCellValidationState.None;
    }

    private static string? ExtractFirstValidationMessage(IEnumerable? errors)
    {
        if (errors is null)
        {
            return null;
        }

        foreach (var error in errors)
        {
            if (error is null)
            {
                continue;
            }

            if (error is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            var text = error.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private sealed class EditingController
    {
        private readonly FastTreeDataGrid _owner;
        private FastTreeDataGridPresenter? _presenter;
        private EditingSession? _session;
        private bool _isShuttingDown;

        public EditingController(FastTreeDataGrid owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool HasActiveSession => _session is not null;

        public void AttachPresenter(FastTreeDataGridPresenter? presenter)
        {
            if (ReferenceEquals(_presenter, presenter))
            {
                return;
            }

            if (_session is not null)
            {
                CancelEdit(FastTreeDataGridEditCancelReason.Recycled);
            }

            _presenter = presenter;
        }

        public bool BeginEdit(int rowIndex, FastTreeDataGridColumn column, FastTreeDataGridEditActivationReason reason, string? initialText)
        {
            if (_owner._itemsSource is null || _presenter is null)
            {
                return false;
            }

            if (column.IsReadOnly)
            {
                return false;
            }

            if (_owner._virtualizationProvider?.IsPlaceholder(rowIndex) == true)
            {
                return false;
            }

            if (rowIndex < 0 || rowIndex >= _owner._itemsSource.RowCount)
            {
                return false;
            }

            if (_session is not null)
            {
                if (!CommitEdit(FastTreeDataGridEditCommitTrigger.Programmatic))
                {
                    return false;
                }
            }

            var row = _owner._itemsSource.GetRow(rowIndex);
            if (row is null)
            {
                return false;
            }

            _owner.EnsureRowVisible(rowIndex);
            _owner.RequestViewportUpdate();

            if (!_presenter.TryGetCell(rowIndex, column, out var rowInfo, out var cellInfo))
            {
                // Allow the viewport to catch up before attempting again.
                Dispatcher.UIThread.Post(() =>
                {
                    if (_presenter is null || _session is not null)
                    {
                        return;
                    }

                    if (_presenter.TryGetCell(rowIndex, column, out var refreshedRow, out var refreshedCell))
                    {
                        BeginEditCore(rowIndex, column, row, refreshedRow, refreshedCell, reason, initialText);
                    }
                }, DispatcherPriority.Render);
                return false;
            }

            return BeginEditCore(rowIndex, column, row, rowInfo, cellInfo, reason, initialText);
        }

        private bool BeginEditCore(
            int rowIndex,
            FastTreeDataGridColumn column,
            FastTreeDataGridRow row,
            FastTreeDataGridPresenter.RowRenderInfo rowInfo,
            FastTreeDataGridPresenter.CellRenderInfo cellInfo,
            FastTreeDataGridEditActivationReason reason,
            string? initialText)
        {
            var editor = BuildEditor(column, row, initialText);
            if (editor is null)
            {
                return false;
            }

            var startingArgs = new FastTreeDataGridCellEditStartingEventArgs(
                _owner,
                rowIndex,
                column,
                row,
                editor,
                reason,
                initialText);

            _owner.RaiseCellEditStarting(startingArgs);
            if (startingArgs.Cancel)
            {
                return false;
            }

            var session = new EditingSession(_owner, rowIndex, column, row, editor, initialText, reason);
            _session = session;

            if (_presenter is null)
            {
                return false;
            }

            _presenter.AttachEditingControl(editor, cellInfo.ContentBounds);

            AutoFocusEditor(editor, reason, initialText);

            if (row.Item is IEditableObject editable)
            {
                session.EditableObject = editable;
                editable.BeginEdit();
            }

            session.ValidationSubscription = SubscribeToValidation(row);
            return true;
        }

        public bool CommitEdit(FastTreeDataGridEditCommitTrigger trigger)
        {
            if (_session is not { } session)
            {
                return false;
            }

            if (_presenter is null)
            {
                return false;
            }

            var args = new FastTreeDataGridCellEditCommittingEventArgs(
                _owner,
                session.RowIndex,
                session.Column,
                session.Row,
                session.Editor,
                trigger);

            _owner.RaiseCellEditCommitting(args);
            if (args.Cancel)
            {
                return false;
            }

            using var shutdown = new ShutdownScope(this);

            session.EditableObject?.EndEdit();

            _presenter.DetachEditingControl(session.Editor);
            session.Dispose();

            _owner.RaiseCellEditCommitted(new FastTreeDataGridCellEditCommittedEventArgs(
                _owner,
                session.RowIndex,
                session.Column,
                session.Row,
                session.Editor,
                trigger));

            _session = null;
            _owner.RequestViewportUpdate();
            return true;
        }

        public void CancelEdit(FastTreeDataGridEditCancelReason reason)
        {
            if (_session is not { } session)
            {
                return;
            }

            if (_presenter is null)
            {
                _session = null;
                return;
            }

            using var shutdown = new ShutdownScope(this);

            session.EditableObject?.CancelEdit();
            _presenter.DetachEditingControl(session.Editor);

            _owner.RaiseCellEditCanceled(new FastTreeDataGridCellEditCanceledEventArgs(
                _owner,
                session.RowIndex,
                session.Column,
                session.Row,
                session.Editor,
                reason));

            session.Dispose();
            _session = null;
            _owner.RequestViewportUpdate();
        }

        public void OnViewportUpdated(IReadOnlyList<FastTreeDataGridPresenter.RowRenderInfo> rows)
        {
            if (_session is not { } session || _presenter is null)
            {
                return;
            }

            if (rows is null || rows.Count == 0)
            {
                CancelEdit(FastTreeDataGridEditCancelReason.Recycled);
                return;
            }

            var rowInfo = rows.FirstOrDefault(r => r.RowIndex == session.RowIndex);
            if (rowInfo is null || rowInfo.IsPlaceholder)
            {
                CancelEdit(FastTreeDataGridEditCancelReason.Recycled);
                return;
            }

            var cellInfo = rowInfo.Cells.FirstOrDefault(c => ReferenceEquals(c.Column, session.Column));
            if (cellInfo is null)
            {
                CancelEdit(FastTreeDataGridEditCancelReason.Recycled);
                return;
            }

            _presenter.UpdateEditingControlBounds(session.Editor, cellInfo.ContentBounds);
        }

        public EditingSessionSnapshot? GetSnapshot()
        {
            if (_session is not { } session)
            {
                return null;
            }

            return new EditingSessionSnapshot(session.RowIndex, session.Column);
        }

        private IDisposable? SubscribeToValidation(FastTreeDataGridRow row)
        {
            if (row.Item is not INotifyDataErrorInfo notify)
            {
                return null;
            }

            void Handler(object? sender, DataErrorsChangedEventArgs e)
            {
                if (_session is null || !ReferenceEquals(_session.Row.Item, sender))
                {
                    return;
                }

                _owner.RequestViewportUpdate();
            }

            notify.ErrorsChanged += Handler;
            return new Subscription(() => notify.ErrorsChanged -= Handler);
        }

        private static void AutoFocusEditor(AvaloniaControl editor, FastTreeDataGridEditActivationReason reason, string? initialText)
        {
            if (!editor.Focusable)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (editor.IsEffectivelyEnabled && !editor.IsKeyboardFocusWithin)
                {
                    editor.Focus();

                    if (editor is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
                    {
                        if (reason == FastTreeDataGridEditActivationReason.TextInput && !string.IsNullOrEmpty(initialText))
                        {
                            textBox.SelectionStart = textBox.Text.Length;
                            textBox.SelectionEnd = textBox.Text.Length;
                        }
                        else
                        {
                            textBox.SelectionStart = 0;
                            textBox.SelectionEnd = textBox.Text.Length;
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        private AvaloniaControl? BuildEditor(FastTreeDataGridColumn column, FastTreeDataGridRow row, string? initialText)
        {
            var item = row.Item;
            var template = column.EditTemplateSelector?.SelectTemplate(item, column) ?? column.EditTemplate;
            AvaloniaControl? editor = null;
            if (template is not null)
            {
                editor = template.Build(item);
            }

            editor ??= BuildDefaultEditor(column, row, initialText);

            if (editor is null)
            {
                return null;
            }

            if (editor.DataContext is null)
            {
                editor.DataContext = item;
            }

            if (editor is Layoutable layoutable)
            {
                layoutable.HorizontalAlignment = HorizontalAlignment.Stretch;
                layoutable.VerticalAlignment = VerticalAlignment.Stretch;
            }

            editor.SetValue(SelectingItemsControl.IsSelectedProperty, true);

            return editor;
        }

        private AvaloniaControl? BuildDefaultEditor(FastTreeDataGridColumn column, FastTreeDataGridRow row, string? initialText)
        {
            var text = initialText ?? _owner.GetCellText(row, column);
            var textBox = new TextBox
            {
                Text = text,
                MinWidth = 40,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
            };

            return textBox;
        }

        private readonly struct Subscription : IDisposable
        {
            private readonly Action _dispose;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose() => _dispose();
        }

        private sealed class EditingSession : IDisposable
        {
            public EditingSession(
                FastTreeDataGrid owner,
                int rowIndex,
                FastTreeDataGridColumn column,
                FastTreeDataGridRow row,
                AvaloniaControl editor,
                string? initialText,
                FastTreeDataGridEditActivationReason activationReason)
            {
                Owner = owner;
                RowIndex = rowIndex;
                Column = column;
                Row = row;
                Editor = editor;
                InitialText = initialText;
                ActivationReason = activationReason;
            }

            public FastTreeDataGrid Owner { get; }

            public int RowIndex { get; }

            public FastTreeDataGridColumn Column { get; }

            public FastTreeDataGridRow Row { get; }

            public AvaloniaControl Editor { get; }

            public string? InitialText { get; }

            public FastTreeDataGridEditActivationReason ActivationReason { get; }

            public IEditableObject? EditableObject { get; set; }

            public IDisposable? ValidationSubscription { get; set; }

            public void Dispose()
            {
                ValidationSubscription?.Dispose();
                ValidationSubscription = null;
            }
        }

        private readonly struct ShutdownScope : IDisposable
        {
            private readonly EditingController _controller;

            public ShutdownScope(EditingController controller)
            {
                _controller = controller;
                _controller._isShuttingDown = true;
            }

            public void Dispose()
            {
                _controller._isShuttingDown = false;
            }
        }
    }
}

internal readonly struct EditingSessionSnapshot
{
    internal EditingSessionSnapshot(int rowIndex, FastTreeDataGridColumn column)
    {
        RowIndex = rowIndex;
        Column = column;
    }

    public int RowIndex { get; }

    public FastTreeDataGridColumn Column { get; }
}
