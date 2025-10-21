using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class Widget
{
    private WidgetVisualState _visualState = WidgetVisualState.Normal;
    private bool _isEnabled = true;
    private bool _isPointerOver;
    private bool _isPressed;
    private static readonly object InstanceLock = new();
    private static readonly List<WeakReference<Widget>> Instances = new();

    static Widget()
    {
        WidgetStyleManager.ThemeChanged += _ => RefreshAllStyles();
    }

    protected Widget()
    {
        lock (InstanceLock)
        {
            Instances.Add(new WeakReference<Widget>(this));
        }

        WidgetStyleManager.Apply(this, _visualState);
    }

    public double X { get; set; }

    public double Y { get; set; }

    public double Rotation { get; set; }

    public ImmutableSolidColorBrush? Foreground { get; set; }

    public string? Key { get; set; }

    public double DesiredWidth { get; set; } = double.NaN;

    public double DesiredHeight { get; set; } = double.NaN;

    public bool ClipToBounds { get; set; } = true;

    public Rect Bounds { get; private set; }

    private string? _styleKey;

    public string? StyleKey
    {
        get => _styleKey;
        set
        {
            if (string.Equals(_styleKey, value, StringComparison.Ordinal))
            {
                return;
            }

            _styleKey = value;
            WidgetStyleManager.Apply(this, _visualState);
        }
    }

    public bool IsInteractive { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            if (!_isEnabled)
            {
                _isPointerOver = false;
                _isPressed = false;
            }
            RefreshVisualState();
        }
    }

    public WidgetVisualState VisualState => _visualState;

    public event Action<WidgetPointerEvent>? PointerInput;

    public event Action<WidgetKeyboardEvent>? KeyboardInput;

    internal bool SupportsPointerInput => IsInteractive || PointerInput is not null;

    internal bool SupportsKeyboardInput => KeyboardInput is not null;

    public virtual void Arrange(Rect bounds)
    {
        Bounds = bounds;
        X = bounds.X;
        Y = bounds.Y;
    }

    protected IDisposable? PushClip(DrawingContext context)
    {
        if (!ClipToBounds)
        {
            return null;
        }

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return null;
        }

        return context.PushClip(Bounds);
    }

    public abstract void Draw(DrawingContext context);

    public virtual void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        WidgetStyleManager.Apply(this, _visualState);
    }

    public virtual bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        UpdatePointerState(e);

        if (PointerInput is not null)
        {
            PointerInput.Invoke(e);
            return true;
        }

        return IsInteractive;
    }

    public virtual bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        if (KeyboardInput is not null)
        {
            KeyboardInput.Invoke(e);
            return true;
        }

        return false;
    }

    public void RefreshStyle() => WidgetStyleManager.Apply(this, _visualState);

    protected void SetVisualState(WidgetVisualState state)
    {
        if (_visualState == state)
        {
            return;
        }

        _visualState = state;
        WidgetStyleManager.Apply(this, _visualState);
    }

    private void RefreshVisualState()
    {
        if (!_isEnabled)
        {
            SetVisualState(WidgetVisualState.Disabled);
            return;
        }

        if (_isPressed)
        {
            SetVisualState(WidgetVisualState.Pressed);
        }
        else if (_isPointerOver)
        {
            SetVisualState(WidgetVisualState.PointerOver);
        }
        else
        {
            SetVisualState(WidgetVisualState.Normal);
        }
    }

    private void UpdatePointerState(in WidgetPointerEvent e)
    {
        if (!IsEnabled)
        {
            return;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Entered:
                _isPointerOver = true;
                if (!_isPressed)
                {
                    SetVisualState(WidgetVisualState.PointerOver);
                }
                break;

            case WidgetPointerEventKind.Exited:
                _isPointerOver = false;
                if (!_isPressed)
                {
                    SetVisualState(WidgetVisualState.Normal);
                }
                break;

            case WidgetPointerEventKind.Pressed:
                _isPressed = true;
                SetVisualState(WidgetVisualState.Pressed);
                break;

            case WidgetPointerEventKind.Released:
                _isPressed = false;
                if (_isPointerOver)
                {
                    SetVisualState(WidgetVisualState.PointerOver);
                }
                else
                {
                    SetVisualState(WidgetVisualState.Normal);
                }
                break;

            case WidgetPointerEventKind.Cancelled:
                _isPressed = false;
                if (_isPointerOver)
                {
                    SetVisualState(WidgetVisualState.PointerOver);
                }
                else
                {
                    SetVisualState(WidgetVisualState.Normal);
                }
                break;
        }
    }

    private static void RefreshAllStyles()
    {
        lock (InstanceLock)
        {
            for (var i = Instances.Count - 1; i >= 0; i--)
            {
                if (Instances[i].TryGetTarget(out var widget))
                {
                    widget.RefreshStyle();
                }
                else
                {
                    Instances.RemoveAt(i);
                }
            }
        }
    }
}
