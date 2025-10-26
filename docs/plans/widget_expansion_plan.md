# FastTreeDataGrid Widget Expansion Plan

This roadmap inventories the Avalonia control surface at `/Users/wieslawsoltes/GitHub/Avalonia/src/Avalonia.Controls` and groups the backlog into numbered milestones. Each milestone targets a coherent control family so we can extend the widget renderer, Fluent theme resource mapping, and high-performance shims (including the requested `TreeViewWidget`, `ListBoxWidget`, and `ItemsControlWidget`) without sacrificing the immediate-mode blueprint that powers `FlatTreeDataGrid`.

**Execution Requirements**
- After finishing any task below, update this document by checking the corresponding box.
- Virtualizing panel widgets must reuse the existing FastTreeDataGrid virtualization infrastructure as shared services. Extract reusable components when needed, but ensure the current `FastTreeDataGrid` control continues to function unchanged.
- Flyouts, popups, and notification surfaces are handled by adapter APIs that host widget content; those adapters are tracked outside this milestone list.
- Refactors that introduce widget base classes or shared behaviors must preserve the immediate-mode performance guarantees. Mirror Avalonia’s inheritance hierarchy only when it simplifies reuse without adding per-frame overhead.

## Control Inventory Snapshot

| Control Group | Avalonia Controls | Planned Widgets |
| --- | --- | --- |
| Layout & Panels | `Panel.cs`, `Canvas.cs`, `DockPanel.cs`, `Grid.cs`, `GridSplitter.cs`, `RelativePanel.cs`, `StackPanel.cs`, `WrapPanel.cs`, `VirtualizingPanel.cs`, `VirtualizingStackPanel.cs`, `VirtualizingCarouselPanel.cs`, `LayoutTransformControl.cs`, `Viewbox.cs`, `Carousel.cs`, `SplitView/SplitView.cs`, `Primitives/UniformGrid.cs`, `PullToRefresh/RefreshContainer.cs`, `PullToRefresh/RefreshVisualizer.cs` | `CanvasLayoutWidget`, `DockLayoutWidget`, `GridLayoutWidget`, `GridSplitterWidget`, `RelativePanelLayoutWidget`, `StackLayoutWidget`, `WrapLayoutWidget`, `UniformGridLayoutWidget`, `VirtualizingPanelWidget`, `VirtualizingStackPanelWidget`, `VirtualizingCarouselPanelWidget`, `LayoutTransformWidget`, `ViewboxWidget`, `CarouselWidget`, `SplitViewWidget`, `RefreshContainerWidget`, `RefreshVisualizerWidget` |
| Content & Decorators | `Border.cs`, `BorderVisual.cs`, `ContentControl.cs`, `UserControl.cs`, `Decorator.cs`, `Expander.cs`, `GroupBox.cs`, `ScrollViewer.cs`, `TransitioningContentControl.cs`, `Presenters/ContentPresenter.cs`, `Presenters/ItemsPresenter.cs` | `BorderWidget`, `BorderVisualWidget`, `DecoratorWidget`, `ContentControlWidget`, `UserControlWidget`, `ExpanderWidget`, `GroupBoxWidget`, `ScrollViewerWidget`, `TransitioningContentWidget`, `ContentPresenterWidget`, `ItemsPresenterWidget` |
| Text, Media & Iconography | `TextBlock.cs`, `SelectableTextBlock.cs`, `TextBox.cs`, `MaskedTextBox.cs`, `Label.cs`, `Documents/*.cs`, `Image.cs`, `IconElement.cs`, `PathIcon.cs` | `TextWidget`, `SelectableTextWidget`, `TextBoxWidget`, `MaskedTextBoxWidget`, `LabelWidget`, `DocumentTextWidget`, `ImageWidget`, `IconElementWidget`, `PathIconWidget` |
| Buttons & Command Surfaces | `Button.cs`, `RepeatButton.cs`, `ButtonSpinner.cs`, `Spinner.cs`, `ToggleButton` (`Primitives`), `Thumb` (`Primitives`), `ToggleSwitch.cs`, `HyperlinkButton.cs`, `DropDownButton.cs`, `SplitButton/SplitButton.cs`, `SplitButton/ToggleSplitButton.cs`, `MenuBase.cs`, `MenuItem.cs` | `ButtonWidget`, `RepeatButtonWidget`, `ButtonSpinnerWidget`, `SpinnerWidget`, `ToggleButtonWidget`, `ThumbWidget`, `ToggleSwitchWidget`, `HyperlinkButtonWidget`, `DropDownButtonWidget`, `SplitButtonWidget`, `ToggleSplitButtonWidget`, `MenuWidget`, `MenuItemWidget` |
| Toggle, Range & Value Pickers | `CheckBox.cs`, `RadioButton.cs`, `Slider.cs`, `ProgressBar.cs`, `Primitives/RangeBase.cs`, `NumericUpDown/NumericUpDown.cs`, `Calendar/Calendar.cs`, `CalendarDatePicker/CalendarDatePicker.cs`, `DateTimePickers/DatePicker.cs`, `DateTimePickers/TimePicker.cs`, `AutoCompleteBox/AutoCompleteBox.cs`, `ScrollBar` (`Primitives`), `TickBar.cs` | `CheckBoxWidget`, `RadioButtonWidget`, `SliderWidget`, `ProgressWidget`, `RangeBaseWidget`, `NumericUpDownWidget`, `CalendarWidget`, `CalendarDatePickerWidget`, `DatePickerWidget`, `TimePickerWidget`, `AutoCompleteBoxWidget`, `ScrollBarWidget`, `TickBarWidget` |
| Items, Selection & Navigation | `ItemsControl.cs`, `ItemsSourceView.cs`, `ListBox.cs`, `ListBoxItem.cs`, `TreeView.cs`, `TreeViewItem.cs`, `ComboBox.cs`, `ComboBoxItem.cs`, `TabControl.cs`, `TabItem.cs`, `Primitives/TabStrip.cs`, `Menu.cs`, `MenuBar/NativeMenuBar.cs`, `Carousel.cs`, `SplitView.cs`, `SelectingItemsControl.cs`, `HeaderedItemsControl.cs`, `HeaderedSelectingItemsControl.cs` | `ItemsControlWidget`, `ListBoxWidget`, `ListBoxItemWidget`, `TreeViewWidget`, `TreeViewItemWidget`, `ComboBoxWidget`, `ComboBoxItemWidget`, `TabControlWidget`, `TabItemWidget`, `TabStripWidget`, `MenuBarWidget`, `CarouselWidget`, `SplitViewWidget`, `SelectingItemsWidget`, `HeaderedItemsWidget`, `HeaderedSelectingItemsWidget` |
| Shapes & Drawing | `Shapes/Arc.cs`, `Shapes/Ellipse.cs`, `Shapes/Line.cs`, `Shapes/Path.cs`, `Shapes/Polygon.cs`, `Shapes/Polyline.cs`, `Shapes/Rectangle.cs`, `Shapes/Sector.cs`, `PathIcon.cs`, `IconElement.cs` | `ArcShapeWidget`, `EllipseShapeWidget`, `LineShapeWidget`, `PathShapeWidget`, `PolygonShapeWidget`, `PolylineShapeWidget`, `RectangleShapeWidget`, `SectorShapeWidget`, `PathIconWidget`, `IconElementWidget` |

> **Note:** Utility, automation, diagnostics, and infrastructure-only classes are tracked as dependencies where required but do not receive dedicated widgets.

## Milestone 1 — Theme & Widget Infrastructure
- [x] Generalize `WidgetStyleManager` and `WidgetFluentPalette` to expose a theming contract per control group, reading Fluent resources from `/Users/wieslawsoltes/GitHub/Avalonia/src/Avalonia.Themes.Fluent`.
- [x] Introduce palette records for text, border, selection, menu, calendar, flyout, and shape visuals; wire them into `WidgetStyleManager.RefreshCurrentTheme()`.
- [x] Extend widget descriptor primitives (records in `Widgets/Core/WidgetDescriptors.cs`) so new controls can share shape, brush, and typography payloads, supporting hierarchical widget inheritance where it improves reuse without adding runtime cost.
- [x] Author automated probes that validate Fluent resource lookups (fallback vs themed) to guard against missing theme keys.

## Milestone 2 — Layout & Panel Widgets
- [x] Deliver high-performance layout widgets for `Canvas`, `RelativePanel`, `UniformGrid`, `VirtualizingPanel`, `VirtualizingStackPanel`, `VirtualizingCarouselPanel`, `LayoutTransformControl`, `Viewbox`, and `SplitView`, complementing the existing grid/dock/stack/wrap surfaces.
- [x] Implement a panel adapter abstraction so layout widgets can translate Avalonia measure/arrange semantics into the existing surface batching.
- [x] Support layout-only styling (padding, spacing, alignment) by mapping Fluent keys such as `ControlCornerRadius`, `ControlContentPadding`, `SplitViewPaneBackground`.
- [x] Add sample scenarios in `samples/FastTreeDataGrid.Demo/MainWindow.axaml` to demonstrate each new layout surface, including one panel type currently unsupported (e.g., `Canvas` free positioning showcase).

- [x] Add `BorderWidget`, `BorderVisualWidget`, `DecoratorWidget`, `ContentControlWidget`, `UserControlWidget`, `ExpanderWidget`, `GroupBoxWidget`, and `ScrollViewerWidget` utilizing lazy child instantiation with widget templates.
- [x] Provide Fluent theme bindings for border thickness, background brushes, and header typography (`ContentControlTheme`, `GroupBoxHeaderForeground`, etc.).
- [x] Integrate `TransitioningContentControl` animations using lightweight transition descriptors that map to the existing immediate-mode animation hooks.
- [x] Document host APIs so consumers can wrap arbitrary user widgets inside content/decorator widgets without breaking pooling.

## Milestone 4 — Text, Media & Iconography
- [x] Expand `TextWidget` variants to cover `TextBlock`, `SelectableTextBlock`, `Label`, and document text spans; incorporate cursor/selection rendering for selectable text.
- [x] Implement `TextBoxWidget` and `MaskedTextBoxWidget` with immediate-mode text editing surfaces, caret/selection rendering, and IME hooks.
- [x] Add `ImageWidget`, `IconElementWidget`, and `PathIconWidget`, leveraging cached `IBitmap` and geometry caches while honoring Fluent keys like `TextControlForeground`, `IconForeground`, `SymbolIconBrush`.
- [x] Provide text styling palettes (font sizes, weights, highlight brushes) synced to Fluent resources for typography consistency.

## Milestone 5 — Buttons & Command Surfaces
- [x] Build widget implementations for `Button`, `RepeatButton`, `HyperlinkButton`, `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `ButtonSpinner`, and `Spinner`, sharing common command/gesture pipelines.
- [x] Extend palette records to include button variants (standard, accent, subtle, destructive) plus spinner stroke/track resources from Fluent theme dictionaries.
- [x] Refactor button-related base classes so toggle and split variants can inherit common drawing/state management; support `Primitives/ToggleButton` behaviors in the shared path without regressing pooling or allocations.
- [x] Provide automation-friendly metadata (access keys, command labels) for integration with keyboard navigation logic.

## Milestone 6 — Toggle, Range & Value Pickers
- [x] Augment existing toggle/range widgets with `CheckBoxWidget`, `RadioButtonWidget`, `ToggleSwitchWidget`, `SliderWidget`, and `ProgressWidget` upgrades that honor Fluent brushes and animations (focus glows, pressed glyphs). Result: all widgets now read `WidgetFluentPalette` brush states (focused, pointer-over, pressed) and refresh visuals through `WidgetStyleManager`.
- [x] Implement `NumericUpDownWidget`, `CalendarWidget`, `CalendarDatePickerWidget`, `DatePickerWidget`, `TimePickerWidget`, `AutoCompleteBoxWidget`, and `ScrollBarWidget`, reusing pooled popup/overlay primitives.
  - ScrollBar infrastructure landed (`ScrollBarWidget`, `ScrollBarWidgetValue`, range palette reuse).
  - NumericUpDown now available (text + spinner surface with palette-aware styling, `NumericUpDownWidgetValue` bindings).
  - Calendar month surface implemented (`CalendarWidget` + `CalendarWidgetValue`), including culture-aware weekday headers and selection highlighting.
  - DatePicker inline drop-down delivered (`DatePickerWidget`, `DatePickerWidgetValue`) and CalendarDatePicker variant added (`CalendarDatePickerWidget`, `CalendarDatePickerWidgetValue`), paving the way for overlay pickers.
  - TimePickerWidget introduced (hour/minute spinners with optional seconds, `TimePickerWidgetValue`), covering core time selection scenarios.
  - AutoCompleteBoxWidget implemented (text + suggestion list via `AutoCompleteBoxWidgetValue`), setting up ComboBox groundwork.
  - ComboBoxWidget landed (`ComboBoxWidgetValue` with items/providers, inline drop-down) with `ComboBoxItemWidget` providing Fluent drop-down rows.
- [x] Create shared range/value palette data (`RangeBasePalette`, `CalendarPalette`, `PickerPalette`) derived from Fluent resources (`SliderTrackFill`, `AccentControlBorderBrush`, `CalendarDayButtonBackground`).
- [x] Validate edge cases such as indeterminate progress, partial check states, min/max enforcement, and calendar blackout dates through targeted unit tests, including verification that any new widget base classes maintain perf characteristics. Tests now cover numeric clamping, calendar range enforcement, and picker selection signals.

## Milestone 7 — ItemsControl Shims & Selection Containers
- [x] Introduce `ItemsControlWidget`, `ListBoxWidget`, and `TreeViewWidget` as shims over `FlatTreeDataGrid`, exposing familiar Avalonia APIs (item templates, selection model, hierarchical data) for ease of use.
- [x] Implement adapter layers that translate `ItemsControl` operations (add/remove, selection changes, expand/collapse) into `FastTreeDataGrid` source mutations while retaining virtualization benefits.
- [x] Provide palette mappings for list backgrounds, selection highlights, and tree indentation (`ListBoxItemBackground`, `TreeViewExpanderGlyphBrush`, etc.), and evaluate introducing shared selection widget bases to align with Avalonia hierarchies when beneficial.
- [x] Update samples and documentation to showcase direct migration paths from Avalonia `TreeView`/`ListBox` into the new widgets, including lazy-loading trees.

## Milestone 8 — Menu & Tab Interaction Parity
- [x] Implement full keyboard parity for menu layers (Alt activation, access keys, submenu traversal, accelerator text updates) matching Avalonia behaviors.
- [x] Integrate menu widgets with the overlay host so drop-downs/context menus position and dismiss like standard popups.
- [x] Extend samples/tests/docs to demonstrate keyboard/touch interactions for `MenuWidget`, `MenuBarWidget`, `ContextMenuWidget`, and `TabControlWidget`.
- [x] Provide focus visuals and automation metadata updates for active menu/tab items to meet accessibility requirements.

## Milestone 9 — Shapes & Vector Visuals
- [x] Introduce dedicated widgets (`LineShapeWidget`, `RectangleShapeWidget`, `EllipseShapeWidget`, `ArcShapeWidget`, `PolygonShapeWidget`, `PolylineShapeWidget`, `PathShapeWidget`, `SectorShapeWidget`) that map directly to `Avalonia.Controls.Shapes` types.
- [x] Add shape palette entries for stroke thickness, dash arrays, joins, and fills using Fluent keys (`ShapeStrokeBrush`, `ShapeFillBrush`).
- [x] Optimize geometry caching/sharing so shapes reuse immutable geometries and brushes between frames.
- [x] Expand the widget demo gallery to include shape samples, verifying hit-testing and resizing.

## Milestone 10 — Validation, Samples & Documentation
- [x] Augment `samples/FastTreeDataGrid.Demo` to include scenario pages for every milestone, ensuring new widgets are discoverable and performance-tested.
- [x] Add regression and performance tests covering new control families (interaction latency, allocation profiles) with benchmarks under `benchmarks/`.
- [x] Update `README.md`, `docs/`, and API comments with guidance for selecting the appropriate widget wrappers and migrating from stock Avalonia controls.
- [x] Track completion with checklist updates and changelog entries summarizing user-facing additions.
