using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class Widget
{
    private WidgetVisualState _visualState = WidgetVisualState.Normal;
    private bool _isEnabled = true;
    private bool _isPointerOver;
    private bool _isPressed;
    private double _rotation;
    private Matrix _renderTransform = Matrix.Identity;
    private static readonly object InstanceLock = new();
    private static readonly List<WeakReference<Widget>> Instances = new();

    static Widget()
    {
        WidgetStyleManager.ThemeChanged += _ => RefreshAllStyles();
        WidgetFluentPalette.EnsureInitialized();
    }

    protected Widget()
    {
        lock (InstanceLock)
        {
            Instances.Add(new WeakReference<Widget>(this));
        }

        WidgetStyleManager.Apply(this, _visualState);
        UpdateRenderTransform();
    }

    public double X { get; set; }

    public double Y { get; set; }

    public double Rotation
    {
        get => _rotation;
        set
        {
            if (Math.Abs(_rotation - value) <= double.Epsilon)
            {
                return;
            }

            _rotation = value;
            UpdateRenderTransform();
        }
    }

    public Matrix RenderTransform => _renderTransform;

    public ImmutableSolidColorBrush? Foreground { get; set; }

    public string? Key { get; set; }

    public double DesiredWidth { get; set; } = double.NaN;

    public double DesiredHeight { get; set; } = double.NaN;

    public bool ClipToBounds { get; set; } = true;

    public Rect Bounds { get; private set; }

    public Thickness Margin { get; set; }

    public CornerRadius CornerRadius { get; set; }

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
        var adjusted = ApplyMargin(bounds);
        Bounds = adjusted;
        X = adjusted.X;
        Y = adjusted.Y;
        UpdateRenderTransform();
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
                break;

            case WidgetPointerEventKind.Exited:
                _isPointerOver = false;
                break;

            case WidgetPointerEventKind.Moved:
                _isPointerOver = HitTestLocalBounds(e.Position);
                break;

            case WidgetPointerEventKind.Pressed:
                _isPressed = true;
                _isPointerOver = true;
                break;

            case WidgetPointerEventKind.Released:
                _isPressed = false;
                _isPointerOver = HitTestLocalBounds(e.Position);
                break;

            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isPressed = false;
                _isPointerOver = false;
                break;
        }

        RefreshVisualState();
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
            if (_isPointerOver)
            {
                SetVisualState(WidgetVisualState.Pressed);
            }
            else
            {
                SetVisualState(WidgetVisualState.Normal);
            }

            return;
        }

        if (_isPointerOver)
        {
            SetVisualState(WidgetVisualState.PointerOver);
        }
        else
        {
            SetVisualState(WidgetVisualState.Normal);
        }
    }

    protected bool HitTestLocalBounds(Point position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return false;
        }

        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(position);
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

    private Rect ApplyMargin(Rect bounds)
    {
        if (Margin == default)
        {
            return bounds;
        }

        var left = Math.Max(0, Margin.Left);
        var top = Math.Max(0, Margin.Top);
        var right = Math.Max(0, Margin.Right);
        var bottom = Math.Max(0, Margin.Bottom);

        var width = Math.Max(0, bounds.Width - left - right);
        var height = Math.Max(0, bounds.Height - top - bottom);
        var x = bounds.X + left;
        var y = bounds.Y + top;

        return new Rect(x, y, width, height);
    }

    protected virtual Matrix CreateRenderTransform()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return Matrix.Identity;
        }

        if (Math.Abs(_rotation) <= double.Epsilon)
        {
            return Matrix.Identity;
        }

        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        return Matrix.CreateTranslation(-centerX, -centerY)
               * Matrix.CreateRotation(Matrix.ToRadians(_rotation))
               * Matrix.CreateTranslation(centerX, centerY);
    }

    protected void UpdateRenderTransform() => _renderTransform = CreateRenderTransform();

    protected IDisposable PushRenderTransform(DrawingContext context) => context.PushTransform(_renderTransform);
}
