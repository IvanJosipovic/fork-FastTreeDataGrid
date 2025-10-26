using System;
using System.Globalization;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CalendarWidget : TemplatedWidget
{
    private readonly ButtonWidget[] _dayButtons = new ButtonWidget[42];
    private readonly DateTime[] _buttonDates = new DateTime[42];
    private readonly FormattedTextWidget[] _weekdayHeaders = new FormattedTextWidget[7];

    private BorderWidget? _rootBorder;
    private StackLayoutWidget? _rootPanel;
    private StackLayoutWidget? _headerPanel;
    private ButtonWidget? _previousButton;
    private ButtonWidget? _nextButton;
    private FormattedTextWidget? _titleText;
    private GridLayoutWidget? _dayGrid;

    private DateTime _displayDate = DateTime.Today;
    private DateTime? _selectedDate;
    private DateTime? _minDate;
    private DateTime? _maxDate;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private DayOfWeek _firstDayOfWeek;

    public event EventHandler<WidgetValueChangedEventArgs<DateTime?>>? SelectedDateChanged;
    public event EventHandler<WidgetValueChangedEventArgs<DateTime>>? DisplayDateChanged;

    static CalendarWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                string.Empty,
                new WidgetStyleRule(
                    typeof(CalendarWidget),
                    state,
                    (widget, theme) =>
                    {
                        if (widget is CalendarWidget calendar)
                        {
                            calendar.ApplyPalette(theme.Palette);
                            calendar.RefreshCalendar();
                        }
                    }));
        }
    }

    public CalendarWidget()
    {
        UpdateFirstDayOfWeek();
    }

    public DateTime DisplayDate
    {
        get => _displayDate;
        set => SetDisplayDate(value, true);
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
            ClampState();
        }
    }

    public DateTime? Maximum
    {
        get => _maxDate;
        set
        {
            _maxDate = value;
            ClampState();
        }
    }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? CultureInfo.CurrentCulture;
            UpdateFirstDayOfWeek();
            RefreshCalendar();
        }
    }

    protected override Widget? CreateDefaultTemplate()
    {
        _rootBorder = new BorderWidget
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8)
        };

        _rootPanel = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        _headerPanel = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        _previousButton = new ButtonWidget
        {
            DesiredWidth = 28,
            DesiredHeight = 28,
            Automation = { Name = "Previous month" }
        };
        _previousButton.SetText("◀");
        _previousButton.Click += (_, __) => ChangeMonth(-1);

        _nextButton = new ButtonWidget
        {
            DesiredWidth = 28,
            DesiredHeight = 28,
            Automation = { Name = "Next month" }
        };
        _nextButton.SetText("▶");
        _nextButton.Click += (_, __) => ChangeMonth(1);

        _titleText = new FormattedTextWidget
        {
            DesiredHeight = 28,
            DesiredWidth = double.NaN,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(32, 32, 32))
        };

        var spacer = new SurfaceWidget
        {
            DesiredWidth = double.NaN,
            DesiredHeight = 1
        };

        _headerPanel.Children.Add(_previousButton);
        _headerPanel.Children.Add(_titleText);
        _headerPanel.Children.Add(spacer);
        _headerPanel.Children.Add(_nextButton);

        _dayGrid = new GridLayoutWidget
        {
            Columns = 7,
            Rows = 7,
            Spacing = 4,
            DesiredHeight = double.NaN,
            DesiredWidth = double.NaN
        };

        // Weekday headers
        for (var i = 0; i < 7; i++)
        {
            var header = new FormattedTextWidget
            {
                DesiredHeight = 24,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96)),
                CornerRadius = new CornerRadius(4)
            };
            _weekdayHeaders[i] = header;
            _dayGrid.Children.Add(header);
        }

        // Day buttons
        for (var i = 0; i < _dayButtons.Length; i++)
        {
            var button = new ButtonWidget
            {
                DesiredHeight = 32,
                DesiredWidth = 36,
                Automation = { Name = "Calendar day" }
            };

            var index = i;
            button.Click += (_, __) => OnDayButtonClicked(index);
            _dayButtons[i] = button;
            _dayGrid.Children.Add(button);
        }

        _rootPanel.Children.Add(_headerPanel);
        _rootPanel.Children.Add(_dayGrid);
        _rootBorder.Child = _rootPanel;

        return _rootBorder;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        RefreshCalendar();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        var display = _displayDate;
        var selected = _selectedDate;
        var min = _minDate;
        var max = _maxDate;
        var culture = _culture;
        var enabled = IsEnabled;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref display, ref selected, ref min, ref max, ref culture, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref display, ref selected, ref min, ref max, ref culture, ref enabled))
        {
            goto Apply;
        }

Apply:
        _culture = culture ?? CultureInfo.CurrentCulture;
        UpdateFirstDayOfWeek();
        _minDate = min;
        _maxDate = max;
        IsEnabled = enabled;
        SetDisplayDate(display, false);
        SetSelectedDate(selected, false);
        RefreshCalendar();
    }

    private bool ApplyValue(object? value, ref DateTime display, ref DateTime? selected, ref DateTime? min, ref DateTime? max, ref CultureInfo? culture, ref bool enabled)
    {
        switch (value)
        {
            case CalendarWidgetValue calendarValue:
                display = calendarValue.DisplayDate;
                selected = calendarValue.SelectedDate;
                min = calendarValue.Minimum ?? min;
                max = calendarValue.Maximum ?? max;
                culture = calendarValue.Culture ?? culture;
                enabled = calendarValue.IsEnabled;
                if (calendarValue.Interaction is { } interaction)
                {
                    enabled = interaction.IsEnabled;
                }
                return true;
            case DateTime dateTime:
                selected = dateTime;
                display = dateTime;
                return true;
            case null:
                selected = null;
                return true;
            default:
                return false;
        }
    }

    private void ChangeMonth(int offset)
    {
        var target = _displayDate.AddMonths(offset);
        SetDisplayDate(target, true);
    }

    private void SetDisplayDate(DateTime date, bool raise)
    {
        var normalized = new DateTime(date.Year, date.Month, 1);
        if (_minDate.HasValue && normalized < new DateTime(_minDate.Value.Year, _minDate.Value.Month, 1))
        {
            normalized = new DateTime(_minDate.Value.Year, _minDate.Value.Month, 1);
        }

        if (_maxDate.HasValue && normalized > new DateTime(_maxDate.Value.Year, _maxDate.Value.Month, 1))
        {
            normalized = new DateTime(_maxDate.Value.Year, _maxDate.Value.Month, 1);
        }

        if (normalized == _displayDate)
        {
            RefreshCalendar();
            return;
        }

        var previous = _displayDate;
        _displayDate = normalized;
        RefreshCalendar();

        if (raise)
        {
            DisplayDateChanged?.Invoke(this, new WidgetValueChangedEventArgs<DateTime>(this, previous, _displayDate));
        }
    }

    private void SetSelectedDate(DateTime? date, bool raise)
    {
        var clamped = date;
        if (clamped.HasValue)
        {
            clamped = ClampToRange(clamped.Value);
        }

        if (Nullable.Equals(clamped, _selectedDate))
        {
            RefreshCalendar();
            return;
        }

        var previous = _selectedDate;
        _selectedDate = clamped;
        if (clamped.HasValue)
        {
            SetDisplayDate(clamped.Value, false);
        }

        RefreshCalendar();

        if (raise)
        {
            SelectedDateChanged?.Invoke(this, new WidgetValueChangedEventArgs<DateTime?>(this, previous, clamped));
        }
    }

    private void ClampState()
    {
        if (_selectedDate.HasValue)
        {
            SetSelectedDate(_selectedDate, false);
        }

        SetDisplayDate(_displayDate, false);
    }

    private DateTime ClampToRange(DateTime value)
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

    private void OnDayButtonClicked(int index)
    {
        if (index < 0 || index >= _buttonDates.Length)
        {
            return;
        }

        var date = _buttonDates[index];
        if (!IsDateEnabled(date))
        {
            return;
        }

        SetSelectedDate(date, true);
    }

    private bool IsDateEnabled(DateTime date)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (_minDate.HasValue && date < _minDate.Value)
        {
            return false;
        }

        if (_maxDate.HasValue && date > _maxDate.Value)
        {
            return false;
        }

        return true;
    }

    private void RefreshCalendar()
    {
        if (_dayGrid is null)
        {
            return;
        }

        var culture = _culture ?? CultureInfo.CurrentCulture;
        var monthName = _displayDate.ToString("MMMM yyyy", culture);
        _titleText?.SetText(monthName);

        var format = culture.DateTimeFormat;
        for (var i = 0; i < 7; i++)
        {
            var day = (int)(_firstDayOfWeek + i) % 7;
            var name = format.AbbreviatedDayNames[day];
            _weekdayHeaders[i]?.SetText(name);
        }

        var firstOfMonth = new DateTime(_displayDate.Year, _displayDate.Month, 1);
        var daysOffset = ((int)firstOfMonth.DayOfWeek - (int)_firstDayOfWeek + 7) % 7;
        var startDate = firstOfMonth.AddDays(-daysOffset);

        for (var i = 0; i < _dayButtons.Length; i++)
        {
            var date = startDate.AddDays(i);
            _buttonDates[i] = date;

            var button = _dayButtons[i];
            if (button is null)
            {
                continue;
            }

            button.SetText(date.Day.ToString(culture));

            var isCurrentMonth = date.Month == _displayDate.Month && date.Year == _displayDate.Year;
            var isToday = date.Date == DateTime.Today;
            var isSelected = _selectedDate.HasValue && date.Date == _selectedDate.Value.Date;
            var enabled = IsDateEnabled(date);
            button.IsEnabled = enabled;

            var palette = WidgetFluentPalette.Current.Calendar;
            var foreground = palette.SelectedForeground ?? new ImmutableSolidColorBrush(Color.FromRgb(32, 32, 32));
            var normalForeground = new ImmutableSolidColorBrush(Color.FromRgb(45, 45, 45));

            if (!isCurrentMonth)
            {
                foreground = palette.OutOfScopeForeground ?? new ImmutableSolidColorBrush(Color.FromRgb(120, 120, 120));
            }
            else if (isToday)
            {
                foreground = palette.TodayForeground ?? new ImmutableSolidColorBrush(Color.FromRgb(26, 95, 180));
            }
            else if (isSelected)
            {
                foreground = palette.SelectedForeground ?? foreground;
            }
            else if (!enabled)
            {
                foreground = palette.BlackoutForeground ?? new ImmutableSolidColorBrush(Color.FromRgb(160, 160, 160));
            }
            else
            {
                foreground = normalForeground;
            }

            button.Foreground = foreground;

            if (isSelected)
            {
                button.BorderBrush = palette.SelectedBorder ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
                button.Background = palette.Background ?? new ImmutableSolidColorBrush(Color.FromRgb(229, 243, 255));
            }
            else
            {
                button.BorderBrush = palette.Border ?? new ImmutableSolidColorBrush(Color.FromRgb(210, 210, 210));
                button.Background = palette.Background ?? new ImmutableSolidColorBrush(Color.FromRgb(250, 250, 250));
            }
        }

        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        if (_previousButton is not null)
        {
            if (_minDate.HasValue)
            {
                var minMonth = new DateTime(_minDate.Value.Year, _minDate.Value.Month, 1);
                _previousButton.IsEnabled = IsEnabled && _displayDate > minMonth;
            }
            else
            {
                _previousButton.IsEnabled = IsEnabled;
            }
        }

        if (_nextButton is not null)
        {
            if (_maxDate.HasValue)
            {
                var maxMonth = new DateTime(_maxDate.Value.Year, _maxDate.Value.Month, 1);
                _nextButton.IsEnabled = IsEnabled && _displayDate < maxMonth;
            }
            else
            {
                _nextButton.IsEnabled = IsEnabled;
            }
        }
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        var calendar = palette.Calendar;

        if (_rootBorder is not null)
        {
            _rootBorder.Background = calendar.Background ?? _rootBorder.Background;
            _rootBorder.BorderBrush = calendar.Border ?? _rootBorder.BorderBrush;
            _rootBorder.BorderThickness = new Thickness(calendar.Border is not null ? 1 : 0);
        }
    }

    private void UpdateFirstDayOfWeek()
    {
        _firstDayOfWeek = _culture.DateTimeFormat.FirstDayOfWeek;
    }
}
