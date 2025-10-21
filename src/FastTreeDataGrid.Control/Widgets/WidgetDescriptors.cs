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
    double? CornerRadius = null,
    double? FontSize = null);

public sealed record CheckBoxWidgetValue(
    bool? IsChecked,
    bool IsEnabled = true);

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
    ImmutableSolidColorBrush? ThumbBrush = null);

public sealed record RadioButtonWidgetValue(
    bool IsChecked,
    bool IsEnabled = true,
    string? Group = null);

public sealed record SliderWidgetValue(
    double Value,
    bool IsEnabled = true,
    double Minimum = 0,
    double Maximum = 1,
    ImmutableSolidColorBrush? TrackBrush = null,
    ImmutableSolidColorBrush? FillBrush = null,
    ImmutableSolidColorBrush? ThumbBrush = null);

public sealed record BadgeWidgetValue(
    string Text,
    ImmutableSolidColorBrush? Background = null,
    ImmutableSolidColorBrush? Foreground = null,
    double? CornerRadius = null,
    double? Padding = null);

public sealed record CustomDrawWidgetValue(Action<DrawingContext, Rect> Draw);
