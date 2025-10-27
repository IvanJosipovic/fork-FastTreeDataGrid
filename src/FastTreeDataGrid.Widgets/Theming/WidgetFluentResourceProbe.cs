using System.Collections.Generic;

namespace FastTreeDataGrid.Control.Theming;

internal static class WidgetFluentResourceProbe
{
    private enum ResourceKind
    {
        Brush,
        Thickness,
        Geometry
    }

    private readonly record struct ResourceProbe(string Key, ResourceKind Kind, bool Optional = false);

    public static IReadOnlyList<string> Probe(WidgetFluentPalette.ResourceAccessor accessor)
    {
        var missing = new List<string>();

        // Buttons
        CheckBrush(accessor, missing, "ButtonBackground");
        CheckBrush(accessor, missing, "ButtonBorderBrush");
        CheckBrush(accessor, missing, "ButtonForeground");
        CheckBrush(accessor, missing, "AccentButtonBackground");
        CheckBrush(accessor, missing, "AccentButtonBorderBrush");
        CheckBrush(accessor, missing, "AccentButtonForeground");
        CheckBrush(accessor, missing, "SubtleButtonBackground", optional: true);
        CheckBrush(accessor, missing, "SubtleButtonBorderBrush", optional: true);
        CheckBrush(accessor, missing, "SubtleButtonForeground", optional: true);
        CheckBrush(accessor, missing, "DestructiveButtonBackground", optional: true);
        CheckBrush(accessor, missing, "DestructiveButtonBorderBrush", optional: true);
        CheckBrush(accessor, missing, "DestructiveButtonForeground", optional: true);
        CheckThickness(accessor, missing, "ButtonPadding");
        CheckThickness(accessor, missing, "ButtonBorderThemeThickness");

        // RepeatButton
        CheckBrush(accessor, missing, "RepeatButtonBackground");
        CheckBrush(accessor, missing, "RepeatButtonBackgroundPointerOver", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonBackgroundPressed", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonBackgroundDisabled", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonForeground");
        CheckBrush(accessor, missing, "RepeatButtonForegroundPointerOver", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonForegroundPressed", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonForegroundDisabled", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonBorderBrush");
        CheckBrush(accessor, missing, "RepeatButtonBorderBrushPointerOver", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonBorderBrushPressed", optional: true);
        CheckBrush(accessor, missing, "RepeatButtonBorderBrushDisabled", optional: true);

        // CheckBox
        CheckThickness(accessor, missing, "CheckBoxBorderThemeThickness");
        CheckGeometry(accessor, missing, "CheckMarkPathData");
        foreach (var state in new[] { "Unchecked", "Checked", "Indeterminate" })
        {
            CheckBrush(accessor, missing, $"CheckBoxBackground{state}");
            CheckBrush(accessor, missing, $"CheckBoxBorderBrush{state}");
            CheckBrush(accessor, missing, $"CheckBoxCheckBackgroundFill{state}");
            CheckBrush(accessor, missing, $"CheckBoxCheckBackgroundStroke{state}");
            CheckBrush(accessor, missing, $"CheckBoxCheckGlyphForeground{state}");
        }

        // RadioButton
        CheckBrush(accessor, missing, "RadioButtonBackground");
        CheckBrush(accessor, missing, "RadioButtonBorderBrush");
        CheckBrush(accessor, missing, "RadioButtonOuterEllipseFill");
        CheckBrush(accessor, missing, "RadioButtonOuterEllipseStroke");
        CheckBrush(accessor, missing, "RadioButtonOuterEllipseCheckedFill");
        CheckBrush(accessor, missing, "RadioButtonOuterEllipseCheckedStroke");
        CheckBrush(accessor, missing, "RadioButtonCheckGlyphFill");
        CheckBrush(accessor, missing, "RadioButtonCheckGlyphStroke");

        // ToggleSwitch
        CheckBrush(accessor, missing, "ToggleSwitchContainerBackground");
        foreach (var mode in new[] { "Off", "On" })
        {
            CheckBrush(accessor, missing, $"ToggleSwitchFill{mode}");
            CheckBrush(accessor, missing, $"ToggleSwitchStroke{mode}");
            CheckBrush(accessor, missing, $"ToggleSwitchKnobFill{mode}");
        }

        // Slider
        CheckBrush(accessor, missing, "SliderTrackFill");
        CheckBrush(accessor, missing, "SliderTrackValueFill");
        CheckBrush(accessor, missing, "SliderThumbBackground");
        CheckBrush(accessor, missing, "SliderThumbBorderBrush");
        CheckBrush(accessor, missing, "AccentControlBorderBrush");

        // ScrollBar
        CheckBrush(accessor, missing, "ScrollBarTrackFill");
        CheckBrush(accessor, missing, "ScrollBarTrackStroke");
        CheckBrush(accessor, missing, "ScrollBarThumbBackgroundColor");
        CheckBrush(accessor, missing, "ScrollBarThumbFillPointerOver", optional: true);
        CheckBrush(accessor, missing, "ScrollBarThumbFillPressed", optional: true);
        CheckBrush(accessor, missing, "ScrollBarThumbFillDisabled", optional: true);
        CheckDouble(accessor, missing, "ScrollBarSize");
        CheckDouble(accessor, missing, "ScrollBarTrackBorderThemeThickness", optional: true);

        // Progress
        CheckBrush(accessor, missing, "ProgressBarIndicatorForeground");
        CheckBrush(accessor, missing, "ProgressBarTrackFill");
        CheckBrush(accessor, missing, "ProgressBarTrackStroke");

        // Chart
        CheckBrush(accessor, missing, "ChartLineBrush");
        CheckBrush(accessor, missing, "ChartFillBrush");
        CheckBrush(accessor, missing, "ChartBaselineBrush");

        // Badge
        CheckBrush(accessor, missing, "SystemControlHighlightAccentBrush");
        CheckBrush(accessor, missing, "SystemControlForegroundChromeWhiteBrush");

        // Text / typography
        CheckBrush(accessor, missing, "TextControlForeground");
        CheckBrush(accessor, missing, "TextControlPlaceholderForeground");
        CheckBrush(accessor, missing, "TextControlSelectionHighlightColor");
        CheckBrush(accessor, missing, "TextControlForegroundFocused", optional: true);
        CheckBrush(accessor, missing, "TextControlForegroundDisabled", optional: true);

        // Spinner
        CheckGeometry(accessor, missing, "ButtonSpinnerIncreaseButtonIcon", optional: true);
        CheckGeometry(accessor, missing, "ButtonSpinnerDecreaseButtonIcon", optional: true);
        CheckBrush(accessor, missing, "TextControlButtonBackground");
        CheckBrush(accessor, missing, "TextControlButtonBorderBrush");
        CheckThickness(accessor, missing, "TextControlBorderThemeThickness");

        // Borders
        CheckBrush(accessor, missing, "TextControlBorderBrush");
        CheckBrush(accessor, missing, "TextControlBorderBrushFocused", optional: true);

        // Selection
        CheckBrush(accessor, missing, "SystemControlHighlightListAccentLowBrush");
        CheckBrush(accessor, missing, "SystemControlHighlightListAccentMediumBrush", optional: true);
        CheckBrush(accessor, missing, "SystemControlHighlightListAccentHighBrush", optional: true);
        CheckBrush(accessor, missing, "SystemControlForegroundBaseHighBrush");

        // Menu & flyout presenters
        CheckBrush(accessor, missing, "MenuFlyoutPresenterBackground");
        CheckBrush(accessor, missing, "MenuFlyoutPresenterBorderBrush");
        CheckThickness(accessor, missing, "MenuFlyoutPresenterBorderThemeThickness");
        CheckThickness(accessor, missing, "MenuFlyoutItemThemePadding");
        CheckBrush(accessor, missing, "MenuFlyoutItemBackground");
        CheckBrush(accessor, missing, "MenuFlyoutItemForeground");

        // Picker flyouts
        CheckBrush(accessor, missing, "DatePickerButtonBackground");
        CheckBrush(accessor, missing, "DatePickerButtonBorderBrush");
        CheckBrush(accessor, missing, "DatePickerButtonForeground");
        CheckBrush(accessor, missing, "TimePickerButtonBackground");
        CheckBrush(accessor, missing, "TimePickerButtonBorderBrush");
        CheckBrush(accessor, missing, "TimePickerButtonForeground");
        CheckBrush(accessor, missing, "DatePickerSpacerFill", optional: true);
        CheckBrush(accessor, missing, "TimePickerSpacerFill", optional: true);
        CheckBrush(accessor, missing, "DatePickerFlyoutPresenterBackground", optional: true);
        CheckBrush(accessor, missing, "DatePickerFlyoutPresenterBorderBrush", optional: true);
        CheckBrush(accessor, missing, "DatePickerFlyoutPresenterHighlightFill", optional: true);
        CheckBrush(accessor, missing, "TimePickerFlyoutPresenterBackground", optional: true);
        CheckBrush(accessor, missing, "TimePickerFlyoutPresenterBorderBrush", optional: true);
        CheckBrush(accessor, missing, "TimePickerFlyoutPresenterHighlightFill", optional: true);

        // Calendar
        CheckBrush(accessor, missing, "CalendarViewBackground");
        CheckBrush(accessor, missing, "CalendarViewBorderBrush");
        CheckBrush(accessor, missing, "CalendarViewSelectedBorderBrush");
        CheckBrush(accessor, missing, "CalendarViewSelectedHoverBorderBrush");
        CheckBrush(accessor, missing, "CalendarViewSelectedPressedBorderBrush");
        CheckBrush(accessor, missing, "CalendarViewCalendarItemForeground");
        CheckBrush(accessor, missing, "CalendarViewTodayForeground");
        CheckBrush(accessor, missing, "CalendarViewBlackoutForeground");
        CheckBrush(accessor, missing, "CalendarViewOutOfScopeForeground");

        return missing;
    }

    private static void CheckBrush(WidgetFluentPalette.ResourceAccessor accessor, IList<string> missing, string key, bool optional = false)
    {
        if (accessor.GetBrush(key) is null && !optional)
        {
            missing.Add(key);
        }
    }

    private static void CheckThickness(WidgetFluentPalette.ResourceAccessor accessor, IList<string> missing, string key, bool optional = false)
    {
        if (accessor.GetThickness(key) is null && !optional)
        {
            missing.Add(key);
        }
    }

    private static void CheckGeometry(WidgetFluentPalette.ResourceAccessor accessor, IList<string> missing, string key, bool optional = false)
    {
        if (accessor.GetGeometry(key) is null && !optional)
        {
            missing.Add(key);
        }
    }

    private static void CheckDouble(WidgetFluentPalette.ResourceAccessor accessor, IList<string> missing, string key, bool optional = false)
    {
        if (accessor.GetDouble(key) is null && !optional)
        {
            missing.Add(key);
        }
    }
}
