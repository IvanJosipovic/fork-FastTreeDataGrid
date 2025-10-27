using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridRowReorderSettings
{
    private bool _isEnabled;
    private double _activationThreshold = 6;
    private bool _showDragPreview = true;
    private bool _showDropIndicator = true;
    private IBrush _dropIndicatorBrush = new ImmutableSolidColorBrush(Color.FromArgb(200, 49, 130, 206));
    private double _dropIndicatorThickness = 2;
    private IBrush _dragPreviewBrush = new ImmutableSolidColorBrush(Color.FromArgb(56, 49, 130, 206));
    private double _dragPreviewCornerRadius = 4;
    private double _dragPreviewOpacity = 1;
    private bool _useSelection = true;
    private bool _allowGroupReorder;

    public event EventHandler? SettingsChanged;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public double ActivationThreshold
    {
        get => _activationThreshold;
        set => SetField(ref _activationThreshold, Math.Clamp(value, 0, 48));
    }

    public bool ShowDragPreview
    {
        get => _showDragPreview;
        set => SetField(ref _showDragPreview, value);
    }

    public bool ShowDropIndicator
    {
        get => _showDropIndicator;
        set => SetField(ref _showDropIndicator, value);
    }

    public IBrush DropIndicatorBrush
    {
        get => _dropIndicatorBrush;
        set => SetField(ref _dropIndicatorBrush, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public double DropIndicatorThickness
    {
        get => _dropIndicatorThickness;
        set => SetField(ref _dropIndicatorThickness, Math.Clamp(value, 1, 8));
    }

    public IBrush DragPreviewBrush
    {
        get => _dragPreviewBrush;
        set => SetField(ref _dragPreviewBrush, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public double DragPreviewCornerRadius
    {
        get => _dragPreviewCornerRadius;
        set => SetField(ref _dragPreviewCornerRadius, Math.Clamp(value, 0, 24));
    }

    public double DragPreviewOpacity
    {
        get => _dragPreviewOpacity;
        set => SetField(ref _dragPreviewOpacity, Math.Clamp(value, 0, 1));
    }

    /// <summary>
    /// When true, dragging any selected row will move the entire contiguous selection.
    /// When false the drag operates on the pressed row only.
    /// </summary>
    public bool UseSelection
    {
        get => _useSelection;
        set => SetField(ref _useSelection, value);
    }

    /// <summary>
    /// Allows drag & drop for group rows when enabled.
    /// </summary>
    public bool AllowGroupReorder
    {
        get => _allowGroupReorder;
        set => SetField(ref _allowGroupReorder, value);
    }

    /// <summary>
    /// Enables drag &amp; drop for group rows (e.g., regions, categories). When false, groups stay anchored.
    /// </summary>

    private void SetField<T>(ref T field, T value)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
