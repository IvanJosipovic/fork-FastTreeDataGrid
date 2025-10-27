using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class MenuItemWidget : TemplatedWidget
{
    private BorderWidget? _container;
    private StackLayoutWidget? _row;
    private SurfaceWidget? _iconHost;
    private AccessTextWidget? _headerText;
    private FormattedTextWidget? _gestureText;
    private GeometryWidget? _subMenuGlyph;
    private MenuItemWidgetValue? _menuValue;
    private object? _dataItem;
    private WidgetCommandSettings? _commandSettings;
    private bool _isPointerOver;
    private bool _isPressed;
    private bool _isKeyboardFocused;
    private bool _showAccessKeys;
    private bool _hasSubMenuIndicator;
    private static readonly StreamGeometry s_subMenuGeometry = StreamGeometry.Parse("M0 0 L6 4 L0 8 Z");

    static MenuItemWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                string.Empty,
                new WidgetStyleRule(
                    typeof(MenuItemWidget),
                    state,
                    static (widget, _) =>
                    {
                        if (widget is MenuItemWidget menuItem)
                        {
                            menuItem.RefreshAppearance();
                        }
                    }));
        }
    }

    public MenuItemWidget()
    {
        IsInteractive = true;
        PointerInput += OnPointerInputInternal;
    }

    public event EventHandler<MenuItemInvokedEventArgs>? Invoked;
    public event EventHandler? PointerEntered;
    public event EventHandler? PointerExited;

    public MenuItemWidgetValue? Value => _menuValue;

    internal bool HasSubMenu => _menuValue?.SubMenu is not null;

    protected override Widget? CreateDefaultTemplate()
    {
        var container = new BorderWidget
        {
            ClipToBounds = true
        };

        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };

        var iconHost = new SurfaceWidget
        {
            DesiredWidth = 0,
            DesiredHeight = 16,
            ClipToBounds = false
        };

        var header = new AccessTextWidget
        {
            EmSize = 13,
            DesiredHeight = 18
        };

        var gesture = new FormattedTextWidget
        {
            EmSize = 11,
            DesiredHeight = 16
        };

        var arrow = new GeometryWidget
        {
            DesiredWidth = 0,
            DesiredHeight = 12,
            ClipToBounds = false,
            Stretch = Stretch.Uniform,
            Padding = 2
        };

        row.Children.Add(iconHost);
        row.Children.Add(header);
        row.Children.Add(gesture);
        row.Children.Add(arrow);

        container.Child = row;

        _container = container;
        _row = row;
        _iconHost = iconHost;
        _headerText = header;
        _gestureText = gesture;
        _subMenuGlyph = arrow;

        RefreshAppearance();
        return container;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        _dataItem = item;

        var enabled = true;
        MenuItemWidgetValue? value;

        if (provider is not null && Key is not null)
        {
            value = ExtractValue(provider.GetValue(item, Key), ref enabled);
        }
        else
        {
            value = ExtractValue(item, ref enabled);
        }

        ApplyValue(value, enabled);
    }

    private MenuItemWidgetValue? ExtractValue(object? data, ref bool enabled)
    {
        switch (data)
        {
            case MenuItemWidgetValue value:
                if (value.Interaction is { } interaction)
                {
                    enabled &= interaction.IsEnabled;
                }

                enabled &= value.IsEnabled;
                _commandSettings = value.Command;
                return value;
            case string text:
                _commandSettings = null;
                return new MenuItemWidgetValue(text);
            case null:
                _commandSettings = null;
                return null;
            default:
                _commandSettings = null;
                return new MenuItemWidgetValue(data.ToString() ?? string.Empty);
        }
    }

    private void ApplyValue(MenuItemWidgetValue? value, bool enabledFromData)
    {
        _menuValue = value;

        _headerText?.SetAccessText(value?.Header ?? string.Empty);

        if (_gestureText is not null)
        {
            _gestureText.SetText(value?.GestureText ?? string.Empty);
        }

        if (_iconHost is not null)
        {
            _iconHost.Children.Clear();
            if (value?.Icon is { } icon)
            {
                _iconHost.Children.Add(icon);
                _iconHost.DesiredWidth = icon.DesiredWidth.Equals(double.NaN) ? 16 : icon.DesiredWidth;
            }
            else
            {
                _iconHost.DesiredWidth = 0;
            }
        }

        UpdateAutomationMetadata(value);
        SetShowAccessKey(_showAccessKeys);

        var isEnabled = enabledFromData
                        && (_commandSettings?.IsEnabled ?? true)
                        && (value?.IsEnabled ?? true)
                        && (value?.Interaction is null || value.Interaction.IsEnabled);

        IsEnabled = isEnabled;

        RefreshAppearance();
    }

    private void UpdateAutomationMetadata(MenuItemWidgetValue? value)
    {
        var header = _headerText?.Text ?? value?.Header ?? string.Empty;
        Automation.Name = header;
        Automation.CommandLabel = value?.GestureText;

        if (_headerText?.AccessKey is { } accessKey && accessKey != '\0')
        {
            Automation.AccessKey = accessKey.ToString();
        }
        else
        {
            Automation.AccessKey = null;
        }
    }

    private void OnPointerInputInternal(WidgetPointerEvent e)
    {
        switch (e.Kind)
        {
            case WidgetPointerEventKind.Entered:
                _isPointerOver = true;
                PointerEntered?.Invoke(this, EventArgs.Empty);
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Exited:
                _isPointerOver = false;
                _isPressed = false;
                PointerExited?.Invoke(this, EventArgs.Empty);
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Pressed:
                if (!IsEnabled)
                {
                    return;
                }

                _isPressed = true;
                RefreshAppearance();
                break;
            case WidgetPointerEventKind.Released:
                if (!IsEnabled)
                {
                    return;
                }

                if (_isPressed)
                {
                    _isPressed = false;
                    RefreshAppearance();
                    Invoke();
                }
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isPressed = false;
                RefreshAppearance();
                break;
        }
    }

    internal void SetKeyboardFocus(bool value)
    {
        if (_isKeyboardFocused == value)
        {
            return;
        }

        _isKeyboardFocused = value;
        RefreshAppearance();
    }

    internal void SetShowAccessKey(bool value)
    {
        _showAccessKeys = value;

        if (_headerText is null && !IsTemplateApplied)
        {
            ApplyTemplate();
        }

        if (_headerText is not null)
        {
            _headerText.ShowAccessKey = value;
        }
    }

    internal void InvokeProgrammatically()
    {
        if (!IsEnabled)
        {
            return;
        }

        Invoke();
    }

    private void Invoke()
    {
        var args = new MenuItemInvokedEventArgs(this, _dataItem, _menuValue);
        Invoked?.Invoke(this, args);

        if (!args.Handled)
        {
            ExecuteCommand();
        }
    }

    private void ExecuteCommand()
    {
        if (_commandSettings?.Command is not { } command)
        {
            return;
        }

        var parameter = _commandSettings.CommandParameter ?? _dataItem;
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    private void UpdateSubMenuIndicator(bool hasSubMenu, ImmutableSolidColorBrush? foreground)
    {
        if (_subMenuGlyph is null)
        {
            return;
        }

        if (!hasSubMenu)
        {
            if (_hasSubMenuIndicator)
            {
                _hasSubMenuIndicator = false;
                _subMenuGlyph.SetGeometry(null);
                _subMenuGlyph.DesiredWidth = 0;
                _subMenuGlyph.DesiredHeight = 0;
            }

            return;
        }

        _hasSubMenuIndicator = true;
        var fill = (IBrush?)foreground ?? WidgetFluentPalette.Current.Menu.ItemForeground.Normal;
        _subMenuGlyph.DesiredWidth = 12;
        _subMenuGlyph.DesiredHeight = 12;
        _subMenuGlyph.SetGeometry(s_subMenuGeometry, Stretch.Uniform, fill, null, 1.5);
    }

    private void RefreshAppearance()
    {
        if (_container is null)
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.Menu;

        var padding = palette.ItemPadding;
        _container.Padding = padding;

        var backgroundState = palette.ItemBackground;
        var foregroundState = palette.ItemForeground;

        ImmutableSolidColorBrush? background;
        ImmutableSolidColorBrush? foreground;

        if (!IsEnabled)
        {
            background = backgroundState.Disabled ?? backgroundState.Normal;
            foreground = foregroundState.Disabled ?? foregroundState.Normal;
        }
        else if (_isPressed)
        {
            background = backgroundState.Pressed ?? backgroundState.PointerOver ?? backgroundState.Normal;
            foreground = foregroundState.Pressed ?? foregroundState.PointerOver ?? foregroundState.Normal;
        }
        else if (_isPointerOver)
        {
            background = backgroundState.PointerOver ?? backgroundState.Normal;
            foreground = foregroundState.PointerOver ?? foregroundState.Normal;
        }
        else if (_isKeyboardFocused)
        {
            background = backgroundState.PointerOver ?? backgroundState.Normal;
            foreground = foregroundState.PointerOver ?? foregroundState.Normal;
        }
        else
        {
            background = backgroundState.Normal;
            foreground = foregroundState.Normal;
        }

        _container.Background = background;
        _container.BorderBrush = null;
        _container.BorderThickness = default;

        if (_headerText is not null && foreground is not null)
        {
            _headerText.Foreground = foreground;
        }

        if (_gestureText is not null && foreground is not null)
        {
            _gestureText.Foreground = foreground;
        }

        UpdateSubMenuIndicator(HasSubMenu, foreground);
    }
}
