using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;
using GridColumn = FastTreeDataGrid.Control.Models.FastTreeDataGridColumn;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public class FastTreeDataGridEditingTests
{
    [AvaloniaFact]
    public void BeginEdit_RaisesStartingEventAndCreatesSession()
    {
        using var harness = new GridTestHarness();

        var startingRaised = false;
        harness.Grid.CellEditStarting += (_, args) =>
        {
            startingRaised = true;
            Assert.Equal(0, args.RowIndex);
            Assert.Same(harness.Column, args.Column);
            Assert.Equal(FastTreeDataGridEditActivationReason.Programmatic, args.ActivationReason);
        };

        Assert.True(harness.Grid.BeginEdit());
        Assert.True(startingRaised);
        Assert.True(harness.Grid.IsEditing);

        harness.Grid.CancelEdit(FastTreeDataGridEditCancelReason.Programmatic);
    }

    [AvaloniaFact]
    public void CancelingStarting_PreventsEditing()
    {
        using var harness = new GridTestHarness();

        harness.Grid.CellEditStarting += (_, args) => args.Cancel = true;

        Assert.False(harness.Grid.BeginEdit());
        Assert.False(harness.Grid.IsEditing);
    }

    [AvaloniaFact]
    public void CancelingCommit_KeepsSessionActive()
    {
        using var harness = new GridTestHarness();

        Assert.True(harness.Grid.BeginEdit());

        harness.Grid.CellEditCommitting += (_, args) => args.Cancel = true;

        Assert.False(harness.Grid.CommitEdit(FastTreeDataGridEditCommitTrigger.Programmatic));
        Assert.True(harness.Grid.IsEditing);

        harness.Grid.CancelEdit(FastTreeDataGridEditCancelReason.Programmatic);
    }

    [AvaloniaFact]
    public void CommitEdit_InvokesEditableObject()
    {
        using var harness = new GridTestHarness();

        Assert.True(harness.Grid.BeginEdit());

        Assert.True(harness.Grid.CommitEdit(FastTreeDataGridEditCommitTrigger.Programmatic));
        Assert.True(harness.Row.EndEditCalled);
        Assert.False(harness.Grid.IsEditing);
    }

    [AvaloniaFact]
    public void CancelEdit_InvokesEditableObject()
    {
        using var harness = new GridTestHarness();

        Assert.True(harness.Grid.BeginEdit());

        harness.Grid.CancelEdit(FastTreeDataGridEditCancelReason.UserCancel);

        Assert.True(harness.Row.CancelEditCalled);
        Assert.False(harness.Grid.IsEditing);
    }

    [AvaloniaFact]
    public void ValidationStateReflectsErrors()
    {
        using var harness = new GridTestHarness();

        var method = typeof(GridControl)
            .GetMethod("GetCellValidationState", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var initialState = (FastTreeDataGridCellValidationState)method.Invoke(
            harness.Grid,
            new object[] { harness.GridRow, harness.Column })!;

        Assert.False(initialState.HasError);

        harness.Row.SetCellError(nameof(EditableRow.Name), "Required");

        var errorState = (FastTreeDataGridCellValidationState)method.Invoke(
            harness.Grid,
            new object[] { harness.GridRow, harness.Column })!;

        Assert.True(errorState.HasError);
        Assert.Equal("Required", errorState.Message);
    }

    private sealed class GridTestHarness : IDisposable
    {
        private readonly FastTreeDataGridPresenter _presenter;

        public GridTestHarness()
        {
        Grid = new GridControl();

        Column = new GridColumn
            {
                Header = "Name",
                ValueKey = nameof(EditableRow.Name),
            };

            Grid.Columns.Add(Column);

            Row = new EditableRow("Initial");
            GridRow = new FastTreeDataGridRow(Row, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: () => { });

            var source = new SingleRowSource(GridRow);
            SetItemsSource(Grid, source);

            Grid.SelectionModel.SelectSingle(0);
            Grid.SetCurrentColumnForRow(0);

            _presenter = new FastTreeDataGridPresenter();
            _presenter.SetOwner(Grid);

            var cellBounds = new Rect(0, 0, 120, 28);
            var rowInfo = new FastTreeDataGridPresenter.RowRenderInfo(
                GridRow,
                rowIndex: 0,
                top: 0,
                height: 28,
                isSelected: true,
                hasChildren: false,
                isExpanded: false,
                toggleRect: default,
                isGroup: false,
                isSummary: false,
                isPlaceholder: false);

            var cellInfo = new FastTreeDataGridPresenter.CellRenderInfo(
                Column,
                cellBounds,
                cellBounds,
                widget: null,
                formattedText: null,
                textOrigin: new Point(cellBounds.X, cellBounds.Y),
                control: null,
                FastTreeDataGridCellValidationState.None);

            rowInfo.Cells.Add(cellInfo);

            _presenter.UpdateContent(
                new List<FastTreeDataGridPresenter.RowRenderInfo> { rowInfo },
                totalWidth: 120,
                totalHeight: 28,
                columnOffsets: new[] { 120d });

            Grid.AttachPresenterForEditing(_presenter);
        }

        public GridControl Grid { get; }

        public GridColumn Column { get; }

        public EditableRow Row { get; }

        public FastTreeDataGridRow GridRow { get; }

        public void Dispose()
        {
            Grid.CancelEdit(FastTreeDataGridEditCancelReason.Programmatic);
        }

        private static void SetItemsSource(GridControl grid, IFastTreeDataGridSource source)
        {
            var field = typeof(GridControl).GetField("_itemsSource", BindingFlags.Instance | BindingFlags.NonPublic);
            field!.SetValue(grid, source);
        }
    }

    private sealed class SingleRowSource : IFastTreeDataGridSource
    {
        private readonly FastTreeDataGridRow _row;

        public SingleRowSource(FastTreeDataGridRow row)
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
        }

        public event EventHandler? ResetRequested
        {
            add { }
            remove { }
        }

        public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated
        {
            add { }
            remove { }
        }

        public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized
        {
            add { }
            remove { }
        }

        public int RowCount => 1;

        public bool SupportsPlaceholders => false;

        public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) =>
            new(1);

        public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
            new(FastTreeDataGridPageResult.Empty);

        public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
        {
            row = _row;
            return index == 0;
        }

        public bool IsPlaceholder(int index) => false;

        public FastTreeDataGridRow GetRow(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _row;
        }

        public void ToggleExpansion(int index)
        {
        }
    }

    private sealed class EditableRow : IFastTreeDataGridValueProvider, IDataErrorInfo, INotifyDataErrorInfo, INotifyPropertyChanged, IEditableObject
    {
        private string _name;
        private readonly Dictionary<string, string?> _errors = new();
        private string? _snapshot;

        public EditableRow(string name)
        {
            _name = name;
        }

        public string Name
        {
            get => _name;
            set
            {
                if (!string.Equals(_name, value, StringComparison.Ordinal))
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, nameof(Name)));
                }
            }
        }

        public bool BeginEditCalled { get; private set; }
        public bool EndEditCalled { get; private set; }
        public bool CancelEditCalled { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

        public bool HasErrors => _errors.Count > 0;

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                if (_errors.TryGetValue(columnName, out var error) && !string.IsNullOrEmpty(error))
                {
                    return error!;
                }

                return string.Empty;
            }
        }

        public object? GetValue(object? item, string key)
        {
            if (string.Equals(key, nameof(Name), StringComparison.Ordinal))
            {
                return Name;
            }

            return null;
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                yield break;
            }

            if (_errors.TryGetValue(propertyName, out var error) && !string.IsNullOrWhiteSpace(error))
            {
                yield return error!;
            }
        }

        public void SetCellError(string propertyName, string? error)
        {
            if (string.IsNullOrEmpty(error))
            {
                if (_errors.Remove(propertyName))
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                }
            }
            else
            {
                _errors[propertyName] = error;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        public void BeginEdit()
        {
            BeginEditCalled = true;
            _snapshot = Name;
        }

        public void EndEdit()
        {
            EndEditCalled = true;
            _snapshot = null;
        }

        public void CancelEdit()
        {
            CancelEditCalled = true;
            if (_snapshot is not null)
            {
                Name = _snapshot;
            }

            _snapshot = null;
        }
    }
}
