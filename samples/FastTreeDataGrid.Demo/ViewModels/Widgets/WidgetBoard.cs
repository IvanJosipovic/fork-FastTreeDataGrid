using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Widgets;

public sealed class WidgetBoard
{
    public string Title { get; }

    public string Description { get; }

    public Avalonia.Controls.Control Board { get; }

    private WidgetBoard(string title, string description, Avalonia.Controls.Control board)
    {
        Title = title;
        Description = description;
        Board = board;
    }

    public static WidgetBoard Create(string title, string description, Widget root)
    {
        var surface = new WidgetSurface(root)
        {
            Width = 400,
            Height = 220,
            Background = Brushes.Transparent,
        };

        root.Arrange(new Rect(0, 0, surface.Width, surface.Height));

        return new WidgetBoard(title, description, surface);
    }
}

internal sealed class WidgetSurface : Avalonia.Controls.Control
{
    private readonly Widget _root;
    private bool _pointerCaptured;

    public WidgetSurface(Widget root)
    {
        _root = root;
        Focusable = true;
        IsHitTestVisible = true;
    }

    public IBrush? Background { get; set; }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Background is not null)
        {
            context.FillRectangle(Background, new Rect(Bounds.Size));
        }

        _root.Arrange(new Rect(new Point(0, 0), Bounds.Size));
        _root.Draw(context);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (RoutePointer(e, WidgetPointerEventKind.Entered))
        {
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointerCaptured || IsPointerOver)
        {
            var handled = RoutePointer(e, WidgetPointerEventKind.Moved);
            if (handled)
            {
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (RoutePointer(e, WidgetPointerEventKind.Pressed))
        {
            e.Handled = true;
            _pointerCaptured = true;
            e.Pointer.Capture(this);
            Focus();
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (RoutePointer(e, WidgetPointerEventKind.Released))
        {
            e.Handled = true;
        }

        if (_pointerCaptured)
        {
            e.Pointer.Capture(null);
            _pointerCaptured = false;
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (RoutePointer(e, WidgetPointerEventKind.Exited))
        {
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_pointerCaptured)
        {
            _root.HandlePointerEvent(new WidgetPointerEvent(WidgetPointerEventKind.Cancelled, default, null));
            InvalidateVisual();
            _pointerCaptured = false;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_root.HandleKeyboardEvent(new WidgetKeyboardEvent(WidgetKeyboardEventKind.KeyDown, e)))
        {
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (_root.HandleKeyboardEvent(new WidgetKeyboardEvent(WidgetKeyboardEventKind.KeyUp, e)))
        {
            e.Handled = true;
            InvalidateVisual();
        }
    }

    private bool RoutePointer(PointerEventArgs e, WidgetPointerEventKind kind)
    {
        _root.Arrange(new Rect(new Point(0, 0), Bounds.Size));
        var point = e.GetCurrentPoint(this).Position;
        var local = new Point(point.X - _root.Bounds.X, point.Y - _root.Bounds.Y);
        var evt = new WidgetPointerEvent(kind, local, e);
        var handled = _root.HandlePointerEvent(evt);
        if (handled && kind != WidgetPointerEventKind.Moved)
        {
            InvalidateVisual();
        }

        return handled;
    }
}
