using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.WidgetsDemo.ViewModels.Widgets;

internal static class WidgetBoardFactory
{
    internal const string VirtualizingBoardTitle = "Virtualizing Stack Layout";

    private sealed record BoardFactoryEntry(string Title, Func<WidgetBoard> Factory);

    private static readonly ImmutableArray<BoardFactoryEntry> BoardFactories = ImmutableArray.Create(
        new BoardFactoryEntry("Buttons", CreateButtonBoard),
        new BoardFactoryEntry("Check Boxes", CreateCheckBoxBoard),
        new BoardFactoryEntry("Toggle Switch", CreateToggleBoard),
        new BoardFactoryEntry("Radio Buttons", CreateRadioBoard),
        new BoardFactoryEntry("Sliders", CreateSliderBoard),
        new BoardFactoryEntry("Numeric UpDown", CreateNumericUpDownBoard),
        new BoardFactoryEntry("Scroll Bars", CreateScrollBarBoard),
        new BoardFactoryEntry("Calendar", CreateCalendarBoard),
        new BoardFactoryEntry("Date Picker", CreateDatePickerBoard),
        new BoardFactoryEntry("Calendar Date Picker", CreateCalendarDatePickerBoard),
        new BoardFactoryEntry("Time Picker", CreateTimePickerBoard),
        new BoardFactoryEntry("AutoComplete", CreateAutoCompleteBoard),
        new BoardFactoryEntry("Combo Box", CreateComboBoxBoard),
        new BoardFactoryEntry("ItemsControl Widget", CreateItemsControlBoard),
        new BoardFactoryEntry("ListBox Widget", CreateListBoxBoard),
        new BoardFactoryEntry("TreeView Widget", CreateTreeViewBoard),
        new BoardFactoryEntry("Tab Control", CreateTabControlBoard),
        new BoardFactoryEntry("Menu Widgets", CreateMenuBoard),
        new BoardFactoryEntry("Content Surfaces", CreateContentSurfacesBoard),
        new BoardFactoryEntry("Headered Content", CreateHeaderedContentBoard),
        new BoardFactoryEntry("Scroll Viewer", CreateScrollViewerBoard),
        new BoardFactoryEntry("Badges", CreateBadgeBoard),
        new BoardFactoryEntry("Stack Layout (Horizontal)", CreateHorizontalLayoutBoard),
        new BoardFactoryEntry("Canvas Layout", CreateCanvasLayoutBoard),
        new BoardFactoryEntry("Relative Layout", CreateRelativeLayoutBoard),
        new BoardFactoryEntry("Stack Layout (Vertical)", CreateVerticalLayoutBoard),
        new BoardFactoryEntry("Wrap Layout", CreateWrapLayoutBoard),
        new BoardFactoryEntry("Uniform Grid Layout", CreateUniformGridLayoutBoard),
        new BoardFactoryEntry("Grid Layout", CreateGridLayoutBoard),
        new BoardFactoryEntry("Dock Layout", CreateDockLayoutBoard),
        new BoardFactoryEntry("Split View Layout", CreateSplitViewLayoutBoard),
        new BoardFactoryEntry("Layout Transform", CreateLayoutTransformLayoutBoard),
        new BoardFactoryEntry("Viewbox Layout", CreateViewboxLayoutBoard),
        new BoardFactoryEntry(VirtualizingBoardTitle, CreateVirtualizingStackBoard),
        new BoardFactoryEntry("Transitioning Content", CreateTransitioningContentBoard),
        new BoardFactoryEntry("Text Widgets", CreateTextBoard),
        new BoardFactoryEntry("Text Input", CreateTextInputBoard),
        new BoardFactoryEntry("Media & Icons", CreateMediaBoard),
        new BoardFactoryEntry("Progress Bars", CreateProgressBoard),
        new BoardFactoryEntry("Icon & Geometry", CreateIconGeometryBoard),
        new BoardFactoryEntry("Shapes", CreateShapesBoard));

    private static readonly ImmutableDictionary<string, Func<WidgetBoard>> BoardFactoryLookup =
        BoardFactories.ToImmutableDictionary(entry => entry.Title, entry => entry.Factory);

    public static IReadOnlyList<WidgetBoard> CreateBoards(IReadOnlyList<WidgetGalleryNode> gallery)
    {
        _ = gallery;
        return BoardFactories.Select(entry => entry.Factory()).ToList();
    }

    public static IReadOnlyList<WidgetBoard> CreateBoardsByTitle(IEnumerable<string> titles)
    {
        return titles.Select(CreateBoard).ToList();
    }

    public static WidgetBoard CreateBoard(string title)
    {
        if (!BoardFactoryLookup.TryGetValue(title, out var factory))
        {
            throw new ArgumentException($"Unknown widget board '{title}'.", nameof(title));
        }

        return factory();
    }

    private static WidgetBoard CreateButtonBoard()
    {
        var root = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 10,
        };

        root.Children.Add(CreateLabeledRow("Primary", ConfigureButton("Primary", "Primary")));
        root.Children.Add(CreateLabeledRow("Secondary", ConfigureButton("Secondary", "Secondary")));
        var disabled = ConfigureButton("Secondary", "Disabled");
        disabled.IsEnabled = false;
        root.Children.Add(CreateLabeledRow("Disabled", disabled));
        return WidgetBoard.Create("Buttons", "Primary and secondary button states (enabled, disabled).", root);
    }

    private static WidgetBoard CreateCheckBoxBoard()
    {
        var root = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var checkedBox = new CheckBoxWidget
        {
            StyleKey = "AccentCheck",
            DesiredWidth = 24,
            DesiredHeight = 24,
        };
        checkedBox.SetValue(true);
        root.Children.Add(CreateLabeledRow("Checked", checkedBox));

        var uncheckedBox = new CheckBoxWidget
        {
            StyleKey = "NeutralCheck",
            DesiredWidth = 24,
            DesiredHeight = 24,
        };
        uncheckedBox.SetValue(false);
        root.Children.Add(CreateLabeledRow("Unchecked", uncheckedBox));

        var disabledBox = new CheckBoxWidget
        {
            StyleKey = "NeutralCheck",
            DesiredWidth = 24,
            DesiredHeight = 24,
            IsEnabled = false,
        };
        disabledBox.SetValue(true);
        root.Children.Add(CreateLabeledRow("Disabled", disabledBox));

        return WidgetBoard.Create("Check Boxes", "Tri-state and disabled check boxes with hover/press support.", root);
    }

    private static WidgetBoard CreateToggleBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        stack.Children.Add(CreateToggleRow("On", true, true));
        stack.Children.Add(CreateToggleRow("Off", false, true));
        stack.Children.Add(CreateToggleRow("Disabled", true, false));

        return WidgetBoard.Create("Toggle Switch", "ToggleSwitchWidget on/off/disabled states.", stack);
    }

    private static WidgetBoard CreateRadioBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        stack.Children.Add(CreateRadioRow("Selected", true, true));
        stack.Children.Add(CreateRadioRow("Unselected", false, true));
        stack.Children.Add(CreateRadioRow("Disabled", true, false));

        return WidgetBoard.Create("Radio Buttons", "RadioButtonWidget selection states.", stack);
    }

    private static WidgetBoard CreateSliderBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 16,
        };

        stack.Children.Add(CreateSliderRow("25%", 0, 100, 25, true,
            new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206))));

        stack.Children.Add(CreateSliderRow("65%", 0, 100, 65, true,
            new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114))));

        stack.Children.Add(CreateSliderRow("Disabled", 0, 100, 80, false,
            new ImmutableSolidColorBrush(Color.FromRgb(180, 180, 180))));

        return WidgetBoard.Create("Sliders", "SliderWidget values and disabled appearance.", stack);
    }

    private static WidgetBoard CreateNumericUpDownBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 16,
        };

        stack.Children.Add(CreateNumericRow("Quantity", 0, 100, 12, 1, 0, true));
        stack.Children.Add(CreateNumericRow("Temperature", -50, 150, 23.5, 0.5, 1, true));
        stack.Children.Add(CreateNumericRow("Disabled", 0, 1, 0.3, 0.05, 2, false));

        return WidgetBoard.Create("Numeric UpDown", "NumericUpDownWidget samples with integer and decimal values.", stack);
    }

    private static WidgetBoard CreateScrollBarBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 16,
        };

        stack.Children.Add(CreateScrollBarRow("Vertical", Orientation.Vertical, 0, 100, 24, 18, 160, true));
        stack.Children.Add(CreateScrollBarRow("Vertical Disabled", Orientation.Vertical, 0, 100, 64, 16, 160, false));
        stack.Children.Add(CreateScrollBarRow("Horizontal", Orientation.Horizontal, 0, 1, 0.35, 0.25, 220, true));

        return WidgetBoard.Create("Scroll Bars", "ScrollBarWidget orientation, viewport, and disabled states.", stack);
    }

    private static WidgetBoard CreateCalendarBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var rangeLabel = new FormattedTextWidget
        {
            EmSize = 13,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredHeight = 20,
        };
        rangeLabel.SetText("Selected date: (none)");

        var calendar = new CalendarWidget
        {
            Minimum = DateTime.Today.AddMonths(-2),
            Maximum = DateTime.Today.AddMonths(3),
            SelectedDate = DateTime.Today
        };
        calendar.SelectedDateChanged += (_, args) =>
        {
            if (args.NewValue.HasValue)
            {
                rangeLabel.SetText($"Selected date: {args.NewValue.Value:d}");
            }
            else
            {
                rangeLabel.SetText("Selected date: (none)");
            }
        };

        stack.Children.Add(calendar);
        stack.Children.Add(rangeLabel);

        return WidgetBoard.Create("Calendar", "CalendarWidget month navigation with selection feedback.", stack);
    }

    private static WidgetBoard CreateDatePickerBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var picker = new DatePickerWidget
        {
            SelectedDate = DateTime.Today,
            Minimum = DateTime.Today.AddMonths(-1),
            Maximum = DateTime.Today.AddMonths(2)
        };

        var label = new FormattedTextWidget
        {
            EmSize = 13,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredHeight = 20,
        };
        label.SetText($"Selected: {DateTime.Today:d}");

        picker.SelectedDateChanged += (_, args) =>
        {
            label.SetText(args.NewValue.HasValue
                ? $"Selected: {args.NewValue.Value:d}"
                : "Selected: (none)");
        };

        stack.Children.Add(picker);
        stack.Children.Add(label);

        return WidgetBoard.Create("Date Picker", "DatePickerWidget with inline drop-down calendar.", stack);
    }

    private static WidgetBoard CreateCalendarDatePickerBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var picker = new CalendarDatePickerWidget
        {
            SelectedDate = DateTime.Today,
            Minimum = DateTime.Today.AddDays(-14),
            Maximum = DateTime.Today.AddDays(30)
        };

        var label = new FormattedTextWidget
        {
            EmSize = 13,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredHeight = 20,
        };
        label.SetText($"Calendar date: {DateTime.Today:MMMM dd, yyyy}");

        picker.SelectedDateChanged += (_, args) =>
        {
            label.SetText(args.NewValue.HasValue
                ? $"Calendar date: {args.NewValue.Value:MMMM dd, yyyy}"
                : "Calendar date: (none)");
        };

        stack.Children.Add(picker);
        stack.Children.Add(label);

        return WidgetBoard.Create("Calendar Date Picker", "CalendarDatePickerWidget quick date selection.", stack);
    }

    private static WidgetBoard CreateTimePickerBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var picker = new TimePickerWidget
        {
            Time = TimeSpan.FromHours(13).Add(TimeSpan.FromMinutes(45)),
            Minimum = TimeSpan.FromHours(8),
            Maximum = TimeSpan.FromHours(20),
        };

        var label = new FormattedTextWidget
        {
            EmSize = 13,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredHeight = 20,
        };
        label.SetText("Time: 13:45");

        picker.TimeChanged += (_, args) =>
        {
            label.SetText(args.NewValue.HasValue
                ? $"Time: {args.NewValue.Value:hh\\:mm}"
                : "Time: (none)");
        };

        stack.Children.Add(picker);
        stack.Children.Add(label);

        return WidgetBoard.Create("Time Picker", "TimePickerWidget with hour/minute selectors.", stack);
    }

    private static WidgetBoard CreateAutoCompleteBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var auto = new AutoCompleteBoxWidget
        {
            Items = ImmutableArray.Create(new[]
            {
                "Seattle", "San Francisco", "Los Angeles", "San Diego", "Sacramento", "Portland", "Phoenix", "Salt Lake City"
            })
        };

        var label = new FormattedTextWidget
        {
            EmSize = 13,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredHeight = 20,
        };
        label.SetText("Selected: (none)");

        auto.SelectionChanged += (_, args) =>
        {
            label.SetText(args.NewValue is { Length: > 0 }
                ? $"Selected: {args.NewValue}"
                : "Selected: (none)");
        };

        stack.Children.Add(auto);
        stack.Children.Add(label);

        return WidgetBoard.Create("AutoComplete", "AutoCompleteBoxWidget suggestions list.", stack);
    }

    private static WidgetBoard CreateComboBoxBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var combo = new ComboBoxWidget
        {
            DisplayMember = nameof(SampleOption.Name),
            Items = ImmutableArray.Create<object?>(new SampleOption("High", 3), new SampleOption("Medium", 2), new SampleOption("Low", 1)),
            SelectedItem = new SampleOption("High", 3)
        };

        var label = new FormattedTextWidget
        {
            EmSize = 13,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredHeight = 20,
        };
        label.SetText("Selection: High");

        combo.SelectionChanged += (_, args) =>
        {
            if (args.NewValue is SampleOption option)
            {
                label.SetText($"Selection: {option.Name}");
            }
        };

        stack.Children.Add(combo);
        stack.Children.Add(label);

        return WidgetBoard.Create("Combo Box", "ComboBoxWidget selection sample.", stack);
    }

    private sealed record SampleOption(string Name, int Priority);

    private static WidgetBoard CreateItemsControlBoard()
    {
        var items = new[] { "Overview", "Timeline", "Discussions", "Files", "Checklist", "Analytics" };

        var itemsControl = new ItemsControlWidget
        {
            ItemsSource = items,
            ItemTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
            {
                EmSize = 13,
                Trimming = TextTrimming.CharacterEllipsis
            }),
            ItemExtent = 28,
            Padding = new Thickness(4),
            DesiredWidth = 220,
            DesiredHeight = 160,
        };

        var border = new BorderWidget
        {
            Background = new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xF8, 0xFF)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xD8, 0xE0, 0xF0)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = itemsControl
        };

        return WidgetBoard.Create("ItemsControl Widget", "ItemsControlWidget renders pooled list entries with a lightweight template.", border);
    }

    private static WidgetBoard CreateListBoxBoard()
    {
        var departments = new[] { "All", "Design", "Engineering", "Product", "Research", "Operations" };

        var list = new ListBoxWidget
        {
            ItemsSource = departments,
            DesiredWidth = 220,
            DesiredHeight = 180,
            Padding = new Thickness(4),
        };
        list.SelectedIndex = 1;

        var border = new BorderWidget
        {
            Background = new ImmutableSolidColorBrush(Color.FromRgb(0xFA, 0xFB, 0xFF)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xD2, 0xDA, 0xED)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = list
        };

        return WidgetBoard.Create("ListBox Widget", "ListBoxWidget applies Fluent selection styling with immediate-mode rows.", border);
    }

    private static WidgetBoard CreateTreeViewBoard()
    {
        var roadmap = new[]
        {
            new SimpleTreeNode("Design System", new[]
            {
                new SimpleTreeNode("Foundations", Array.Empty<SimpleTreeNode>()),
                new SimpleTreeNode("Components", new[]
                {
                    new SimpleTreeNode("Buttons", Array.Empty<SimpleTreeNode>()),
                    new SimpleTreeNode("Inputs", Array.Empty<SimpleTreeNode>()),
                    new SimpleTreeNode("Shell", Array.Empty<SimpleTreeNode>()),
                }),
                new SimpleTreeNode("Guidelines", Array.Empty<SimpleTreeNode>())
            }),
            new SimpleTreeNode("Product Areas", new[]
            {
                new SimpleTreeNode("Workspace", Array.Empty<SimpleTreeNode>()),
                new SimpleTreeNode("Automations", new[]
                {
                    new SimpleTreeNode("Approvals", Array.Empty<SimpleTreeNode>()),
                    new SimpleTreeNode("Notifications", Array.Empty<SimpleTreeNode>())
                })
            })
        };

        var tree = new TreeViewWidget
        {
            ItemsSource = roadmap,
            ItemChildrenSelector = item => item is SimpleTreeNode node ? node.Children : Array.Empty<SimpleTreeNode>(),
            ItemTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
            {
                EmSize = 13,
                Trimming = TextTrimming.CharacterEllipsis
            }),
            DesiredWidth = 260,
            DesiredHeight = 200,
            Padding = new Thickness(4),
        };
        tree.ExpandToLevel(1);

        var border = new BorderWidget
        {
            Background = new ImmutableSolidColorBrush(Color.FromRgb(0xF4, 0xF8, 0xFF)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xC9, 0xD8, 0xF4)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = tree
        };

        return WidgetBoard.Create("TreeView Widget", "TreeViewWidget mirrors hierarchical navigation with expander glyphs.", border);
    }

    private sealed record SimpleTreeNode(string Name, IReadOnlyList<SimpleTreeNode> Children)
    {
        public override string ToString() => Name;
    }

    private sealed record TabSample(string Title, string Description, string Badge);

    private static WidgetBoard CreateTabControlBoard()
    {
        var samples = new[]
        {
            new TabSample("Overview", "Immediate-mode tabs reuse pooled batching so switching stays silky.", "Insights"),
            new TabSample("Details", "Header and content factories give full control without templated controls.", "Details"),
            new TabSample("Settings", "Selection indicator, padding, and typography flow from Fluent resources.", "Settings"),
        };

        var tabControl = new TabControlWidget
        {
            DesiredWidth = 340,
            DesiredHeight = 220,
        };

        var value = new TabControlWidgetValue(
            Items: samples,
            SelectedIndex: 0,
            HeaderFactory: (_, item) => CreateTabHeader((TabSample)item!),
            ContentFactory: (_, item) => CreateTabContent((TabSample)item!));

        tabControl.UpdateValue(null, value);

        return WidgetBoard.Create("Tab Control", "TabControlWidget renders Fluent tab headers and pooled content.", tabControl);

        static Widget CreateTabHeader(TabSample sample)
        {
            var header = new FormattedTextWidget
            {
                EmSize = 13,
                DesiredHeight = 20,
            };
            header.SetText(sample.Title);
            return header;
        }

        static Widget CreateTabContent(TabSample sample)
        {
            var stack = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
                Padding = new Thickness(16, 12, 16, 12),
            };

            var badge = new BadgeWidget
            {
                DesiredHeight = 22,
                BackgroundBrush = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                ForegroundBrush = new ImmutableSolidColorBrush(Colors.White),
                Padding = 6,
            };
            badge.SetText(sample.Badge);
            badge.RefreshStyle();

            var description = new FormattedTextWidget
            {
                EmSize = 12,
                DesiredWidth = 260,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(82, 90, 110)),
                Trimming = TextTrimming.CharacterEllipsis,
            };
            description.SetText(sample.Description);

            var progress = new ProgressWidget
            {
                DesiredHeight = 4,
                Progress = 0.65,
            };

            stack.Children.Add(badge);
            stack.Children.Add(description);
            stack.Children.Add(progress);
            return stack;
        }
    }

    private static WidgetBoard CreateMenuBoard()
    {
        var layout = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var menuBar = new MenuBarWidget
        {
            DesiredWidth = 340,
            DesiredHeight = 44,
        };

        var menuBarItems = new[]
        {
            new MenuBarItemWidgetValue("_File", new MenuWidgetValue(new[]
            {
                new MenuItemWidgetValue("_New", GestureText: "Ctrl+N"),
                new MenuItemWidgetValue("_Open…", GestureText: "Ctrl+O"),
                new MenuItemWidgetValue(string.Empty, IsSeparator: true),
                new MenuItemWidgetValue("_Export", SubMenu: new MenuWidgetValue(new[]
                {
                    new MenuItemWidgetValue("_Csv"),
                    new MenuItemWidgetValue("_Json"),
                    new MenuItemWidgetValue("_Xml"),
                })),
                new MenuItemWidgetValue("E_xit", GestureText: "Alt+F4"),
            })),
            new MenuBarItemWidgetValue("_Edit", new MenuWidgetValue(new[]
            {
                new MenuItemWidgetValue("_Undo", GestureText: "Ctrl+Z"),
                new MenuItemWidgetValue("_Redo", GestureText: "Ctrl+Y"),
                new MenuItemWidgetValue(string.Empty, IsSeparator: true),
                new MenuItemWidgetValue("_Preferences…"),
            })),
            new MenuBarItemWidgetValue("_Help", new MenuWidgetValue(new[]
            {
                new MenuItemWidgetValue("_Documentation"),
                new MenuItemWidgetValue("_Report Issue"),
                new MenuItemWidgetValue(string.Empty, IsSeparator: true),
                new MenuItemWidgetValue("_About FastTreeDataGrid"),
            })),
        };
        menuBar.UpdateValue(null, new MenuBarWidgetValue(menuBarItems));
        layout.Children.Add(menuBar);

        var menuChrome = new BorderWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 150,
            Background = new ImmutableSolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFF)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xD4, 0xDC, 0xED)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
        };

        var menu = new MenuWidget
        {
            DesiredWidth = 260,
            DesiredHeight = 138,
        };

        var menuItems = new[]
        {
            new MenuItemWidgetValue("_Rename", GestureText: "F2"),
            new MenuItemWidgetValue("_Duplicate"),
            new MenuItemWidgetValue("_Archive"),
            new MenuItemWidgetValue(string.Empty, IsSeparator: true),
            new MenuItemWidgetValue("_Delete", GestureText: "Shift+Delete"),
        };
        menu.UpdateValue(null, new MenuWidgetValue(menuItems));
        menuChrome.Child = menu;
        layout.Children.Add(menuChrome);

        return WidgetBoard.Create("Menu Widgets", "MenuBarWidget and MenuWidget demonstrate keyboard-aware command surfaces.", layout);
    }

    private static WidgetBoard CreateContentSurfacesBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var border = new BorderWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 72,
            CornerRadius = new CornerRadius(12),
            Background = new ImmutableSolidColorBrush(Color.FromRgb(0xF3, 0xF6, 0xFF)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xD7, 0xF3)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
        };
        var borderText = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(78, 88, 112)),
            DesiredWidth = 240,
            Trimming = TextTrimming.WordEllipsis,
        };
        borderText.SetText("BorderWidget adds Fluent chrome and pooled padding without templated controls.");
        border.Child = borderText;
        stack.Children.Add(CreateLabeledRow("BorderWidget", border));

        var content = new ContentControlWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 96,
        };

        var contentStack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };

        var header = new FormattedTextWidget
        {
            EmSize = 14,
            FontWeight = FontWeight.SemiBold,
            DesiredHeight = 22,
        };
        header.SetText("Status Card");

        var details = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(90, 96, 120)),
            DesiredWidth = 240,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        details.SetText("ContentControlWidget hosts arbitrary composed widgets with theme-aware spacing.");

        var action = ConfigureButton("Primary", "View details");
        action.DesiredWidth = 120;
        action.DesiredHeight = 32;

        contentStack.Children.Add(header);
        contentStack.Children.Add(details);
        contentStack.Children.Add(action);

        content.Content = contentStack;
        stack.Children.Add(CreateLabeledRow("ContentControl", content));

        var decorator = new DecoratorWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 60,
        };

        var decoratorRow = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
        };

        var accent = new BorderWidget
        {
            DesiredWidth = 8,
            DesiredHeight = 44,
            CornerRadius = new CornerRadius(4),
            Background = new ImmutableSolidColorBrush(Color.FromRgb(79, 154, 255)),
        };

        var decoratorText = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(78, 88, 112)),
            DesiredWidth = 240,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        decoratorText.SetText("DecoratorWidget skips chrome while still applying pooled layout padding.");

        decoratorRow.Children.Add(accent);
        decoratorRow.Children.Add(decoratorText);

        decorator.Content = decoratorRow;
        stack.Children.Add(CreateLabeledRow("Decorator", decorator));

        return WidgetBoard.Create("Content Surfaces", "Border, ContentControl, and Decorator widgets wrap child content with Fluent chrome.", stack);
    }

    private static WidgetBoard CreateHeaderedContentBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var expander = new ExpanderWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 120,
        };
        expander.HeaderText = "Filters";

        var filterStack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };
        filterStack.Children.Add(CreateToggleRow("Errors", true, true));
        filterStack.Children.Add(CreateToggleRow("Warnings", false, true));
        filterStack.Children.Add(CreateToggleRow("Info", true, true));

        expander.Content = filterStack;
        stack.Children.Add(CreateLabeledRow("Expander", expander));

        var group = new GroupBoxWidget
        {
            HeaderText = "Notifications",
            DesiredWidth = 280,
            DesiredHeight = 120,
        };

        var checkStack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };
        checkStack.Children.Add(CreateCheckOptionRow("Email alerts", true));
        checkStack.Children.Add(CreateCheckOptionRow("SMS alerts", false));
        checkStack.Children.Add(CreateCheckOptionRow("Push notifications", true));

        group.Content = checkStack;
        stack.Children.Add(CreateLabeledRow("GroupBox", group));

        return WidgetBoard.Create("Headered Content", "ExpanderWidget and GroupBoxWidget demonstrate header typography and collapsible sections.", stack);
    }

    private static WidgetBoard CreateScrollViewerBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var chrome = new BorderWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 150,
            Background = new ImmutableSolidColorBrush(Color.FromRgb(0xF3, 0xF6, 0xFF)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xD7, 0xF3)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(6),
        };

        var scroll = new ScrollViewerWidget
        {
            DesiredWidth = 260,
            DesiredHeight = 138,
            Padding = new Thickness(4),
            VerticalOffset = 24,
            ExtentHeight = 260,
        };

        var items = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
        };

        var badges = new (string Label, ImmutableSolidColorBrush Background, ImmutableSolidColorBrush Foreground)[]
        {
            ("Overview", new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), new ImmutableSolidColorBrush(Colors.White)),
            ("Billing", new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)), new ImmutableSolidColorBrush(Colors.White)),
            ("Activity", new ImmutableSolidColorBrush(Color.FromRgb(250, 204, 21)), new ImmutableSolidColorBrush(Color.FromRgb(120, 53, 15))),
            ("Audit Trail", new ImmutableSolidColorBrush(Color.FromRgb(244, 114, 182)), new ImmutableSolidColorBrush(Colors.White)),
            ("Exports", new ImmutableSolidColorBrush(Color.FromRgb(96, 165, 250)), new ImmutableSolidColorBrush(Colors.White)),
            ("Automation", new ImmutableSolidColorBrush(Color.FromRgb(16, 185, 129)), new ImmutableSolidColorBrush(Colors.White)),
        };

        foreach (var badgeInfo in badges)
        {
            var badge = new BadgeWidget
            {
                DesiredWidth = 160,
                DesiredHeight = 28,
                BackgroundBrush = badgeInfo.Background,
                ForegroundBrush = badgeInfo.Foreground,
                Padding = 8,
            };
            badge.SetText(badgeInfo.Label);
            badge.RefreshStyle();
            items.Children.Add(badge);
        }

        scroll.Children.Add(items);
        chrome.Child = scroll;
        stack.Children.Add(CreateLabeledRow("ScrollViewer", chrome));

        return WidgetBoard.Create("Scroll Viewer", "ScrollViewerWidget offsets pooled children within a clipped viewport.", stack);
    }

    private static WidgetBoard CreateBadgeBoard()
    {
        var wrap = new WrapLayoutWidget
        {
            Padding = new Thickness(16),
            Spacing = 12,
            DefaultItemWidth = 150,
            DefaultItemHeight = 40,
        };

        wrap.Children.Add(CreateLabeledBadge("Info", new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10 }, "INFO", new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), new ImmutableSolidColorBrush(Colors.White)));
        wrap.Children.Add(CreateLabeledBadge("Success", new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10 }, "SUCCESS", new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)), new ImmutableSolidColorBrush(Colors.White)));
        wrap.Children.Add(CreateLabeledBadge("Warning", new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10 }, "WARN", new ImmutableSolidColorBrush(Color.FromRgb(255, 191, 0)), new ImmutableSolidColorBrush(Color.FromRgb(90, 60, 0))));
        wrap.Children.Add(CreateLabeledBadge("Neutral", new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10 }, "NEW", new ImmutableSolidColorBrush(Color.FromRgb(230, 230, 230)), new ImmutableSolidColorBrush(Color.FromRgb(70, 70, 70))));

        return WidgetBoard.Create("Badges", "BadgeWidget pill samples.", wrap);
    }

    private static WidgetBoard CreateHorizontalLayoutBoard()
    {
        var layout = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Padding = new Thickness(16),
            Spacing = 16,
        };

        layout.Children.Add(CreateToggleRow("Toggle", true, true));
        layout.Children.Add(CreateSliderRow("Volume", 0, 100, 40, true, new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206))));
        layout.Children.Add(CreateLabeledBadge("Status", new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10 }, "SYNC", new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)), new ImmutableSolidColorBrush(Colors.White)));

        return WidgetBoard.Create("Stack Layout (Horizontal)", "StackLayoutWidget arranged horizontally with interactive widgets.", layout);
    }

    private static WidgetBoard CreateCanvasLayoutBoard()
    {
        var canvas = new CanvasLayoutWidget();

        var surface = new BorderWidget
        {
            DesiredWidth = 360,
            DesiredHeight = 160,
            CornerRadius = new CornerRadius(14),
            Background = new ImmutableSolidColorBrush(Color.FromRgb(244, 247, 252)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(214, 226, 247)),
            BorderThickness = new Thickness(1),
        };
        canvas.Children.Add(surface);

        var warning = new BadgeWidget
        {
            DesiredWidth = 96,
            DesiredHeight = 30,
        };
        warning.SetText("Warning");
        canvas.Children.Add(warning);
        canvas.SetLeft(warning, 18);
        canvas.SetTop(warning, 18);

        var description = new FormattedTextWidget
        {
            EmSize = 13,
            DesiredWidth = 240,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(67, 80, 103)),
        };
        description.SetText("CanvasLayoutWidget positions widgets absolutely for overlays and alerts.");
        canvas.Children.Add(description);
        canvas.SetLeft(description, 18);
        canvas.SetTop(description, 60);

        var dismiss = ConfigureButton("Secondary", "Dismiss");
        dismiss.DesiredWidth = 110;
        dismiss.DesiredHeight = 34;
        canvas.Children.Add(dismiss);
        canvas.SetRight(dismiss, 24);
        canvas.SetBottom(dismiss, 24);

        var progress = new ProgressWidget
        {
            DesiredWidth = 280,
            DesiredHeight = 8,
            Progress = 0.6,
            TrackForeground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            TrackBackground = new ImmutableSolidColorBrush(Color.FromRgb(222, 234, 252)),
        };
        canvas.Children.Add(progress);
        canvas.SetLeft(progress, 18);
        canvas.SetBottom(progress, 28);

        return WidgetBoard.Create("Canvas Layout", "CanvasLayoutWidget with absolute card overlays.", canvas);
    }

    private static WidgetBoard CreateRelativeLayoutBoard()
    {
        var relative = new RelativePanelLayoutWidget
        {
            Padding = new Thickness(16),
        };

        var header = new BadgeWidget
        {
            DesiredWidth = 96,
            DesiredHeight = 28,
        };
        header.SetText("Profile");
        relative.Children.Add(header);
        relative.SetAlignLeftWithPanel(header, true);
        relative.SetAlignTopWithPanel(header, true);

        var avatar = new IconWidget
        {
            DesiredWidth = 52,
            DesiredHeight = 52,
        };
        avatar.SetIcon(WidgetSamplesFactory.CreatePolygonGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(99, 200, 255)), null, 4);
        relative.Children.Add(avatar);
        relative.SetBelow(avatar, header);
        relative.SetAlignLeftWithPanel(avatar, true);
        relative.SetMargin(avatar, 6);

        var summary = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
        };
        var name = new FormattedTextWidget
        {
            EmSize = 14,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(44, 62, 103)),
            DesiredWidth = 180,
        };
        name.SetText("Alicia Rivers");
        var subtitle = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(107, 119, 142)),
            DesiredWidth = 220,
        };
        subtitle.SetText("Design Lead • Updated 2 hours ago");
        summary.Children.Add(name);
        summary.Children.Add(subtitle);
        relative.Children.Add(summary);
        relative.SetRightOf(summary, avatar);
        relative.SetAlignTopWith(summary, avatar);
        relative.SetMargin(summary, 10);

        var actions = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };
        actions.Children.Add(ConfigureButton("Primary", "Message"));
        actions.Children.Add(ConfigureButton("Secondary", "Follow"));
        relative.Children.Add(actions);
        relative.SetBelow(actions, avatar);
        relative.SetAlignLeftWithPanel(actions, true);
        relative.SetMargin(actions, 10);

        return WidgetBoard.Create("Relative Layout", "RelativePanelLayoutWidget aligning widgets by relationships.", relative);
    }

    private static WidgetBoard CreateVerticalLayoutBoard()
    {
        var layout = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 10,
        };

        layout.Children.Add(CreateLabeledRow("Primary Action", ConfigureButton("Primary", "Launch")));
        layout.Children.Add(CreateToggleRow("Notifications", true, true));
        layout.Children.Add(CreateSliderRow("Brightness", 0, 100, 70, true, new ImmutableSolidColorBrush(Color.FromRgb(255, 191, 0))));

        return WidgetBoard.Create("Stack Layout (Vertical)", "StackLayoutWidget arranged vertically with input widgets.", layout);
    }

    private static WidgetBoard CreateWrapLayoutBoard()
    {
        var wrap = new WrapLayoutWidget
        {
            Padding = new Thickness(16),
            Spacing = 12,
            DefaultItemWidth = 160,
            DefaultItemHeight = 48,
        };

        wrap.Children.Add(ConfigureButton("Primary", "Approve"));
        wrap.Children.Add(ConfigureButton("Secondary", "Ignore"));
        var wrapToggle = new ToggleSwitchWidget { DesiredWidth = 120, DesiredHeight = 36 };
        wrapToggle.SetState(false);
        wrap.Children.Add(wrapToggle);

        wrap.Children.Add(new SliderWidget { Minimum = 0, Maximum = 10, Value = 5, DesiredWidth = 200, TrackBrush = new ImmutableSolidColorBrush(Color.FromRgb(220, 220, 220)), FillBrush = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)) });

        var readyBadge = new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10, BackgroundBrush = new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)), ForegroundBrush = new ImmutableSolidColorBrush(Colors.White) };
        readyBadge.SetText("READY");
        readyBadge.RefreshStyle();
        wrap.Children.Add(readyBadge);

        return WidgetBoard.Create("Wrap Layout", "WrapLayoutWidget flowing widgets across rows.", wrap);
    }

    private static WidgetBoard CreateUniformGridLayoutBoard()
    {
        var uniform = new UniformGridLayoutWidget
        {
            Rows = 2,
            Columns = 3,
            Padding = new Thickness(16),
        };

        var statuses = new[]
        {
            ("Review", Colors.Orange),
            ("Build", Colors.CornflowerBlue),
            ("Ship", Colors.SeaGreen),
            ("Logs", Colors.SlateBlue),
            ("Alerts", Colors.IndianRed),
            ("Backlog", Colors.SlateGray),
        };

        foreach (var status in statuses)
        {
            var badge = new BadgeWidget
            {
                DesiredWidth = 110,
                DesiredHeight = 40,
                BackgroundBrush = new ImmutableSolidColorBrush(status.Item2),
                ForegroundBrush = new ImmutableSolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(14),
            };
            badge.SetText(status.Item1);
            uniform.Children.Add(badge);
        }

        return WidgetBoard.Create("Uniform Grid Layout", "UniformGridLayoutWidget balancing items evenly.", uniform);
    }

    private static WidgetBoard CreateGridLayoutBoard()
    {
        var grid = new GridLayoutWidget
        {
            Columns = 2,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        grid.Children.Add(ConfigureButton("Primary", "Save"));
        grid.Children.Add(CreateToggleRow("Auto-sync", true, true));
        grid.Children.Add(CreateSliderRow("Opacity", 0, 1, 0.6, true, new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206))));
        grid.Children.Add(CreateLabeledBadge("Role", new BadgeWidget { CornerRadius = new CornerRadius(12), Padding = 10 }, "ADMIN", new ImmutableSolidColorBrush(Color.FromRgb(79, 154, 255)), new ImmutableSolidColorBrush(Colors.White)));

        return WidgetBoard.Create("Grid Layout", "GridLayoutWidget in a 2-column configuration.", grid);
    }

    private static WidgetBoard CreateDockLayoutBoard()
    {
        var dock = new DockLayoutWidget
        {
            Padding = new Thickness(12),
            Spacing = 8,
            DefaultDockLength = 70,
        };

        var header = new FormattedTextWidget
        {
            EmSize = 15,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40)),
            DesiredHeight = 24,
        };
        header.SetText("Layout Header");
        dock.Children.Add(header);
        dock.SetDock(header, Avalonia.Controls.Dock.Top);

        var footer = new ProgressWidget
        {
            Progress = 0.45,
            TrackForeground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            TrackBackground = new ImmutableSolidColorBrush(Color.FromRgb(224, 228, 236)),
            DesiredHeight = 10,
        };
        dock.Children.Add(footer);
        dock.SetDock(footer, Avalonia.Controls.Dock.Bottom);

        var side = new IconWidget
        {
            DesiredWidth = 32,
            DesiredHeight = 32,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(255, 191, 0)),
            ClipToBounds = false,
        };
        side.SetIcon(WidgetSamplesFactory.CreateWarningGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(255, 197, 61)), new Pen(new ImmutableSolidColorBrush(Color.FromRgb(212, 128, 0)), 1.5), 6);
        dock.Children.Add(side);
        dock.SetDock(side, Avalonia.Controls.Dock.Left);

        var content = new StackLayoutWidget
        {
            Spacing = 6,
        };
        var description = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(80, 80, 80)),
            DesiredHeight = 36,
        };
        description.SetText("Dock layout showcases stacked content with docked edges.");
        content.Children.Add(description);

        var detailsButton = ConfigureButton("Primary", "Details");
        detailsButton.DesiredHeight = 30;
        content.Children.Add(detailsButton);
        dock.Children.Add(content);

        return WidgetBoard.Create("Dock Layout", "DockLayoutWidget combining header, footer, side icon, and stacked content.", dock);
    }

    private static WidgetBoard CreateSplitViewLayoutBoard()
    {
        var splitView = new SplitViewLayoutWidget
        {
            Padding = new Thickness(0),
            CompactPaneLength = 56,
            OpenPaneLength = 150,
            IsPaneOpen = true,
        };

        var paneContent = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(10),
            Spacing = 12,
        };
        paneContent.Children.Add(CreateNavButton("Overview", true));
        paneContent.Children.Add(CreateNavButton("Members", false));
        paneContent.Children.Add(CreateNavButton("Settings", false));

        var pane = new BorderWidget
        {
            Background = new ImmutableSolidColorBrush(Color.FromRgb(240, 244, 248)),
            CornerRadius = new CornerRadius(0),
            Child = paneContent,
            StyleKey = SplitViewLayoutWidget.PaneStyleKey,
        };
        splitView.Children.Add(pane);

        var content = new BorderWidget
        {
            Padding = new Thickness(18),
            Background = new ImmutableSolidColorBrush(Colors.White),
            StyleKey = SplitViewLayoutWidget.ContentStyleKey,
        };

        var body = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };
        var title = new FormattedTextWidget { EmSize = 16, Foreground = new ImmutableSolidColorBrush(Color.FromRgb(33, 42, 62)) };
        title.SetText("SplitView content area");
        var info = new FormattedTextWidget { EmSize = 12, Foreground = new ImmutableSolidColorBrush(Color.FromRgb(116, 125, 143)), DesiredWidth = 220 };
        info.SetText("Switch between compact and expanded navigation panes.");
        body.Children.Add(title);
        body.Children.Add(info);
        content.Child = body;
        splitView.Children.Add(content);

        return WidgetBoard.Create("Split View Layout", "SplitViewLayoutWidget with navigation pane and content.", splitView);
    }

    private static WidgetBoard CreateLayoutTransformLayoutBoard()
    {
        var transform = new LayoutTransformLayoutWidget
        {
            Padding = new Thickness(16),
            ScaleX = 0.92,
            ScaleY = 0.92,
            Angle = -6,
        };

        var card = new BorderWidget
        {
            Background = new ImmutableSolidColorBrush(Color.FromRgb(242, 246, 255)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(210, 224, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            DesiredWidth = 240,
            DesiredHeight = 140,
        };

        var content = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
        };
        var header = new FormattedTextWidget { EmSize = 14, Foreground = new ImmutableSolidColorBrush(Color.FromRgb(38, 61, 110)) };
        header.SetText("Layout Transform");
        var description = new FormattedTextWidget { EmSize = 12, Foreground = new ImmutableSolidColorBrush(Color.FromRgb(99, 110, 131)), DesiredWidth = 200 };
        description.SetText("Scale and rotate child widgets without new allocations.");
        content.Children.Add(header);
        content.Children.Add(description);
        card.Child = content;

        transform.Children.Add(card);

        return WidgetBoard.Create("Layout Transform", "LayoutTransformLayoutWidget applying rotation and scale.", transform);
    }

    private static WidgetBoard CreateViewboxLayoutBoard()
    {
        var viewbox = new ViewboxLayoutWidget
        {
            Padding = new Thickness(16),
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
        };

        var icon = new IconWidget
        {
            DesiredWidth = 220,
            DesiredHeight = 160,
        };
        icon.SetIcon(WidgetSamplesFactory.CreateWaveGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(186, 230, 253)), new Pen(new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), 2));
        viewbox.Children.Add(icon);

        return WidgetBoard.Create("Viewbox Layout", "ViewboxLayoutWidget scales vector content uniformly.", viewbox);
    }

private static WidgetBoard CreateVirtualizingStackBoard()
{
    var items = Enumerable.Range(1, 480)
        .Select(i => new VirtualizingListItem(
            $"Activity #{i}",
            (i % 3) switch
            {
                0 => "Status: In progress · Updated moments ago",
                1 => "Status: Waiting for review · Updated 4 hours ago",
                _ => "Status: Complete · Updated yesterday"
            }))
        .ToList();

        var source = new FastTreeDataGridFlatSource<VirtualizingListItem>(
            items,
            _ => Array.Empty<VirtualizingListItem>());

        var scroll = new ScrollViewerWidget
        {
            Padding = new Thickness(0),
        };

        var panel = new VirtualizingStackPanelWidget
        {
            ItemHeight = 42,
            Spacing = 6,
            BufferItemCount = 6,
            ItemsSource = source,
        };

        panel.VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 32,
            PrefetchRadius = 48,
        };

        panel.ItemFactory = (_, item) =>
        {
            if (item is not VirtualizingListItem data)
            {
                return null;
            }

            var card = new BorderWidget
            {
                Background = new ImmutableSolidColorBrush(Color.FromRgb(247, 249, 255)),
                BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(220, 228, 246)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
            };

            var stack = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
            };

            var title = new FormattedTextWidget
            {
                EmSize = 13,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(41, 56, 92)),
            };
            title.SetText(data.Title);
            stack.Children.Add(title);

            var subtitle = new FormattedTextWidget
            {
                EmSize = 11,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(110, 122, 148)),
            };
            subtitle.SetText(data.Subtitle);
            stack.Children.Add(subtitle);

            card.Child = stack;
            return card;
        };

        scroll.Children.Add(panel);

        var slider = new SliderWidget
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0,
            DesiredHeight = 20,
            DesiredWidth = 340,
            FillBrush = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            TrackBrush = new ImmutableSolidColorBrush(Color.FromRgb(216, 223, 242)),
        };

        var container = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(14),
            Spacing = 10,
        };

        var heading = new FormattedTextWidget
        {
            EmSize = 14,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(44, 61, 101)),
        };
        heading.SetText("VirtualizingStackPanelWidget scrolling thousands of items.");
        container.Children.Add(heading);

        container.Children.Add(slider);

        var listFrame = new BorderWidget
        {
            DesiredHeight = 160,
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(12),
            Background = new ImmutableSolidColorBrush(Color.FromRgb(252, 253, 255)),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(212, 220, 236)),
            BorderThickness = new Thickness(1),
        };
        listFrame.Child = scroll;
        container.Children.Add(listFrame);

        slider.ValueChanged += (_, args) =>
        {
            var viewportHeight = Math.Max(0, listFrame.DesiredHeight - (scroll.Padding.Top + scroll.Padding.Bottom) - 20);
            var totalExtent = panel.DesiredHeight;
            var maxOffset = Math.Max(0, totalExtent - viewportHeight);
            scroll.VerticalOffset = maxOffset * args.NewValue;
        };

        slider.Value = 0;

    return WidgetBoard.Create(VirtualizingBoardTitle, "VirtualizingStackPanelWidget rendering pooled cards with slider navigation.", container);
}

    private static WidgetBoard CreateTransitioningContentBoard()
    {
        var transition = new TransitioningContentWidget
        {
            DesiredWidth = 300,
            DesiredHeight = 120,
            Transition = WidgetTransitionDescriptor.Fade(TimeSpan.FromMilliseconds(220)),
        };

        transition.Content = CreateTransitionCard(
            "Insights enabled",
            "Streaming live metrics and alerts",
            new ImmutableSolidColorBrush(Color.FromRgb(224, 244, 255)),
            new ImmutableSolidColorBrush(Color.FromRgb(96, 165, 250)));

        var toggle = new ToggleSwitchWidget
        {
            DesiredWidth = 120,
            DesiredHeight = 36,
        };
        toggle.SetState(true);

        toggle.Toggled += (_, args) =>
        {
            transition.Content = args.NewValue
                ? CreateTransitionCard(
                    "Insights enabled",
                    "Streaming live metrics and alerts",
                    new ImmutableSolidColorBrush(Color.FromRgb(224, 244, 255)),
                    new ImmutableSolidColorBrush(Color.FromRgb(96, 165, 250)))
                : CreateTransitionCard(
                    "Insights paused",
                    "Resume to continue receiving updates",
                    new ImmutableSolidColorBrush(Color.FromRgb(252, 231, 233)),
                    new ImmutableSolidColorBrush(Color.FromRgb(244, 114, 182)));
        };

        var layout = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };
        layout.Children.Add(transition);

        var toggleRow = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };
        var label = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 110,
        };
        label.SetText("Enabled");
        toggleRow.Children.Add(label);
        toggleRow.Children.Add(toggle);
        layout.Children.Add(toggleRow);

        return WidgetBoard.Create("Transitioning Content", "TransitioningContentWidget animates between cards with fades or slides.", layout);
    }

    private static WidgetBoard CreateTextBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var textBlock = new TextBlockWidget
        {
            DesiredWidth = 320,
            EmSize = 13,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        textBlock.SetText("TextBlockWidget renders wrapped text with immediate-mode drawing. Resize the host to watch wrapping update without layout passes.");
        stack.Children.Add(textBlock);

        var selectable = new SelectableTextWidget
        {
            DesiredWidth = 320,
            DesiredHeight = 60,
        };
        selectable.SetText("Drag to select text without Avalonia controls. Selection highlights and caret are painted directly onto the widget surface.");
        stack.Children.Add(selectable);

        var document = new DocumentTextWidget
        {
            DesiredWidth = 320,
            EmSize = 12,
        };
        document.SetDocument(new DocumentTextWidgetValue(new List<DocumentTextSpan>
        {
            new("Rich ``Document`` spans ", new ImmutableSolidColorBrush(Color.FromRgb(71, 85, 105))),
            new("with " , new ImmutableSolidColorBrush(Color.FromRgb(107, 114, 128))),
            new("accent", new ImmutableSolidColorBrush(Color.FromRgb(37, 99, 235)), FontWeight: FontWeight.SemiBold),
            new(" and " , new ImmutableSolidColorBrush(Color.FromRgb(107, 114, 128))),
            new("emphasis", new ImmutableSolidColorBrush(Color.FromRgb(220, 38, 38)), FontWeight: FontWeight.Bold, FontStyle: FontStyle.Italic),
            new(" styling." , new ImmutableSolidColorBrush(Color.FromRgb(71, 85, 105)))
        }));
        stack.Children.Add(document);

        return WidgetBoard.Create("Text Widgets", "TextBlock, selectable text, and rich document spans rendered via immediate-mode widgets.", stack);
    }

    private static WidgetBoard CreateTextInputBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var input = new TextInputWidget
        {
            DesiredWidth = 260,
            Placeholder = "Type a message...",
        };
        input.Text = "Hello widgets";
        stack.Children.Add(CreateLabeledRow("TextInputWidget", input));

        var masked = new MaskedTextBoxWidget
        {
            DesiredWidth = 260,
            Placeholder = "Password",
            MaskChar = '●',
        };
        masked.Text = "Secret123";
        stack.Children.Add(CreateLabeledRow("MaskedTextBoxWidget", masked));

        return WidgetBoard.Create("Text Input", "Immediate-mode text boxes, including a masked variant.", stack);
    }

    private static WidgetBoard CreateMediaBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 12,
        };

        var imageWidget = new ImageWidget
        {
            DesiredWidth = 140,
            DesiredHeight = 100,
            Padding = 6,
            Stretch = Stretch.Uniform,
        };
        imageWidget.Source = CreateSampleImage(Color.FromRgb(59, 130, 246), "IMG");
        stack.Children.Add(CreateLabeledRow("ImageWidget", imageWidget));

        var iconElement = new IconElementWidget
        {
            DesiredWidth = 72,
            DesiredHeight = 72,
        };
        iconElement.UpdateValue(null, new IconElementWidgetValue(
            Geometry: StreamGeometry.Parse("M4,12 L12,4 28,20 20,28 Z"),
            Foreground: new ImmutableSolidColorBrush(Colors.White),
            Background: new ImmutableSolidColorBrush(Color.FromRgb(16, 185, 129)),
            Padding: 8));
        stack.Children.Add(CreateLabeledRow("IconElementWidget", iconElement));

        var pathIcon = new PathIconWidget
        {
            DesiredWidth = 72,
            DesiredHeight = 72,
        };
        pathIcon.UpdateValue(null, new PathIconWidgetValue(
            Data: "M8,4 L24,4 L28,12 L28,28 L8,28 Z M18,10 L18,18 24,18 16,28 16,20 10,20 18,10 Z",
            Foreground: new ImmutableSolidColorBrush(Color.FromRgb(55, 48, 163)),
            Padding: 8));
        stack.Children.Add(CreateLabeledRow("PathIconWidget", pathIcon));

        return WidgetBoard.Create("Media & Icons", "ImageWidget, IconElementWidget, and PathIconWidget demonstrate bitmap and geometry icon rendering.", stack);
    }

    private static Widget CreateTransitionCard(string title, string subtitle, ImmutableSolidColorBrush background, ImmutableSolidColorBrush accent)
    {
        var border = new BorderWidget
        {
            Background = background,
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
        };

        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };

        var titleWidget = new FormattedTextWidget
        {
            EmSize = 14,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(30, 41, 59)),
        };
        titleWidget.SetText(title);
        stack.Children.Add(titleWidget);

        var subtitleWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(71, 85, 105)),
            DesiredWidth = 240,
        };
        subtitleWidget.SetText(subtitle);
        stack.Children.Add(subtitleWidget);

        var accentBar = new BorderWidget
        {
            Background = accent,
            DesiredHeight = 4,
            DesiredWidth = 60,
            CornerRadius = new CornerRadius(2),
        };
        stack.Children.Add(accentBar);

        border.Child = stack;
        return border;
    }

    private static WidgetBoard CreateProgressBoard()
    {
        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 10,
        };

        stack.Children.Add(CreateLabeledRow("25%", new ProgressWidget
        {
            Progress = 0.25,
            TrackForeground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            TrackBackground = new ImmutableSolidColorBrush(Color.FromRgb(230, 236, 247)),
            DesiredHeight = 12,
            DesiredWidth = 200,
        }));

        stack.Children.Add(CreateLabeledRow("65%", new ProgressWidget
        {
            Progress = 0.65,
            TrackForeground = new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)),
            TrackBackground = new ImmutableSolidColorBrush(Color.FromRgb(223, 244, 231)),
            DesiredHeight = 12,
            DesiredWidth = 200,
        }));

        stack.Children.Add(CreateLabeledRow("Indeterminate", new ProgressWidget
        {
            Progress = 0.5,
            IsIndeterminate = true,
            TrackForeground = new ImmutableSolidColorBrush(Color.FromRgb(128, 128, 128)),
            TrackBackground = new ImmutableSolidColorBrush(Color.FromRgb(230, 230, 230)),
            DesiredHeight = 12,
            DesiredWidth = 200,
        }));

        return WidgetBoard.Create("Progress Bars", "Determinate and indeterminate progress widgets.", stack);
    }

    private static WidgetBoard CreateIconGeometryBoard()
    {
        var wrap = new WrapLayoutWidget
        {
            Padding = new Thickness(12),
            Spacing = 12,
            DefaultItemWidth = 48,
            DefaultItemHeight = 48,
        };

        var warningIcon = new IconWidget { Padding = 10 };
        warningIcon.SetIcon(WidgetSamplesFactory.CreateWarningGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(255, 197, 61)), new Pen(new ImmutableSolidColorBrush(Color.FromRgb(212, 128, 0)), 1.5));
        wrap.Children.Add(warningIcon);

        var waveGeometry = new GeometryWidget { Padding = 10 };
        waveGeometry.SetGeometry(WidgetSamplesFactory.CreateWaveGeometry(), Stretch.Uniform, new ImmutableSolidColorBrush(Color.FromRgb(186, 230, 253)), new Pen(new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), 1.5));
        wrap.Children.Add(waveGeometry);

        var documentIcon = new IconWidget { Padding = 10 };
        documentIcon.SetIcon(WidgetSamplesFactory.CreateDocumentGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(79, 154, 255)), new Pen(new ImmutableSolidColorBrush(Color.FromRgb(36, 98, 156)), 1));
        wrap.Children.Add(documentIcon);

        var polygonGeometry = new GeometryWidget { Padding = 10 };
        polygonGeometry.SetGeometry(WidgetSamplesFactory.CreatePolygonGeometry(), Stretch.Uniform, new ImmutableSolidColorBrush(Color.FromRgb(99, 200, 255)), new Pen(new ImmutableSolidColorBrush(Color.FromRgb(28, 124, 172)), 1.5));
        wrap.Children.Add(polygonGeometry);

        return WidgetBoard.Create("Icon & Geometry", "Vector icon and geometry widgets with padding/scale.", wrap);
    }

    private static WidgetBoard CreateShapesBoard()
    {
        const double initialSize = 84;

        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 14,
        };

        var info = new TextBlockWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
        };
        info.SetText("Hover shapes to highlight. Drag the slider to resize every sample.");

        var sizeLabel = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(60, 60, 60)),
            DesiredWidth = 120,
            DesiredHeight = 24,
        };
        sizeLabel.SetText($"Size: {initialSize:0}px");

        var sizeSlider = new SliderWidget
        {
            Minimum = 48,
            Maximum = 160,
            Value = initialSize,
            DesiredWidth = 220,
            TrackBrush = new ImmutableSolidColorBrush(Color.FromRgb(224, 224, 224)),
            FillBrush = new ImmutableSolidColorBrush(Color.FromRgb(59, 130, 246)),
        };

        var sliderRow = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
        };
        sliderRow.Children.Add(sizeLabel);
        sliderRow.Children.Add(sizeSlider);

        var wrap = new WrapLayoutWidget
        {
            Spacing = 12,
            DefaultItemWidth = initialSize,
            DefaultItemHeight = initialSize,
        };

        var shapes = new List<ShapeWidget>();

        void AddShape(ShapeWidget shape)
        {
            shapes.Add(shape);
            wrap.Children.Add(shape);
        }

        AddShape(CreateInteractiveRectangleShape(initialSize));
        AddShape(CreateInteractiveEllipseShape(initialSize));
        AddShape(CreateInteractiveLineShape(initialSize));
        AddShape(CreateInteractivePolygonShape(initialSize));
        AddShape(CreateInteractivePolylineShape(initialSize));
        AddShape(CreateInteractiveArcShape(initialSize));
        AddShape(CreateInteractiveSectorShape(initialSize));
        AddShape(CreateInteractivePathShape(initialSize));

        sizeSlider.ValueChanged += (_, args) =>
        {
            var clamped = Math.Clamp(args.NewValue, sizeSlider.Minimum, sizeSlider.Maximum);
            sizeLabel.SetText($"Size: {Math.Round(clamped):0}px");
            wrap.DefaultItemWidth = clamped;
            wrap.DefaultItemHeight = clamped;

            foreach (var shape in shapes)
            {
                shape.DesiredWidth = clamped;
                shape.DesiredHeight = clamped;
            }
        };

        stack.Children.Add(info);
        stack.Children.Add(sliderRow);
        stack.Children.Add(wrap);

        return WidgetBoard.Create("Shapes", "Shape widgets showcase hover hit-testing and live resizing.", stack);
    }

    private static ShapeWidget CreateInteractiveRectangleShape(double size)
    {
        var normal = new RectangleShapeWidgetValue(
            RadiusX: 10,
            RadiusY: 10,
            Fill: new ImmutableSolidColorBrush(Color.FromRgb(59, 130, 246)),
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(30, 64, 175)),
            StrokeThickness: 2);

        var hover = normal with
        {
            Fill = new ImmutableSolidColorBrush(Color.FromRgb(37, 99, 235)),
        };

        var shape = new RectangleShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractiveEllipseShape(double size)
    {
        var normal = new EllipseShapeWidgetValue(
            Fill: new ImmutableSolidColorBrush(Color.FromRgb(16, 185, 129)),
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(4, 120, 87)),
            StrokeThickness: 2);

        var hover = normal with
        {
            Fill = new ImmutableSolidColorBrush(Color.FromRgb(13, 148, 136)),
        };

        var shape = new EllipseShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractiveLineShape(double size)
    {
        var normal = new LineShapeWidgetValue(
            StartPoint: new Point(4, 28),
            EndPoint: new Point(28, 4),
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(249, 115, 22)),
            StrokeThickness: 3,
            StrokeLineCap: PenLineCap.Round);

        var hover = normal with
        {
            Stroke = new ImmutableSolidColorBrush(Color.FromRgb(234, 88, 12)),
        };

        var shape = new LineShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractivePolygonShape(double size)
    {
        var points = new[]
        {
            new Point(16, 2),
            new Point(29, 12),
            new Point(26, 30),
            new Point(6, 30),
            new Point(3, 12),
        };

        var normal = new PolygonShapeWidgetValue(
            Points: points,
            Fill: new ImmutableSolidColorBrush(Color.FromRgb(96, 165, 250)),
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(29, 78, 216)),
            StrokeThickness: 2);

        var hover = normal with
        {
            Fill = new ImmutableSolidColorBrush(Color.FromRgb(59, 130, 246)),
        };

        var shape = new PolygonShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractivePolylineShape(double size)
    {
        var points = new[]
        {
            new Point(2, 20),
            new Point(10, 6),
            new Point(18, 18),
            new Point(26, 4),
            new Point(30, 16),
        };

        var normal = new PolylineShapeWidgetValue(
            Points: points,
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(168, 85, 247)),
            StrokeThickness: 2,
            StrokeDashArray: new[] { 3.0, 2.0 },
            StrokeLineCap: PenLineCap.Round);

        var hover = normal with
        {
            StrokeDashOffset = 2,
            Stroke = new ImmutableSolidColorBrush(Color.FromRgb(147, 51, 234)),
        };

        var shape = new PolylineShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractiveArcShape(double size)
    {
        var normal = new ArcShapeWidgetValue(
            StartAngle: 200,
            SweepAngle: 250,
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(14, 116, 144)),
            StrokeThickness: 3,
            StrokeLineCap: PenLineCap.Round);

        var hover = normal with
        {
            Stroke = new ImmutableSolidColorBrush(Color.FromRgb(8, 145, 178)),
            StrokeThickness = 3.5,
        };

        var shape = new ArcShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractiveSectorShape(double size)
    {
        var normal = new SectorShapeWidgetValue(
            StartAngle: 300,
            SweepAngle: 120,
            Fill: new ImmutableSolidColorBrush(Color.FromRgb(251, 191, 36)),
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(217, 119, 6)),
            StrokeThickness: 1.5);

        var hover = normal with
        {
            Fill = new ImmutableSolidColorBrush(Color.FromRgb(253, 230, 138)),
        };

        var shape = new SectorShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget CreateInteractivePathShape(double size)
    {
        const string heartPath = "M16,29 C6,20 4,14 4,10 C4,6 7,3 11,3 C13,3 15,4 16,6 C17,4 19,3 21,3 C25,3 28,6 28,10 C28,14 26,20 16,29 Z";

        var normal = new PathShapeWidgetValue(
            Data: null,
            DataString: heartPath,
            Fill: new ImmutableSolidColorBrush(Color.FromRgb(244, 63, 94)),
            Stroke: new ImmutableSolidColorBrush(Color.FromRgb(190, 24, 60)),
            StrokeThickness: 1.5);

        var hover = normal with
        {
            Fill = new ImmutableSolidColorBrush(Color.FromRgb(225, 29, 72)),
        };

        var shape = new PathShapeWidget
        {
            Stretch = Stretch.Uniform,
        };

        return ConfigureInteractiveShape(shape, normal, hover, size);
    }

    private static ShapeWidget ConfigureInteractiveShape(ShapeWidget widget, ShapeWidgetValue normal, ShapeWidgetValue hover, double size)
    {
        widget.DesiredWidth = size;
        widget.DesiredHeight = size;
        widget.IsInteractive = true;

        ApplyShapeValue(widget, normal);

        widget.PointerInput += evt =>
        {
            switch (evt.Kind)
            {
                case WidgetPointerEventKind.Entered:
                case WidgetPointerEventKind.Pressed:
                    ApplyShapeValue(widget, hover);
                    break;
                case WidgetPointerEventKind.Released:
                case WidgetPointerEventKind.Exited:
                case WidgetPointerEventKind.CaptureLost:
                    ApplyShapeValue(widget, normal);
                    break;
            }
        };

        return widget;
    }

    private static void ApplyShapeValue(ShapeWidget widget, ShapeWidgetValue value)
    {
        widget.UpdateValue(null, value);
        widget.RefreshStyle();
    }

    private static Widget CreateLabeledRow(string label, Widget widget)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var labelWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 110,
        };
        labelWidget.SetText(label);

        row.Children.Add(labelWidget);
        row.Children.Add(widget);

        return row;
    }

    private static ButtonWidget ConfigureButton(string styleKey, string text)
    {
        var button = new ButtonWidget
        {
            StyleKey = styleKey,
            DesiredHeight = 32,
            DesiredWidth = 120,
        };
        button.SetText(text);
        return button;
    }

    private static ButtonWidget CreateNavButton(string text, bool isSelected)
    {
        var button = ConfigureButton(isSelected ? "Primary" : "Secondary", text);
        button.DesiredWidth = 120;
        button.DesiredHeight = 32;
        return button;
    }

    private static Widget CreateLabeledBadge(string label, BadgeWidget badge, string text, ImmutableSolidColorBrush background, ImmutableSolidColorBrush foreground)
    {
        badge.BackgroundBrush = background;
        badge.ForegroundBrush = foreground;
        badge.SetText(text);
        badge.RefreshStyle();
        badge.DesiredHeight = 32;
        badge.DesiredWidth = 100;
        return CreateLabeledRow(label, badge);
    }

    private static Widget CreateSliderRow(string label, double min, double max, double value, bool isEnabled, ImmutableSolidColorBrush fillBrush)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var labelWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 110,
        };

        var slider = new SliderWidget
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            DesiredWidth = 220,
            IsEnabled = isEnabled,
            TrackBrush = new ImmutableSolidColorBrush(Color.FromRgb(220, 220, 220)),
            FillBrush = fillBrush,
        };

        labelWidget.SetText($"{label} ({Math.Round(slider.Value)})");
        slider.ValueChanged += (_, args) => labelWidget.SetText($"{label} ({Math.Round(args.NewValue)})");

        row.Children.Add(labelWidget);
        row.Children.Add(slider);

        return row;
    }

    private static Widget CreateNumericRow(string label, double min, double max, double value, double increment, int decimals, bool isEnabled)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var labelWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 140,
        };
        labelWidget.SetText($"{label} ({FormatNumeric(value, decimals)})");

        var numeric = new NumericUpDownWidget
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            Increment = increment,
            DecimalPlaces = decimals,
            IsEnabled = isEnabled,
        };

        numeric.ValueChanged += (_, args) =>
        {
            labelWidget.SetText($"{label} ({FormatNumeric(args.NewValue, decimals)})");
        };

        row.Children.Add(labelWidget);
        row.Children.Add(numeric);
        return row;
    }

    private static string FormatNumeric(double value, int decimals)
    {
        return value.ToString($"F{Math.Max(0, decimals)}", CultureInfo.CurrentCulture);
    }

    private static Widget CreateScrollBarRow(string label, Orientation orientation, double min, double max, double value, double viewportSize, double length, bool isEnabled)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var labelWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 140,
        };
        labelWidget.SetText($"{label} ({Math.Round(value, 1)})");

        var scrollBar = new ScrollBarWidget
        {
            Orientation = orientation,
            Minimum = min,
            Maximum = max,
            ViewportSize = viewportSize,
            Value = value,
            IsEnabled = isEnabled,
        };

        if (orientation == Orientation.Horizontal)
        {
            scrollBar.DesiredWidth = length;
            scrollBar.DesiredHeight = 18;
        }
        else
        {
            scrollBar.DesiredWidth = 18;
            scrollBar.DesiredHeight = length;
        }

        scrollBar.ValueChanged += (_, args) =>
        {
            labelWidget.SetText($"{label} ({Math.Round(args.NewValue, 1)})");
        };

        row.Children.Add(labelWidget);
        row.Children.Add(scrollBar);
        return row;
    }

    private static Widget CreateToggleRow(string label, bool initialState, bool isEnabled)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var labelWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 110,
        };

        var toggle = new ToggleSwitchWidget
        {
            DesiredWidth = 120,
            DesiredHeight = 36,
            IsEnabled = isEnabled,
        };
        toggle.SetState(initialState);

        labelWidget.SetText($"{label} ({(toggle.IsOn ? "On" : "Off")})");
        toggle.Toggled += (_, args) => labelWidget.SetText($"{label} ({(args.NewValue ? "On" : "Off")})");

        row.Children.Add(labelWidget);
        row.Children.Add(toggle);

        return row;
    }

    private static Widget CreateRadioRow(string label, bool isChecked, bool isEnabled)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var labelWidget = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 22,
            DesiredWidth = 110,
        };

        var radio = new RadioButtonWidget
        {
            DesiredWidth = 24,
            DesiredHeight = 24,
            IsEnabled = isEnabled,
        };
        radio.SetChecked(isChecked);

        labelWidget.SetText($"{label} ({(radio.IsChecked ? "Checked" : "Unchecked")})");
        radio.IsCheckedChanged += (_, args) =>
            labelWidget.SetText($"{label} ({(args.NewValue ? "Checked" : "Unchecked")})");

        row.Children.Add(labelWidget);
        row.Children.Add(radio);

        return row;
    }

    private static Widget CreateCheckOptionRow(string label, bool isChecked, bool isEnabled = true)
    {
        var row = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };

        var check = new CheckBoxWidget
        {
            DesiredWidth = 24,
            DesiredHeight = 24,
            IsEnabled = isEnabled,
        };
        check.SetValue(isChecked);

        var text = new FormattedTextWidget
        {
            EmSize = 12,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            DesiredHeight = 20,
            DesiredWidth = double.NaN,
        };
        text.SetText(label);

        row.Children.Add(check);
        row.Children.Add(text);

        return row;
    }

    private sealed record VirtualizingListItem(string Title, string Subtitle);

    private static IImage CreateSampleImage(Color background, string glyph)
    {
        var size = new PixelSize(96, 96);
        var dpi = new Vector(96, 96);
        var bitmap = new RenderTargetBitmap(size, dpi);

        using (var ctx = bitmap.CreateDrawingContext())
        {
            ctx.FillRectangle(new SolidColorBrush(background), new Rect(0, 0, size.Width, size.Height));

            var formatted = new FormattedText(
                glyph,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                32,
                Brushes.White);

            var origin = new Point((size.Width - formatted.Width) / 2, (size.Height - formatted.Height) / 2);
            ctx.DrawText(formatted, origin);
        }

        return bitmap;
    }
}
