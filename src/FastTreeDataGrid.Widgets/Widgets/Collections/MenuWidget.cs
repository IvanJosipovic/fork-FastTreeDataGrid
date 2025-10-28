using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class MenuWidget : ItemsControlWidget
{
    private double _lastItemExtent = double.NaN;
    private int _focusedIndex = -1;
    private bool _keyboardFocusActive;
    private MenuWidget? _activeSubMenu;
    private MenuItemWidget? _activeSubMenuItem;
    private int _activeSubMenuIndex = -1;
    private bool _showAccessKeys;
    private MenuWidget? _parentMenu;
    private Action<MenuNavigationDirection>? _navigateRoot;
    private Action? _rootCloseRequested;

    public MenuWidget()
    {
        ItemFactory = CreateMenuItem;
        Spacing = 0;
        BufferItemCount = 0;
    }

    public event EventHandler<MenuItemInvokedEventArgs>? ItemInvoked;

    internal int VisibleItemCount => TotalItemCount > 0 ? TotalItemCount : RootItems.Count;

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        var handled = base.HandleKeyboardEvent(e);

        if (!IsEnabled)
        {
            return handled;
        }

        if (e.Kind != WidgetKeyboardEventKind.KeyDown)
        {
            return handled;
        }

        var key = e.Args.Key;
        var result = false;

        switch (key)
        {
            case Avalonia.Input.Key.Down:
                result = MoveFocus(1);
                break;
            case Avalonia.Input.Key.Up:
                result = MoveFocus(-1);
                break;
            case Avalonia.Input.Key.Left:
                result = HandleLeftKey();
                break;
            case Avalonia.Input.Key.Right:
                result = HandleRightKey();
                break;
            case Avalonia.Input.Key.Home:
                result = MoveToEdge(first: true);
                break;
            case Avalonia.Input.Key.End:
                result = MoveToEdge(first: false);
                break;
            case Avalonia.Input.Key.Enter:
            case Avalonia.Input.Key.Space:
                result = InvokeFocusedItem();
                break;
            case Avalonia.Input.Key.Escape:
                CloseSubMenu();
                ClearKeyboardFocus();
                _rootCloseRequested?.Invoke();
                result = true;
                break;
        }

        if (result)
        {
            e.Args.Handled = true;
            return true;
        }

        return handled;
    }

    protected override void ApplyValue(object? value)
    {
        switch (value)
        {
            case MenuWidgetValue menu:
                if (menu.ItemsSource is not null)
                {
                    ItemsSource = menu.ItemsSource;
                    ResetFocus();
                }

                IsEnabled = menu.Interaction?.IsEnabled ?? true;
                break;
            case IEnumerable enumerable and not string:
                ItemsSource = enumerable;
                ResetFocus();
                break;
            default:
                base.ApplyValue(value);
                ResetFocus();
                break;
        }
    }

    protected override void OnItemPresentationChanged()
    {
        base.OnItemPresentationChanged();
        ItemFactory = CreateMenuItem;
    }

    public override void Draw(DrawingContext context)
    {
        var palette = WidgetFluentPalette.Current.Menu;
        UpdateMetrics(palette);

        using var clip = PushClip(context);

        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            Pen? borderPen = null;
            if (palette.PresenterBorder is not null && palette.PresenterBorderThickness.Left > 0)
            {
                borderPen = new Pen(palette.PresenterBorder, palette.PresenterBorderThickness.Left);
            }

            context.DrawRectangle(palette.PresenterBackground, borderPen, Bounds);
        }

        base.Draw(context);
    }

    private void UpdateMetrics(WidgetFluentPalette.MenuPalette palette)
    {
        var targetExtent = Math.Max(24, palette.ItemPadding.Top + palette.ItemPadding.Bottom + 18);

        if (!double.IsNaN(_lastItemExtent) && Math.Abs(_lastItemExtent - targetExtent) <= double.Epsilon)
        {
            return;
        }

        _lastItemExtent = targetExtent;
        ItemExtent = targetExtent;
    }

    private Widget? CreateMenuItem(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (item is MenuItemWidgetValue menuValue && menuValue.IsSeparator)
        {
            var separator = new MenuSeparatorWidget();
            separator.UpdateValue(provider, item);
            return separator;
        }

        var widget = new MenuItemWidget();
        widget.Invoked += OnMenuItemInvoked;
        widget.PointerEntered += OnMenuItemPointerEntered;
        widget.PointerExited += OnMenuItemPointerExited;
        widget.UpdateValue(provider, item);
        widget.SetShowAccessKey(_showAccessKeys);
        return widget;
    }

    private void OnMenuItemInvoked(object? sender, MenuItemInvokedEventArgs e)
    {
        if (e.Value?.SubMenu is not null && sender is MenuItemWidget item)
        {
            var index = GetRealizedIndex(item);
            if (index >= 0)
            {
                OpenSubMenu(index, item, e.Value);
                _activeSubMenu?.FocusFirstItemFromParent();
                e.Handled = true;
            }

            return;
        }

        ItemInvoked?.Invoke(this, e);
        _rootCloseRequested?.Invoke();
    }

    private void OnMenuItemPointerEntered(object? sender, EventArgs e)
    {
        if (sender is not MenuItemWidget item)
        {
            return;
        }

        var index = GetRealizedIndex(item);
        if (index < 0)
        {
            return;
        }

        if (!TryGetSelectableIndex(index, out var menuValue))
        {
            return;
        }

        SetFocusedIndex(index, keyboardInitiated: false);

        if (menuValue?.SubMenu is not null)
        {
            OpenSubMenu(index, item, menuValue);
        }
        else
        {
            CloseSubMenu();
        }
    }

    private void OnMenuItemPointerExited(object? sender, EventArgs e)
    {
        if (_keyboardFocusActive)
        {
            return;
        }

        UpdateActiveStates();
    }

    private void ResetFocus()
    {
        _focusedIndex = -1;
        _keyboardFocusActive = false;
        CloseSubMenu();
        UpdateActiveStates();
    }

    private bool MoveFocus(int delta)
    {
        if (TotalItemCount <= 0)
        {
            return false;
        }

        var start = _focusedIndex;
        if (start < 0)
        {
            start = delta > 0 ? -1 : TotalItemCount;
        }

        var next = FindNextSelectable(start, delta);
        if (next < 0)
        {
            return false;
        }

        SetFocusedIndex(next, keyboardInitiated: true);
        return true;
    }

    private bool MoveToEdge(bool first)
    {
        if (TotalItemCount <= 0)
        {
            return false;
        }

        var start = first ? -1 : TotalItemCount;
        var delta = first ? 1 : -1;
        var next = FindNextSelectable(start, delta);
        if (next < 0)
        {
            return false;
        }

        SetFocusedIndex(next, keyboardInitiated: true);
        return true;
    }

    private void SetFocusedIndex(int index, bool keyboardInitiated)
    {
        if (index < 0 || index >= TotalItemCount)
        {
            _focusedIndex = -1;
            _keyboardFocusActive = false;
            CloseSubMenu();
            UpdateActiveStates();
            return;
        }

        if (!TryGetSelectableIndex(index, out _))
        {
            return;
        }

        _focusedIndex = index;
        _keyboardFocusActive = keyboardInitiated;

        if (_activeSubMenu is not null && index != _activeSubMenuIndex)
        {
            CloseSubMenu();
        }

        UpdateActiveStates();
    }

    private int FindNextSelectable(int start, int delta)
    {
        var count = TotalItemCount;
        if (count <= 0)
        {
            return -1;
        }

        var index = start;
        for (var i = 0; i < count; i++)
        {
            index = delta > 0 ? (index + 1) % count : (index - 1 + count) % count;
            if (TryGetSelectableIndex(index, out _))
            {
                return index;
            }
        }

        return -1;
    }

    private bool TryGetSelectableIndex(int index, out MenuItemWidgetValue? resolved)
    {
        resolved = null;

        if (index < 0 || index >= TotalItemCount)
        {
            return false;
        }

        if (!TryGetRow(index, out var row))
        {
            return false;
        }

        resolved = row.Item switch
        {
            MenuItemWidgetValue menuValue => menuValue,
            string text => new MenuItemWidgetValue(text),
            null => null,
            _ => new MenuItemWidgetValue(row.Item?.ToString() ?? string.Empty)
        };

        if (resolved is null || resolved.IsSeparator)
        {
            return false;
        }

        var enabled = resolved.IsEnabled
                      && (resolved.Interaction?.IsEnabled ?? true)
                      && (resolved.Command?.IsEnabled ?? true);

        return enabled;
    }

    private int GetRealizedIndex(Widget widget)
    {
        foreach (var pair in RealizedWidgets)
        {
            if (ReferenceEquals(pair.Value, widget))
            {
                return pair.Key;
            }
        }

        return -1;
    }

    private bool InvokeFocusedItem()
    {
        if (TotalItemCount <= 0)
        {
            return false;
        }

        if (_focusedIndex < 0 || !TryGetSelectableIndex(_focusedIndex, out var value))
        {
            var next = FindNextSelectable(-1, 1);
            if (next < 0)
            {
                return false;
            }

            _focusedIndex = next;
            TryGetSelectableIndex(_focusedIndex, out value);
        }

        _keyboardFocusActive = true;
        UpdateActiveStates();

        if (RealizedWidgets.TryGetValue(_focusedIndex, out var widget) && widget is MenuItemWidget menuItem)
        {
            if (value?.SubMenu is not null)
            {
                OpenSubMenu(_focusedIndex, menuItem, value);
                _activeSubMenu?.FocusFirstItemFromParent();
                return true;
            }

            menuItem.InvokeProgrammatically();
            _rootCloseRequested?.Invoke();
            return true;
        }

        return false;
    }

    private bool HandleLeftKey()
    {
        if (_activeSubMenu is not null)
        {
            var parentIndex = _activeSubMenuIndex;
            CloseSubMenu();
            if (parentIndex >= 0)
            {
                SetFocusedIndex(parentIndex, keyboardInitiated: true);
            }
            return true;
        }

        if (_parentMenu is not null)
        {
            _parentMenu.HandleChildNavigateBack(this);
            return true;
        }

        if (_navigateRoot is not null)
        {
            _navigateRoot(MenuNavigationDirection.Previous);
            return true;
        }

        return false;
    }

    private bool HandleRightKey()
    {
        if (TryActivateSubMenuFromFocus())
        {
            return true;
        }

        if (_parentMenu is null && _navigateRoot is not null)
        {
            _navigateRoot(MenuNavigationDirection.Next);
            return true;
        }

        return false;
    }

    private void ClearKeyboardFocus()
    {
        _keyboardFocusActive = false;
        UpdateActiveStates();
    }

    private void UpdateActiveStates()
    {
        foreach (var pair in RealizedWidgets)
        {
            if (pair.Value is MenuItemWidget menuItem)
            {
                var active = _keyboardFocusActive && pair.Key == _focusedIndex;
                menuItem.SetKeyboardFocus(active);
            }
        }
    }

    internal void ConfigureNavigation(MenuWidget? parent, Action<MenuNavigationDirection>? navigateRoot, Action? closeRoot)
    {
        _parentMenu = parent;
        _navigateRoot = navigateRoot;
        _rootCloseRequested = closeRoot;

        _activeSubMenu?.ConfigureNavigation(this, navigateRoot, closeRoot);
    }

    internal void CloseAllSubMenus()
    {
        CloseSubMenu();
    }

    internal void SetAccessKeyVisibility(bool show)
    {
        if (_showAccessKeys == show)
        {
            return;
        }

        _showAccessKeys = show;

        foreach (var pair in RealizedWidgets)
        {
            if (pair.Value is MenuItemWidget menuItem)
            {
                menuItem.SetShowAccessKey(show);
            }
        }

        _activeSubMenu?.SetAccessKeyVisibility(show);
    }

    internal void FocusFirstItemFromParent()
    {
        var first = FindNextSelectable(-1, 1);
        if (first >= 0)
        {
            SetFocusedIndex(first, keyboardInitiated: true);
        }
    }

    internal void HandleChildNavigateBack(MenuWidget child)
    {
        if (ReferenceEquals(_activeSubMenu, child))
        {
            var focusIndex = _activeSubMenuIndex;
            CloseSubMenu();
            if (focusIndex >= 0)
            {
                SetFocusedIndex(focusIndex, keyboardInitiated: true);
            }

            return;
        }

        _parentMenu?.HandleChildNavigateBack(child);
    }

    private bool TryActivateSubMenuFromFocus()
    {
        if (_focusedIndex < 0)
        {
            return false;
        }

        if (!TryGetSelectableIndex(_focusedIndex, out var value) || value?.SubMenu is null)
        {
            return false;
        }

        if (!RealizedWidgets.TryGetValue(_focusedIndex, out var widget) || widget is not MenuItemWidget menuItem)
        {
            return false;
        }

        OpenSubMenu(_focusedIndex, menuItem, value);
        _activeSubMenu?.FocusFirstItemFromParent();
        return true;
    }

    private void OpenSubMenu(int index, MenuItemWidget item, MenuItemWidgetValue descriptor)
    {
        if (descriptor.SubMenu is null)
        {
            return;
        }

        if (_activeSubMenu is not null)
        {
            if (ReferenceEquals(_activeSubMenuItem, item) && _activeSubMenuIndex == index)
            {
                return;
            }

            CloseSubMenu();
        }

        var subMenu = _activeSubMenu;
        if (subMenu is null)
        {
            subMenu = new MenuWidget();
            subMenu.ItemInvoked += OnSubMenuItemInvoked;
            _activeSubMenu = subMenu;
        }

        _activeSubMenuItem = item;
        _activeSubMenuIndex = index;
        subMenu.ConfigureNavigation(this, _navigateRoot, _rootCloseRequested);
        subMenu.SetAccessKeyVisibility(_showAccessKeys);
        subMenu.UpdateValue(null, descriptor.SubMenu);

        var options = new WidgetOverlayOptions(
            Owner: this,
            CloseOnPointerDownOutside: true,
            CloseOnEscape: true,
            OnClosed: widget => OnSubMenuClosed(widget as MenuWidget ?? subMenu));

        WidgetOverlayManager.ShowOverlay(subMenu, item.Bounds, WidgetOverlayPlacement.RightStart, options);
        UpdateActiveStates();
    }

    private void CloseSubMenu()
    {
        if (_activeSubMenu is null)
        {
            return;
        }

        _activeSubMenu.CloseSubMenu();
        WidgetOverlayManager.HideOverlay(_activeSubMenu);
        ResetSubMenuState();
        UpdateActiveStates();
    }

    private void ResetSubMenuState()
    {
        _activeSubMenu = null;
        _activeSubMenuItem = null;
        _activeSubMenuIndex = -1;
    }

    private void OnSubMenuClosed(MenuWidget? submenu)
    {
        if (submenu is null || !ReferenceEquals(_activeSubMenu, submenu))
        {
            return;
        }

        ResetSubMenuState();
        UpdateActiveStates();
    }

    private void OnSubMenuItemInvoked(object? sender, MenuItemInvokedEventArgs e)
    {
        ItemInvoked?.Invoke(this, e);
    }
}
