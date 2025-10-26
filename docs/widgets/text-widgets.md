# Text Widgets

FastTreeDataGrid ships lightweight text widgets that mirror Avalonia text controls without instantiating templated elements. They reuse the immediate-mode renderer so text drawing, selection, and span styling execute without bindings or layout passes.

## TextBlockWidget
- Wraps and trims text like `TextBlock` while honouring `TextAlignment`, `TextTrimming`, and `EmSize`.
- Use directly inside cell templates or dashboards:

```csharp
new TextBlockWidget
{
    EmSize = 13,
    Trimming = TextTrimming.CharacterEllipsis,
}.SetText("Immediate-mode text rendering mirrors TextBlock behaviour.");
```

## LabelWidget
- Inherits from `TextBlockWidget` and applies Fluent typography defaults for captions.
- Ideal for headers or short annotations next to interactive widgets.

## SelectableTextWidget
- Supports pointer selection and caret rendering without relying on `SelectableTextBlock`.
- Drag to highlight text; the widget renders selection rectangles and a caret inside the surface.
- The current selection is available via `GetSelectedText()` and can be preset through `SelectableTextWidgetValue` captured by value providers.

```csharp
var selectable = new SelectableTextWidget
{
    DesiredWidth = 320,
    SelectionBrush = new ImmutableSolidColorBrush(Color.FromArgb(128, 99, 102, 241)),
    CaretBrush = new ImmutableSolidColorBrush(Color.FromRgb(30, 64, 175)),
};
selectable.SetText("Drag to select text without Avalonia controls.");
```

## DocumentTextWidget
- Renders rich inline spans using `DocumentTextWidgetValue` and `DocumentTextSpan` to control colour, weight, and style.
- Internally reuses `FormattedText`, so span updates remain allocation friendly.

```csharp
var document = new DocumentTextWidget { EmSize = 12 };
document.SetDocument(new DocumentTextWidgetValue(new []
{
    new DocumentTextSpan("Rich text "),
    new DocumentTextSpan("spans ", new ImmutableSolidColorBrush(Color.FromRgb(37, 99, 235)), FontWeight: FontWeight.Bold),
    new DocumentTextSpan("inside a widget.", new ImmutableSolidColorBrush(Color.FromRgb(220, 38, 38)), FontStyle: FontStyle.Italic),
}));
```

## TextInputWidget
- Immediate-mode text entry with caret blinking, selection, placeholder text, and optional read-only mode.
- Keyboard navigation supports basic editing (arrows, Home/End, Backspace/Delete, Ctrl+A).

```csharp
var input = new TextInputWidget
{
    DesiredWidth = 260,
    Placeholder = "Type a message...",
};
input.Text = "Hello widgets";
input.TextChanged += (_, args) => Console.WriteLine($"New text: {args.NewValue}");
```

## MaskedTextBoxWidget
- Extends `TextInputWidget` and masks characters with a configurable glyph (default ●). Toggle `RevealText` to temporarily show the underlying value.

```csharp
var password = new MaskedTextBoxWidget
{
    DesiredWidth = 220,
    MaskChar = '●',
    Placeholder = "Password",
};
password.Text = "Secret123";
```

## Value descriptors
- `SelectableTextWidgetValue` supplies text plus optional selection start/length for data-driven scenarios.
- `DocumentTextWidgetValue` composes multiple `DocumentTextSpan` instances so cell templates can reuse styled snippets from view models.

## Samples
- The **Widgets** tab in the demo app now includes a "Text Widgets" board showcasing text wrapping, selection, and document spans.
- `WidgetSamplesFactory` exposes gallery nodes for text widgets so you can inspect usage directly inside the FastTreeDataGrid sample browser.
