using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridColumnFilterFlyout
{
    private readonly Flyout _flyout;
    private readonly TextBox _textBox;
    private readonly Button _applyButton;
    private readonly Button _clearButton;
    private readonly Border _container;
    private readonly StackPanel _rootPanel;

    private FastTreeDataGrid? _owner;
    private int _columnIndex = -1;
    private FastTreeDataGridColumn? _column;

    public FastTreeDataGridColumnFilterFlyout()
    {
        _textBox = new TextBox
        {
            MinWidth = 200,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 8),
        };
        _textBox.KeyDown += OnTextBoxKeyDown;

        _applyButton = new Button
        {
            Content = "Apply",
            MinWidth = 72,
        };
        _applyButton.Click += (_, __) => ApplyAndClose();

        _clearButton = new Button
        {
            Content = "Clear",
            MinWidth = 72,
        };
        _clearButton.Click += (_, __) => ClearAndClose();

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        buttonPanel.Children.Add(_clearButton);
        buttonPanel.Children.Add(_applyButton);

        _rootPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        _rootPanel.Children.Add(_textBox);
        _rootPanel.Children.Add(buttonPanel);

        _container = new Border
        {
            Padding = new Thickness(12),
            Child = _rootPanel,
            CornerRadius = new CornerRadius(6),
        };

        _flyout = new Flyout
        {
            Content = _container,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Transient,
        };
        _flyout.Closed += OnFlyoutClosed;
    }

    public void Attach(FastTreeDataGrid owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _container.Classes.Add("fast-tree-data-grid-filter-flyout");
    }

    public void Show(Avalonia.Controls.Control target, FastTreeDataGridColumn column, int columnIndex, string currentValue)
    {
        if (_owner is null)
        {
            throw new InvalidOperationException("Filter flyout must be attached before use.");
        }

        if (target is null)
        {
            return;
        }

        _column = column ?? throw new ArgumentNullException(nameof(column));
        _columnIndex = columnIndex;

        _textBox.Watermark = column.FilterPlaceholder ?? "Filter";

        _textBox.Text = currentValue ?? string.Empty;

        if (_flyout.IsOpen)
        {
            _flyout.Hide();
        }

        _flyout.ShowAt(target);
        Dispatcher.UIThread.Post(() =>
        {
            _textBox.Focus();
            _textBox.SelectionStart = _textBox.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    private void ApplyAndClose()
    {
        if (_owner is null || _columnIndex < 0)
        {
            return;
        }

        var text = _textBox.Text ?? string.Empty;
        _owner.ApplyFilterFromFlyout(_columnIndex, text);
        _flyout.Hide();
    }

    private void ClearAndClose()
    {
        if (_owner is null || _columnIndex < 0)
        {
            return;
        }

        _owner.ApplyFilterFromFlyout(_columnIndex, string.Empty);
        _flyout.Hide();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ApplyAndClose();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _flyout.Hide();
        }
    }

    private void OnFlyoutClosed(object? sender, EventArgs e)
    {
        _columnIndex = -1;
        _column = null;
    }
}
