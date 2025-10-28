using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridGroupingBand : Border
{
    private const double DragActivationThreshold = 4;
    private const double ChipSpacing = 6;
    private const double DropIndicatorWidth = 3;

    private readonly StackPanel _panel;
    private readonly Border _dropIndicator;
    private readonly List<GroupingChip> _chips = new();
    private IReadOnlyList<FastTreeDataGridGroupDescriptor> _descriptors = Array.Empty<FastTreeDataGridGroupDescriptor>();
    private Func<FastTreeDataGridGroupDescriptor, string>? _labelProvider;
    private GroupingChip? _pressedChip;
    private Point _pressPoint;
    private bool _dragInitiated;

    public FastTreeDataGridGroupingBand()
    {
        Height = 36;
        Background = Brushes.Transparent;
        Padding = new Thickness(6, 4, 6, 4);

        var grid = new Grid();
        _panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = ChipSpacing,
            VerticalAlignment = VerticalAlignment.Center,
        };

        grid.Children.Add(_panel);

        _dropIndicator = new Border
        {
            Width = DropIndicatorWidth,
            Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            CornerRadius = new CornerRadius(1.5),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            Opacity = 0,
        };
        _dropIndicator.SetValue(Panel.ZIndexProperty, 100);
        grid.Children.Add(_dropIndicator);

        Child = grid;

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    public event Action<int>? RemoveRequested;

    public event Action<int, int>? ReorderRequested;

    public event Action<int, int>? ColumnDropRequested;

    public event Action<int, KeyModifiers>? KeyboardCommandRequested;

    public bool HasDescriptors => _descriptors.Count > 0;

    public void UpdateDescriptors(
        IReadOnlyList<FastTreeDataGridGroupDescriptor> descriptors,
        Func<FastTreeDataGridGroupDescriptor, string> labelProvider)
    {
        _descriptors = descriptors ?? Array.Empty<FastTreeDataGridGroupDescriptor>();
        _labelProvider = labelProvider ?? throw new ArgumentNullException(nameof(labelProvider));

        EnsureChipCount(_descriptors.Count);
        UpdateVisibility();
    }

    public int GetInsertIndex(Point bandPoint)
    {
        if (_chips.Count == 0)
        {
            return 0;
        }

        var ordered = _chips.Where(c => c.IsVisible).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            var chip = ordered[i];
            var origin = chip.TranslatePoint(new Point(0, 0), this);
            if (origin is null)
            {
                continue;
            }

            var mid = origin.Value.X + (chip.Bounds.Width / 2);
            if (bandPoint.X < mid)
            {
                return i;
            }
        }

        return ordered.Count;
    }

    private void EnsureChipCount(int count)
    {
        while (_chips.Count < count)
        {
            var chip = new GroupingChip();
            chip.PointerPressed += OnChipPointerPressed;
            chip.PointerMoved += OnChipPointerMoved;
            chip.PointerReleased += OnChipPointerReleased;
            chip.PointerCaptureLost += OnChipPointerCaptureLost;
            chip.RemoveRequested += OnChipRemoveRequested;
            chip.KeyDown += OnChipKeyDown;
            _panel.Children.Add(chip);
            _chips.Add(chip);
        }

        while (_chips.Count > count)
        {
            var index = _chips.Count - 1;
            var chip = _chips[index];
            chip.PointerPressed -= OnChipPointerPressed;
            chip.PointerMoved -= OnChipPointerMoved;
            chip.PointerReleased -= OnChipPointerReleased;
            chip.PointerCaptureLost -= OnChipPointerCaptureLost;
            chip.RemoveRequested -= OnChipRemoveRequested;
            chip.KeyDown -= OnChipKeyDown;
            _panel.Children.Remove(chip);
            _chips.RemoveAt(index);
        }

        for (var i = 0; i < _chips.Count; i++)
        {
            var descriptor = _descriptors[i];
            var label = _labelProvider?.Invoke(descriptor) ?? descriptor.ColumnKey ?? $"Group {i + 1}";
            _chips[i].Update(i, label);
        }
    }

    private void UpdateVisibility()
    {
        IsVisible = _descriptors.Count > 0;
        IsHitTestVisible = IsVisible;
    }

    private void OnChipRemoveRequested(int index)
    {
        RemoveRequested?.Invoke(index);
    }

    private void OnChipPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not GroupingChip chip)
        {
            return;
        }

        if (!e.GetCurrentPoint(chip).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is Button)
        {
            return;
        }

        _pressedChip = chip;
        _pressPoint = e.GetPosition(this);
        _dragInitiated = false;
        chip.Focus();
    }

    private void OnChipPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedChip is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(_pressedChip).Properties.IsLeftButtonPressed)
        {
            EndChipDrag();
            return;
        }

        if (_dragInitiated)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _pressPoint.X) > DragActivationThreshold ||
            Math.Abs(current.Y - _pressPoint.Y) > DragActivationThreshold)
        {
            _dragInitiated = true;
            StartChipDrag(_pressedChip, e);
        }
    }

    private void OnChipPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndChipDrag();
    }

    private void OnChipPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        EndChipDrag();
    }

    private async void StartChipDrag(GroupingChip chip, PointerEventArgs triggerEvent)
    {
        var data = new DataObject();
        data.Set(FastTreeDataGridDragFormats.GroupChipIndex, chip.Index);

        await DragDrop.DoDragDrop(triggerEvent, data, DragDropEffects.Move);
        EndChipDrag();
    }

    private void EndChipDrag()
    {
        _pressedChip = null;
        _dragInitiated = false;
        HideDropIndicator();
    }

    private void OnChipKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not GroupingChip chip)
        {
            return;
        }

        var modifiers = e.KeyModifiers;
        switch (e.Key)
        {
            case Key.Delete:
                RemoveRequested?.Invoke(chip.Index);
                e.Handled = true;
                break;
            case Key.Left:
                if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == (KeyModifiers.Control | KeyModifiers.Alt))
                {
                    ReorderRequested?.Invoke(chip.Index, Math.Max(0, chip.Index - 1));
                    e.Handled = true;
                }
                break;
            case Key.Right:
                if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == (KeyModifiers.Control | KeyModifiers.Alt))
                {
                    ReorderRequested?.Invoke(chip.Index, Math.Min(_descriptors.Count, chip.Index + 2));
                    e.Handled = true;
                }
                break;
            default:
                KeyboardCommandRequested?.Invoke(chip.Index, modifiers);
                break;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (!TryGetDragContext(e, out _))
        {
            HideDropIndicator();
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var position = e.GetPosition(this);
        var insertIndex = GetInsertIndex(position);
        ShowDropIndicator(insertIndex);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        HideDropIndicator();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!TryGetDragContext(e, out var context))
        {
            HideDropIndicator();
            return;
        }

        var position = e.GetPosition(this);
        var insertIndex = GetInsertIndex(position);

        switch (context)
        {
            case DragContext.Column column:
                ColumnDropRequested?.Invoke(column.Index, insertIndex);
                break;
            case DragContext.Chip chip:
                ReorderRequested?.Invoke(chip.Index, insertIndex);
                break;
        }

        HideDropIndicator();
        e.Handled = true;
    }

    private bool TryGetDragContext(DragEventArgs e, out DragContext? context)
    {
        context = null;

        var chipData = e.Data.Get(FastTreeDataGridDragFormats.GroupChipIndex);
        if (chipData is int chipIndex)
        {
            context = new DragContext.Chip(chipIndex);
            return true;
        }

        var columnData = e.Data.Get(FastTreeDataGridDragFormats.ColumnIndex);
        if (columnData is int columnIndex)
        {
            context = new DragContext.Column(columnIndex);
            return true;
        }

        return false;
    }

    private void ShowDropIndicator(int insertIndex)
    {
        double x;

        if (_chips.Count == 0 || insertIndex <= 0)
        {
            x = Padding.Left;
        }
        else if (insertIndex >= _chips.Count)
        {
            var last = _chips[^1];
            var point = last.TranslatePoint(new Point(last.Bounds.Width, 0), this);
            x = point?.X ?? Padding.Left;
        }
        else
        {
            var chip = _chips[insertIndex];
            var point = chip.TranslatePoint(new Point(0, 0), this);
            x = point?.X ?? Padding.Left;
        }

        _dropIndicator.Margin = new Thickness(Math.Max(0, x - (DropIndicatorWidth / 2)), 0, 0, 0);
        _dropIndicator.Opacity = 0.9;
    }

    private void HideDropIndicator()
    {
        _dropIndicator.Opacity = 0;
    }

    private sealed class GroupingChip : Border
    {
        private readonly TextBlock _label;
        private readonly Button _removeButton;

        public GroupingChip()
        {
            CornerRadius = new CornerRadius(12);
            Padding = new Thickness(10, 4, 8, 4);
            BorderThickness = new Thickness(1);
            Background = new SolidColorBrush(Color.FromRgb(238, 242, 248));
            BorderBrush = new SolidColorBrush(Color.FromRgb(189, 209, 229));
            Focusable = true;
            IsHitTestVisible = true;

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
            };

            _label = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.SemiBold,
            };

            _removeButton = new Button
            {
                Content = "x",
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = "Remove",
            };
            _removeButton.Click += (_, _) => RemoveRequested?.Invoke(Index);

            panel.Children.Add(_label);
            panel.Children.Add(_removeButton);
            Child = panel;
        }

        public int Index { get; private set; }

        public event Action<int>? RemoveRequested;

        public void Update(int index, string label)
        {
            Index = index;
            _label.Text = label;
        }
    }

    private abstract record DragContext
    {
        private DragContext()
        {
        }

        public sealed record Column(int Index) : DragContext;

        public sealed record Chip(int Index) : DragContext;
    }
}
