# Menu & Flyout Widgets

Immediate-mode menu surfaces let dashboards and `FastTreeDataGrid` columns share Fluent-styled navigation without dropping back to Avalonia controls. The widgets reuse the existing batching/value-provider pipeline, so menu rows can render data-bound icons, commands, and keyboard accelerators while staying allocation-friendly.

## MenuWidget
- Vertical command list that virtualizes items through `ItemsControlWidget`. Provide data via `MenuWidgetValue` or any `IEnumerable` of `MenuItemWidgetValue`.
- Uses `WidgetFluentPalette.Menu` for presenter background, border, and row brushes. Item height adjusts automatically from Fluent padding metrics.
- Raises `ItemInvoked` whenever a row triggers; payload includes the resolved `MenuItemWidgetValue` plus the original data item.
- Arrow keys cycle through enabled items, Home/End jump to extremes, and Enter/Space execute the highlighted command without leaving widget space. `Left`/`Right` mirror Avalonia’s menu navigation, so right opens child menus and left collapses back to the parent item.
- Access keys are parsed with the same `_` markers used by `AccessText`. Pressing `F10` or `Alt` toggles key tips for the active menu bar and the current menu hierarchy.

```csharp
var menu = new MenuWidget
{
    DesiredWidth = 220
};

var items = new[]
{
    new MenuItemWidgetValue("New", GestureText: "Ctrl+N"),
    new MenuItemWidgetValue("Open...", GestureText: "Ctrl+O"),
    new MenuItemWidgetValue(string.Empty, IsSeparator: true),
    new MenuItemWidgetValue("Exit", GestureText: "Alt+F4")
};

menu.UpdateValue(null, new MenuWidgetValue(items));
menu.ItemInvoked += (_, e) =>
{
    Console.WriteLine($"Invoked {e.Value?.Header}");
};
```

Submenus can be attached to any entry by supplying another `MenuWidgetValue` via the `SubMenu` property:

```csharp
var exportMenu = new MenuWidgetValue(new[]
{
    new MenuItemWidgetValue("_Csv"),
    new MenuItemWidgetValue("_Json"),
    new MenuItemWidgetValue("_Xml")
});

var fileItems = new[]
{
    new MenuItemWidgetValue("_New", GestureText: "Ctrl+N"),
    new MenuItemWidgetValue("_Open", GestureText: "Ctrl+O"),
    new MenuItemWidgetValue(string.Empty, IsSeparator: true),
    new MenuItemWidgetValue("_Export", SubMenu: exportMenu)
};

menu.UpdateValue(null, new MenuWidgetValue(fileItems));
```

## MenuItemWidget
- Represents a single menu row, applying hover/pressed/disabled chrome from the Fluent palette.
- Supports optional icon (`Widget`), gesture text, separators, and command execution through `WidgetCommandSettings`.
- Shows its access key underline whenever the owning menu has key tips active, and exposes the resolved access key/gesture through `WidgetAutomationProperties`.
- Automatically renders a Fluent submenu arrow when `MenuItemWidgetValue.SubMenu` is present.
- Can be used standalone (e.g., inside custom boards) or as the item factory for `MenuWidget`.

## MenuBarWidget
- Renders top-level menus with a horizontal bar while delegating drop-down chrome to the shared widget overlay host, so menus dismiss automatically on outside clicks just like Avalonia’s native `Menu`.
- Accepts `MenuBarWidgetValue` describing each top-level item (`MenuBarItemWidgetValue`) and the nested `MenuWidgetValue` for its drop-down. Prefix headers with `_` to light up access keys.
- Mirrors Avalonia’s keyboard model: `Alt`/`F10` toggle key tips, `Left`/`Right` move between top-level menus, `Down` drops into the current menu, and `Escape` collapses the hierarchy. Pointer navigation keeps the active drop-down in sync.

```csharp
var menuBar = new MenuBarWidget { DesiredWidth = 360, DesiredHeight = 48 };

var fileMenu = new MenuWidgetValue(new[]
{
    new MenuItemWidgetValue("_New", GestureText: "Ctrl+N"),
    new MenuItemWidgetValue("_Open...", GestureText: "Ctrl+O"),
    new MenuItemWidgetValue(string.Empty, IsSeparator: true),
    new MenuItemWidgetValue("_Export", SubMenu: new MenuWidgetValue(new[]
    {
        new MenuItemWidgetValue("_Csv"),
        new MenuItemWidgetValue("_Json"),
        new MenuItemWidgetValue("_Xml")
    })),
    new MenuItemWidgetValue("E_xit", GestureText: "Alt+F4")
});

var helpMenu = new MenuWidgetValue(new[]
{
    new MenuItemWidgetValue("_Documentation"),
    new MenuItemWidgetValue("_Report Issue"),
    new MenuItemWidgetValue(string.Empty, IsSeparator: true),
    new MenuItemWidgetValue("_About FastTreeDataGrid")
});

var barValue = new MenuBarWidgetValue(new[]
{
    new MenuBarItemWidgetValue("_File", fileMenu),
    new MenuBarItemWidgetValue("_Help", helpMenu)
});

menuBar.UpdateValue(null, barValue);
```

## ContextMenuWidget
- Lightweight wrapper that hosts a `MenuWidget` inside Fluent presenter chrome. Supply `ContextMenuWidgetValue` (items + `IsOpen` flag) or any enumerable of `MenuItemWidgetValue`.
- Integrates with the overlay host: provide `ContextMenuWidgetValue.Anchor` or call `ShowAt(Rect anchor)` to display the menu at a screen position. The overlay closes automatically when the user clicks elsewhere or presses `Escape`.

```csharp
var contextMenu = new ContextMenuWidget
{
    DesiredWidth = 240
};

var menuItems = new[]
{
    new MenuItemWidgetValue("_Refresh", GestureText: "F5"),
    new MenuItemWidgetValue("_Paste", GestureText: "Ctrl+V"),
    new MenuItemWidgetValue(string.Empty, IsSeparator: true),
    new MenuItemWidgetValue("Inspect _Element"),
    new MenuItemWidgetValue("_Properties", GestureText: "Alt+Enter")
};

contextMenu.UpdateValue(null, new ContextMenuWidgetValue(menuItems));

var anchor = new Rect(new Point(320, 180), new Size(0, 0));
contextMenu.ShowAt(anchor);
```

## Theming Notes
- All widgets read from `WidgetFluentPalette.Menu` (`PresenterBackground`, `PresenterBorder`, `PresenterBorderThickness`, `ItemPadding`, `ItemBackground`, `ItemForeground`), so they follow the active Fluent theme variant automatically.
- Row height scales from `ItemPadding`, keeping pointer targets in line with Avalonia’s Fluent menus.
- `MenuItemWidget` forwards interaction metadata (name, command label, access key) to `WidgetAutomationProperties` for accessibility integration.
