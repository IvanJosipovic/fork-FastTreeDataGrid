using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public enum ButtonWidgetVariant
{
    Standard,
    Accent,
    Subtle,
    Destructive
}

public sealed record WidgetCommandSettings(
    bool IsEnabled = true,
    ICommand? Command = null,
    object? CommandParameter = null);

public sealed record WidgetTypography(
    double? FontSize = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null,
    FontFamily? FontFamily = null,
    FontStretch? FontStretch = null);

public sealed record IconWidgetValue(
    Geometry Geometry,
    ImmutableSolidColorBrush? Fill = null,
    Pen? Stroke = null,
    double Padding = 4);

public sealed record GeometryWidgetValue(
    Geometry Geometry,
    Stretch Stretch = Stretch.Uniform,
    ImmutableSolidColorBrush? Fill = null,
    Pen? Stroke = null,
    double Padding = 4);

public record ButtonWidgetValue(
    string Text,
    bool IsPrimary = false,
    bool IsPressed = false,
    bool IsEnabled = true,
    ImmutableSolidColorBrush? Background = null,
    ImmutableSolidColorBrush? BorderBrush = null,
    double? CornerRadius = null,
    double? FontSize = null,
    ICommand? Command = null,
    object? CommandParameter = null,
    WidgetCommandSettings? CommandSettings = null,
    WidgetTypography? Typography = null,
    ButtonWidgetVariant? Variant = null,
    WidgetAutomationSettings? Automation = null);

public sealed record ButtonSpinnerWidgetValue(
    Widget? Content = null,
    IWidgetTemplate? ContentTemplate = null,
    Func<Widget?>? ContentFactory = null,
    bool? ShowSpinner = null,
    ButtonSpinnerLocation? SpinnerLocation = null,
    WidgetCommandSettings? IncreaseCommand = null,
    WidgetCommandSettings? DecreaseCommand = null,
    WidgetTypography? Typography = null,
    WidgetValidSpinDirections? ValidSpinDirections = null);

public sealed record SpinnerWidgetValue(
    WidgetValidSpinDirections? ValidSpinDirections = null);

public record DropDownButtonWidgetValue : ButtonWidgetValue
{
    public DropDownButtonWidgetValue(
        string Text,
        bool IsPrimary = false,
        bool IsPressed = false,
        bool IsEnabled = true,
        ImmutableSolidColorBrush? Background = null,
        ImmutableSolidColorBrush? BorderBrush = null,
        double? CornerRadius = null,
        double? FontSize = null,
        ICommand? Command = null,
        object? CommandParameter = null,
        WidgetCommandSettings? CommandSettings = null,
        WidgetTypography? Typography = null,
        ButtonWidgetVariant? Variant = null,
        WidgetAutomationSettings? Automation = null)
        : base(Text, IsPrimary, IsPressed, IsEnabled, Background, BorderBrush, CornerRadius, FontSize, Command, CommandParameter, CommandSettings, Typography, Variant, Automation)
    {
    }

    public Widget? DropDownContent { get; init; }

    public IWidgetTemplate? DropDownContentTemplate { get; init; }

    public Func<Widget?>? DropDownContentFactory { get; init; }

    public bool ExecutePrimaryCommand { get; init; }
}

public record SplitButtonWidgetValue(
    ButtonWidgetValue PrimaryButton,
    DropDownButtonWidgetValue DropDownButton,
    WidgetTypography? Typography = null);

public sealed record ToggleSplitButtonWidgetValue(
    ButtonWidgetValue PrimaryButton,
    DropDownButtonWidgetValue DropDownButton,
    bool IsChecked,
    WidgetTypography? Typography = null)
    : SplitButtonWidgetValue(PrimaryButton, DropDownButton, Typography);

public sealed record CheckBoxWidgetValue(
    bool? IsChecked,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record ProgressWidgetValue(
    double Progress,
    bool IsIndeterminate = false,
    ImmutableSolidColorBrush? Foreground = null,
    ImmutableSolidColorBrush? Background = null);

public sealed record ToggleSwitchWidgetValue(
    bool IsOn,
    bool IsEnabled = true,
    ImmutableSolidColorBrush? OnBrush = null,
    ImmutableSolidColorBrush? OffBrush = null,
    ImmutableSolidColorBrush? ThumbBrush = null,
    WidgetCommandSettings? Interaction = null);

public sealed record RadioButtonWidgetValue(
    bool IsChecked,
    bool IsEnabled = true,
    string? Group = null,
    WidgetCommandSettings? Interaction = null);

public sealed record SliderWidgetValue(
    double Value,
    bool IsEnabled = true,
    double Minimum = 0,
    double Maximum = 1,
    ImmutableSolidColorBrush? TrackBrush = null,
    ImmutableSolidColorBrush? FillBrush = null,
    ImmutableSolidColorBrush? ThumbBrush = null,
    WidgetCommandSettings? Interaction = null);

public sealed record NumericUpDownWidgetValue(
    double Value,
    double Minimum = 0,
    double Maximum = 100,
    double Increment = 1,
    int DecimalPlaces = 0,
    string? FormatString = null,
    CultureInfo? Culture = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record CalendarWidgetValue(
    DateTime DisplayDate,
    DateTime? SelectedDate = null,
    DateTime? Minimum = null,
    DateTime? Maximum = null,
    CultureInfo? Culture = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record DatePickerWidgetValue(
    DateTime? SelectedDate,
    DateTime? Minimum = null,
    DateTime? Maximum = null,
    CultureInfo? Culture = null,
    string? FormatString = null,
    bool? IsDropDownOpen = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record CalendarDatePickerWidgetValue(
    DateTime? SelectedDate,
    DateTime? Minimum = null,
    DateTime? Maximum = null,
    CultureInfo? Culture = null,
    string? FormatString = null,
    bool? IsDropDownOpen = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record TimePickerWidgetValue(
    TimeSpan? Time,
    bool Is24Hour = true,
    TimeSpan? Minimum = null,
    TimeSpan? Maximum = null,
    CultureInfo? Culture = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record ItemsControlWidgetValue(
    IEnumerable? ItemsSource = null,
    Func<object?, IEnumerable?>? ChildrenSelector = null,
    Func<object?, string?>? KeySelector = null,
    IWidgetTemplate? ItemTemplate = null,
    Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ItemFactory = null,
    double? ItemExtent = null,
    double? CrossAxisItemLength = null,
    int? BufferItemCount = null);

public sealed record ListBoxWidgetValue(
    IEnumerable? ItemsSource = null,
    object? SelectedItem = null,
    int? SelectedIndex = null,
    Func<object?, IEnumerable?>? ChildrenSelector = null,
    Func<object?, string?>? KeySelector = null,
    IWidgetTemplate? ItemTemplate = null,
    Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ItemFactory = null,
    double? ItemExtent = null,
    bool SelectedItemSet = false);

public sealed record TreeViewWidgetValue(
    IEnumerable? ItemsSource = null,
    object? SelectedItem = null,
    int? SelectedIndex = null,
    Func<object?, IEnumerable?>? ChildrenSelector = null,
    Func<object?, string?>? KeySelector = null,
    IWidgetTemplate? ItemTemplate = null,
    Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ItemFactory = null,
    double? ItemExtent = null,
    double? IndentWidth = null,
    double? GlyphSize = null,
    bool SelectedItemSet = false);

public sealed record AutoCompleteBoxWidgetValue(
    string? Text = null,
    IReadOnlyList<string>? Suggestions = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record ComboBoxWidgetValue(
    IEnumerable<object?>? Items = null,
    Func<IEnumerable<object?>>? ItemsProvider = null,
    object? SelectedItem = null,
    string? DisplayMemberPath = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record MenuItemWidgetValue(
    string Header,
    string? GestureText = null,
    Widget? Icon = null,
    bool IsEnabled = true,
    bool IsSeparator = false,
    WidgetCommandSettings? Command = null,
    WidgetCommandSettings? Interaction = null,
    MenuWidgetValue? SubMenu = null);

public sealed record MenuWidgetValue(
    IEnumerable? ItemsSource = null,
    WidgetCommandSettings? Interaction = null);

public sealed record MenuBarItemWidgetValue(
    string Header,
    MenuWidgetValue Menu,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record MenuBarWidgetValue(
    IEnumerable<MenuBarItemWidgetValue>? Items = null,
    WidgetCommandSettings? Interaction = null);

public sealed record ContextMenuWidgetValue(
    IEnumerable? ItemsSource = null,
    bool IsOpen = false,
    Rect? Anchor = null,
    WidgetCommandSettings? Interaction = null);

public sealed record TabControlWidgetValue(
    IEnumerable? Items = null,
    object? SelectedItem = null,
    int? SelectedIndex = null,
    IWidgetTemplate? HeaderTemplate = null,
    IWidgetTemplate? ContentTemplate = null,
    Func<IFastTreeDataGridValueProvider?, object?, Widget?>? HeaderFactory = null,
    Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ContentFactory = null,
    bool SelectedItemSet = false,
    WidgetCommandSettings? Interaction = null);

public sealed record ScrollBarWidgetValue(
    double Value,
    double Minimum = 0,
    double Maximum = 1,
    double ViewportSize = 0,
    Orientation Orientation = Orientation.Vertical,
    double? SmallChange = null,
    double? LargeChange = null,
    ImmutableSolidColorBrush? TrackBrush = null,
    ImmutableSolidColorBrush? TrackBorderBrush = null,
    ImmutableSolidColorBrush? ThumbBrush = null,
    ImmutableSolidColorBrush? ThumbBorderBrush = null,
    bool IsEnabled = true,
    WidgetCommandSettings? Interaction = null);

public sealed record BadgeWidgetValue(
    string Text,
    ImmutableSolidColorBrush? Background = null,
    ImmutableSolidColorBrush? Foreground = null,
    double? CornerRadius = null,
    double? Padding = null);

public sealed record CustomDrawWidgetValue(Action<DrawingContext, Rect> Draw);

public sealed record ChartSeriesValue(
    IReadOnlyList<double> Points,
    ImmutableSolidColorBrush? Stroke = null,
    ImmutableSolidColorBrush? Fill = null,
    double StrokeThickness = 1.5,
    bool FillToBaseline = false);

public sealed record ChartWidgetValue(
    IReadOnlyList<ChartSeriesValue> Series,
    double? Minimum = null,
    double? Maximum = null,
    double? Baseline = null,
    bool ShowBaseline = false,
    ImmutableSolidColorBrush? BaselineBrush = null,
    double BaselineThickness = 1);

public sealed record TransitioningContentWidgetValue(
    Widget? Content = null,
    IWidgetTemplate? ContentTemplate = null,
    Func<Widget?>? ContentFactory = null,
    WidgetTransitionDescriptor? Transition = null);

public sealed record SelectableTextWidgetValue(
    string Text,
    int? SelectionStart = null,
    int? SelectionLength = null);

public sealed record DocumentTextWidgetValue(IReadOnlyList<DocumentTextSpan> Spans);

public sealed record TextInputWidgetValue(
    string Text,
    string? Placeholder = null,
    bool IsReadOnly = false,
    bool? IsEnabled = null,
    int? CaretIndex = null,
    int? SelectionStart = null,
    int? SelectionLength = null);

public sealed record ImageWidgetValue(
    IImage? Source,
    Stretch Stretch = Stretch.Uniform,
    StretchDirection StretchDirection = StretchDirection.Both,
    double Padding = 0);

public sealed record IconElementWidgetValue(
    Geometry? Geometry = null,
    IImage? Image = null,
    ImmutableSolidColorBrush? Foreground = null,
    ImmutableSolidColorBrush? Background = null,
    Pen? Stroke = null,
    double Padding = 4,
    string? PathData = null,
    Stretch Stretch = Stretch.Uniform,
    StretchDirection StretchDirection = StretchDirection.Both);

public sealed record PathIconWidgetValue(
    string Data,
    ImmutableSolidColorBrush? Foreground = null,
    Pen? Stroke = null,
    double Padding = 4);

public abstract record ShapeWidgetValue(
    ImmutableSolidColorBrush? Fill = null,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null);

public sealed record LineShapeWidgetValue(
    Point StartPoint,
    Point EndPoint,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(null, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record RectangleShapeWidgetValue(
    double? RadiusX = null,
    double? RadiusY = null,
    ImmutableSolidColorBrush? Fill = null,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(Fill, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record EllipseShapeWidgetValue(
    ImmutableSolidColorBrush? Fill = null,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(Fill, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record ArcShapeWidgetValue(
    double StartAngle,
    double SweepAngle,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(null, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record PolygonShapeWidgetValue(
    IReadOnlyList<Point>? Points,
    ImmutableSolidColorBrush? Fill = null,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(Fill, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record PolylineShapeWidgetValue(
    IReadOnlyList<Point>? Points,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(null, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record PathShapeWidgetValue(
    Geometry? Data,
    string? DataString = null,
    ImmutableSolidColorBrush? Fill = null,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(Fill, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);

public sealed record SectorShapeWidgetValue(
    double StartAngle,
    double SweepAngle,
    ImmutableSolidColorBrush? Fill = null,
    ImmutableSolidColorBrush? Stroke = null,
    double? StrokeThickness = null,
    IReadOnlyList<double>? StrokeDashArray = null,
    double? StrokeDashOffset = null,
    PenLineCap? StrokeLineCap = null,
    PenLineJoin? StrokeLineJoin = null,
    double? StrokeMiterLimit = null,
    Stretch? Stretch = null)
    : ShapeWidgetValue(Fill, Stroke, StrokeThickness, StrokeDashArray, StrokeDashOffset, StrokeLineCap, StrokeLineJoin, StrokeMiterLimit, Stretch);
