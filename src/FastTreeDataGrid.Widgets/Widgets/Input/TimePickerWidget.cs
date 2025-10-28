using System;
using System.Globalization;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class TimePickerWidget : TemplatedWidget
{
    private StackLayoutWidget? _root;
    private NumericUpDownWidget? _hourPicker;
    private NumericUpDownWidget? _minutePicker;
    private NumericUpDownWidget? _secondPicker;
    private FormattedTextWidget? _separator1;
    private FormattedTextWidget? _separator2;
    private bool _showSeconds;
    private bool _is24Hour = true;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private TimeSpan? _minTime;
    private TimeSpan? _maxTime;
    private TimeSpan? _time;

    public event EventHandler<WidgetValueChangedEventArgs<TimeSpan?>>? TimeChanged;

    static TimePickerWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(TimePickerWidget),
                WidgetVisualState.Normal,
                (widget, theme) =>
                {
                    if (widget is TimePickerWidget picker)
                    {
                        picker.ApplyPalette(theme.Palette);
                    }
                }));
    }

    public bool Is24Hour
    {
        get => _is24Hour;
        set
        {
            if (_is24Hour == value)
            {
                return;
            }

            _is24Hour = value;
            ConfigureHourRange();
            RefreshText();
        }
    }

    public bool ShowSeconds
    {
        get => _showSeconds;
        set
        {
            if (_showSeconds == value)
            {
                return;
            }

            _showSeconds = value;
            EnsureSecondsPresence();
        }
    }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? CultureInfo.CurrentCulture;
            RefreshText();
        }
    }

    public TimeSpan? Minimum
    {
        get => _minTime;
        set
        {
            _minTime = value;
            ClampTime();
        }
    }

    public TimeSpan? Maximum
    {
        get => _maxTime;
        set
        {
            _maxTime = value;
            ClampTime();
        }
    }

    public TimeSpan? Time
    {
        get => _time;
        set => SetTime(value, true);
    }

    protected override Widget? CreateDefaultTemplate()
    {
        _root = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        _hourPicker = CreateNumeric(0, 23, 0);
        _minutePicker = CreateNumeric(0, 59, 0);

        _hourPicker.ValueChanged += (_, args) => UpdateFromPickers();
        _minutePicker.ValueChanged += (_, args) => UpdateFromPickers();

        _separator1 = CreateSeparator();

        _root.Children.Add(_hourPicker);
        _root.Children.Add(_separator1);
        _root.Children.Add(_minutePicker);

        EnsureSecondsPresence();
        RefreshText();
        ApplyPalette(WidgetFluentPalette.Current);

        return _root;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        TimeSpan? time = _time;
        var is24 = _is24Hour;
        var showSeconds = _showSeconds;
        var min = _minTime;
        var max = _maxTime;
        var culture = _culture;
        var enabled = IsEnabled;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref time, ref is24, ref showSeconds, ref min, ref max, ref culture, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref time, ref is24, ref showSeconds, ref min, ref max, ref culture, ref enabled))
        {
            goto Apply;
        }

Apply:
        _is24Hour = is24;
        _showSeconds = showSeconds;
        _minTime = min;
        _maxTime = max;
        Culture = culture ?? CultureInfo.CurrentCulture;
        IsEnabled = enabled;
        EnsureSecondsPresence();
        ConfigureHourRange();
        SetTime(time, false);
    }

    private bool ApplyValue(object? source, ref TimeSpan? time, ref bool is24, ref bool showSeconds, ref TimeSpan? min, ref TimeSpan? max, ref CultureInfo? culture, ref bool enabled)
    {
        switch (source)
        {
            case TimePickerWidgetValue value:
                time = value.Time ?? time;
                is24 = value.Is24Hour;
                min = value.Minimum ?? min;
                max = value.Maximum ?? max;
                culture = value.Culture ?? culture;
                enabled = value.IsEnabled;
                if (value.Interaction is { } interaction)
                {
                    enabled = interaction.IsEnabled;
                }
                showSeconds = value.Time?.Seconds > 0 || _showSeconds;
                return true;
            case TimeSpan span:
                time = span;
                return true;
            default:
                return false;
        }
    }

    private NumericUpDownWidget CreateNumeric(double min, double max, double value)
    {
        var numeric = new NumericUpDownWidget
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            Increment = 1,
            DecimalPlaces = 0,
            DesiredWidth = 60
        };

        numeric.ValueChanged += (_, __) => UpdateFromPickers();
        return numeric;
    }

    private FormattedTextWidget CreateSeparator()
    {
        var separator = new FormattedTextWidget
        {
            DesiredHeight = 32,
            DesiredWidth = 10,
            Foreground = new ImmutableSolidColorBrush(Color.FromRgb(80, 80, 80)),
        };
        separator.SetText(":");
        return separator;
    }

    private void EnsureSecondsPresence()
    {
        if (_root is null)
        {
            return;
        }

        if (_showSeconds)
        {
            if (_secondPicker is null)
            {
                _separator2 = CreateSeparator();
                _secondPicker = CreateNumeric(0, 59, 0);
                if (!_root.Children.Contains(_secondPicker))
                {
                    _root.Children.Add(_separator2);
                    _root.Children.Add(_secondPicker);
                }
            }
        }
        else
        {
            if (_secondPicker is not null)
            {
                _root.Children.Remove(_secondPicker);
                _root.Children.Remove(_separator2!);
                _secondPicker = null;
                _separator2 = null;
            }
        }
    }

    private void ConfigureHourRange()
    {
        if (_hourPicker is null)
        {
            return;
        }

        if (_is24Hour)
        {
            _hourPicker.Minimum = 0;
            _hourPicker.Maximum = 23;
        }
        else
        {
            _hourPicker.Minimum = 1;
            _hourPicker.Maximum = 12;
        }
    }

    private void SetTime(TimeSpan? time, bool raise)
    {
        var clamped = Clamp(time);
        var old = _time;
        _time = clamped;

        if (_hourPicker is not null && clamped.HasValue)
        {
            var hours = clamped.Value.Hours;
            if (!_is24Hour)
            {
                hours = hours % 12;
                if (hours == 0)
                {
                    hours = 12;
                }
            }
            _hourPicker.Value = hours;
        }

        if (_minutePicker is not null && clamped.HasValue)
        {
            _minutePicker.Value = clamped.Value.Minutes;
        }

        if (_secondPicker is not null && clamped.HasValue)
        {
            _secondPicker.Value = clamped.Value.Seconds;
        }

        RefreshText();

        if (raise && !Nullable.Equals(old, clamped))
        {
            TimeChanged?.Invoke(this, new WidgetValueChangedEventArgs<TimeSpan?>(this, old, clamped));
        }
    }

    private void UpdateFromPickers()
    {
        if (_hourPicker is null || _minutePicker is null)
        {
            return;
        }

        var hour = (int)_hourPicker.Value;
        if (!_is24Hour)
        {
            var hasExisting = _time.HasValue ? _time.Value.Hours >= 12 : false;
            if (hour >= 12)
            {
                hour %= 12;
            }
            if (hasExisting)
            {
                hour += 12;
            }
        }

        var minute = (int)_minutePicker.Value;
        var second = _secondPicker is null ? 0 : (int)_secondPicker.Value;

        var newTime = new TimeSpan(hour, minute, second);
        SetTime(newTime, true);
    }

    private TimeSpan? Clamp(TimeSpan? input)
    {
        var value = input;
        if (value.HasValue)
        {
            if (_minTime.HasValue && value.Value < _minTime.Value)
            {
                value = _minTime.Value;
            }

            if (_maxTime.HasValue && value.Value > _maxTime.Value)
            {
                value = _maxTime.Value;
            }
        }

        return value;
    }

    private void ClampTime()
    {
        if (_time.HasValue)
        {
            SetTime(_time, false);
        }
    }

    private void RefreshText()
    {
        if (_root is null)
        {
            return;
        }

        var separators = _root.Children;
        if (_separator1 is not null)
        {
            _separator1.SetText(_culture.DateTimeFormat.TimeSeparator ?? ":");
        }

        if (_separator2 is not null)
        {
            _separator2.SetText(_culture.DateTimeFormat.TimeSeparator ?? ":");
        }
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        var picker = palette.Picker;

        foreach (var numeric in new[] { _hourPicker, _minutePicker, _secondPicker })
        {
            if (numeric is null)
            {
                continue;
            }

            numeric.IsEnabled = IsEnabled;
        }

        foreach (var separator in new[] { _separator1, _separator2 })
        {
            if (separator is not null && picker.ButtonForeground.Normal is not null)
            {
                separator.Foreground = picker.ButtonForeground.Normal;
            }
        }
    }
}
