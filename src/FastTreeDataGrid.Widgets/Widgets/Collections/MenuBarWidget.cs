using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using AvaloniaKey = Avalonia.Input.Key;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class MenuBarWidget : TemplatedWidget
{
    private BorderWidget? _rootBorder;
    private StackLayoutWidget? _itemsHost;
    private readonly List<MenuBarItemHost> _items = new();
    private MenuBarItemHost? _openItem;
    private MenuWidget? _openMenu;
    private bool _isAccessKeyMode;
    private bool _altKeyDown;
    private bool _altConsumed;

    static MenuBarWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(MenuBarWidget),
                WidgetVisualState.Normal,
                static (widget, theme) =>
                {
                    if (widget is MenuBarWidget bar)
                    {
                        bar.ApplyPalette(theme.Palette);
                    }
                }));
    }

    public MenuBarWidget()
    {
    }

    public event EventHandler<MenuItemInvokedEventArgs>? ItemInvoked;

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        var handled = base.HandleKeyboardEvent(e);

        if (!IsEnabled)
        {
            return handled;
        }

        switch (e.Kind)
        {
            case WidgetKeyboardEventKind.KeyDown:
                if (HandleKeyDown(e.Args))
                {
                    handled = true;
                }
                break;
            case WidgetKeyboardEventKind.KeyUp:
                if (HandleKeyUp(e.Args))
                {
                    handled = true;
                }
                break;
        }

        return handled;
    }

    protected override Widget? CreateDefaultTemplate()
    {
        var outerBorder = new BorderWidget
        {
            ClipToBounds = true
        };

        var itemsHost = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Padding = new Thickness(12, 2, 12, 2)
        };

        outerBorder.Child = itemsHost;

        _rootBorder = outerBorder;
        _itemsHost = itemsHost;

        ApplyPalette(WidgetFluentPalette.Current);
        return outerBorder;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        object? payload;
        if (provider is not null && Key is not null)
        {
            payload = provider.GetValue(item, Key);
        }
        else
        {
            payload = item;
        }

        ApplyValue(payload);
    }

    private void ApplyValue(object? value)
    {
        switch (value)
        {
            case MenuBarWidgetValue menuBar:
                BuildItems(menuBar.Items);
                IsEnabled = menuBar.Interaction?.IsEnabled ?? true;
                if (!IsEnabled)
                {
                    CloseDropDown();
                }
                break;
            case IEnumerable<MenuBarItemWidgetValue> enumerable:
                BuildItems(enumerable);
                break;
        }
    }

    private void BuildItems(IEnumerable<MenuBarItemWidgetValue>? items)
    {
        CloseDropDown();
        _items.Clear();
        _itemsHost?.Children.Clear();

        if (items is null || _itemsHost is null)
        {
            return;
        }

        foreach (var item in items)
        {
            var host = CreateItemHost(item);
            _items.Add(host);
            _itemsHost.Children.Add(host.Button);
        }

        UpdateAccessKeyVisibility(_isAccessKeyMode);
    }

    private MenuBarItemHost CreateItemHost(MenuBarItemWidgetValue value)
    {
        var button = new ButtonWidget
        {
            DesiredHeight = 30,
            DesiredWidth = double.NaN
        };
        button.SetText(value.Header);
        button.SetShowAccessKey(_isAccessKeyMode);

        AccessTextWidget.ParseAccessText(value.Header, out var accessKey, out _);

        var menu = new MenuWidget();
        menu.ItemInvoked += OnMenuItemInvoked;
        menu.UpdateValue(null, value.Menu);

        var host = new MenuBarItemHost(value, button, menu, accessKey);

        button.IsEnabled = value.IsEnabled && (value.Interaction?.IsEnabled ?? true);
        button.Click += (_, __) => OnMenuButtonClicked(host);
        button.PointerInput += e => OnMenuButtonPointerInput(host, e);

        return host;
    }

    private void OnMenuButtonClicked(MenuBarItemHost host)
    {
        if (!IsEnabled || (host.Value.Interaction is { IsEnabled: false }))
        {
            return;
        }

        if (_openItem == host)
        {
            CloseDropDown();
            return;
        }

        OpenDropDown(host);
    }

    private void OnMenuButtonPointerInput(MenuBarItemHost host, WidgetPointerEvent e)
    {
        if (e.Kind == WidgetPointerEventKind.Entered
            && _openItem is not null
            && !ReferenceEquals(_openItem, host))
        {
            OpenDropDown(host);
        }
    }

    private void OpenDropDown(MenuBarItemHost host, bool openedFromKeyboard = false)
    {
        if (!IsEnabled || (host.Value.Interaction is { IsEnabled: false }))
        {
            return;
        }

        if (ReferenceEquals(_openItem, host) && _openMenu is not null)
        {
            return;
        }

        _openMenu?.CloseAllSubMenus();
        WidgetOverlayManager.HideOwnedOverlays(this);

        var menu = host.Menu;
        menu.ConfigureNavigation(null, NavigateSibling, CloseDropDown);
        menu.SetAccessKeyVisibility(_isAccessKeyMode);
        menu.UpdateValue(null, host.Value.Menu);
        menu.IsEnabled = host.Value.Menu.Interaction?.IsEnabled ?? true;

        _openItem = host;
        _openMenu = menu;

        var options = new WidgetOverlayOptions(
            Owner: this,
            CloseOnPointerDownOutside: true,
            CloseOnEscape: true,
            MatchWidthToAnchor: true,
            Offset: new Thickness(0, 2, 0, 0),
            OnClosed: _ => OnDropDownClosed(host));

        WidgetOverlayManager.ShowOverlay(menu, host.Button.Bounds, WidgetOverlayPlacement.BottomStart, options);

        if (openedFromKeyboard)
        {
            menu.FocusFirstItemFromParent();
        }
    }

    private void CloseDropDown()
    {
        if (_openMenu is null && _openItem is null)
        {
            DeactivateAccessKeyMode();
            _altKeyDown = false;
            _altConsumed = false;
            return;
        }

        _openMenu?.CloseAllSubMenus();
        WidgetOverlayManager.HideOwnedOverlays(this);

        _openMenu = null;
        _openItem = null;
        _altKeyDown = false;
        _altConsumed = false;
        DeactivateAccessKeyMode();
    }

    private void OnDropDownClosed(MenuBarItemHost host)
    {
        if (!ReferenceEquals(_openItem, host))
        {
            return;
        }

        _openMenu = null;
        _openItem = null;
        _altKeyDown = false;
        _altConsumed = false;
        DeactivateAccessKeyMode();
    }

    private bool HandleKeyDown(KeyEventArgs e)
    {
        var key = e.Key;
        var modifiers = e.KeyModifiers;

        if (key == AvaloniaKey.LeftAlt || key == AvaloniaKey.RightAlt)
        {
            _altKeyDown = true;
            _altConsumed = false;
            return false;
        }

        if (key == AvaloniaKey.F10)
        {
            ActivateAccessKeyMode();
            _altConsumed = true;
            e.Handled = true;
            return true;
        }

        if (_openItem is not null)
        {
            switch (key)
            {
                case AvaloniaKey.Left:
                    NavigateSibling(MenuNavigationDirection.Previous);
                    e.Handled = true;
                    return true;
                case AvaloniaKey.Right:
                    NavigateSibling(MenuNavigationDirection.Next);
                    e.Handled = true;
                    return true;
                case AvaloniaKey.Escape:
                    CloseDropDown();
                    e.Handled = true;
                    return true;
                case AvaloniaKey.Down:
                    _openMenu?.FocusFirstItemFromParent();
                    e.Handled = true;
                    return true;
            }
        }

        if (key == AvaloniaKey.Down && _openItem is null && _items.Count > 0)
        {
            OpenDropDown(_items[0], openedFromKeyboard: true);
            e.Handled = true;
            return true;
        }

        var altModifier = modifiers.HasFlag(KeyModifiers.Alt);

        if (_isAccessKeyMode || _altKeyDown || altModifier)
        {
            var ch = KeyToAccessChar(key);
            if (ch is not null && TryActivateAccessKey(ch.Value))
            {
                _altConsumed = true;
                e.Handled = true;
                return true;
            }
        }

        return false;
    }

    private bool HandleKeyUp(KeyEventArgs e)
    {
        var key = e.Key;

        if (key == AvaloniaKey.LeftAlt || key == AvaloniaKey.RightAlt)
        {
            if (!_altConsumed)
            {
                if (_openItem is not null)
                {
                    CloseDropDown();
                }
                else if (_isAccessKeyMode)
                {
                    DeactivateAccessKeyMode();
                }
                else
                {
                    ActivateAccessKeyMode();
                }
            }

            _altKeyDown = false;
            _altConsumed = false;
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void ActivateAccessKeyMode()
    {
        if (_isAccessKeyMode)
        {
            return;
        }

        _isAccessKeyMode = true;
        UpdateAccessKeyVisibility(true);
    }

    private void DeactivateAccessKeyMode()
    {
        if (!_isAccessKeyMode)
        {
            return;
        }

        _isAccessKeyMode = false;
        UpdateAccessKeyVisibility(false);
    }

    private void UpdateAccessKeyVisibility(bool show)
    {
        foreach (var host in _items)
        {
            host.Button.SetShowAccessKey(show);
        }

        _openMenu?.SetAccessKeyVisibility(show);
    }

    private void NavigateSibling(MenuNavigationDirection direction)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var index = _openItem is not null ? _items.IndexOf(_openItem) : 0;
        if (index < 0)
        {
            index = 0;
        }

        index = direction switch
        {
            MenuNavigationDirection.Next => (index + 1) % _items.Count,
            MenuNavigationDirection.Previous => (index - 1 + _items.Count) % _items.Count,
            _ => index
        };

        OpenDropDown(_items[index], openedFromKeyboard: true);
    }

    private bool TryActivateAccessKey(char key)
    {
        var target = char.ToUpperInvariant(key);

        foreach (var host in _items)
        {
            if (!host.Button.IsEnabled)
            {
                continue;
            }

            if (host.AccessKey is { } accessKey && char.ToUpperInvariant(accessKey) == target)
            {
                OpenDropDown(host, openedFromKeyboard: true);
                return true;
            }
        }

        return false;
    }

    private static char? KeyToAccessChar(AvaloniaKey key)
    {
        if (key >= AvaloniaKey.A && key <= AvaloniaKey.Z)
        {
            return (char)('A' + (key - AvaloniaKey.A));
        }

        if (key >= AvaloniaKey.D0 && key <= AvaloniaKey.D9)
        {
            return (char)('0' + (key - AvaloniaKey.D0));
        }

        if (key >= AvaloniaKey.NumPad0 && key <= AvaloniaKey.NumPad9)
        {
            return (char)('0' + (key - AvaloniaKey.NumPad0));
        }

        return null;
    }

    private void OnMenuItemInvoked(object? sender, MenuItemInvokedEventArgs e)
    {
        ItemInvoked?.Invoke(this, e);
        CloseDropDown();
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        if (_rootBorder is not null)
        {
            _rootBorder.Background = palette.Layout.SplitViewPaneBackground ?? new ImmutableSolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
            _rootBorder.BorderBrush = null;
            _rootBorder.BorderThickness = default;
        }
    }

    private sealed class MenuBarItemHost
    {
        public MenuBarItemHost(MenuBarItemWidgetValue value, ButtonWidget button, MenuWidget menu, char? accessKey)
        {
            Value = value;
            Button = button;
            Menu = menu;
            AccessKey = accessKey;
        }

        public MenuBarItemWidgetValue Value { get; }

        public ButtonWidget Button { get; }

        public MenuWidget Menu { get; }

        public char? AccessKey { get; }
    }
}
