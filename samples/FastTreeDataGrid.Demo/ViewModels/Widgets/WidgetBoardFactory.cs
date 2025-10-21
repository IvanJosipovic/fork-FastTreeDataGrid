using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Widgets;

internal static class WidgetBoardFactory
{
    public static IReadOnlyList<WidgetBoard> CreateBoards(IReadOnlyList<WidgetGalleryNode> gallery)
    {
        _ = gallery;
        var boards = new List<WidgetBoard>();

        boards.Add(CreateButtonBoard());
        boards.Add(CreateCheckBoxBoard());
        boards.Add(CreateToggleBoard());
        boards.Add(CreateRadioBoard());
        boards.Add(CreateSliderBoard());
        boards.Add(CreateBadgeBoard());
        boards.Add(CreateHorizontalLayoutBoard());
        boards.Add(CreateVerticalLayoutBoard());
        boards.Add(CreateWrapLayoutBoard());
        boards.Add(CreateGridLayoutBoard());
        boards.Add(CreateDockLayoutBoard());
        boards.Add(CreateProgressBoard());
        boards.Add(CreateIconGeometryBoard());

        return boards;
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
}
