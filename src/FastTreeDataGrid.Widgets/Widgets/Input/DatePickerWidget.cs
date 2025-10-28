using System;
using System.Globalization;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public class DatePickerWidget : TemplatedWidget
{
    private BorderWidget? _rootBorder;
    private StackLayoutWidget? _root;
    private ButtonWidget? _toggleButton;
    private CalendarWidget? _calendar;

    private bool _isDropDownOpen;
    private DateTime? _selectedDate;
    private DateTime _displayDate = DateTime.Today;
    private DateTime? _minDate;
    private DateTime? _maxDate;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private string _format = "d";

    public event EventHandler<WidgetValueChangedEventArgs<DateTime?>>? SelectedDateChanged;
    public event EventHandler<WidgetValueChangedEventArgs<bool>>? DropDownOpenChanged;

    static DatePickerWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(DatePickerWidget),
                WidgetVisualState.Normal,
                (widget, theme) =>
                {
                    if (widget is DatePickerWidget picker)
                    {
                        picker.ApplyPickerPalette(theme.Palette);
                    }
                }));
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set => SetSelectedDate(value, true);
    }

    public DateTime? Minimum
    {
        get => _minDate;
        set
        {
            _minDate = value;
            RefreshCalendarRange();
        }
    }

    public DateTime? Maximum
    {
        get => _maxDate;
        set
        {
            _maxDate = value;
            RefreshCalendarRange();
        }
    }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? CultureInfo.CurrentCulture;
            RefreshText();
            if (_calendar is not null)
            {
                _calendar.Culture = _culture;
            }
        }
    }

    public string FormatString
    {
        get => _format;
        set
        {
            _format = string.IsNullOrWhiteSpace(value) ? "d" : value;
            RefreshText();
        }
    }

    public bool IsDropDownOpen
    {
        get => _isDropDownOpen;
        set => SetDropDownState(value, true);
    }

    protected override Widget? CreateDefaultTemplate()
    {
        _rootBorder = new BorderWidget
        {
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4)
        };

        _root = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 6
        };

        _toggleButton = new ButtonWidget
        {
            DesiredHeight = 32,
            DesiredWidth = double.NaN,
            Automation = { Name = "Open date picker" }
        };
        _toggleButton.Click += (_, __) => ToggleDropDown();

        _calendar = new CalendarWidget
        {
            DesiredHeight = 220,
            DesiredWidth = 240
        };
        _calendar.SelectedDateChanged += (_, args) =>
        {
            SetSelectedDate(args.NewValue, true);
            SetDropDownState(false, true);
        };

        _root.Children.Add(_toggleButton);
        _rootBorder.Child = _root;

        RefreshText();
        EnsureCalendar(false);

        return _rootBorder;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        var selected = _selectedDate;
        var min = _minDate;
        var max = _maxDate;
        var culture = _culture;
        var format = _format;
        var dropdown = _isDropDownOpen;
        var enabled = IsEnabled;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref selected, ref min, ref max, ref culture, ref format, ref dropdown, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref selected, ref min, ref max, ref culture, ref format, ref dropdown, ref enabled))
        {
            goto Apply;
        }

Apply:
        _minDate = min;
        _maxDate = max;
        Culture = culture ?? CultureInfo.CurrentCulture;
        FormatString = format ?? "d";
        IsEnabled = enabled;
        SetSelectedDate(selected, false);
        SetDropDownState(dropdown, false);
        RefreshCalendarRange();
    }

    private bool ApplyValue(object? value, ref DateTime? selected, ref DateTime? min, ref DateTime? max, ref CultureInfo? culture, ref string? format, ref bool dropdown, ref bool enabled)
    {
        switch (value)
        {
            case DatePickerWidgetValue picker:
                return ApplyPickerValue(picker, ref selected, ref min, ref max, ref culture, ref format, ref dropdown, ref enabled);
            case CalendarDatePickerWidgetValue calendarPicker:
                return ApplyPickerValue(new DatePickerWidgetValue(
                    calendarPicker.SelectedDate,
                    calendarPicker.Minimum,
                    calendarPicker.Maximum,
                    calendarPicker.Culture,
                    calendarPicker.FormatString,
                    calendarPicker.IsDropDownOpen,
                    calendarPicker.IsEnabled,
                    calendarPicker.Interaction),
                    ref selected,
                    ref min,
                    ref max,
                    ref culture,
                    ref format,
                    ref dropdown,
                    ref enabled);
            case DateTime dateTime:
                selected = dateTime;
                return true;
            case null:
                selected = null;
                return true;
            default:
                return false;
        }
    }

    private static bool ApplyPickerValue(
        DatePickerWidgetValue picker,
        ref DateTime? selected,
        ref DateTime? min,
        ref DateTime? max,
        ref CultureInfo? culture,
        ref string? format,
        ref bool dropdown,
        ref bool enabled)
    {
        selected = picker.SelectedDate ?? selected;
        min = picker.Minimum ?? min;
        max = picker.Maximum ?? max;
        culture = picker.Culture ?? culture;
        format = picker.FormatString ?? format;
        dropdown = picker.IsDropDownOpen ?? dropdown;
        enabled = picker.IsEnabled;
        if (picker.Interaction is { } interaction)
        {
            enabled = interaction.IsEnabled;
        }

        return true;
    }

    private void ToggleDropDown()
    {
        SetDropDownState(!IsDropDownOpen, true);
    }

    private void SetDropDownState(bool open, bool raise)
    {
        if (_isDropDownOpen == open)
        {
            return;
        }

        _isDropDownOpen = open;
        EnsureCalendar(_isDropDownOpen);

        if (raise)
        {
            DropDownOpenChanged?.Invoke(this, new WidgetValueChangedEventArgs<bool>(this, !open, open));
        }
    }

    private void EnsureCalendar(bool present)
    {
        if (_root is null || _calendar is null)
        {
            return;
        }

        var contains = _root.Children.Contains(_calendar);

        if (present && !contains)
        {
            _root.Children.Add(_calendar);
            RefreshCalendarRange();
        }
        else if (!present && contains)
        {
            _root.Children.Remove(_calendar);
        }
    }

    private void SetSelectedDate(DateTime? date, bool raise)
    {
        var oldValue = _selectedDate;
        if (date.HasValue)
        {
            date = Clamp(date.Value);
        }

        if (Nullable.Equals(oldValue, date))
        {
            RefreshText();
            return;
        }

        _selectedDate = date;
        RefreshText();
        if (_calendar is not null)
        {
            _calendar.SelectedDate = date;
        }

        if (raise)
        {
            SelectedDateChanged?.Invoke(this, new WidgetValueChangedEventArgs<DateTime?>(this, oldValue, _selectedDate));
        }
    }

    private void RefreshText()
    {
        var text = _selectedDate.HasValue
            ? _selectedDate.Value.ToString(_format, _culture)
            : "Select a date";

        _toggleButton?.SetText(text + " ▼");
    }

    private void RefreshCalendarRange()
    {
        if (_calendar is null)
        {
            return;
        }

        _calendar.Minimum = _minDate;
        _calendar.Maximum = _maxDate;
        if (_selectedDate.HasValue)
        {
            _calendar.SelectedDate = _selectedDate;
        }
    }

    private DateTime Clamp(DateTime value)
    {
        if (_minDate.HasValue && value < _minDate.Value)
        {
            value = _minDate.Value;
        }

        if (_maxDate.HasValue && value > _maxDate.Value)
        {
            value = _maxDate.Value;
        }

        return value;
    }

    private void ApplyPickerPalette(WidgetFluentPalette.PaletteData palette)
    {
        var picker = palette.Picker;

        if (_toggleButton is not null)
        {
            _toggleButton.Background = picker.ButtonBackground.Normal ?? _toggleButton.Background;
            _toggleButton.BorderBrush = picker.ButtonBorder.Normal ?? _toggleButton.BorderBrush;
        }

        if (_rootBorder is not null)
        {
            _rootBorder.BorderBrush = palette.Border.ControlBorder.Normal ?? _rootBorder.BorderBrush;
            _rootBorder.Background = palette.Flyout.Background ?? _rootBorder.Background;
        }
    }
}
