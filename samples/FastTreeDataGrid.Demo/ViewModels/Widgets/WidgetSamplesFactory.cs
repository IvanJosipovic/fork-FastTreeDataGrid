using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Widgets;

internal static class WidgetSamplesFactory
{
    public static IReadOnlyList<WidgetGalleryNode> Create()
    {
        return new[]
        {
            CreateIconGroup(),
            CreateGeometryGroup(),
            CreateButtonGroup(),
            CreateCheckBoxGroup(),
            CreateToggleGroup(),
            CreateRadioGroup(),
            CreateSliderGroup(),
            CreateBadgeGroup(),
            CreateProgressGroup(),
            CreateCustomGroup(),
            CreateLayoutGroup(),
        };
    }

    private static WidgetGalleryNode CreateIconGroup()
    {
        var group = new WidgetGalleryNode("Icons", "Vector icons rendered via IconWidget with automatic scaling");

        var folder = new WidgetGalleryNode("Folder", "StreamGeometry with uniform scaling")
        {
            IconValue = new IconWidgetValue(
                StreamGeometry.Parse("M3,14 L3,31 L29,31 L29,18 L16,18 L12,14 Z"),
                new ImmutableSolidColorBrush(Color.FromRgb(255, 191, 0)),
                Padding: 10)
        };
        group.AddChild(folder);

        var document = new WidgetGalleryNode("Document", "Icon parsed from path figures")
        {
            IconValue = new IconWidgetValue(
                StreamGeometry.Parse("M6,6 L20,6 L28,14 L28,30 L6,30 Z M20,6 L20,14 L28,14"),
                new ImmutableSolidColorBrush(Color.FromRgb(79, 154, 255)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(36, 98, 156)), 1),
                Padding: 10)
        };
        group.AddChild(document);

        var warning = new WidgetGalleryNode("Warning", "Triangle icon using fill + stroke")
        {
            IconValue = new IconWidgetValue(
                CreateWarningGeometry(),
                new ImmutableSolidColorBrush(Color.FromRgb(255, 197, 61)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(212, 128, 0)), 1.5),
                Padding: 12)
        };
        group.AddChild(warning);

        return group;
    }

    private static WidgetGalleryNode CreateGeometryGroup()
    {
        var group = new WidgetGalleryNode("Geometry", "GeometryWidget with stretch modes and strokes");

        var polygon = new WidgetGalleryNode("Polygon", "UniformToFill stretch with outline")
        {
            GeometryValue = new GeometryWidgetValue(
                CreatePolygonGeometry(),
                Stretch.Uniform,
                new ImmutableSolidColorBrush(Color.FromRgb(99, 200, 255)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(28, 124, 172)), 1.5),
                Padding: 12)
        };
        group.AddChild(polygon);

        var wave = new WidgetGalleryNode("Wave", "Fill stretch applied to sine wave path")
        {
            GeometryValue = new GeometryWidgetValue(
                CreateWaveGeometry(),
                Stretch.Uniform,
                new ImmutableSolidColorBrush(Color.FromRgb(186, 230, 253)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), 1.5),
                Padding: 12)
        };
        group.AddChild(wave);

        return group;
    }

    private static WidgetGalleryNode CreateLayoutGroup()
    {
        var group = new WidgetGalleryNode("Layouts", "Surface-based layout widgets replicating common panels");

        group.AddChild(CreateHorizontalStackNode());
        group.AddChild(CreateVerticalStackNode());
        group.AddChild(CreateWrapNode());
        group.AddChild(CreateGridNode());
        group.AddChild(CreateDockNode());

        return group;
    }

    public static Geometry CreateWarningGeometry() => StreamGeometry.Parse("M16,4 L30,28 H2 Z");

    public static Geometry CreateDocumentGeometry() => StreamGeometry.Parse("M6,6 L20,6 L28,14 L28,30 L6,30 Z M20,6 L20,14 L28,14");

    public static Geometry CreatePolygonGeometry() => StreamGeometry.Parse("M16,2 L30,12 L26,30 L6,30 L2,12 Z");

    private static WidgetGalleryNode CreateButtonGroup()
    {
        var group = new WidgetGalleryNode("Buttons", "ButtonWidget renders themed call-to-action badges");

        var primary = new WidgetGalleryNode("Primary", "Primary emphasised state")
        {
            ButtonValue = new ButtonWidgetValue("Launch", IsPrimary: true)
        };
        group.AddChild(primary);

        var pressed = new WidgetGalleryNode("Pressed", "Pressed accent state")
        {
            ButtonValue = new ButtonWidgetValue("Pressed", IsPrimary: true, IsPressed: true)
        };
        group.AddChild(pressed);

        var secondaryDisabled = new WidgetGalleryNode("Disabled", "Secondary button disabled")
        {
            ButtonValue = new ButtonWidgetValue("Disabled", IsEnabled: false)
        };
        group.AddChild(secondaryDisabled);

        return group;
    }

    private static WidgetGalleryNode CreateCheckBoxGroup()
    {
        var group = new WidgetGalleryNode("Check Boxes", "CheckBoxWidget for tri-state visuals");

        var checkedNode = new WidgetGalleryNode("Checked", "Checked and enabled")
        {
            CheckBoxValue = new CheckBoxWidgetValue(true)
        };
        group.AddChild(checkedNode);

        var indeterminate = new WidgetGalleryNode("Indeterminate", "Null value draws bar")
        {
            CheckBoxValue = new CheckBoxWidgetValue(null)
        };
        group.AddChild(indeterminate);

        var disabled = new WidgetGalleryNode("Disabled", "Disabled but checked")
        {
            CheckBoxValue = new CheckBoxWidgetValue(true, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateToggleGroup()
    {
        var group = new WidgetGalleryNode("Toggle Switch", "ToggleSwitchWidget showing on/off/disabled states");

        var onNode = new WidgetGalleryNode("On", "Active toggle")
        {
            ToggleValue = new ToggleSwitchWidgetValue(true)
        };
        group.AddChild(onNode);

        var offNode = new WidgetGalleryNode("Off", "Inactive toggle")
        {
            ToggleValue = new ToggleSwitchWidgetValue(false)
        };
        group.AddChild(offNode);

        var disabledNode = new WidgetGalleryNode("Disabled", "Toggle switch disabled")
        {
            ToggleValue = new ToggleSwitchWidgetValue(true, IsEnabled: false)
        };
        group.AddChild(disabledNode);

        return group;
    }

    private static WidgetGalleryNode CreateRadioGroup()
    {
        var group = new WidgetGalleryNode("Radio Button", "RadioButtonWidget in various states");

        var selected = new WidgetGalleryNode("Selected", "Radio selected")
        {
            RadioValue = new RadioButtonWidgetValue(true)
        };
        group.AddChild(selected);

        var unselected = new WidgetGalleryNode("Unselected", "Radio not selected")
        {
            RadioValue = new RadioButtonWidgetValue(false)
        };
        group.AddChild(unselected);

        var disabled = new WidgetGalleryNode("Disabled", "Radio disabled")
        {
            RadioValue = new RadioButtonWidgetValue(true, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateSliderGroup()
    {
        var group = new WidgetGalleryNode("Slider", "SliderWidget showing track and interactive thumb");

        var low = new WidgetGalleryNode("Low", "Value near minimum")
        {
            SliderValue = new SliderWidgetValue(0.2)
        };
        group.AddChild(low);

        var mid = new WidgetGalleryNode("Mid", "Value near middle")
        {
            SliderValue = new SliderWidgetValue(0.6, FillBrush: new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)))
        };
        group.AddChild(mid);

        var disabled = new WidgetGalleryNode("Disabled", "Slider disabled")
        {
            SliderValue = new SliderWidgetValue(0.75, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateBadgeGroup()
    {
        var group = new WidgetGalleryNode("Badge", "BadgeWidget renders pill text indicators");

        var info = new WidgetGalleryNode("Info", "Standard info badge")
        {
            BadgeValue = new BadgeWidgetValue("INFO", new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)))
        };
        group.AddChild(info);

        var success = new WidgetGalleryNode("Success", "Success indicator")
        {
            BadgeValue = new BadgeWidgetValue("SUCCESS", new ImmutableSolidColorBrush(Color.FromRgb(60, 180, 114)))
        };
        group.AddChild(success);

        var warning = new WidgetGalleryNode("Warning", "Warning badge")
        {
            BadgeValue = new BadgeWidgetValue("WARN", new ImmutableSolidColorBrush(Color.FromRgb(255, 191, 0)), new ImmutableSolidColorBrush(Color.FromRgb(90, 60, 0)))
        };
        group.AddChild(warning);

        return group;
    }

    private static WidgetGalleryNode CreateProgressGroup()
    {
        var group = new WidgetGalleryNode("Progress", "ProgressWidget for completion bars");

        var partial = new WidgetGalleryNode("75%", "Determinate progress state")
        {
            ProgressValue = new ProgressWidgetValue(0.75)
        };
        group.AddChild(partial);

        var indeterminate = new WidgetGalleryNode("Indeterminate", "Animated segment placeholder")
        {
            ProgressValue = new ProgressWidgetValue(0.5, IsIndeterminate: true,
                Foreground: new ImmutableSolidColorBrush(Color.FromRgb(36, 98, 156)))
        };
        group.AddChild(indeterminate);

        return group;
    }

    private static WidgetGalleryNode CreateCustomGroup()
    {
        var group = new WidgetGalleryNode("Custom Draw", "CustomDrawWidget executes arbitrary drawing callbacks");

        var sparkline = new WidgetGalleryNode("Sparkline", "Inline trend line")
        {
            CustomValue = new CustomDrawWidgetValue(DrawSparkline)
        };
        group.AddChild(sparkline);

        var target = new WidgetGalleryNode("Target", "Composite drawing with guides")
        {
            CustomValue = new CustomDrawWidgetValue(DrawTarget)
        };
        group.AddChild(target);

        return group;
    }

    private static WidgetGalleryNode CreateHorizontalStackNode()
    {
        var node = new WidgetGalleryNode("Stack (Horizontal)", "Icon, text, and status in a horizontal stack")
        {
            IconValue = new IconWidgetValue(
                StreamGeometry.Parse("M16,4 L30,28 H2 Z"),
                new ImmutableSolidColorBrush(Color.FromRgb(255, 197, 61)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(212, 128, 0)), 1.2),
                Padding: 10),
            ProgressValue = new ProgressWidgetValue(0.68, IsIndeterminate: false,
                Foreground: new ImmutableSolidColorBrush(Color.FromRgb(36, 128, 196)),
                Background: new ImmutableSolidColorBrush(Color.FromRgb(225, 238, 251)))
        };

        node.LayoutFactory = () =>
        {
            var layout = new StackLayoutWidget
            {
                Orientation = Orientation.Horizontal,
                Padding = new Thickness(6, 4, 6, 4),
                Spacing = 8,
            };

            layout.Children.Add(new IconWidget
            {
                Key = WidgetGalleryNode.KeyIcon,
                DesiredWidth = 24,
                DesiredHeight = 24,
                Padding = 8,
            });

            var textColumn = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 2,
            };

            textColumn.Children.Add(new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyName,
                EmSize = 14,
                DesiredHeight = 18,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40)),
            });

            textColumn.Children.Add(new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyDescription,
                EmSize = 12,
                DesiredHeight = 16,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(104, 104, 104)),
            });

            layout.Children.Add(textColumn);

            layout.Children.Add(new ProgressWidget
            {
                Key = WidgetGalleryNode.KeyProgress,
                DesiredWidth = 80,
                DesiredHeight = 10,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(224, 236, 249)),
            });

            return layout;
        };

        return node;
    }

    private static WidgetGalleryNode CreateVerticalStackNode()
    {
        var node = new WidgetGalleryNode("Stack (Vertical)", "Vertical stack combining action buttons and progress");

        node.SetValue("Widget.Layout.Vertical.Button.0", new ButtonWidgetValue("Approve", IsPrimary: true));
        node.SetValue("Widget.Layout.Vertical.Button.1", new ButtonWidgetValue("Reject", IsPrimary: false));
        node.SetValue("Widget.Layout.Vertical.Progress", new ProgressWidgetValue(0.35));

        node.LayoutFactory = () =>
        {
            var layout = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Padding = new Thickness(6),
                Spacing = 6,
            };

            layout.Children.Add(new ButtonWidget
            {
                Key = "Widget.Layout.Vertical.Button.0",
                DesiredHeight = 26,
            });

            layout.Children.Add(new ButtonWidget
            {
                Key = "Widget.Layout.Vertical.Button.1",
                DesiredHeight = 26,
            });

            layout.Children.Add(new ProgressWidget
            {
                Key = "Widget.Layout.Vertical.Progress",
                DesiredHeight = 10,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(224, 236, 249)),
            });

            return layout;
        };

        return node;
    }

    private static WidgetGalleryNode CreateWrapNode()
    {
        var node = new WidgetGalleryNode("Wrap Layout", "WrapLayoutWidget flows badges across multiple rows");

        var colors = new[]
        {
            Color.FromRgb(240, 128, 128),
            Color.FromRgb(110, 201, 178),
            Color.FromRgb(130, 170, 255),
            Color.FromRgb(255, 196, 109),
            Color.FromRgb(182, 162, 255),
            Color.FromRgb(255, 140, 187),
        };

        for (var i = 0; i < colors.Length; i++)
        {
            var key = $"Widget.Layout.Wrap.Icon.{i}";
            node.SetValue(key, new IconWidgetValue(
                StreamGeometry.Parse("M4,4 L28,4 L28,28 L4,28 Z"),
                new ImmutableSolidColorBrush(colors[i]),
                Padding: 8));
        }

        node.LayoutFactory = () =>
        {
            var wrap = new WrapLayoutWidget
            {
                Orientation = Orientation.Horizontal,
                Padding = new Thickness(4),
                Spacing = 4,
                DefaultItemWidth = 28,
                DefaultItemHeight = 28,
            };

            for (var i = 0; i < colors.Length; i++)
            {
                var key = $"Widget.Layout.Wrap.Icon.{i}";
                wrap.Children.Add(new IconWidget
                {
                    Key = key,
                    DesiredWidth = 24,
                    DesiredHeight = 24,
                    Padding = 6,
                });
            }

            return wrap;
        };

        return node;
    }

    private static WidgetGalleryNode CreateGridNode()
    {
        var node = new WidgetGalleryNode("Grid Layout", "GridLayoutWidget distributes items in a fixed matrix");

        for (var i = 0; i < 4; i++)
        {
            var key = $"Widget.Layout.Grid.Button.{i}";
            node.SetValue(key, new ButtonWidgetValue($"Action {i + 1}", IsPrimary: i == 0));
        }

        node.LayoutFactory = () =>
        {
            var grid = new GridLayoutWidget
            {
                Columns = 2,
                Padding = new Thickness(6),
                Spacing = 6,
            };

            for (var i = 0; i < 4; i++)
            {
                var key = $"Widget.Layout.Grid.Button.{i}";
                grid.Children.Add(new ButtonWidget
                {
                    Key = key,
                    DesiredHeight = 26,
                });
            }

            return grid;
        };

        return node;
    }

    private static WidgetGalleryNode CreateDockNode()
    {
        var node = new WidgetGalleryNode("Dock Layout", "DockLayoutWidget reserves banded edges with fill content")
        {
            IconValue = new IconWidgetValue(
                CreateDocumentGeometry(),
                new ImmutableSolidColorBrush(Color.FromRgb(79, 154, 255)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(36, 98, 156)), 1),
                Padding: 10),
            ProgressValue = new ProgressWidgetValue(0.5, IsIndeterminate: true,
                Foreground: new ImmutableSolidColorBrush(Color.FromRgb(36, 98, 156)),
                Background: new ImmutableSolidColorBrush(Color.FromRgb(220, 232, 248)))
        };

        node.SetValue("Widget.Layout.Dock.Button", new ButtonWidgetValue("Details", IsPrimary: false));

        node.LayoutFactory = () =>
        {
            var dock = new DockLayoutWidget
            {
                Padding = new Thickness(6),
                Spacing = 6,
                DefaultDockLength = 64,
                LastChildFill = true,
            };

            var header = new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyName,
                EmSize = 14,
                DesiredHeight = 20,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40)),
            };
            dock.Children.Add(header);
            dock.SetDock(header, Dock.Top);

            var footer = new ProgressWidget
            {
                Key = WidgetGalleryNode.KeyProgress,
                DesiredHeight = 10,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(224, 236, 249)),
            };
            dock.Children.Add(footer);
            dock.SetDock(footer, Dock.Bottom);

            var leadingIcon = new IconWidget
            {
                Key = WidgetGalleryNode.KeyIcon,
                DesiredWidth = 28,
                DesiredHeight = 28,
                Padding = 8,
            };
            dock.Children.Add(leadingIcon);
            dock.SetDock(leadingIcon, Dock.Left);

            var content = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
            };

            content.Children.Add(new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyDescription,
                EmSize = 12,
                DesiredHeight = 18,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
            });

            content.Children.Add(new ButtonWidget
            {
                Key = "Widget.Layout.Dock.Button",
                DesiredHeight = 26,
            });

            dock.Children.Add(content);

            return dock;
        };

        return node;
    }

    private static void DrawSparkline(DrawingContext context, Rect bounds)
    {
        var rect = bounds.Deflate(4);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(246, 250, 255)), null, rect);

        var data = new[] { 4, 12, 8, 16, 10, 18, 6, 14 };
        var min = data.Min();
        var max = data.Max();
        var range = Math.Max(1, max - min);
        var step = rect.Width / Math.Max(1, data.Length - 1);

        var stroke = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)), 1.5);
        Point? previous = null;

        for (var i = 0; i < data.Length; i++)
        {
            var normalized = (data[i] - min) / range;
            var x = rect.X + (step * i);
            var y = rect.Bottom - (rect.Height * normalized);
            var point = new Point(x, y);

            if (previous is { } previousPoint)
            {
                context.DrawLine(stroke, previousPoint, point);
            }

            previous = point;
        }
    }

    private static void DrawTarget(DrawingContext context, Rect bounds)
    {
        var rect = bounds.Deflate(4);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var center = rect.Center;
        var radius = Math.Min(rect.Width, rect.Height) / 2;

        var background = new ImmutableSolidColorBrush(Color.FromRgb(255, 245, 245));
        var stroke = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(206, 68, 64)), 1.2);
        context.DrawEllipse(background, stroke, center, radius, radius);

        context.DrawEllipse(null, new Pen(stroke.Brush, 1), center, radius * 0.66, radius * 0.66);
        context.DrawEllipse(null, new Pen(stroke.Brush, 1), center, radius * 0.33, radius * 0.33);

        var crossPen = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(158, 45, 42)), 1);
        context.DrawLine(crossPen, new Point(center.X - radius, center.Y), new Point(center.X + radius, center.Y));
        context.DrawLine(crossPen, new Point(center.X, center.Y - radius), new Point(center.X, center.Y + radius));
    }

    public static Geometry CreateWaveGeometry()
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            const int segments = 24;
            context.BeginFigure(new Point(0, 1), isFilled: true);
            for (var i = 0; i <= segments; i++)
            {
                var t = (double)i / segments;
                var y = 0.5 - (Math.Sin(t * Math.PI * 2) * 0.35);
                context.LineTo(new Point(t, y));
            }
            context.LineTo(new Point(1, 1));
            context.LineTo(new Point(0, 1));
            context.EndFigure(true);
        }

        // Wave geometry uses 0..1 normalized coordinates, so stretch handles scaling.
        return geometry;
    }
}
