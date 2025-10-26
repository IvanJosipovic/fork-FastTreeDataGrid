# Layout & Virtualizing Widgets

The widget renderer now includes layout surfaces that mirror Avalonia panels without falling back to the control tree. They reuse the same immediate-mode batching and value-provider pipeline as cell widgets, making it possible to build rich overlays and dashboards inside a `FastTreeDataGrid` column or host them independently.

> **Tip:** The demo application's **Widgets Gallery → Layouts** page hosts interactive boards for each widget in this guide so you can inspect palette values, pointer behavior, and recommended usage without digging through code.

## PanelLayoutWidget family

`PanelLayoutWidget` adapts Avalonia panel semantics into widget space. Each concrete panel injects a lightweight adapter that runs during `Arrange` to position child widgets.

| Widget | Purpose | Notes |
| --- | --- | --- |
| `CanvasLayoutWidget` | Absolute positioning with `SetLeft/Top/Right/Bottom/Width/Height`. | Great for alert overlays or draggable badges. |
| `DockLayoutWidget` | Dock header/footer/side regions with a remaining fill. | Use `SetDock(widget, Dock)` on the panel. |
| `StackLayoutWidget` | Horizontal or vertical stacking with spacing/padding. | Spacing/padding comes from Fluent layout palette if unset. |
| `WrapLayoutWidget` | Flow items across rows/columns when they hit the edge. | Honors `DefaultItemWidth`/`DefaultItemHeight`. |
| `GridLayoutWidget` / `UniformGridLayoutWidget` | Grid-style placement without XAML definitions. | Provide `Rows`, `Columns`, or `FirstColumn`. |
| `RelativePanelLayoutWidget` | Constraint-based positioning (align/above/below). | Methods mirror Avalonia’s `RelativePanel`. |
| `SplitViewLayoutWidget` | Two-pane layout with compact/expanded pane widths. | Style pane content using `SplitViewPane`/`SplitViewContent` style keys. |
| `LayoutTransformLayoutWidget` | Apply scale/rotation to a single child tree. | Uses `ScaleX`, `ScaleY`, and `Angle` for lightweight transforms. |
| `ViewboxLayoutWidget` | Stretch a child uniformly inside available space. | Honors `Stretch` and `StretchDirection`. |
| `ScrollViewerWidget` | Host a single child with offset awareness. | Automatically updates children that implement `IVirtualizingWidgetHost`. |

All layout widgets:

- Pull padding/spacing defaults from `WidgetFluentPalette.Layout` when values stay at their default.
- Accept immediate-mode children (`Widget` instances) instead of Avalonia controls.
- Run without a measure pass; adapters position children directly inside the `Arrange` call.

### Quick example

```csharp
var card = new CanvasLayoutWidget();

var background = new BorderWidget
{
    DesiredWidth = 320,
    DesiredHeight = 140,
    CornerRadius = new CornerRadius(12),
};
card.Children.Add(background);

var badge = new BadgeWidget { DesiredWidth = 80, DesiredHeight = 28 };
badge.SetText("ALERT");
card.Children.Add(badge);
card.SetLeft(badge, 16);
card.SetTop(badge, 16);

var action = new ButtonWidget { StyleKey = "Primary", DesiredWidth = 100 };
action.SetText("Fix it");
card.Children.Add(action);
card.SetRight(action, 16);
card.SetBottom(action, 16);
```

Attach widgets to a `FastTreeDataGridColumn` via a `WidgetTemplate` or `WidgetFactory`, or host them in standalone dashboards using `WidgetBoardFactory`.

## VirtualizingPanelWidget

`VirtualizingPanelWidget` wraps the shared `FastTreeDataGridVirtualizationProviderRegistry` so widget trees can load thousands of records without inflating the visual tree. It provides two ready-made orientations:

- `VirtualizingStackPanelWidget` – vertical list with `ItemHeight`, `Spacing`, and buffer controls.
- `VirtualizingCarouselPanelWidget` – horizontal layout for uniform cards or carousels.

Each panel expects:

1. `ItemsSource` – an `IFastTreeDataGridSource` implementation. `FastTreeDataGridFlatSource<T>` works well for in-memory collections; plug in custom providers for async/remote data.
2. `ItemFactory` or `ItemTemplate` – produces widgets for realized rows. Factories receive the row’s `IFastTreeDataGridValueProvider` and item object so they can reuse existing widget descriptors.
3. Optional `VirtualizationSettings` – `PageSize`, `PrefetchRadius`, and concurrency knobs map directly to the grid’s virtualization scheduler.

Panels request pages via the shared viewport scheduler, so a slider or scroll host can control offsets without duplicating virtualization logic. The demo’s “Layouts (Widgets)” tab shows a slider-driven example:

```csharp
var source = new FastTreeDataGridFlatSource<ActivityNode>(
    activities,
    _ => Array.Empty<ActivityNode>());

var panel = new VirtualizingStackPanelWidget
{
    ItemsSource = source,
    ItemHeight = 42,
    BufferItemCount = 6,
    ItemFactory = (_, item) => CreateActivityCard((ActivityNode)item!)
};

var scroll = new ScrollViewerWidget();
scroll.Children.Add(panel);
```

Because `VirtualizingPanelWidget` implements `IVirtualizingWidgetHost`, the enclosing `ScrollViewerWidget` automatically forwards viewport updates. Consumers, therefore, only need to manipulate `ScrollViewerWidget.HorizontalOffset` / `VerticalOffset` to trigger fresh page requests.

## TransitioningContentWidget

`TransitioningContentWidget` mirrors Avalonia’s `TransitioningContentControl` with immediate-mode transitions. Provide a widget instance, factory, or template—the control fades or slides between the previous and new content without touching the visual tree.

```csharp
var transitioning = new TransitioningContentWidget
{
    Transition = WidgetTransitionDescriptor.Fade(TimeSpan.FromMilliseconds(220))
};

transitioning.Content = BuildCard("Insights enabled", "Streaming telemetry");

toggle.Toggled += (_, args) =>
{
    transitioning.Content = args.NewValue
        ? BuildCard("Insights enabled", "Streaming telemetry")
        : BuildCard("Insights paused", "Resume to continue");
};
```

Supported transitions include `Fade`, `SlideLeft`, `SlideRight`, `SlideUp`, and `SlideDown`. Customize duration and slide offset via `WidgetTransitionDescriptor`. Widgets queue redraws through the animation scheduler, so animations run inside `FastTreeDataGrid`, widget boards, or any other control that registers with `WidgetAnimationFrameScheduler`.

## TabControlWidget & TabStripWidget

`TabControlWidget` introduces familiar tabbed navigation built entirely in widget space. It pairs with `TabStripWidget` and `TabItemWidget` to render Fluent headers, selection indicators, and tab content without templated controls.

- Headers honor `TabItemHeader*` resources via the new `WidgetFluentPalette.Tab` palette (backgrounds, foregrounds, indicator thickness, corner radii).
- Supply data through `TabControlWidgetValue.Items` plus header/content factories or templates to reuse existing widget blueprints.
- Selection changes emit `WidgetValueChangedEventArgs<object?>`, enabling dashboards to react without touching the visual tree.
- Keyboard navigation mirrors Avalonia: Left/Up select the previous tab, Right/Down advance, and Home/End jump to the first/last tab while raising the same selection notifications.

```csharp
var pages = new[]
{
    new { Title = "Overview", Description = "Immediate-mode tabs share virtualization with FlatTreeDataGrid.", Badge = "Insights" },
    new { Title = "Details", Description = "Header/content factories let each tab render bespoke widget trees.", Badge = "Details" },
    new { Title = "Settings", Description = "Indicator thickness, padding, and corners flow from Fluent resources.", Badge = "Settings" },
};

var tabs = new TabControlWidget { DesiredWidth = 340, DesiredHeight = 220 };

tabs.UpdateValue(null, new TabControlWidgetValue(
    Items: pages,
    SelectedIndex: 0,
    HeaderFactory: (_, item) =>
    {
        var header = new FormattedTextWidget { EmSize = 13, DesiredHeight = 20 };
        header.SetText(item!.Title);
        return header;
    },
    ContentFactory: (_, item) =>
    {
        var stack = new StackLayoutWidget { Orientation = Orientation.Vertical, Spacing = 8, Padding = new Thickness(16, 12, 16, 12) };
        var badge = new BadgeWidget { BackgroundBrush = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), ForegroundBrush = new ImmutableSolidColorBrush(Colors.White), DesiredHeight = 22, Padding = 6 };
        badge.SetText(item!.Badge);
        var text = new FormattedTextWidget { EmSize = 12 };
        text.SetText(item.Description);
        stack.Children.Add(badge);
        stack.Children.Add(text);
        stack.Children.Add(new ProgressWidget { DesiredHeight = 4, Progress = 0.6 });
        return stack;
    }));
```

This combination keeps tab headers virtualized alongside the rest of the widget tree, so switching tabs never instantiates Avalonia controls or allocates new visual elements.

## Styling

Panel widgets pick up Fluent resources through `WidgetStyleManager`:

- `Padding` defaults to `ControlContentPadding`.
- `Spacing` defaults to `ControlSpacing` for adapters that support it.
- `SplitView` widgets bind pane background brushes via `SplitViewPaneBackground`.
- `ContentControlWidget` and `BorderWidget` reuse Fluent border thickness, corner radius, and brushes.

Override values explicitly to diverge from the theme.

## Hosting guidance

1. Prefer `WidgetTemplate` for simple bindings inside grid columns; use `ItemFactory` and direct instantiation when widgets need runtime data.
2. Reuse `FastTreeDataGridFlatSource<T>` for demo boards and dashboards—it already exposes `IFastTreeDataGridValueProvider` so widgets can react to updates.
3. Wrap virtualizing panels in `ScrollViewerWidget` or expose manual offsets so parent containers can scroll without pulling in Avalonia controls.
4. Keep widget trees shallow; call `SetDock`, `SetAlignTopWithPanel`, etc., instead of wrapping widgets in additional containers.

Review `WidgetBoardFactory` and `VirtualizingLayoutsTab` in the demo project for concrete patterns that combine these widgets into reusable surfaces.
