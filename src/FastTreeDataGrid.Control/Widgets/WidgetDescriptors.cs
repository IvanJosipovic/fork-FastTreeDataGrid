using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace FastTreeDataGrid.Control.Widgets;

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

public sealed record ButtonWidgetValue(
    string Text,
    bool IsPrimary = false,
    bool IsPressed = false,
    bool IsEnabled = true,
    ImmutableSolidColorBrush? Background = null,
    ImmutableSolidColorBrush? BorderBrush = null,
    double CornerRadius = 4,
    double? FontSize = null);

public sealed record CheckBoxWidgetValue(
    bool? IsChecked,
    bool IsEnabled = true);

public sealed record ProgressWidgetValue(
    double Progress,
    bool IsIndeterminate = false,
    ImmutableSolidColorBrush? Foreground = null,
    ImmutableSolidColorBrush? Background = null);

public sealed record CustomDrawWidgetValue(Action<DrawingContext, Rect> Draw);
