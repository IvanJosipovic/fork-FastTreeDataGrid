using System.Collections.Generic;
using FastTreeDataGrid.WidgetsDemo.ViewModels.Widgets;

namespace FastTreeDataGrid.WidgetsDemo.ViewModels;

public static class WidgetsGalleryScenarioFactory
{
    public static IReadOnlyList<WidgetsGalleryScenario> Create()
    {
        var scenarios = new List<WidgetsGalleryScenario>
        {
            CreateScenario(
                "Theme & Widget Infrastructure",
                "Fluent palette integration and shared descriptor primitives lay the groundwork for every widget family.",
                new[]
                {
                    "WidgetStyleManager centralizes Fluent resource lookups for borders, typography, and selection chrome.",
                    "Palette records unify brushes across buttons, toggles, menus, and range controls without per-frame churn.",
                    "Descriptor primitives share geometry/brush payloads so new widgets avoid additional allocations.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Buttons",
                    "Text Widgets",
                    "Icon & Geometry",
                })),
            CreateScenario(
                "Layout & Panel Widgets",
                "Immediate-mode panel widgets mirror Avalonia layout semantics while reusing FastTreeDataGrid batching.",
                new[]
                {
                    "Panel adapters translate Avalonia measure/arrange into pooled widget surfaces.",
                    "Canvas, Grid, Relative, UniformGrid, and SplitView widgets cover dashboard and shell scenarios.",
                    "Demo includes an absolute Canvas overlay and slider-driven virtualizing stack showcase.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Canvas Layout",
                    "Grid Layout",
                    "Split View Layout",
                })),
            CreateScenario(
                "Content & Decorators",
                "Bordered, headered, and scrolling hosts supply Fluent chrome while keeping virtualization intact.",
                new[]
                {
                    "Border, ContentControl, and Decorator widgets inherit Fluent padding and corner radius automatically.",
                    "Expander and GroupBox align header typography and glyphs with shared palette data.",
                    "ScrollViewer widget offsets pooled content without breaking FastTreeDataGrid virtualization.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Content Surfaces",
                    "Headered Content",
                    "Scroll Viewer",
                    "Transitioning Content",
                })),
            CreateScenario(
                "Text, Media & Iconography",
                "Text inputs and media surfaces deliver immediate-mode rendering with shared typography palettes.",
                new[]
                {
                    "TextBlock, selectable text, and document spans reuse Fluent typography and highlight brushes.",
                    "TextInput and MaskedTextBox widgets provide caret, selection, and IME support without templated controls.",
                    "Bitmap, icon element, and path icon widgets cache resources for sharp rendering across themes.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Text Widgets",
                    "Text Input",
                    "Media & Icons",
                })),
            CreateScenario(
                "Buttons & Command Surfaces",
                "Command surfaces share a unified gesture pipeline and expose automation metadata for accessibility.",
                new[]
                {
                    "Button family shares pointer state and palette plumbing for primary, secondary, and disabled variants.",
                    "ToggleSwitch, split, and spinner widgets inherit common button logic to stay allocation-free.",
                    "Menu widgets light up access keys, accelerators, and overlay hosting for keyboard parity.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Buttons",
                    "Toggle Switch",
                    "Menu Widgets",
                })),
            CreateScenario(
                "Toggle, Range & Value Pickers",
                "Range controls and pickers reuse pooled overlays and Fluent palettes for consistent affordances.",
                new[]
                {
                    "CheckBox, radio, slider, and progress surfaces read from the shared range palette.",
                    "NumericUpDown, calendar, and date/time pickers reuse popup infrastructure for smooth transitions.",
                    "Scroll bars integrate with the virtualization host to keep scrolling responsive under load.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Check Boxes",
                    "Sliders",
                    "Scroll Bars",
                    "Numeric UpDown",
                })),
            CreateScenario(
                "ItemsControl Shims & Selection Containers",
                "Familiar collection controls wrap FlatTreeDataGrid sources to retain virtualization and selection semantics.",
                new[]
                {
                    "ItemsControl, ListBox, and TreeView widgets adapt Avalonia APIs onto FastTreeDataGrid data sources.",
                    "Adapter layers translate add/remove, selection, and expansion events into source mutations.",
                    "Samples showcase hierarchical data, lazy loading, and migration from stock controls.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "ItemsControl Widget",
                    "ListBox Widget",
                    "TreeView Widget",
                })),
            CreateScenario(
                "Menu & Tab Interaction Parity",
                "Tabbed navigation and menu layers match Avalonia keyboard, pointer, and overlay behaviors.",
                new[]
                {
                    "TabControlWidget mirrors Fluent tab strips with indicator animations and pooled content.",
                    "MenuBar and MenuWidget honor Alt navigation, access keys, and accelerator text updates.",
                    "ComboBox builds on shared drop-down infrastructure for consistent selection affordances.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Tab Control",
                    "Menu Widgets",
                    "Combo Box",
                })),
            CreateScenario(
                "Shapes & Vector Visuals",
                "Shape and geometry widgets reuse cached visuals for crisp vector rendering.",
                new[]
                {
                    "Shape widgets map directly to Avalonia geometries with shared brushes and dash patterns.",
                    "Icon and geometry boards highlight vector reuse with stretch modes and padding.",
                    "Media widgets verify bitmap + glyph rendering on the same immediate-mode surface.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    "Shapes",
                    "Icon & Geometry",
                    "Media & Icons",
                })),
            CreateScenario(
                "Validation, Samples & Documentation",
                "Testing, docs, and sample coverage ensure the expanded widget surface stays approachable.",
                new[]
                {
                    "Gallery hub surfaces every widget family so scenarios remain easy to discover.",
                    "Virtualizing stack layout board validates pooled rendering under slider stress.",
                    "Progress instrumentation pairs with regression tests and docs to lock in expected behavior.",
                },
                WidgetBoardFactory.CreateBoardsByTitle(new[]
                {
                    WidgetBoardFactory.VirtualizingBoardTitle,
                    "Progress Bars",
                    "Stack Layout (Horizontal)",
                })),
        };

        return scenarios;
    }

    private static WidgetsGalleryScenario CreateScenario(string title, string summary, IEnumerable<string> highlights, IReadOnlyList<WidgetBoard> boards)
    {
        return new WidgetsGalleryScenario(title, summary, highlights, boards);
    }
}
