using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Metadata;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class SurfaceWidget : Widget
{
    private Widget? _pointerCapturedChild;
    private Widget? _pointerOverChild;
    private Widget? _focusedChild;

    [Content]
    public IList<Widget> Children { get; } = new List<Widget>();

    protected override Size MeasureCore(Size available)
    {
        double width = 0;
        double height = 0;

        foreach (var child in Children)
        {
            var childSize = child.Measure(available);
            width = Math.Max(width, childSize.Width);
            height = Math.Max(height, childSize.Height);
        }

        return new Size(width, height);
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        foreach (var child in Children)
        {
            child.Draw(context);
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);
        foreach (var child in Children)
        {
            child.UpdateValue(provider, item);
            if (child.Foreground is null && Foreground is not null)
            {
                child.Foreground = Foreground;
            }
        }
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Entered:
                handled |= HandlePointerEntered(e);
                break;
            case WidgetPointerEventKind.Moved:
                handled |= HandlePointerMoved(e);
                break;
            case WidgetPointerEventKind.Pressed:
                handled |= HandlePointerPressed(e);
                break;
            case WidgetPointerEventKind.Released:
                handled |= HandlePointerReleased(e);
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                handled |= HandlePointerCancelled(e);
                break;
            case WidgetPointerEventKind.Exited:
                handled |= HandlePointerExited(e);
                break;
        }

        return handled;
    }

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        var handled = base.HandleKeyboardEvent(e);

        if (_focusedChild is { } focused)
        {
            if (focused.HandleKeyboardEvent(e))
            {
                handled = true;
            }

            return handled;
        }

        foreach (var child in Children)
        {
            if (!child.SupportsKeyboardInput)
            {
                continue;
            }

            if (child.HandleKeyboardEvent(e))
            {
                handled = true;
                break;
            }
        }

        return handled;
    }

    public override bool HandleTextInput(string text)
    {
        var handled = base.HandleTextInput(text);

        if (string.IsNullOrEmpty(text))
        {
            return handled;
        }

        if (_focusedChild is { } focused && focused.SupportsTextInput)
        {
            if (focused.HandleTextInput(text))
            {
                handled = true;
            }

            return handled;
        }

        foreach (var child in Children)
        {
            if (!child.SupportsTextInput)
            {
                continue;
            }

            if (child.HandleTextInput(text))
            {
                handled = true;
                break;
            }
        }

        return handled;
    }

    private bool HandlePointerEntered(in WidgetPointerEvent e)
    {
        var target = HitTestChild(e.Position);
        return UpdatePointerOver(e, target);
    }

    private bool HandlePointerMoved(in WidgetPointerEvent e)
    {
        if (_pointerCapturedChild is not null)
        {
            return RoutePointerToChild(_pointerCapturedChild, e, WidgetPointerEventKind.Moved);
        }

        var target = HitTestChild(e.Position);
        var handled = UpdatePointerOver(e, target);

        if (_pointerOverChild is not null)
        {
            handled |= RoutePointerToChild(_pointerOverChild, e, WidgetPointerEventKind.Moved);
        }

        return handled;
    }

    private bool HandlePointerPressed(in WidgetPointerEvent e)
    {
        var target = HitTestChild(e.Position) ?? _pointerOverChild;
        var handled = UpdatePointerOver(e, target);

        if (target is not null)
        {
            var childHandled = RoutePointerToChild(target, e, WidgetPointerEventKind.Pressed);
            if (childHandled)
            {
                SetFocusedChild(target);
                _pointerCapturedChild = target;
            }
            else if (!ReferenceEquals(_focusedChild, target))
            {
                ClearFocus();
            }

            handled |= childHandled;
        }
        else
        {
            if (_focusedChild is not null)
            {
                ClearFocus();
            }
        }

        return handled;
    }

    private bool HandlePointerReleased(in WidgetPointerEvent e)
    {
        var target = _pointerCapturedChild ?? HitTestChild(e.Position) ?? _pointerOverChild;
        var handled = false;

        if (target is not null)
        {
            handled = RoutePointerToChild(target, e, WidgetPointerEventKind.Released);
        }

        _pointerCapturedChild = null;

        var next = HitTestChild(e.Position);
        handled |= UpdatePointerOver(e, next);

        return handled;
    }

    private bool HandlePointerCancelled(in WidgetPointerEvent e)
    {
        var handled = false;

        if (_pointerCapturedChild is not null)
        {
            handled |= RoutePointerToChild(_pointerCapturedChild, e, e.Kind);
            _pointerCapturedChild = null;
        }

        if (_pointerOverChild is not null)
        {
            handled |= RoutePointerToChild(_pointerOverChild, e, WidgetPointerEventKind.Exited);
            _pointerOverChild = null;
        }

        return handled;
    }

    internal void ClearFocus()
    {
        if (_focusedChild is null)
        {
            return;
        }

        switch (_focusedChild)
        {
            case SurfaceWidget surface:
                surface.ClearFocus();
                break;
            case TextInputWidget input:
                input.Defocus();
                break;
        }

        _focusedChild = null;
    }

    private void SetFocusedChild(Widget child)
    {
        if (ReferenceEquals(_focusedChild, child))
        {
            return;
        }

        if (child is not SurfaceWidget && !child.SupportsKeyboardInput && !child.SupportsTextInput)
        {
            ClearFocus();
            return;
        }

        ClearFocus();
        _focusedChild = child;
    }

    private bool HandlePointerExited(in WidgetPointerEvent e)
    {
        var handled = false;

        if (_pointerCapturedChild is not null)
        {
            handled |= RoutePointerToChild(_pointerCapturedChild, e, WidgetPointerEventKind.Cancelled);
            _pointerCapturedChild = null;
        }

        if (_pointerOverChild is not null)
        {
            handled |= RoutePointerToChild(_pointerOverChild, e, WidgetPointerEventKind.Exited);
            _pointerOverChild = null;
        }

        return handled;
    }

    private Rect GetChildLocalBounds(Widget child)
    {
        var childBounds = child.Bounds;
        var offsetX = childBounds.X - Bounds.X;
        var offsetY = childBounds.Y - Bounds.Y;
        return new Rect(offsetX, offsetY, childBounds.Width, childBounds.Height);
    }

    private Widget? HitTestChild(Point position)
    {
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            var child = Children[i];
            if (!child.IsEnabled)
            {
                continue;
            }

            var supportsPointer = child.SupportsPointerInput || child is SurfaceWidget;
            if (!supportsPointer)
            {
                continue;
            }

            var childBounds = GetChildLocalBounds(child);
            if (childBounds.Contains(position))
            {
                return child;
            }
        }

        return null;
    }

    private bool UpdatePointerOver(in WidgetPointerEvent e, Widget? target)
    {
        if (ReferenceEquals(target, _pointerOverChild))
        {
            return false;
        }

        var handled = false;

        if (_pointerOverChild is not null)
        {
            handled |= RoutePointerToChild(_pointerOverChild, e, WidgetPointerEventKind.Exited);
        }

        _pointerOverChild = target;

        if (_pointerOverChild is not null)
        {
            handled |= RoutePointerToChild(_pointerOverChild, e, WidgetPointerEventKind.Entered);
        }

        return handled;
    }

    private bool RoutePointerToChild(Widget child, in WidgetPointerEvent e, WidgetPointerEventKind kind)
    {
        var childBounds = GetChildLocalBounds(child);
        var local = new Point(e.Position.X - childBounds.X, e.Position.Y - childBounds.Y);
        var forwarded = new WidgetPointerEvent(kind, local, e.Args);
        return child.HandlePointerEvent(forwarded);
    }
}
