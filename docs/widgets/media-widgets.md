# Media & Icon Widgets

These widgets bring Avalonia's image and icon surfaces into the FastTreeDataGrid immediate-mode renderer without templated controls.

## ImageWidget
- Accepts any `IImage` (including `IBitmap` and `RenderTargetBitmap`).
- Supports `Stretch`, `StretchDirection`, and `Padding` to control scaling behavior.
- Example:

```csharp
new ImageWidget
{
    Source = assetBitmap,
    Stretch = Stretch.Uniform,
    Padding = 6,
    DesiredWidth = 96,
    DesiredHeight = 96,
};
```

## IconElementWidget
- Mirrors Avalonia's `IconElement` hierarchy: can render geometry, image sources, or parsed path data.
- Optional background brush, padding, stroke, and stretch settings.
- Use `IconElementWidgetValue` to populate a widget from data providers, or call `SetIcon`/`SetImage` directly.

```csharp
var icon = new IconElementWidget
{
    DesiredWidth = 48,
    DesiredHeight = 48,
};
icon.UpdateValue(null, new IconElementWidgetValue(
    Geometry: StreamGeometry.Parse("M4,12 L12,4 28,20 20,28 Z"),
    Foreground: new ImmutableSolidColorBrush(Colors.White),
    Background: new ImmutableSolidColorBrush(Color.FromRgb(16, 185, 129)),
    Padding: 8));
```

## PathIconWidget
- Convenience wrapper that parses a path string (e.g., `Geometry.Parse` data) into an icon.
- Backed by the same immediate-mode renderer as `IconElementWidget`.

```csharp
var downloadIcon = new PathIconWidget
{
    DesiredWidth = 48,
    DesiredHeight = 48,
};
downloadIcon.UpdateValue(null, new PathIconWidgetValue(
    Data: "M8,4 L24,4 L28,12 L28,28 L8,28 Z M18,10 L18,18 24,18 16,28 16,20 10,20 18,10 Z",
    Foreground: new ImmutableSolidColorBrush(Color.FromRgb(55, 48, 163)),
    Padding: 6));
```

## Data descriptors
- `ImageWidgetValue` carries an `IImage` source along with stretch and padding metadata.
- `IconElementWidgetValue` aggregates geometry, images, background, and path data into a single payload.
- `PathIconWidgetValue` keeps lightweight path/icon metadata for easy value-provider usage.

## Samples
- The demo app's **Media & Icons** board showcases all three widgets.
- Gallery entries in `WidgetSamplesFactory` demonstrate usage in the `Widgets` tab backed by `FastTreeDataGrid` rows.
