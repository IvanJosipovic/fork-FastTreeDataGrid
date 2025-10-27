using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.WidgetsDemo.ViewModels.Widgets;

internal static class WidgetSamplesFactory
{
    public static IReadOnlyList<WidgetGalleryNode> Create()
    {
        return new[]
        {
            CreateIconGroup(),
            CreateGeometryGroup(),
            CreateShapesGroup(),
            CreateTextGroup(),
            CreateButtonGroup(),
            CreateCheckBoxGroup(),
            CreateToggleGroup(),
            CreateRadioGroup(),
            CreateSliderGroup(),
            CreateNumericUpDownGroup(),
            CreateScrollBarGroup(),
            CreateCalendarGroup(),
            CreateDatePickerGroup(),
            CreateCalendarDatePickerGroup(),
            CreateTimePickerGroup(),
            CreateAutoCompleteGroup(),
            CreateComboBoxGroup(),
            CreateTabControlGroup(),
            CreateMenuGroup(),
            CreateBadgeGroup(),
            CreateProgressGroup(),
            CreateCustomGroup(),
            CreateLayoutGroup(),
            CreateMediaGroup(),
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

    private static WidgetGalleryNode CreateShapesGroup()
    {
        var group = new WidgetGalleryNode("Shapes", "Shape widgets mirroring Avalonia.Controls.Shapes with Fluent styling");

        group.AddChild(CreateRectangleShapeSample());
        group.AddChild(CreateEllipseShapeSample());
        group.AddChild(CreateLineShapeSample());
        group.AddChild(CreatePolygonShapeSample());
        group.AddChild(CreatePolylineShapeSample());
        group.AddChild(CreateArcShapeSample());
        group.AddChild(CreateSectorShapeSample());
        group.AddChild(CreatePathShapeSample());

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

    private static WidgetGalleryNode CreateRectangleShapeSample()
    {
        var node = new WidgetGalleryNode("Rectangle", "RectangleShapeWidget draws rounded rectangles with fill and stroke");
        node.LayoutFactory = () =>
        {
            var shape = new RectangleShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            shape.UpdateValue(null, new RectangleShapeWidgetValue(
                RadiusX: 8,
                RadiusY: 8,
                Fill: new ImmutableSolidColorBrush(Color.FromRgb(59, 130, 246)),
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(19, 78, 166)),
                StrokeThickness: 2));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreateEllipseShapeSample()
    {
        var node = new WidgetGalleryNode("Ellipse", "EllipseShapeWidget scales uniformly with Fluent palette brushes");
        node.LayoutFactory = () =>
        {
            var shape = new EllipseShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            shape.UpdateValue(null, new EllipseShapeWidgetValue(
                Fill: new ImmutableSolidColorBrush(Color.FromRgb(52, 211, 153)),
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(4, 120, 87)),
                StrokeThickness: 2));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreateLineShapeSample()
    {
        var node = new WidgetGalleryNode("Line", "LineShapeWidget renders crisp stroked segments");
        node.LayoutFactory = () =>
        {
            var shape = new LineShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            shape.UpdateValue(null, new LineShapeWidgetValue(
                StartPoint: new Point(2, 28),
                EndPoint: new Point(30, 4),
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(248, 113, 113)),
                StrokeThickness: 3,
                StrokeLineCap: PenLineCap.Round));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreatePolygonShapeSample()
    {
        var node = new WidgetGalleryNode("Polygon", "PolygonShapeWidget fills closed point collections");
        node.LayoutFactory = () =>
        {
            var shape = new PolygonShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            var points = new[]
            {
                new Point(16, 2),
                new Point(30, 14),
                new Point(26, 30),
                new Point(6, 30),
                new Point(2, 14),
            };

            shape.UpdateValue(null, new PolygonShapeWidgetValue(
                Points: points,
                Fill: new ImmutableSolidColorBrush(Color.FromRgb(96, 165, 250)),
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(29, 78, 216)),
                StrokeThickness: 2));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreatePolylineShapeSample()
    {
        var node = new WidgetGalleryNode("Polyline", "PolylineShapeWidget renders open strokes with dash patterns");
        node.LayoutFactory = () =>
        {
            var shape = new PolylineShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            var points = new[]
            {
                new Point(2, 20),
                new Point(10, 6),
                new Point(18, 18),
                new Point(26, 4),
                new Point(30, 16),
            };

            shape.UpdateValue(null, new PolylineShapeWidgetValue(
                Points: points,
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(168, 85, 247)),
                StrokeThickness: 2,
                StrokeDashArray: new[] { 2.0, 2.0 },
                StrokeLineCap: PenLineCap.Round));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreateArcShapeSample()
    {
        var node = new WidgetGalleryNode("Arc", "ArcShapeWidget traces circular arcs with scaling stretch");
        node.LayoutFactory = () =>
        {
            var shape = new ArcShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            shape.UpdateValue(null, new ArcShapeWidgetValue(
                StartAngle: 220,
                SweepAngle: 250,
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(14, 116, 144)),
                StrokeThickness: 3,
                StrokeLineCap: PenLineCap.Round));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreateSectorShapeSample()
    {
        var node = new WidgetGalleryNode("Sector", "SectorShapeWidget fills radial segments for pie visuals");
        node.LayoutFactory = () =>
        {
            var shape = new SectorShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            shape.UpdateValue(null, new SectorShapeWidgetValue(
                StartAngle: 300,
                SweepAngle: 120,
                Fill: new ImmutableSolidColorBrush(Color.FromRgb(251, 191, 36)),
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(217, 119, 6)),
                StrokeThickness: 1.5));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreatePathShapeSample()
    {
        var node = new WidgetGalleryNode("Path", "PathShapeWidget draws complex geometries via data strings");
        node.LayoutFactory = () =>
        {
            var shape = new PathShapeWidget
            {
                DesiredWidth = 120,
                DesiredHeight = 90,
                Stretch = Stretch.Uniform,
            };

            shape.UpdateValue(null, new PathShapeWidgetValue(
                Data: null,
                DataString: "M4,26 C10,16 14,10 16,6 C18,10 22,16 28,26",
                Stroke: new ImmutableSolidColorBrush(Color.FromRgb(220, 38, 38)),
                StrokeThickness: 2,
                StrokeLineCap: PenLineCap.Round));

            return shape;
        };

        return node;
    }

    private static WidgetGalleryNode CreateTextBlockSample()
    {
        var node = new WidgetGalleryNode("TextBlock", "TextBlockWidget renders wrapped static text");
        node.LayoutFactory = () =>
        {
            var border = new BorderWidget
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(8),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(247, 249, 255)),
                DesiredWidth = 320,
            };

            var text = new TextBlockWidget
            {
                EmSize = 13,
                Trimming = TextTrimming.CharacterEllipsis,
            };
            text.SetText("Immediate-mode text rendering mirrors TextBlock behavior, including wrapping and trimming, without the templated control overhead.");
            border.Child = text;
            return border;
        };
        return node;
    }

    private static WidgetGalleryNode CreateLabelSample()
    {
        var node = new WidgetGalleryNode("Label", "LabelWidget with emphasis styling");
        node.LayoutFactory = () =>
        {
            var label = new LabelWidget
            {
                EmSize = 14,
                FontWeight = FontWeight.SemiBold,
                DesiredWidth = 260,
            };
            label.SetText("LabelWidget provides lightweight captions aligned with Fluent typography.");
            return label;
        };
        return node;
    }

    private static WidgetGalleryNode CreateSelectableTextSample()
    {
        var node = new WidgetGalleryNode("SelectableText", "SelectableTextWidget supports pointer selection and caret rendering");
        node.LayoutFactory = () =>
        {
            var selectable = new SelectableTextWidget
            {
                DesiredWidth = 320,
                DesiredHeight = 72,
                EmSize = 12,
            };
            selectable.SetText("Drag to highlight text without Avalonia controls. Selection rectangles and the caret are drawn directly on the widget surface.");
            selectable.SetSelection(5, 9);
            return selectable;
        };
        return node;
    }

    private static WidgetGalleryNode CreateDocumentTextSample()
    {
        var node = new WidgetGalleryNode("DocumentText", "DocumentTextWidget styles spans with color and weight");
        node.LayoutFactory = () =>
        {
            var document = new DocumentTextWidget
            {
                DesiredWidth = 320,
                EmSize = 12,
            };

            var spans = new List<DocumentTextSpan>
            {
                new("Rich text "),
                new("spans ", new ImmutableSolidColorBrush(Color.FromRgb(37, 99, 235)), FontWeight: FontWeight.Bold),
                new("combine multiple styles "),
                new("inside ", new ImmutableSolidColorBrush(Color.FromRgb(220, 38, 38)), FontWeight: FontWeight.SemiBold),
                new("a single widget.", new ImmutableSolidColorBrush(Color.FromRgb(15, 118, 110)), FontStyle: FontStyle.Italic),
            };

            document.SetDocument(new DocumentTextWidgetValue(spans));
            return document;
        };
        return node;
    }

    private static WidgetGalleryNode CreateMediaGroup()
    {
        var group = new WidgetGalleryNode("Media & Icons", "Image and icon element widgets with geometry and bitmap sources");

        group.AddChild(CreateImageWidgetSample());
        group.AddChild(CreateIconElementSample());
        group.AddChild(CreatePathIconSample());

        return group;
    }

    private static WidgetGalleryNode CreateImageWidgetSample()
    {
        var node = new WidgetGalleryNode("Image", "ImageWidget renders IImage with stretch and padding");
        node.LayoutFactory = () =>
        {
            var imageWidget = new ImageWidget
            {
                DesiredWidth = 140,
                DesiredHeight = 100,
                Padding = 6,
                Stretch = Stretch.Uniform,
            };
            imageWidget.Source = CreateSampleImage(Color.FromRgb(59, 130, 246), "IMG");
            return imageWidget;
        };
        return node;
    }

    private static WidgetGalleryNode CreateIconElementSample()
    {
        var node = new WidgetGalleryNode("IconElement", "IconElementWidget combines background fills and geometry");
        node.LayoutFactory = () =>
        {
            var icon = new IconElementWidget
            {
                DesiredWidth = 72,
                DesiredHeight = 72,
            };
            icon.UpdateValue(null, new IconElementWidgetValue(
                Geometry: StreamGeometry.Parse("M4,12 L12,4 28,20 20,28 Z"),
                Foreground: new ImmutableSolidColorBrush(Color.FromRgb(255, 255, 255)),
                Background: new ImmutableSolidColorBrush(Color.FromRgb(52, 211, 153)),
                Padding: 10));
            return icon;
        };
        return node;
    }

    private static WidgetGalleryNode CreatePathIconSample()
    {
        var node = new WidgetGalleryNode("PathIcon", "PathIconWidget parses path data similarly to Avalonia's PathIcon");
        node.LayoutFactory = () =>
        {
            var icon = new PathIconWidget
            {
                DesiredWidth = 72,
                DesiredHeight = 72,
            };
            icon.UpdateValue(null, new PathIconWidgetValue(
                Data: "M8,4 L24,4 L28,12 L28,28 L8,28 Z M18,10 L18,18 24,18 16,28 16,20 10,20 18,10 Z",
                Foreground: new ImmutableSolidColorBrush(Color.FromRgb(79, 70, 229)),
                Padding: 8));
            return icon;
        };
        return node;
    }

    private static IImage CreateSampleImage(Color background, string text)
    {
        var size = new PixelSize(96, 96);
        var dpi = new Vector(96, 96);
        var bitmap = new RenderTargetBitmap(size, dpi);

        using (var ctx = bitmap.CreateDrawingContext())
        {
            ctx.FillRectangle(new SolidColorBrush(background), new Rect(0, 0, size.Width, size.Height));

            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                28,
                Brushes.White);

            var origin = new Point(
                (size.Width - formatted.Width) / 2,
                (size.Height - formatted.Height) / 2);

            ctx.DrawText(formatted, origin);
        }

        return bitmap;
    }

    private static WidgetGalleryNode CreateTextGroup()
    {
        var group = new WidgetGalleryNode("Text", "Text widgets for content, labels, selection, and rich styling");

        group.AddChild(CreateTextBlockSample());
        group.AddChild(CreateLabelSample());
        group.AddChild(CreateSelectableTextSample());
        group.AddChild(CreateDocumentTextSample());
        group.AddChild(CreateTextInputSample());
        group.AddChild(CreateMaskedTextSample());

        return group;
    }

    private static WidgetGalleryNode CreateTextInputSample()
    {
        var node = new WidgetGalleryNode("TextInput", "TextInputWidget with caret and placeholder support");
        node.LayoutFactory = () =>
        {
            var input = new TextInputWidget
            {
                DesiredWidth = 260,
                Placeholder = "Type here",
            };
            input.Text = "Immediate-mode input";
            return input;
        };

        return node;
    }

    private static WidgetGalleryNode CreateMaskedTextSample()
    {
        var node = new WidgetGalleryNode("MaskedText", "MaskedTextBoxWidget hides characters behind a mask char");
        node.LayoutFactory = () =>
        {
            var masked = new MaskedTextBoxWidget
            {
                DesiredWidth = 260,
                Placeholder = "Password",
                MaskChar = '●',
            };
            masked.Text = "Secret123";
            return masked;
        };

        return node;
    }

    private static WidgetGalleryNode CreateLayoutGroup()
    {
        var group = new WidgetGalleryNode("Layouts", "Surface-based layout widgets replicating common panels");

        group.AddChild(CreateHorizontalStackNode());
        group.AddChild(CreateVerticalStackNode());
        group.AddChild(CreateWrapNode());
        group.AddChild(CreateCanvasNode());
        group.AddChild(CreateUniformGridNode());
        group.AddChild(CreateSplitViewNode());
        group.AddChild(CreateViewboxNode());
        group.AddChild(CreateLayoutTransformNode());
        group.AddChild(CreateContentControlNode());
        group.AddChild(CreateDecoratorNode());
        group.AddChild(CreateBorderVisualNode());
        group.AddChild(CreateGroupBoxNode());
        group.AddChild(CreateExpanderNode());
        group.AddChild(CreateScrollViewerNode());
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
            ButtonValue = new ButtonWidgetValue("Launch", IsPrimary: true, Automation: new WidgetAutomationSettings(CommandLabel: "Launch workflow", AccessKey: "L"))
        };
        group.AddChild(primary);

        var pressed = new WidgetGalleryNode("Pressed", "Pressed accent state")
        {
            ButtonValue = new ButtonWidgetValue("Pressed", IsPrimary: true, IsPressed: true, Automation: new WidgetAutomationSettings(CommandLabel: "Confirm selection"))
        };
        group.AddChild(pressed);

        var secondaryDisabled = new WidgetGalleryNode("Disabled", "Secondary button disabled")
        {
            ButtonValue = new ButtonWidgetValue("Disabled", IsEnabled: false)
        };
        group.AddChild(secondaryDisabled);

        var subtle = new WidgetGalleryNode("Subtle", "Low-emphasis ghost button")
        {
            ButtonValue = new ButtonWidgetValue("More _Options", Variant: ButtonWidgetVariant.Subtle, Automation: new WidgetAutomationSettings(CommandLabel: "Show more options"))
        };
        group.AddChild(subtle);

        var destructive = new WidgetGalleryNode("Destructive", "Danger action styling")
        {
            ButtonValue = new ButtonWidgetValue("_Delete", Variant: ButtonWidgetVariant.Destructive, Automation: new WidgetAutomationSettings(CommandLabel: "Delete item", AccessKey: "D"))
        };
        group.AddChild(destructive);

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

    private static WidgetGalleryNode CreateNumericUpDownGroup()
    {
        var group = new WidgetGalleryNode("Numeric UpDown", "NumericUpDownWidget integer and decimal configurations");

        var whole = new WidgetGalleryNode("Whole numbers", "Step 1 increments")
        {
            NumericValue = new NumericUpDownWidgetValue(12, Minimum: 0, Maximum: 100, Increment: 1, DecimalPlaces: 0)
        };
        group.AddChild(whole);

        var decimalSample = new WidgetGalleryNode("Decimals", "Two decimal places with smaller step")
        {
            NumericValue = new NumericUpDownWidgetValue(23.5, Minimum: -50, Maximum: 150, Increment: 0.5, DecimalPlaces: 1)
        };
        group.AddChild(decimalSample);

        var disabled = new WidgetGalleryNode("Disabled", "Read-only sample")
        {
            NumericValue = new NumericUpDownWidgetValue(0.75, Minimum: 0, Maximum: 1, Increment: 0.05, DecimalPlaces: 2, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateScrollBarGroup()
    {
        var group = new WidgetGalleryNode("ScrollBar", "ScrollBarWidget vertical and horizontal samples");

        var vertical = new WidgetGalleryNode("Vertical", "Viewport aware vertical bar")
        {
            ScrollBarValue = new ScrollBarWidgetValue(24, Minimum: 0, Maximum: 100, ViewportSize: 18, Orientation: Orientation.Vertical)
        };
        group.AddChild(vertical);

        var horizontal = new WidgetGalleryNode("Horizontal", "Horizontal scroll offset")
        {
            ScrollBarValue = new ScrollBarWidgetValue(0.4, Minimum: 0, Maximum: 1, ViewportSize: 0.25, Orientation: Orientation.Horizontal)
        };
        group.AddChild(horizontal);

        var disabled = new WidgetGalleryNode("Disabled", "Scroll bar disabled")
        {
            ScrollBarValue = new ScrollBarWidgetValue(60, Minimum: 0, Maximum: 100, ViewportSize: 16, Orientation: Orientation.Vertical, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateCalendarGroup()
    {
        var group = new WidgetGalleryNode("Calendar", "CalendarWidget month navigation and selection");

        var current = new WidgetGalleryNode("Current", "Current month with today selected")
        {
            CalendarValue = new CalendarWidgetValue(DateTime.Today, SelectedDate: DateTime.Today)
        };
        group.AddChild(current);

        var range = new WidgetGalleryNode("Range", "Calendar limited to Q1 2024")
        {
            CalendarValue = new CalendarWidgetValue(new DateTime(2024, 2, 1), SelectedDate: new DateTime(2024, 2, 14), Minimum: new DateTime(2024, 1, 1), Maximum: new DateTime(2024, 3, 31))
        };
        group.AddChild(range);

        var disabled = new WidgetGalleryNode("Disabled", "Calendar disabled state")
        {
            CalendarValue = new CalendarWidgetValue(DateTime.Today, SelectedDate: null, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateDatePickerGroup()
    {
        var group = new WidgetGalleryNode("Date Picker", "DatePickerWidget inline drop-down calendar");

        var today = new WidgetGalleryNode("Today", "Default culture format")
        {
            DatePickerValue = new DatePickerWidgetValue(DateTime.Today)
        };
        group.AddChild(today);

        var formatted = new WidgetGalleryNode("Formatted", "Custom format and bounds")
        {
            DatePickerValue = new DatePickerWidgetValue(
                new DateTime(2024, 4, 15),
                Minimum: new DateTime(2024, 1, 1),
                Maximum: new DateTime(2024, 12, 31),
                FormatString: "dddd, MMM dd" )
        };
        group.AddChild(formatted);

        var disabled = new WidgetGalleryNode("Disabled", "Picker disabled state")
        {
            DatePickerValue = new DatePickerWidgetValue(null, IsEnabled: false)
        };
        group.AddChild(disabled);

        return group;
    }

    private static WidgetGalleryNode CreateCalendarDatePickerGroup()
    {
        var group = new WidgetGalleryNode("Calendar Date Picker", "CalendarDatePickerWidget single date selection");

        var start = new WidgetGalleryNode("Current", "Formatted long date")
        {
            CalendarDatePickerValue = new CalendarDatePickerWidgetValue(DateTime.Today, FormatString: "MMMM dd, yyyy")
        };
        group.AddChild(start);

        var limited = new WidgetGalleryNode("Range", "Quarter-limited picker")
        {
            CalendarDatePickerValue = new CalendarDatePickerWidgetValue(
                new DateTime(2024, 5, 10),
                Minimum: new DateTime(2024, 4, 1),
                Maximum: new DateTime(2024, 6, 30),
                FormatString: "dd MMM yyyy")
        };
        group.AddChild(limited);

        var closed = new WidgetGalleryNode("Closed", "Drop-down closed state")
        {
            CalendarDatePickerValue = new CalendarDatePickerWidgetValue(new DateTime(2024, 7, 4), IsDropDownOpen: false)
        };
        group.AddChild(closed);

        return group;
    }

    private static WidgetGalleryNode CreateTimePickerGroup()
    {
        var group = new WidgetGalleryNode("Time Picker", "TimePickerWidget hour/minute selection");

        var defaultNode = new WidgetGalleryNode("Default", "24-hour time")
        {
            TimePickerValue = new TimePickerWidgetValue(TimeSpan.FromHours(14).Add(TimeSpan.FromMinutes(30)))
        };
        group.AddChild(defaultNode);

        var rangeNode = new WidgetGalleryNode("Range", "Business hours")
        {
            TimePickerValue = new TimePickerWidgetValue(
                Time: TimeSpan.FromHours(9.5),
                Minimum: TimeSpan.FromHours(8),
                Maximum: TimeSpan.FromHours(18))
        };
        group.AddChild(rangeNode);

        var disabledNode = new WidgetGalleryNode("Disabled", "Picker disabled state")
        {
            TimePickerValue = new TimePickerWidgetValue(TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(15)), IsEnabled: false)
        };
        group.AddChild(disabledNode);

        return group;
    }

    private static WidgetGalleryNode CreateAutoCompleteGroup()
    {
        var group = new WidgetGalleryNode("AutoComplete", "AutoCompleteBoxWidget suggestions");

        var suggestions = new[] { "Apple", "Apricot", "Banana", "Blackberry", "Blueberry", "Cherry", "Date", "Fig", "Grape", "Kiwi" };

        var defaultNode = new WidgetGalleryNode("Default", "Shows matching suggestions")
        {
            AutoCompleteValue = new AutoCompleteBoxWidgetValue("Ap", suggestions)
        };
        group.AddChild(defaultNode);

        var emptyNode = new WidgetGalleryNode("Empty", "No pre-filled text")
        {
            AutoCompleteValue = new AutoCompleteBoxWidgetValue(null, suggestions)
        };
        group.AddChild(emptyNode);

        var disabledNode = new WidgetGalleryNode("Disabled", "Auto complete disabled")
        {
            AutoCompleteValue = new AutoCompleteBoxWidgetValue("Cherry", suggestions, IsEnabled: false)
        };
        group.AddChild(disabledNode);

        return group;
    }

    private sealed record City(string Name, string State);

    private static WidgetGalleryNode CreateComboBoxGroup()
    {
        var group = new WidgetGalleryNode("Combo Box", "ComboBoxWidget static and generated items");

        var cities = new[]
        {
            new City("Seattle", "WA"),
            new City("Portland", "OR"),
            new City("San Francisco", "CA"),
            new City("Los Angeles", "CA"),
            new City("Phoenix", "AZ"),
        };

        var defaultNode = new WidgetGalleryNode("Cities", "Select from provided list")
        {
            ComboBoxValue = new ComboBoxWidgetValue(cities, SelectedItem: cities[0], DisplayMemberPath: nameof(City.Name))
        };
        group.AddChild(defaultNode);

        var generatedNode = new WidgetGalleryNode("Generated", "Items provider")
        {
            ComboBoxValue = new ComboBoxWidgetValue(
                ItemsProvider: () => Enumerable.Range(1, 5).Select(i => new City($"Option {i}", "Value")),
                SelectedItem: null,
                DisplayMemberPath: nameof(City.Name))
        };
        group.AddChild(generatedNode);

        var disabledNode = new WidgetGalleryNode("Disabled", "Combo disabled state")
        {
            ComboBoxValue = new ComboBoxWidgetValue(cities, SelectedItem: cities[2], DisplayMemberPath: nameof(City.Name), IsEnabled: false)
        };
        group.AddChild(disabledNode);

        return group;
    }

    private sealed record TabSample(string Title, string Description, string Badge);

    private static WidgetGalleryNode CreateTabControlGroup()
    {
        var group = new WidgetGalleryNode("Tab Control", "TabControlWidget with TabItemWidget headers");

        var navigationNode = new WidgetGalleryNode("Navigation", "Three-tab layout with Fluent indicator")
        {
            LayoutFactory = () =>
            {
                var samples = new[]
                {
                    new TabSample("Overview", "Immediate-mode tabs reuse the FlatTreeDataGrid batching infrastructure for headers so switching remains smooth.", "Insights"),
                    new TabSample("Details", "Header and content factories provide full control over per-tab visuals without templated controls.", "Details"),
                    new TabSample("Settings", "Indicator thickness, padding, and corner radius all flow from Fluent resource lookups.", "Settings")
                };

                var tabControl = new TabControlWidget
                {
                    DesiredWidth = 340,
                    DesiredHeight = 220
                };

                var value = new TabControlWidgetValue(
                    Items: samples,
                    SelectedIndex: 0,
                    HeaderFactory: (_, item) => CreateTabHeader((TabSample)item!),
                    ContentFactory: (_, item) => CreateTabContent((TabSample)item!));

                tabControl.UpdateValue(null, value);
                return tabControl;
            }
        };

        group.AddChild(navigationNode);
        return group;

        static Widget CreateTabHeader(TabSample sample)
        {
            var header = new FormattedTextWidget
            {
                EmSize = 13,
                DesiredHeight = 20,
                DesiredWidth = double.NaN
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
                Padding = new Thickness(16, 12, 16, 12)
            };

            var badge = new BadgeWidget
            {
                DesiredHeight = 22,
                BackgroundBrush = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                ForegroundBrush = new ImmutableSolidColorBrush(Colors.White),
                Padding = 6
            };
            badge.SetText(sample.Badge);

            var description = new FormattedTextWidget
            {
                EmSize = 12,
                DesiredWidth = double.NaN,
                DesiredHeight = double.NaN
            };
            description.SetText(sample.Description);

            var progress = new ProgressWidget
            {
                DesiredHeight = 4,
                Progress = 0.6
            };

            stack.Children.Add(badge);
            stack.Children.Add(description);
            stack.Children.Add(progress);
            return stack;
        }
    }

    private static WidgetGalleryNode CreateMenuGroup()
    {
        var group = new WidgetGalleryNode("Menus", "MenuWidget and MenuBarWidget samples");

        var menuBarNode = new WidgetGalleryNode("Menu Bar", "Top-level menu with drop-down items")
        {
            LayoutFactory = () =>
            {
                var menuBar = new MenuBarWidget
                {
                    DesiredWidth = 360,
                    DesiredHeight = 48
                };

                var items = new[]
                {
                    new MenuBarItemWidgetValue("_File", new MenuWidgetValue(new[]
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
                    })),
                    new MenuBarItemWidgetValue("_Edit", new MenuWidgetValue(new[]
                    {
                        new MenuItemWidgetValue("_Undo", GestureText: "Ctrl+Z"),
                        new MenuItemWidgetValue("_Redo", GestureText: "Ctrl+Y"),
                        new MenuItemWidgetValue(string.Empty, IsSeparator: true),
                        new MenuItemWidgetValue("_Preferences...")
                    })),
                    new MenuBarItemWidgetValue("_Help", new MenuWidgetValue(new[]
                    {
                        new MenuItemWidgetValue("_Documentation"),
                        new MenuItemWidgetValue("_Report Issue"),
                        new MenuItemWidgetValue(string.Empty, IsSeparator: true),
                        new MenuItemWidgetValue("_About FastTreeDataGrid")
                    }))
                };

                menuBar.UpdateValue(null, new MenuBarWidgetValue(items));
                return menuBar;
            }
        };
        group.AddChild(menuBarNode);

        var contextMenuNode = new WidgetGalleryNode("Context Menu", "Standalone context menu surface")
        {
            LayoutFactory = () =>
            {
                var contextMenu = new ContextMenuWidget
                {
                    DesiredWidth = 240,
                    DesiredHeight = 160
                };

                var items = new[]
                {
                    new MenuItemWidgetValue("_Refresh", GestureText: "F5"),
                    new MenuItemWidgetValue("_Paste", GestureText: "Ctrl+V"),
                    new MenuItemWidgetValue(string.Empty, IsSeparator: true),
                    new MenuItemWidgetValue("Inspect _Element"),
                    new MenuItemWidgetValue("_Properties", GestureText: "Alt+Enter")
                };

                contextMenu.UpdateValue(null, new ContextMenuWidgetValue(items));
                contextMenu.ShowAt(new Rect(new Point(140, 60), new Size(0, 0)));

                return contextMenu;
            }
        };
        group.AddChild(contextMenuNode);

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
            BadgeValue = new BadgeWidgetValue("READY", new ImmutableSolidColorBrush(Color.FromRgb(36, 128, 196)), new ImmutableSolidColorBrush(Colors.White), 6, 8)
        };

        node.LayoutFactory = () =>
        {
            var layout = new StackLayoutWidget
            {
                Orientation = Orientation.Horizontal,
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

            layout.Children.Add(new BadgeWidget
            {
                Key = WidgetGalleryNode.KeyBadge,
                DesiredWidth = 64,
                DesiredHeight = 28,
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

    private static WidgetGalleryNode CreateCanvasNode()
    {
        var node = new WidgetGalleryNode("Canvas Layout", "Absolute positioned overlay with alerts")
        {
            IconValue = new IconWidgetValue(WidgetSamplesFactory.CreateWarningGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(255, 197, 61)), null, 4),
            BadgeValue = new BadgeWidgetValue("ALERT", new ImmutableSolidColorBrush(Color.FromRgb(222, 84, 84)), new ImmutableSolidColorBrush(Colors.White), 4, 6),
            PrimaryActionValue = new ButtonWidgetValue("Dismiss"),
        };

        node.LayoutFactory = () =>
        {
            var canvas = new CanvasLayoutWidget();

            var background = new BorderWidget
            {
                Background = new ImmutableSolidColorBrush(Color.FromRgb(243, 244, 246)),
                CornerRadius = new CornerRadius(8),
                DesiredWidth = 260,
                DesiredHeight = 140,
            };
            canvas.Children.Add(background);

            var icon = new IconWidget
            {
                Key = WidgetGalleryNode.KeyIcon,
                DesiredWidth = 48,
                DesiredHeight = 48,
            };
            canvas.Children.Add(icon);
            canvas.SetLeft(icon, 18);
            canvas.SetTop(icon, 20);

            var title = new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyName,
                EmSize = 16,
                DesiredWidth = 160,
            };
            canvas.Children.Add(title);
            canvas.SetLeft(title, 80);
            canvas.SetTop(title, 24);

            var description = new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyDescription,
                EmSize = 12,
                DesiredWidth = 160,
            };
            canvas.Children.Add(description);
            canvas.SetLeft(description, 80);
            canvas.SetTop(description, 50);

            var badge = new BadgeWidget
            {
                Key = WidgetGalleryNode.KeyBadge,
                DesiredWidth = 64,
                DesiredHeight = 28,
            };
            canvas.Children.Add(badge);
            canvas.SetRight(badge, 16);
            canvas.SetTop(badge, 20);

            var button = new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyPrimaryAction,
                DesiredWidth = 96,
                DesiredHeight = 32,
            };
            canvas.Children.Add(button);
            canvas.SetLeft(button, 80);
            canvas.SetBottom(button, 16);

            return canvas;
        };

        return node;
    }

    private static WidgetGalleryNode CreateUniformGridNode()
    {
        var node = new WidgetGalleryNode("Uniform Grid", "Uniform layout for quick actions");

        node.SetValue("Widget.Layout.Uniform.Badge.0", new BadgeWidgetValue("New", new ImmutableSolidColorBrush(Color.FromRgb(56, 189, 248)), new ImmutableSolidColorBrush(Colors.White)));
        node.SetValue("Widget.Layout.Uniform.Badge.1", new BadgeWidgetValue("Sync", new ImmutableSolidColorBrush(Color.FromRgb(129, 140, 248)), new ImmutableSolidColorBrush(Colors.White)));
        node.SetValue("Widget.Layout.Uniform.Badge.2", new BadgeWidgetValue("Alerts", new ImmutableSolidColorBrush(Color.FromRgb(248, 113, 113)), new ImmutableSolidColorBrush(Colors.White)));
        node.SetValue("Widget.Layout.Uniform.Badge.3", new BadgeWidgetValue("Archive", new ImmutableSolidColorBrush(Color.FromRgb(74, 222, 128)), new ImmutableSolidColorBrush(Colors.White)));

        node.LayoutFactory = () =>
        {
            var uniform = new UniformGridLayoutWidget
            {
                Rows = 2,
                Columns = 2,
                DesiredWidth = 220,
                DesiredHeight = 120,
            };

            for (var i = 0; i < 4; i++)
            {
                uniform.Children.Add(new BadgeWidget
                {
                    Key = $"Widget.Layout.Uniform.Badge.{i}",
                    DesiredWidth = 100,
                    DesiredHeight = 36,
                });
            }

            return uniform;
        };

        return node;
    }

    private static WidgetGalleryNode CreateSplitViewNode()
    {
        var node = new WidgetGalleryNode("Split View", "Pane + content layout")
        {
            BadgeValue = new BadgeWidgetValue("Pane", new ImmutableSolidColorBrush(Color.FromRgb(94, 234, 212)), new ImmutableSolidColorBrush(Color.FromRgb(15, 118, 110)), 6, 8),
            ProgressValue = new ProgressWidgetValue(0.45),
        };

        node.LayoutFactory = () =>
        {
            var splitView = new SplitViewLayoutWidget
            {
                OpenPaneLength = 180,
                CompactPaneLength = 60,
                IsPaneOpen = true,
                DesiredWidth = 300,
                DesiredHeight = 140,
            };

            var pane = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
            };
            pane.StyleKey = SplitViewLayoutWidget.PaneStyleKey;
            pane.Children.Add(new BadgeWidget
            {
                Key = WidgetGalleryNode.KeyBadge,
                DesiredWidth = 120,
                DesiredHeight = 32,
            });
            pane.Children.Add(new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyPrimaryAction,
                DesiredHeight = 28,
            });

            node.PrimaryActionValue = new ButtonWidgetValue("Open");

            var content = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
            };
            content.StyleKey = SplitViewLayoutWidget.ContentStyleKey;
            content.Children.Add(new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyDescription,
                EmSize = 14,
                DesiredHeight = 18,
            });
            content.Children.Add(new ProgressWidget
            {
                Key = WidgetGalleryNode.KeyProgress,
                DesiredHeight = 8,
            });

            splitView.Children.Add(pane);
            splitView.Children.Add(content);

            return splitView;
        };

        return node;
    }

    private static WidgetGalleryNode CreateViewboxNode()
    {
        var node = new WidgetGalleryNode("Viewbox", "Scales content uniformly")
        {
            GeometryValue = new GeometryWidgetValue(
                WidgetSamplesFactory.CreatePolygonGeometry(),
                Stretch.Uniform,
                new ImmutableSolidColorBrush(Color.FromRgb(129, 140, 248)),
                new Pen(new ImmutableSolidColorBrush(Color.FromRgb(67, 56, 202)), 1.5),
                Padding: 6),
        };

        node.LayoutFactory = () =>
        {
            var viewbox = new ViewboxLayoutWidget
            {
                Stretch = Stretch.UniformToFill,
                StretchDirection = StretchDirection.Both,
                DesiredWidth = 220,
                DesiredHeight = 140,
            };

            viewbox.Children.Add(new GeometryWidget
            {
                Key = WidgetGalleryNode.KeyGeometry,
                Stretch = Stretch.Fill,
                Padding = 0,
            });

            return viewbox;
        };

        return node;
    }

    private static WidgetGalleryNode CreateLayoutTransformNode()
    {
        var node = new WidgetGalleryNode("Layout Transform", "Applies rotation and scale to content")
        {
            ButtonValue = new ButtonWidgetValue("Transform"),
        };

        node.LayoutFactory = () =>
        {
            var layoutTransform = new LayoutTransformLayoutWidget
            {
                ScaleX = 1,
                ScaleY = 1,
                Angle = 12,
                DesiredWidth = 220,
                DesiredHeight = 140,
            };

            layoutTransform.Children.Add(new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyButton,
                DesiredWidth = 110,
                DesiredHeight = 40,
            });

            return layoutTransform;
        };

        return node;
    }

    private static WidgetGalleryNode CreateContentControlNode()
    {
        var node = new WidgetGalleryNode("Content Control", "Fluent chrome around content")
        {
            IconValue = new IconWidgetValue(WidgetSamplesFactory.CreateDocumentGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(129, 140, 248)), null, 6),
            ButtonValue = new ButtonWidgetValue("Details", IsPrimary: true),
        };

        node.LayoutFactory = () =>
        {
            var content = new ContentControlWidget
            {
                DesiredWidth = 240,
                DesiredHeight = 140,
            };

            var stack = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
            };

            stack.Children.Add(new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyName,
                EmSize = 16,
                DesiredHeight = 22,
            });

            stack.Children.Add(new FormattedTextWidget
            {
                Key = WidgetGalleryNode.KeyDescription,
                EmSize = 12,
                DesiredHeight = 18,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(90, 90, 90)),
            });

            stack.Children.Add(new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyButton,
                DesiredWidth = 100,
                DesiredHeight = 32,
            });

            content.Content = stack;
            return content;
        };

        return node;
    }

    private static WidgetGalleryNode CreateGroupBoxNode()
    {
        var node = new WidgetGalleryNode("Group Box", "Headered container with grouped actions")
        {
            CheckBoxValue = new CheckBoxWidgetValue(true),
        };

        for (var i = 0; i < 3; i++)
        {
            node.SetValue($"Widget.Layout.GroupBox.Check.{i}", new CheckBoxWidgetValue(i == 0));
        }

        node.LayoutFactory = () =>
        {
            var group = new GroupBoxWidget
            {
                HeaderText = "Data Sources",
                DesiredWidth = 240,
                DesiredHeight = 160,
            };

            var stack = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
            };

            for (var i = 0; i < 3; i++)
            {
                stack.Children.Add(new CheckBoxWidget
                {
                    Key = $"Widget.Layout.GroupBox.Check.{i}",
                    DesiredHeight = 24,
                });
            }

            group.Content = stack;
            return group;
        };

        return node;
    }

    private static WidgetGalleryNode CreateExpanderNode()
    {
        var node = new WidgetGalleryNode("Expander", "Collapsible section for filters")
        {
            ToggleValue = new ToggleSwitchWidgetValue(true),
        };

        for (var i = 0; i < 3; i++)
        {
            node.SetValue($"Widget.Layout.Expander.Toggle.{i}", new ToggleSwitchWidgetValue(i == 0));
        }

        node.LayoutFactory = () =>
        {
            var expander = new ExpanderWidget
            {
                HeaderText = "Filters",
                IsExpanded = true,
                DesiredWidth = 240,
                DesiredHeight = 150,
            };

            var stack = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
            };

            for (var i = 0; i < 3; i++)
            {
                stack.Children.Add(new ToggleSwitchWidget
                {
                    Key = $"Widget.Layout.Expander.Toggle.{i}",
                    DesiredHeight = 28,
                });
            }

            expander.Content = stack;
            return expander;
        };

        return node;
    }

    private static WidgetGalleryNode CreateScrollViewerNode()
    {
        var node = new WidgetGalleryNode("Scroll Viewer", "Offsets content within a clipped viewport")
        {
            BadgeValue = new BadgeWidgetValue("Scrollable", new ImmutableSolidColorBrush(Color.FromRgb(250, 204, 21)), new ImmutableSolidColorBrush(Color.FromRgb(120, 53, 15)), 6, 8),
        };

        for (var i = 0; i < 5; i++)
        {
            node.SetValue($"Widget.Layout.Scroll.Badge.{i}", new BadgeWidgetValue($"Item {i + 1}", new ImmutableSolidColorBrush(Color.FromRgb(244, 244, 245)), new ImmutableSolidColorBrush(Color.FromRgb(63, 63, 70)), 6, 10));
        }

        node.LayoutFactory = () =>
        {
            var scroll = new ScrollViewerWidget
            {
                HorizontalOffset = 12,
                VerticalOffset = 24,
                DesiredWidth = 220,
                DesiredHeight = 120,
                Padding = new Thickness(4),
                ExtentHeight = 200,
            };

            var stack = new StackLayoutWidget
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
            };

            for (var i = 0; i < 5; i++)
            {
                stack.Children.Add(new BadgeWidget
                {
                    Key = i == 0 ? WidgetGalleryNode.KeyBadge : $"Widget.Layout.Scroll.Badge.{i}",
                    DesiredWidth = 160,
                    DesiredHeight = 28,
                });
            }

            scroll.Children.Add(stack);

            return scroll;
        };

        return node;
    }

    private static WidgetGalleryNode CreateDecoratorNode()
    {
        var node = new WidgetGalleryNode("Decorator", "Content without chrome padding the child")
        {
            IconValue = new IconWidgetValue(WidgetSamplesFactory.CreatePolygonGeometry(), new ImmutableSolidColorBrush(Color.FromRgb(56, 189, 248)), null, 6),
        };

        node.LayoutFactory = () =>
        {
            var decorator = new DecoratorWidget
            {
                DesiredWidth = 220,
                DesiredHeight = 100,
            };

            decorator.Content = new StackLayoutWidget
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new IconWidget { Key = WidgetGalleryNode.KeyIcon, DesiredWidth = 36, DesiredHeight = 36 },
                    new StackLayoutWidget
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new FormattedTextWidget { Key = WidgetGalleryNode.KeyName, EmSize = 14, DesiredHeight = 18 },
                            new FormattedTextWidget { Key = WidgetGalleryNode.KeyDescription, EmSize = 12, DesiredHeight = 16, Foreground = new ImmutableSolidColorBrush(Color.FromRgb(110, 110, 110)) }
                        }
                    }
                }
            };

            return decorator;
        };

        return node;
    }

    private static WidgetGalleryNode CreateBorderVisualNode()
    {
        var node = new WidgetGalleryNode("Border Visual", "Simple outline for cards")
        {
            GeometryValue = new GeometryWidgetValue(WidgetSamplesFactory.CreateWarningGeometry(), Stretch.Uniform, new ImmutableSolidColorBrush(Color.FromArgb(0x20, 255, 213, 128)), null, 0),
        };

        node.LayoutFactory = () =>
        {
            var border = new BorderVisualWidget
            {
                DesiredWidth = 220,
                DesiredHeight = 120,
                CornerRadius = new CornerRadius(12),
                StrokeThickness = 2,
                Stroke = new ImmutableSolidColorBrush(Color.FromRgb(255, 213, 128)),
                Fill = new ImmutableSolidColorBrush(Color.FromArgb(0x0F, 255, 213, 128)),
            };

            return border;
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
