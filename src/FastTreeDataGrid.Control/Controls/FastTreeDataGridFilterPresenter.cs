using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridFilterPresenter : Canvas
{
    private readonly List<TextBox> _filters = new();
    private readonly Dictionary<FastTreeDataGridColumn, TextBox> _columnMap = new();
    private readonly Dictionary<FastTreeDataGridColumn, string?> _filterValues = new();
    private bool _suppressNotifications;

    public double FilterHeight { get; set; } = 28;

    public event Action<int, string?>? FilterChanged;

    public void BindColumns(
        IReadOnlyList<FastTreeDataGridColumn> columns,
        IReadOnlyList<double> widths,
        IReadOnlyList<double> positions,
        double totalWidth)
    {
        if (columns is null)
        {
            return;
        }

        EnsureFilterCount(columns.Count);
        _columnMap.Clear();

        Width = Math.Max(0, totalWidth);
        Height = FilterHeight;

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var textBox = _filters[i];

            var width = i < widths.Count ? widths[i] : 0;
            var left = i < positions.Count ? positions[i] : 0;

            textBox.Width = Math.Max(0, width);
            textBox.Height = FilterHeight;
            textBox.IsEnabled = column.CanUserFilter;
            textBox.Watermark = column.FilterPlaceholder;
            textBox.Tag = column;

            if (!_filterValues.TryGetValue(column, out var value))
            {
                value = string.Empty;
            }

            if (!Equals(textBox.Text, value))
            {
                _suppressNotifications = true;
                textBox.Text = value;
                _suppressNotifications = false;
            }

            Canvas.SetLeft(textBox, left);
            Canvas.SetTop(textBox, 0);

            _columnMap[column] = textBox;
        }
    }

    public void SetFilterValue(int columnIndex, string? value)
    {
        if ((uint)columnIndex >= (uint)_filters.Count)
        {
            return;
        }

        var textBox = _filters[columnIndex];
        if (textBox.Tag is not FastTreeDataGridColumn column)
        {
            return;
        }

        _filterValues[column] = value ?? string.Empty;

        if (!Equals(textBox.Text, value))
        {
            _suppressNotifications = true;
            textBox.Text = value ?? string.Empty;
            _suppressNotifications = false;
        }
    }

    public void SetFilterValue(FastTreeDataGridColumn column, string? value)
    {
        if (_columnMap.TryGetValue(column, out var textBox))
        {
            _filterValues[column] = value ?? string.Empty;
            if (!Equals(textBox.Text, value))
            {
                _suppressNotifications = true;
                textBox.Text = value ?? string.Empty;
                _suppressNotifications = false;
            }
        }
    }

    public string? GetFilterValue(FastTreeDataGridColumn column)
    {
        return _filterValues.TryGetValue(column, out var value) ? value : string.Empty;
    }

    public void FocusFilter(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_filters.Count)
        {
            return;
        }

        _filters[columnIndex].Focus();
        _filters[columnIndex].SelectionStart = _filters[columnIndex].Text?.Length ?? 0;
    }

    private void EnsureFilterCount(int count)
    {
        if (_filters.Count == count)
        {
            return;
        }

        while (_filters.Count < count)
        {
            var textBox = CreateTextBox();
            _filters.Add(textBox);
            Children.Add(textBox);
        }

        while (_filters.Count > count)
        {
            var index = _filters.Count - 1;
            var textBox = _filters[index];
            if (textBox.Tag is FastTreeDataGridColumn mappedColumn)
            {
                _columnMap.Remove(mappedColumn);
                _filterValues.Remove(mappedColumn);
            }
            Children.Remove(textBox);
            _filters.RemoveAt(index);
        }
    }

    private TextBox CreateTextBox()
    {
        var textBox = new TextBox
        {
            BorderBrush = Brushes.Transparent,
            Background = Brushes.Transparent,
            Margin = new Thickness(0),
            Padding = new Thickness(4, 0, 4, 0),
        };

        textBox.AddHandler(KeyDownEvent, OnTextBoxKeyDown, RoutingStrategies.Tunnel);
        textBox.TextChanged += OnTextBoxTextChanged;
        return textBox;
    }

    private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressNotifications)
        {
            return;
        }

        if (sender is not TextBox textBox || textBox.Tag is not FastTreeDataGridColumn column)
        {
            return;
        }

        var value = textBox.Text ?? string.Empty;
        _filterValues[column] = value;

        var index = _filters.IndexOf(textBox);
        if (index >= 0)
        {
            FilterChanged?.Invoke(index, value);
        }
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && sender is TextBox textBox && textBox.Tag is FastTreeDataGridColumn column)
        {
            e.Handled = true;
            _suppressNotifications = true;
            textBox.Clear();
            _suppressNotifications = false;

            _filterValues[column] = string.Empty;
            var index = _filters.IndexOf(textBox);
            if (index >= 0)
            {
                FilterChanged?.Invoke(index, string.Empty);
            }
        }
    }
}
