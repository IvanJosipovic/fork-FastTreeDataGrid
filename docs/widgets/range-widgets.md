# Range & Toggle Widgets

FastTreeDataGrid now mirrors Avalonia’s range-based controls with immediate-mode counterparts. The shared `RangeBasePalette` captures Fluent brushes for tracks, value fills, and thumb borders so `SliderWidget`, `ProgressWidget`, and the new `ScrollBarWidget` stay colour-consistent without duplicating resource lookups.

## ScrollBarWidget
- Orientation-aware track/thumb layout that honours `Minimum`, `Maximum`, `ViewportSize`, and `Value`.
- Consumes `ScrollBarWidgetValue` from data providers, exposing optional brush overrides plus `SmallChange`/`LargeChange` metadata for command surfaces.
- Uses the shared range palette and `ScrollBarPalette` to bind `ScrollBarTrackFill`, `ScrollBarThumbFillPointerOver`, and `AccentControlBorderBrush`, keeping parity with Fluent hover/pressed states.
- Dragging and track clicks update the immediate-mode surface without instantiating templated controls, so scrollbars can host virtualized grids or dashboards without leaving widget space.

```csharp
var scrollBar = new ScrollBarWidget
{
    Orientation = Orientation.Vertical,
    Minimum = 0,
    Maximum = 100,
    ViewportSize = 16,
    DesiredHeight = 180,
};
scrollBar.ValueChanged += (_, args) =>
{
    // Sync a virtualized source or update a linked ScrollViewerWidget.
    viewportOffset = args.NewValue;
};
```

## Toggle upgrades
- `CheckBoxWidget`, `RadioButtonWidget`, `ToggleSwitchWidget`, and `SliderWidget` now refresh their templates through the shared range palette, picking up Fluent focus glows, pressed glyphs, and disabled styling.
- `ProgressWidget` renders determinate and indeterminate bars by tapping into the same palette so loading indicators match control theme variants.

## NumericUpDownWidget
- Builds on `ButtonSpinnerWidget` to provide a numeric editor with inline text entry and repeat buttons.
- Supports `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, and optional `FormatString`/`Culture` for culture-aware number formatting.
- Synchronizes `ValidSpinDirections` with the current value so spinner buttons disable automatically at bounds.
- Emits `ValueChanged` events and respects `NumericUpDownWidgetValue` payloads from data providers.

## CalendarWidget
- Renders a navigable month view with weekday headers and 6×7 day grid, honouring `CalendarPalette` colors for borders, selection fills, and today/disabled states.
- Accepts `CalendarWidgetValue` inputs or direct property setters for `DisplayDate`, `SelectedDate`, `Minimum`, `Maximum`, and `Culture`.
- Exposes `SelectedDateChanged`/`DisplayDateChanged` events; navigation buttons automatically disable when reaching min/max months.
- Designed to sit in picker overlays (next milestone) but also works standalone inside dashboards or grid cells.

## DatePickerWidget
- Wraps a clickable header and inline calendar drop-down, relying entirely on widgets so it stays virtualizable and theme-friendly.
- Bind with `DatePickerWidgetValue` to push current date, min/max range, custom format strings, culture, or force the drop-down state.
- Raises `SelectedDateChanged` and `DropDownOpenChanged` for host coordination (e.g., closing other pickers, updating filters).
- Uses `PickerPalette` for button chrome and `CalendarPalette` for drop-down visuals, so Fluent styling remains consistent.

## CalendarDatePickerWidget
- Inherits the date picker shell but defaults to long-date formatting, making it a plug-and-play widget equivalent to Avalonia’s `CalendarDatePicker`.
- Accepts `CalendarDatePickerWidgetValue` in addition to the base value descriptors, so providers can tailor culture, range, and drop-down state independently from other pickers.
- Ideal for settings panes that need both formatted headers and quick calendar navigation without relying on control overlays.

## TimePickerWidget
- Lightweight time selector powered by three `NumericUpDownWidget` instances (hours, minutes, optional seconds) so it remains virtualization-friendly.
- Bind via `TimePickerWidgetValue` to supply current time, min/max bounds, culture-aware display, and enablement.
- Emits `TimeChanged` events; respects 24-hour or 12-hour display modes when required.
- Shares `PickerPalette` stylings with other range widgets for consistent button/separator visuals.

## AutoCompleteBoxWidget
- Text entry with suggestion filtering entirely in widget space; backed by `AutoCompleteBoxWidgetValue` for data-driven text and suggestion lists.
- Filters suggestions with case-insensitive contains matching and displays up to ten results; clicking a suggestion raises `SelectionChanged` and updates text.
- Host styling adopts picker/flyout palettes, so suggestion surfaces align with Fluent colors without invoking Avalonia popups.

## ComboBoxWidget
- Widget-first drop-down selection list using the same suggestion shell as auto-complete but driven by discrete items.
- `ComboBoxWidgetValue` supports static collections, deferred providers, selected item binding, and display member paths.
- Emits `SelectionChanged` and manages drop-down state without Avalonia popups, making it suitable for virtualized layouts.
- Drop-down rows render through the new `ComboBoxItemWidget`, which maps pointer/pressed/disabled states to `WidgetFluentPalette.Picker` brushes for Fluent-accurate highlighting.

## Palette summary
- `RangeBasePalette` captures `SliderTrackFill`, `SliderTrackValueFill`, and `AccentControlBorderBrush` to keep track, indicator, and thumb visuals consistent.
- `ScrollBarPalette` layers `ScrollBarTrackFill`, `ScrollBarThumbFill*`, and `ScrollBarTrackBorderThemeThickness` to produce Fluent-accurate scrollbars.
- `PickerPalette` consolidates `DatePicker`/`TimePicker` button resources ahead of future picker widgets, ensuring popup scaffolding inherits Fluent chroming.
