using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Avalonia.Styling;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridPresenter : Avalonia.Controls.Control, IWidgetOverlayHost
{
    private readonly List<RowRenderInfo> _rows = new();
    private FastTreeDataGrid? _owner;
    private readonly List<double> _columnOffsets = new();
    private IFastTreeDataVirtualizationProvider? _virtualizationProvider;

    private Widget? _pointerCapturedWidget;
    private Widget? _pointerOverWidget;
    private Widget? _focusedWidget;
    private readonly Dictionary<Widget, OverlayEntry> _overlayMap = new();
    private readonly List<OverlayEntry> _overlayOrder = new();
    private readonly Dictionary<AvaloniaControl, Rect> _controlLayouts = new();
    private readonly List<AvaloniaControl> _controlChildren = new();
    private readonly Dictionary<AvaloniaControl, FastTreeDataGridColumn> _controlColumnMap = new();
    private AvaloniaControl? _editingControl;
    private Rect _editingControlBounds;

    private IBrush _selectionBrush = new SolidColorBrush(Color.FromArgb(40, 49, 130, 206));
    private IBrush _placeholderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200));
    private IBrush _toggleGlyphBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));
    private IPen _gridPen = new Pen(new SolidColorBrush(Color.FromRgb(210, 210, 210)), 1);
    private IBrush _skeletonBarBrush = new SolidColorBrush(Color.FromRgb(214, 219, 225));
    private IBrush _summaryBrush = new SolidColorBrush(Color.FromRgb(236, 242, 248));
    private IBrush _validationErrorBrush = new SolidColorBrush(Color.FromRgb(213, 63, 63));
    private IBrush _validationWarningBrush = new SolidColorBrush(Color.FromRgb(213, 141, 38));
    private IPen? _focusPen = new Pen(new SolidColorBrush(Color.FromRgb(49, 130, 206)), 1.5);
    private static readonly StreamGeometry s_collapsedGlyph = StreamGeometry.Parse("M 1,0 10,10 l -9,10 -1,-1 L 8,10 -0,1 Z");
    private static readonly StreamGeometry s_expandedGlyph = StreamGeometry.Parse("M0,1 L10,10 20,1 19,0 10,8 1,0 Z");
    private IDisposable? _animationRegistration;
    private IDisposable? _overlayRegistration;
    private const double ValidationBadgeSize = 8;

    private bool ShouldRenderSkeletons => _owner?.VirtualizationSettings.ShowPlaceholderSkeletons ?? true;

    public FastTreeDataGridPresenter()
    {
        ClipToBounds = true;
        IsHitTestVisible = true;
        Focusable = true;
        AddHandler(InputElement.PointerExitedEvent, PresenterOnPointerLeave, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _animationRegistration = WidgetAnimationFrameScheduler.RegisterHost(this);
        _overlayRegistration = WidgetOverlayManager.RegisterHost(this);
        ApplyThemeResources();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _animationRegistration?.Dispose();
        _animationRegistration = null;
        _overlayRegistration?.Dispose();
        _overlayRegistration = null;
        ClearControlChildren();
        base.OnDetachedFromVisualTree(e);
    }

    public void SetOwner(FastTreeDataGrid? owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            return;
        }

        if (_owner is not null)
        {
            _owner.ResourcesChanged -= OnOwnerResourcesChanged;
        }

        _owner = owner;
        if (_owner is not null)
        {
            _owner.ResourcesChanged += OnOwnerResourcesChanged;
            ApplyThemeResources();
        }

        SetVirtualizationProvider(_owner?.VirtualizationProvider);
    }

    public void SetVirtualizationProvider(IFastTreeDataVirtualizationProvider? provider)
    {
        if (ReferenceEquals(_virtualizationProvider, provider))
        {
            return;
        }

        if (_virtualizationProvider is not null)
        {
            _virtualizationProvider.RowMaterialized -= OnProviderRowMaterialized;
            _virtualizationProvider.CountChanged -= OnProviderCountChanged;
        }

        _virtualizationProvider = provider;

        if (_virtualizationProvider is not null)
        {
            _virtualizationProvider.RowMaterialized += OnProviderRowMaterialized;
            _virtualizationProvider.CountChanged += OnProviderCountChanged;
        }
    }

    public void UpdateContent(IReadOnlyList<RowRenderInfo> rows, double totalWidth, double totalHeight, IReadOnlyList<double> columnOffsets)
    {
        ReturnRowWidgets(_rows);
        ClearControlChildren();
        _rows.Clear();
        _rows.AddRange(rows);
        _columnOffsets.Clear();
        _columnOffsets.AddRange(columnOffsets);
        Width = totalWidth;
        Height = totalHeight;

        AttachControlsForRows(_rows);

        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();

        _pointerCapturedWidget = null;
        _pointerOverWidget = null;
        _focusedWidget = null;
        ClearOverlays();
    }

    public void UpdateSelection(IReadOnlyList<int> selectedIndices)
    {
        var selection = selectedIndices ?? Array.Empty<int>();
        HashSet<int>? selectionLookup = null;
        if (selection.Count > 0)
        {
            selectionLookup = selection is IReadOnlyCollection<int> collection
                ? new HashSet<int>(collection)
                : new HashSet<int>(selection);
        }

        var changed = false;
        foreach (var row in _rows)
        {
            var shouldSelect = selectionLookup?.Contains(row.RowIndex) ?? false;
            if (row.IsSelected != shouldSelect)
            {
                row.IsSelected = shouldSelect;
                changed = true;
            }

            UpdateControlSelectionState(row);
        }

        if (changed)
        {
            InvalidateVisual();
        }
    }

    private static void UpdateControlSelectionState(RowRenderInfo row)
    {
        if (row is null)
        {
            return;
        }

        foreach (var cell in row.Cells)
        {
            if (cell.Control is { } control)
            {
                control.SetValue(SelectingItemsControl.IsSelectedProperty, row.IsSelected);
            }
        }
    }

    private void AttachControlsForRows(IReadOnlyList<RowRenderInfo> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return;
        }

        foreach (var row in rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Control is { } control)
                {
                    AttachControl(control, cell.ContentBounds, row.IsSelected, cell.Column, cell.ValidationState);
                }
            }
        }
    }

    private void AttachControl(AvaloniaControl control, Rect bounds, bool isSelected, FastTreeDataGridColumn column, FastTreeDataGridCellValidationState validationState)
    {
        if (control is null)
        {
            return;
        }

        control.RemoveHandler(InputElement.PointerPressedEvent, ControlOnPointerPressed);
        control.AddHandler(InputElement.PointerPressedEvent, ControlOnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        control.SetValue(SelectingItemsControl.IsSelectedProperty, isSelected);
        control.ClipToBounds = true;
        if (!string.IsNullOrEmpty(validationState.Message))
        {
            Avalonia.Controls.ToolTip.SetTip(control, validationState.Message);
        }
        else
        {
            Avalonia.Controls.ToolTip.SetTip(control, null);
        }
        _controlLayouts[control] = bounds;

        if (!_controlChildren.Contains(control))
        {
            VisualChildren.Add(control);
            if (control is ILogical logical)
            {
                LogicalChildren.Add(logical);
            }

            _controlChildren.Add(control);
        }

        _controlColumnMap[control] = column;
    }

    private void ClearControlChildren()
    {
        if (_controlChildren.Count == 0)
        {
            _controlLayouts.Clear();
            return;
        }

        for (var i = _controlChildren.Count - 1; i >= 0; i--)
        {
            var control = _controlChildren[i];
            control.RemoveHandler(InputElement.PointerPressedEvent, ControlOnPointerPressed);
            VisualChildren.Remove(control);
            if (control is ILogical logical)
            {
                LogicalChildren.Remove(logical);
            }

            Avalonia.Controls.ToolTip.SetTip(control, null);
            if (_controlColumnMap.TryGetValue(control, out var column))
            {
                _controlColumnMap.Remove(control);
                column.ReturnControl(control);
            }
        }

        _controlChildren.Clear();
        _controlLayouts.Clear();
        if (_controlColumnMap.Count > 0)
        {
            _controlColumnMap.Clear();
        }
    }

    private static void ReturnRowWidgets(IEnumerable<RowRenderInfo> rows)
    {
        if (rows is null)
        {
            return;
        }

        foreach (var row in rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Widget is FormattedTextWidget textWidget)
                {
                    cell.Column.ReturnTextWidget(textWidget);
                }
            }
        }
    }

    internal bool TryGetCell(int rowIndex, FastTreeDataGridColumn column, out RowRenderInfo? rowInfo, out CellRenderInfo? cellInfo)
    {
        rowInfo = null;
        cellInfo = null;

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (row.RowIndex != rowIndex)
            {
                continue;
            }

            rowInfo = row;
            for (var j = 0; j < row.Cells.Count; j++)
            {
                var cell = row.Cells[j];
                if (ReferenceEquals(cell.Column, column))
                {
                    cellInfo = cell;
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    internal void AttachEditingControl(AvaloniaControl editor, Rect bounds)
    {
        if (editor is null)
        {
            return;
        }

        if (!ReferenceEquals(_editingControl, editor))
        {
            DetachEditingControl(_editingControl);

            _editingControl = editor;
        }

        _editingControlBounds = bounds;

        VisualChildren.Remove(editor);
        VisualChildren.Add(editor);

        if (editor is ILogical logical)
        {
            LogicalChildren.Remove(logical);
            LogicalChildren.Add(logical);
        }

        InvalidateMeasure();
        InvalidateArrange();
    }

    internal void UpdateEditingControlBounds(AvaloniaControl editor, Rect bounds)
    {
        if (!ReferenceEquals(_editingControl, editor))
        {
            AttachEditingControl(editor, bounds);
            return;
        }

        _editingControlBounds = bounds;
        InvalidateArrange();
    }

    internal void DetachEditingControl(AvaloniaControl? editor)
    {
        if (editor is null || !ReferenceEquals(_editingControl, editor))
        {
            return;
        }

        VisualChildren.Remove(editor);
        if (editor is ILogical logical)
        {
            LogicalChildren.Remove(logical);
        }

        _editingControl = null;
        _editingControlBounds = default;
        InvalidateVisual();
    }

    private void ControlOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_owner is null)
        {
            return;
        }

        if (!IsFocused && Focusable && IsEffectivelyEnabled)
        {
            Focus();
        }

        var point = e.GetCurrentPoint(this).Position;

        if (RoutePointerEvent(point, e, WidgetPointerEventKind.Pressed))
        {
            return;
        }

        HandlePointer(point, e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using var overlayScope = WidgetOverlayManager.PushCurrentHost(this);
        using var animationScope = WidgetAnimationFrameScheduler.PushCurrentHost(this);

        foreach (var row in _rows)
        {
            var rowRect = new Rect(0, row.Top, Width, row.Height);

            // Transparent fill ensures pointer hit-testing works across the entire row surface.
            context.FillRectangle(Brushes.Transparent, rowRect);

            if (row.IsPlaceholder)
            {
                context.FillRectangle(_placeholderBrush, rowRect);
                if (ShouldRenderSkeletons)
                {
                    DrawSkeletonRow(context, row);
                }
            }

            if (row.IsSummary)
            {
                context.FillRectangle(_summaryBrush, rowRect);
            }

            if (row.IsSelected)
            {
                context.FillRectangle(_selectionBrush, rowRect);
            }

            if (_owner?.IsKeyboardFocusWithin == true && row.IsSelected && _focusPen is not null)
            {
                var focusRect = rowRect.Deflate(0.5);
                context.DrawRectangle(_focusPen, focusRect);
            }

            if (row.HasChildren)
            {
                DrawToggle(context, row.ToggleRect, row.IsExpanded);
            }

            foreach (var cell in row.Cells)
            {
                if (cell.Widget is { } widget)
                {
                    widget.Draw(context);
                }
                else if (cell.FormattedText is { } formatted)
                {
                    context.DrawText(formatted, cell.TextOrigin);
                }

                DrawCellValidation(context, cell.Bounds, cell.ValidationState);
            }

            DrawRowValidation(context, row);

            // Horizontal separator
            context.DrawLine(_gridPen, new Point(0, row.Top + row.Height), new Point(Width, row.Top + row.Height));
        }

        // Vertical separators
        foreach (var offset in _columnOffsets)
        {
            if (offset <= 0 || offset >= Width)
            {
                continue;
            }

            context.DrawLine(_gridPen, new Point(offset, 0), new Point(offset, Height));
        }

        foreach (var overlay in _overlayOrder)
        {
            overlay.Widget.Draw(context);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_owner is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this).Position;

        // Always attempt to regain focus when the presenter receives pointer input so keyboard navigation keeps working.
        if (!IsFocused && Focusable && IsEffectivelyEnabled)
        {
            Focus();
        }

        if (RoutePointerEvent(point, e, WidgetPointerEventKind.Pressed))
        {
            e.Handled = true;
            return;
        }

        if (HandlePointer(point, e))
        {
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var point = e.GetCurrentPoint(this).Position;

        if (_pointerCapturedWidget is not null)
        {
            RoutePointerToWidget(_pointerCapturedWidget, point, e, WidgetPointerEventKind.Moved);
            e.Handled = true;
            return;
        }

        var widget = HitTestWidget(point, out _);
        if (!ReferenceEquals(widget, _pointerOverWidget))
        {
            if (_pointerOverWidget is not null)
            {
                RoutePointerToWidget(_pointerOverWidget, point, e, WidgetPointerEventKind.Exited);
            }

            _pointerOverWidget = widget;
            if (widget is not null)
            {
                RoutePointerToWidget(widget, point, e, WidgetPointerEventKind.Entered);
            }
        }

        if (widget is not null)
        {
            RoutePointerToWidget(widget, point, e, WidgetPointerEventKind.Moved);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var point = e.GetCurrentPoint(this).Position;

        if (_pointerCapturedWidget is not null)
        {
            RoutePointerToWidget(_pointerCapturedWidget, point, e, WidgetPointerEventKind.Released);
            e.Pointer.Capture(null);
            _pointerCapturedWidget = null;
            e.Handled = true;
            return;
        }

        if (RoutePointerEvent(point, e, WidgetPointerEventKind.Released))
        {
            e.Handled = true;
        }
    }

    private void PresenterOnPointerLeave(object? sender, PointerEventArgs e)
    {
        if (_pointerCapturedWidget is not null)
        {
            RoutePointerToWidget(_pointerCapturedWidget, e.GetCurrentPoint(this).Position, e, WidgetPointerEventKind.Cancelled);
            _pointerCapturedWidget = null;
        }

        if (_pointerOverWidget is not null)
        {
            RoutePointerToWidget(_pointerOverWidget, e.GetCurrentPoint(this).Position, e, WidgetPointerEventKind.Exited);
            _pointerOverWidget = null;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (_pointerCapturedWidget is not null)
        {
            var origin = new Point(_pointerCapturedWidget.Bounds.X, _pointerCapturedWidget.Bounds.Y);
            RoutePointerToWidget(_pointerCapturedWidget, origin, null, WidgetPointerEventKind.CaptureLost);
            _pointerCapturedWidget = null;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        using var overlayScope = WidgetOverlayManager.PushCurrentHost(this);

        if (_focusedWidget is not null)
        {
            var handled = _focusedWidget.HandleKeyboardEvent(new WidgetKeyboardEvent(WidgetKeyboardEventKind.KeyDown, e));
            if (handled)
            {
                e.Handled = true;
            }
        }

        if (!e.Handled && e.Key == Key.Escape && TryDismissTopOverlay(escapeOnly: true))
        {
            e.Handled = true;
        }

        if (!e.Handled && _owner is not null && _owner.HandlePresenterKeyDown(e))
        {
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        using var overlayScope = WidgetOverlayManager.PushCurrentHost(this);

        if (_focusedWidget is not null)
        {
            var handled = _focusedWidget.HandleKeyboardEvent(new WidgetKeyboardEvent(WidgetKeyboardEventKind.KeyUp, e));
            if (handled)
            {
                e.Handled = true;
            }
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        using var overlayScope = WidgetOverlayManager.PushCurrentHost(this);

        if (_focusedWidget is not null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(e.Text) && _owner is not null && _owner.HandlePresenterTextInput(e.Text))
        {
            e.Handled = true;
        }
    }

    internal bool HandlePointer(Point point, PointerPressedEventArgs e)
    {
        var row = HitTestRow(point);
        if (row is null)
        {
            return false;
        }

        var pointerProps = e.GetCurrentPoint(this).Properties;
        if (!pointerProps.IsLeftButtonPressed)
        {
            return false;
        }

        var toggleHit = HitTestToggle(row, point);
        _owner?.HandlePresenterPointerPressed(row, point, e.ClickCount, toggleHit, e.KeyModifiers);
        return true;
    }

    private RowRenderInfo? HitTestRow(Point point)
    {
        foreach (var row in _rows)
        {
            if (point.Y >= row.Top && point.Y < row.Top + row.Height)
            {
                return row;
            }
        }

        return null;
    }

    private Widget? HitTestWidget(Point point, out RowRenderInfo? rowInfo)
    {
        if (HitTestOverlay(point) is { } overlayEntry)
        {
            rowInfo = null;
            return overlayEntry.Widget;
        }

        foreach (var row in _rows)
        {
            if (point.Y < row.Top || point.Y >= row.Top + row.Height)
            {
                continue;
            }

            if (row.IsPlaceholder)
            {
                rowInfo = row;
                return null;
            }

            foreach (var cell in row.Cells)
            {
                if (cell.Widget is { } widget && widget.SupportsPointerInput && widget.IsEnabled && widget.Bounds.Contains(point))
                {
                    rowInfo = row;
                    return widget;
                }
            }

            rowInfo = row;
            return null;
        }

        rowInfo = null;
        return null;
    }

    private OverlayEntry? HitTestOverlay(Point point)
    {
        for (var i = _overlayOrder.Count - 1; i >= 0; i--)
        {
            var overlay = _overlayOrder[i];
            if (overlay.Bounds.Contains(point))
            {
                return overlay;
            }
        }

        return null;
    }

    private bool RoutePointerEvent(Point point, PointerEventArgs args, WidgetPointerEventKind kind)
    {
        if (_pointerCapturedWidget is not null)
        {
            if (kind == WidgetPointerEventKind.Pressed)
            {
                return true;
            }

            RoutePointerToWidget(_pointerCapturedWidget, point, args, kind);
            return true;
        }

        var widget = HitTestWidget(point, out var rowInfo);
        var isOverlay = widget is not null && _overlayMap.ContainsKey(widget);

        if (!isOverlay && rowInfo is { IsPlaceholder: true })
        {
            return false;
        }

        if (widget is null)
        {
            if (kind == WidgetPointerEventKind.Pressed)
            {
                DismissPointerDismissibleOverlays(point);
            }

            return false;
        }

        if (kind == WidgetPointerEventKind.Pressed && !ReferenceEquals(_pointerOverWidget, widget))
        {
            RoutePointerToWidget(widget, point, args, WidgetPointerEventKind.Entered);
            _pointerOverWidget = widget;
        }

        var handled = RoutePointerToWidget(widget, point, args, kind);

        if (kind == WidgetPointerEventKind.Pressed && handled)
        {
            _pointerCapturedWidget = widget;
            _focusedWidget = widget.SupportsKeyboardInput || widget.IsInteractive ? widget : null;
            args.Pointer.Capture(this);
        }

        if (kind == WidgetPointerEventKind.Pressed)
        {
            _pointerOverWidget = widget;

            if (!isOverlay)
            {
                DismissPointerDismissibleOverlays(point);
            }
        }

        return handled;
    }

    private void DismissPointerDismissibleOverlays(Point point)
    {
        if (_overlayOrder.Count == 0)
        {
            return;
        }

        for (var i = _overlayOrder.Count - 1; i >= 0; i--)
        {
            var entry = _overlayOrder[i];
            if (!entry.Options.CloseOnPointerDownOutside)
            {
                continue;
            }

            if (entry.Bounds.Contains(point))
            {
                continue;
            }

            RemoveOverlay(entry);
        }
    }

    private bool TryDismissTopOverlay(bool escapeOnly)
    {
        for (var i = _overlayOrder.Count - 1; i >= 0; i--)
        {
            var entry = _overlayOrder[i];
            if (escapeOnly && !entry.Options.CloseOnEscape)
            {
                continue;
            }

            RemoveOverlay(entry);
            return true;
        }

        return false;
    }

    private bool RoutePointerToWidget(Widget widget, Point point, PointerEventArgs? args, WidgetPointerEventKind kind)
    {
        var local = new Point(point.X - widget.Bounds.X, point.Y - widget.Bounds.Y);
        var evt = new WidgetPointerEvent(kind, local, args);
        using var overlayScope = WidgetOverlayManager.PushCurrentHost(this);
        var handled = widget.HandlePointerEvent(evt);
        if (handled && kind != WidgetPointerEventKind.Moved)
        {
            InvalidateVisual();
        }

        return handled;
    }

    bool IWidgetOverlayHost.ShowOverlay(Widget widget, Rect anchor, WidgetOverlayPlacement placement, WidgetOverlayOptions options)
    {
        return ShowOverlayInternal(widget, anchor, placement, options ?? new WidgetOverlayOptions());
    }

    bool IWidgetOverlayHost.HideOverlay(Widget widget)
    {
        return HideOverlayInternal(widget);
    }

    void IWidgetOverlayHost.HideOwnedOverlays(Widget owner)
    {
        if (owner is null)
        {
            return;
        }

        RemoveOwnedOverlaysExcept(owner, preserve: null);
    }

    private static bool HitTestToggle(RowRenderInfo row, Point point)
    {
        if (!row.HasChildren)
        {
            return false;
        }

        return row.ToggleRect.Contains(point);
    }

    private void OnOwnerResourcesChanged(object? sender, ResourcesChangedEventArgs e) =>
        ApplyThemeResources();

    private void ApplyThemeResources()
    {
        if (_owner is null)
        {
            return;
        }

        _selectionBrush = ResolveBrush(_owner, "FastTreeDataGrid.SelectionBrush", _selectionBrush);
        _placeholderBrush = ResolveBrush(_owner, "FastTreeDataGrid.PlaceholderBrush", _placeholderBrush);
        _toggleGlyphBrush = ResolveBrush(_owner, "FastTreeDataGrid.ToggleGlyphBrush", _toggleGlyphBrush);

        var gridPen = ResolvePen(_owner, "FastTreeDataGrid.GridLinePen", _gridPen);
        if (gridPen is not null)
        {
            _gridPen = gridPen;
        }

        _skeletonBarBrush = ResolveBrush(_owner, "FastTreeDataGrid.SkeletonBrush", _skeletonBarBrush);
        _summaryBrush = ResolveBrush(_owner, "FastTreeDataGrid.SummaryBrush", _summaryBrush);
        _validationErrorBrush = ResolveBrush(_owner, "FastTreeDataGrid.ValidationErrorBrush", _validationErrorBrush);
        _validationWarningBrush = ResolveBrush(_owner, "FastTreeDataGrid.ValidationWarningBrush", _validationWarningBrush);
        _focusPen = ResolvePen(_owner, "FastTreeDataGrid.FocusBorderPen", _focusPen);
    }

    private static IBrush ResolveBrush(StyledElement element, string resourceKey, IBrush current)
    {
        if (element.TryFindResource(resourceKey, out var resource))
        {
            return resource switch
            {
                IBrush brush => brush,
                Color color => new SolidColorBrush(color),
                _ => current,
            };
        }

        return current;
    }

    private static IPen? ResolvePen(StyledElement element, string resourceKey, IPen? current)
    {
        if (element.TryFindResource(resourceKey, out var resource))
        {
            return resource switch
            {
                IPen pen => pen,
                IBrush brush => new Pen(brush, current?.Thickness ?? 1),
                Color color => new Pen(new SolidColorBrush(color), current?.Thickness ?? 1),
                _ => current,
            };
        }

        return current;
    }

    private void DrawToggle(DrawingContext context, Rect rect, bool isExpanded)
    {
        if (rect == default)
        {
            return;
        }

        var glyph = isExpanded ? s_expandedGlyph : s_collapsedGlyph;
        var bounds = glyph.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(rect.Width / bounds.Width, rect.Height / bounds.Height);
        if (scale <= 0)
        {
            return;
        }

        scale *= 0.85;
        var targetWidth = bounds.Width * scale;
        var targetHeight = bounds.Height * scale;

        var offsetX = rect.X + (rect.Width - targetWidth) / 2;
        var offsetY = rect.Y + (rect.Height - targetHeight) / 2;

        var transform = Matrix.CreateTranslation(-bounds.X, -bounds.Y)
                       * Matrix.CreateScale(scale, scale)
                       * Matrix.CreateTranslation(offsetX, offsetY);

        using var glyphTransform = context.PushTransform(transform);
        context.DrawGeometry(_toggleGlyphBrush, null, glyph);
    }

    private bool ShowOverlayInternal(Widget widget, Rect anchor, WidgetOverlayPlacement placement, WidgetOverlayOptions options)
    {
        if (options.Owner is { } owner)
        {
            RemoveOwnedOverlaysExcept(owner, widget);
        }

        if (!_overlayMap.TryGetValue(widget, out var entry))
        {
            entry = new OverlayEntry(widget);
            _overlayMap[widget] = entry;
            _overlayOrder.Add(entry);
        }

        entry.Anchor = anchor;
        entry.Placement = placement;
        entry.Options = options;

        ArrangeOverlay(entry);

        var wasOpen = entry.IsOpen;
        entry.IsOpen = true;

        if (!wasOpen)
        {
            entry.Options.OnOpened?.Invoke(widget);
        }

        InvalidateVisual();
        return true;
    }

    private bool HideOverlayInternal(Widget widget)
    {
        if (!_overlayMap.TryGetValue(widget, out var entry))
        {
            return false;
        }

        RemoveOverlay(entry);
        return true;
    }

    private void ClearOverlays()
    {
        if (_overlayOrder.Count == 0)
        {
            return;
        }

        for (var i = _overlayOrder.Count - 1; i >= 0; i--)
        {
            RemoveOverlay(_overlayOrder[i]);
        }
    }

    private void RemoveOverlay(OverlayEntry entry)
    {
        _overlayMap.Remove(entry.Widget);
        _overlayOrder.Remove(entry);

        if (ReferenceEquals(_pointerCapturedWidget, entry.Widget))
        {
            _pointerCapturedWidget = null;
        }

        if (ReferenceEquals(_pointerOverWidget, entry.Widget))
        {
            _pointerOverWidget = null;
        }

        if (ReferenceEquals(_focusedWidget, entry.Widget))
        {
            _focusedWidget = null;
        }

        entry.IsOpen = false;
        entry.Options.OnClosed?.Invoke(entry.Widget);
        InvalidateVisual();
    }

    private void RemoveOwnedOverlaysExcept(Widget owner, Widget? preserve)
    {
        for (var i = _overlayOrder.Count - 1; i >= 0; i--)
        {
            var entry = _overlayOrder[i];
            if (!ReferenceEquals(entry.Options.Owner, owner))
            {
                continue;
            }

            if (preserve is not null && ReferenceEquals(entry.Widget, preserve))
            {
                continue;
            }

            RemoveOverlay(entry);
        }
    }

    private void ArrangeOverlay(OverlayEntry entry)
    {
        var hostSize = GetHostSize();
        var size = EstimateOverlaySize(entry, hostSize);
        var offset = entry.Options.Offset;

        var left = entry.Placement switch
        {
            WidgetOverlayPlacement.RightStart => entry.Anchor.Right + offset.Left,
            _ => entry.Anchor.X + offset.Left
        };

        var top = entry.Placement switch
        {
            WidgetOverlayPlacement.RightStart => entry.Anchor.Y + offset.Top,
            _ => entry.Anchor.Bottom + offset.Top
        };

        if (left + size.Width > hostSize.Width)
        {
            left = Math.Max(0, hostSize.Width - size.Width);
        }

        if (top + size.Height > hostSize.Height)
        {
            top = Math.Max(0, hostSize.Height - size.Height);
        }

        left = Math.Max(0, left);
        top = Math.Max(0, top);

        var rect = new Rect(left, top, size.Width, size.Height);
        entry.Bounds = rect;

        entry.Widget.Arrange(rect);

        if (entry.Widget is IVirtualizingWidgetHost virtualizing)
        {
            virtualizing.UpdateViewport(new VirtualizingWidgetViewport(rect.Size, new Point(0, 0)));
        }
    }

    private void DrawCellValidation(DrawingContext context, Rect bounds, FastTreeDataGridCellValidationState validation)
    {
        if (validation.Level == FastTreeDataGridValidationLevel.None)
        {
            return;
        }

        var brush = validation.Level switch
        {
            FastTreeDataGridValidationLevel.Error => _validationErrorBrush,
            FastTreeDataGridValidationLevel.Warning => _validationWarningBrush,
            _ => null,
        };

        if (brush is null)
        {
            return;
        }

        var size = Math.Min(ValidationBadgeSize, Math.Min(bounds.Width, bounds.Height) / 2);
        if (size <= 0)
        {
            return;
        }

        var badgeRect = new Rect(bounds.Right - size - 2, bounds.Top + 2, size, size);
        context.FillRectangle(brush, badgeRect);
    }

    private void DrawSkeletonRow(DrawingContext context, RowRenderInfo row)
    {
        if (row.Cells.Count == 0)
        {
            return;
        }

        foreach (var cell in row.Cells)
        {
            var bounds = cell.ContentBounds;
            if (bounds.Width <= 16 || bounds.Height <= 12)
            {
                continue;
            }

            var barWidth = Math.Min(bounds.Width - 8, Math.Max(24, bounds.Width * 0.65));
            var barHeight = 6;
            var spacing = 6;
            var step = barHeight + spacing;
            var availableHeight = Math.Max(0, bounds.Height - 12);
            var maxBars = Math.Max(1, Math.Min(3, (int)Math.Floor(availableHeight / step)));
            var y = bounds.Y + 6;

            for (var i = 0; i < maxBars; i++)
            {
                if (y + barHeight > bounds.Bottom - 4)
                {
                    break;
                }

                var rect = new Rect(bounds.X + 4, y, barWidth, barHeight);
                context.FillRectangle(_skeletonBarBrush, rect);
                y += step;
            }
        }
    }

    private void DrawRowValidation(DrawingContext context, RowRenderInfo row)
    {
        var validation = row.Validation;
        if (!validation.HasErrors && !validation.HasWarnings)
        {
            return;
        }

        var brush = validation.HasErrors ? _validationErrorBrush : _validationWarningBrush;
        var rect = new Rect(0, row.Top, 3, row.Height);
        context.FillRectangle(brush, rect);
    }

    private Size EstimateOverlaySize(OverlayEntry entry, Size hostSize)
    {
        var widget = entry.Widget;
        var width = widget.DesiredWidth;
        var height = widget.DesiredHeight;

        if (entry.Options.MatchWidthToAnchor)
        {
            width = entry.Anchor.Width;
        }

        if (double.IsNaN(width) || width <= 0)
        {
            width = Math.Max(entry.Anchor.Width, 220);
        }

        if (double.IsNaN(height) || height <= 0)
        {
            var estimated = EstimateWidgetSize(widget);
            width = Math.Max(width, estimated.Width);
            height = estimated.Height;
        }

        if (entry.Options.MaxWidth.HasValue)
        {
            width = Math.Min(width, entry.Options.MaxWidth.Value);
        }

        if (entry.Options.MaxHeight.HasValue)
        {
            height = Math.Min(height, entry.Options.MaxHeight.Value);
        }

        width = Math.Min(width, hostSize.Width);
        height = Math.Min(height, hostSize.Height);

        return new Size(Math.Max(1, width), Math.Max(1, height));
    }

    private Size EstimateWidgetSize(Widget widget, int depth = 0)
    {
        if (depth > 6)
        {
            return new Size(200, 200);
        }

        var width = widget.DesiredWidth;
        var height = widget.DesiredHeight;

        switch (widget)
        {
            case MenuWidget menu:
                var menuSize = EstimateMenuSize(menu);
                if (double.IsNaN(width) || width <= 0)
                {
                    width = menuSize.Width;
                }

                if (double.IsNaN(height) || height <= 0)
                {
                    height = menuSize.Height;
                }

                break;
            case BorderWidget border:
                var childSize = border.Child is Widget child
                    ? EstimateWidgetSize(child, depth + 1)
                    : new Size(0, 0);

                var horizontalPadding = border.Padding.Left + border.Padding.Right
                                        + border.BorderThickness.Left + border.BorderThickness.Right;
                var verticalPadding = border.Padding.Top + border.Padding.Bottom
                                      + border.BorderThickness.Top + border.BorderThickness.Bottom;

                if (double.IsNaN(width) || width <= 0)
                {
                    width = childSize.Width + horizontalPadding;
                }

                if (double.IsNaN(height) || height <= 0)
                {
                    height = childSize.Height + verticalPadding;
                }

                break;
            case SurfaceWidget surface:
                double maxWidth = 0;
                double maxHeight = 0;
                foreach (var surfaceChild in surface.Children)
                {
                    if (surfaceChild is not Widget childWidget)
                    {
                        continue;
                    }

                    var surfaceChildSize = EstimateWidgetSize(childWidget, depth + 1);
                    maxWidth = Math.Max(maxWidth, surfaceChildSize.Width);
                    maxHeight = Math.Max(maxHeight, surfaceChildSize.Height);
                }

                if (double.IsNaN(width) || width <= 0)
                {
                    width = maxWidth;
                }

                if (double.IsNaN(height) || height <= 0)
                {
                    height = maxHeight;
                }

                break;
        }

        if (double.IsNaN(width) || width <= 0)
        {
            width = Math.Max(180, widget.Bounds.Width > 0 ? widget.Bounds.Width : 220);
        }

        if (double.IsNaN(height) || height <= 0)
        {
            height = Math.Max(48, widget.Bounds.Height > 0 ? widget.Bounds.Height : 180);
        }

        return new Size(width, height);
    }

    private Size EstimateMenuSize(MenuWidget menu)
    {
        var itemCount = menu.VisibleItemCount;
        var extent = menu.ItemExtent;
        var spacing = menu.Spacing;
        var padding = menu.Padding;

        if (itemCount <= 0)
        {
            itemCount = 1;
        }

        var height = padding.Top + padding.Bottom + itemCount * extent + Math.Max(0, itemCount - 1) * spacing;
        var width = menu.DesiredWidth;

        if (double.IsNaN(width) || width <= 0)
        {
            width = Math.Max(menu.Bounds.Width, 220);
        }

        return new Size(width, height);
    }

    private Size GetHostSize()
    {
        var width = Bounds.Width;
        if (double.IsNaN(width) || width <= 0)
        {
            width = Width;
        }

        if (double.IsNaN(width) || width <= 0)
        {
            width = 1;
        }

        var height = Bounds.Height;
        if (double.IsNaN(height) || height <= 0)
        {
            height = Height;
        }

        if (double.IsNaN(height) || height <= 0)
        {
            height = 1;
        }

        return new Size(width, height);
    }

    private sealed class OverlayEntry
    {
        public OverlayEntry(Widget widget)
        {
            Widget = widget ?? throw new ArgumentNullException(nameof(widget));
        }

        public Widget Widget { get; }
        public Rect Anchor { get; set; }
        public Rect Bounds { get; set; }
        public WidgetOverlayPlacement Placement { get; set; }
        public WidgetOverlayOptions Options { get; set; } = new();
        public bool IsOpen { get; set; }
    }

    internal sealed class RowRenderInfo
    {
        public RowRenderInfo(
            FastTreeDataGridRow row,
            int rowIndex,
            double top,
            double height,
            bool isSelected,
            bool hasChildren,
            bool isExpanded,
            Rect toggleRect,
            bool isGroup,
            bool isSummary,
            bool isPlaceholder)
        {
            Row = row;
            RowIndex = rowIndex;
            Top = top;
            Height = height;
            IsSelected = isSelected;
            HasChildren = hasChildren;
            IsExpanded = isExpanded;
            ToggleRect = toggleRect;
            IsGroup = isGroup;
            IsSummary = isSummary;
            IsPlaceholder = isPlaceholder;
            Validation = FastTreeDataGridRowValidationState.None;
        }

        public FastTreeDataGridRow Row { get; }
        public int RowIndex { get; }
        public double Top { get; }
        public double Height { get; }
        public bool IsSelected { get; set; }
        public bool HasChildren { get; }
        public bool IsExpanded { get; }
        public Rect ToggleRect { get; }
        public Rect? ToggleBounds { get; set; }
        public bool IsGroup { get; }
        public bool IsSummary { get; }
        public bool IsPlaceholder { get; }
        public List<CellRenderInfo> Cells { get; } = new();
        public FastTreeDataGridRowValidationState Validation { get; set; }
    }

    internal sealed class CellRenderInfo
    {
        public CellRenderInfo(FastTreeDataGridColumn column, Rect bounds, Rect contentBounds, Widget? widget, FormattedText? formattedText, Point textOrigin, AvaloniaControl? control, FastTreeDataGridCellValidationState validationState)
        {
            Column = column;
            Bounds = bounds;
            ContentBounds = contentBounds;
            Widget = widget;
            FormattedText = formattedText;
            TextOrigin = textOrigin;
            Control = control;
            ValidationState = validationState;
        }

        public FastTreeDataGridColumn Column { get; }
        public Rect Bounds { get; }
        public Rect ContentBounds { get; }
        public Widget? Widget { get; }
        public FormattedText? FormattedText { get; }
        public Point TextOrigin { get; }
        public AvaloniaControl? Control { get; }
        public FastTreeDataGridCellValidationState ValidationState { get; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(Width) && Width > 0 ? Width : availableSize.Width;
        var height = double.IsFinite(Height) && Height > 0 ? Height : availableSize.Height;

        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            width = 0;
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            height = 0;
        }

        if (_controlLayouts.Count > 0)
        {
            foreach (var pair in _controlLayouts)
            {
                var bounds = pair.Value;
                var measure = new Size(Math.Max(0, bounds.Width), Math.Max(0, bounds.Height));
                pair.Key.Measure(measure);
            }
        }

        if (_editingControl is { } editing)
        {
            var bounds = _editingControlBounds;
            var measure = new Size(Math.Max(0, bounds.Width), Math.Max(0, bounds.Height));
            editing.Measure(measure);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = double.IsFinite(Width) && Width > 0 ? Width : finalSize.Width;
        var height = double.IsFinite(Height) && Height > 0 ? Height : finalSize.Height;

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            width = double.IsNaN(finalSize.Width) || double.IsInfinity(finalSize.Width) || finalSize.Width < 0
                ? 0
                : finalSize.Width;
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            height = double.IsNaN(finalSize.Height) || double.IsInfinity(finalSize.Height) || finalSize.Height < 0
                ? 0
                : finalSize.Height;
        }

        if (_controlLayouts.Count > 0)
        {
            foreach (var pair in _controlLayouts)
            {
                var bounds = pair.Value;
                pair.Key.Arrange(bounds);
            }
        }

        if (_editingControl is { } editing)
        {
            editing.Arrange(_editingControlBounds);
        }

        return new Size(width, height);
    }

    private void OnProviderRowMaterialized(object? sender, FastTreeDataGridRowMaterializedEventArgs e)
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    private void OnProviderCountChanged(object? sender, FastTreeDataGridCountChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }
}
