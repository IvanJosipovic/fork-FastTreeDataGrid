using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using FastTreeDataGrid.Control.Widgets;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.DataSourcesDemo.ViewModels.Widgets;

public sealed class WidgetBoard
{
    public string Title { get; }

    public string Description { get; }

    public AvaloniaControl Board { get; }

    private WidgetBoard(string title, string description, AvaloniaControl board)
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

public sealed class WidgetBoardPresenter : ContentControl
{
    public static readonly StyledProperty<WidgetBoard?> BoardProperty =
        AvaloniaProperty.Register<WidgetBoardPresenter, WidgetBoard?>(nameof(Board));

    public WidgetBoard? Board
    {
        get => GetValue(BoardProperty);
        set => SetValue(BoardProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoardProperty)
        {
            UpdateSurface();
        }
    }

    private void UpdateSurface()
    {
        var board = Board;
        if (board is null)
        {
            Content = null;
            return;
        }

        var control = board.Board;
        if (ReferenceEquals(Content, control))
        {
            return;
        }

        if (control.Parent is { } parent)
        {
            DetachFromParent(control, parent);
        }

        Content = control;
    }

    private static void DetachFromParent(AvaloniaControl control, StyledElement parent)
    {
        switch (parent)
        {
            case ContentPresenter presenter when ReferenceEquals(presenter.Content, control):
                presenter.Content = null;
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, control):
                contentControl.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, control):
                decorator.Child = null;
                break;
            case Panel panel:
                panel.Children.Remove(control);
                break;
        }
    }
}

internal sealed class WidgetSurface : Avalonia.Controls.Control
{
    private readonly Widget _root;
    private bool _pointerCaptured;
    private IDisposable? _animationRegistration;

    public WidgetSurface(Widget root)
    {
        _root = root;
        Focusable = true;
        IsHitTestVisible = true;
    }

    public IBrush? Background { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _animationRegistration = WidgetAnimationFrameScheduler.RegisterHost(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _animationRegistration?.Dispose();
        _animationRegistration = null;
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using var scope = WidgetAnimationFrameScheduler.PushCurrentHost(this);

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
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_pointerCaptured)
        {
            RoutePointer(null, WidgetPointerEventKind.CaptureLost);
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

    private bool RoutePointer(PointerEventArgs? e, WidgetPointerEventKind kind, Point? positionOverride = null)
    {
        _root.Arrange(new Rect(new Point(0, 0), Bounds.Size));

        var point = positionOverride ?? (e?.GetCurrentPoint(this).Position ?? default);
        var local = new Point(point.X - _root.Bounds.X, point.Y - _root.Bounds.Y);
        var evt = new WidgetPointerEvent(kind, local, e);
        var handled = _root.HandlePointerEvent(evt);
        if (handled)
        {
            InvalidateVisual();
        }

        return handled;
    }
}
