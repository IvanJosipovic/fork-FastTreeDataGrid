using Avalonia;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Lightweight base for templated widgets that react to pointer interaction with click semantics.
/// Mirrors Avalonia's interactive control hierarchy while keeping the immediate-mode footprint minimal.
/// </summary>
public abstract class InteractiveTemplatedWidget : TemplatedWidget
{
    private bool _isPointerPressed;

    protected InteractiveTemplatedWidget()
    {
        IsInteractive = true;
    }

    /// <summary>
    /// Gets a value indicating whether the widget is currently tracking a pressed pointer.
    /// </summary>
    protected bool IsPointerPressed => _isPointerPressed;

    /// <summary>
    /// Called when the widget detects a pointer press inside its bounds.
    /// </summary>
    /// <param name="e">Pointer event data.</param>
    protected virtual void OnPointerPressed(in WidgetPointerEvent e)
    {
    }

    /// <summary>
    /// Called when the pointer is released. <paramref name="executedClick"/> indicates whether <see cref="OnClick"/> will run.
    /// </summary>
    /// <param name="executedClick">True when the release should trigger a click.</param>
    /// <param name="e">Pointer event data.</param>
    protected virtual void OnPointerReleased(bool executedClick, in WidgetPointerEvent e)
    {
    }

    /// <summary>
    /// Called when tracking is cancelled (capture lost or cancelled).
    /// </summary>
    protected virtual void OnPointerCancelled()
    {
    }

    /// <summary>
    /// Determines whether a click should be executed for the current pointer release.
    /// </summary>
    /// <param name="e">Pointer event data.</param>
    /// <returns>True when the release should trigger <see cref="OnClick"/>.</returns>
    protected virtual bool ShouldExecuteClick(in WidgetPointerEvent e)
    {
        return new Rect(0, 0, Bounds.Width, Bounds.Height).Contains(e.Position);
    }

    /// <summary>
    /// Invoked when a full click gesture (press + release) completes.
    /// </summary>
    protected virtual void OnClick()
    {
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsInteractive || !IsEnabled)
        {
            return handled;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Pressed:
                _isPointerPressed = true;
                OnPointerPressed(e);
                break;
            case WidgetPointerEventKind.Released:
                var wasPressed = _isPointerPressed;
                _isPointerPressed = false;
                var shouldClick = wasPressed && ShouldExecuteClick(e);
                OnPointerReleased(shouldClick, e);
                if (shouldClick)
                {
                    OnClick();
                }
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                if (_isPointerPressed)
                {
                    OnPointerCancelled();
                }
                _isPointerPressed = false;
                break;
        }

        return handled || IsInteractive;
    }
}
