using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ComboBoxItemWidget : BorderWidget
{
    private bool _isPointerOver;
    private bool _isPressed;
    private bool _isSelected;

    static ComboBoxItemWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                string.Empty,
                new WidgetStyleRule(
                    typeof(ComboBoxItemWidget),
                    state,
                    static (widget, _) =>
                    {
                        if (widget is ComboBoxItemWidget item)
                        {
                            item.RefreshAppearance();
                        }
                    }));
        }
    }

    public ComboBoxItemWidget()
    {
        IsInteractive = true;
        ClipToBounds = true;
        PointerInput += OnPointerInput;
    }

    public event EventHandler<WidgetEventArgs>? Clicked;

    public Widget? ContentWidget
    {
        get => Child;
        set => Child = value;
    }

    public object? Item { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            RefreshAppearance();
        }
    }

    private void OnPointerInput(WidgetPointerEvent e)
    {
        switch (e.Kind)
        {
            case WidgetPointerEventKind.Entered:
                _isPointerOver = true;
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Exited:
                _isPointerOver = false;
                _isPressed = false;
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Pressed:
                _isPressed = true;
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Released:
                if (_isPressed)
                {
                    Clicked?.Invoke(this, new WidgetEventArgs(this));
                }

                _isPressed = false;
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isPressed = false;
                _isPointerOver = false;
                RefreshAppearance();
                break;
        }
    }

    private void RefreshAppearance()
    {
        var palette = WidgetFluentPalette.Current.Picker;
        var layout = WidgetFluentPalette.Current.Layout;

        Padding = new Thickness(12, 6, 12, 6);
        CornerRadius = layout.ControlCornerRadius;

        var backgroundState = palette.ButtonBackground;
        var borderState = palette.ButtonBorder;
        var foregroundState = palette.ButtonForeground;

        ImmutableSolidColorBrush? background;
        ImmutableSolidColorBrush? border;
        ImmutableSolidColorBrush? foreground;

        if (!IsEnabled)
        {
            background = backgroundState.Disabled ?? backgroundState.Normal;
            border = borderState.Disabled ?? borderState.Normal;
            foreground = foregroundState.Disabled ?? foregroundState.Normal;
        }
        else if (_isPressed || _isSelected)
        {
            background = backgroundState.Pressed ?? backgroundState.Normal;
            border = borderState.Pressed ?? borderState.Normal;
            foreground = foregroundState.Pressed ?? foregroundState.Normal;
        }
        else if (_isPointerOver)
        {
            background = backgroundState.PointerOver ?? backgroundState.Normal;
            border = borderState.PointerOver ?? borderState.Normal;
            foreground = foregroundState.PointerOver ?? foregroundState.Normal;
        }
        else
        {
            background = backgroundState.Normal;
            border = borderState.Normal;
            foreground = foregroundState.Normal;
        }

        Background = background;
        BorderBrush = border;
        BorderThickness = border is null ? default : new Thickness(1);

        if (Child is Widget child && foreground is not null)
        {
            child.Foreground = foreground;
        }
    }
}
