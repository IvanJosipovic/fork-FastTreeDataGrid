using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using AvaloniaControl = global::Avalonia.Controls.Control;

namespace FastTreeDataGrid.Control.Widgets.Hosting;

/// <summary>
/// Provides a simple factory for embedding immediate-mode widgets inside standard Avalonia layouts.
/// </summary>
public static class WidgetHost
{
    /// <summary>
    /// Creates an Avalonia control that can render and interact with the specified widget.
    /// </summary>
    /// <param name="root">The widget tree to host.</param>
    /// <param name="width">Optional fixed width for the host surface. Leave as <see cref="double.NaN"/> for auto.</param>
    /// <param name="height">Optional fixed height for the host surface. Leave as <see cref="double.NaN"/> for auto.</param>
    /// <param name="background">Optional background brush rendered behind the widget.</param>
    /// <returns>An Avalonia <see cref="AvaloniaControl"/> ready to embed in the visual tree.</returns>
    public static AvaloniaControl Create(Widget root, double width = double.NaN, double height = double.NaN, IBrush? background = null)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        var surface = new WidgetHostSurface(root)
        {
            Background = background
        };

        surface.Width = width;
        surface.Height = height;

        // Ensure the widget has initial layout data before it renders.
        var measureWidth = double.IsNaN(width) ? double.PositiveInfinity : width;
        var measureHeight = double.IsNaN(height) ? double.PositiveInfinity : height;
        root.Measure(new Size(measureWidth, measureHeight));

        if (!double.IsNaN(width) && !double.IsNaN(height))
        {
            root.Arrange(new Rect(0, 0, width, height));
        }

        return surface;
    }
}

/// <summary>
/// Content presenter that safely embeds controls created by <see cref="WidgetHost.Create"/>.
/// </summary>
public sealed class WidgetHostPresenter : ContentControl
{
    public static readonly StyledProperty<AvaloniaControl?> HostProperty =
        AvaloniaProperty.Register<WidgetHostPresenter, AvaloniaControl?>(nameof(Host));

    public static readonly StyledProperty<Widget?> WidgetRootProperty =
        AvaloniaProperty.Register<WidgetHostPresenter, Widget?>(nameof(WidgetRoot));

    /// <summary>
    /// Gets or sets the hosted control (typically the result of <see cref="WidgetHost.Create"/>).
    /// </summary>
    public AvaloniaControl? Host
    {
        get => GetValue(HostProperty);
        set => SetValue(HostProperty, value);
    }

    /// <summary>
    /// Gets or sets the root widget tree to host. When specified, <see cref="WidgetHost.Create(Widget, double, double, IBrush?)"/> is used to generate the surface automatically.
    /// </summary>
    [Content]
    public Widget? WidgetRoot
    {
        get => GetValue(WidgetRootProperty);
        set => SetValue(WidgetRootProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HostProperty)
        {
            UpdateSurface();
        }
        else if (change.Property == WidgetRootProperty)
        {
            UpdateWidgetRoot();
        }
    }

    private void UpdateSurface()
    {
        var host = Host;
        if (host is null)
        {
            Content = null;
            return;
        }

        if (ReferenceEquals(Content, host))
        {
            return;
        }

        if (host.Parent is StyledElement parent)
        {
            DetachFromParent(host, parent);
        }

        Content = host;
    }

    private void UpdateWidgetRoot()
    {
        if (WidgetRoot is { } widget)
        {
            Host = WidgetHost.Create(widget);
        }
        else if (GetValue(HostProperty) != null && WidgetRoot is null)
        {
            Host = null;
        }
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

/// <summary>
/// Avalonia control that renders a widget tree and forwards input events.
/// </summary>
public sealed class WidgetHostSurface : AvaloniaControl
{
    private readonly Widget _root;
    private bool _pointerCaptured;
    private IDisposable? _animationRegistration;

    public WidgetHostSurface(Widget root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
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

        _root.Draw(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var measured = _root.Measure(availableSize);
        if (!double.IsNaN(Width) && Width > 0)
        {
            measured = measured.WithWidth(Width);
        }

        if (!double.IsNaN(Height) && Height > 0)
        {
            measured = measured.WithHeight(Height);
        }

        return measured;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _root.Arrange(new Rect(finalSize));
        return finalSize;
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
