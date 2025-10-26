using System;
using System.Globalization;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class NumericUpDownWidget : ButtonSpinnerWidget
{
    private const double ValueEpsilon = 1e-9;

    private readonly TextInputWidget _textInput;
    private double _minimum;
    private double _maximum = 100;
    private double _value;
    private double _increment = 1;
    private int _decimalPlaces;
    private string? _formatString;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private bool _updatingText;

    static NumericUpDownWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                string.Empty,
                new WidgetStyleRule(
                    typeof(NumericUpDownWidget),
                    state,
                    (widget, theme) =>
                    {
                        if (widget is NumericUpDownWidget numeric)
                        {
                            numeric.ApplyPalette(theme.Palette, state);
                        }
                    }));
        }
    }

    public NumericUpDownWidget()
    {
        _textInput = new TextInputWidget
        {
            DesiredWidth = 90,
            DesiredHeight = 28,
        };

        _textInput.TextChanged += OnTextChanged;
        _textInput.Submitted += OnSubmitted;
        _textInput.Automation.Name = "Numeric value editor";

        Content = _textInput;
        Spin += OnSpin;

        UpdateTextFromValue(force: true);
        UpdateSpinDirections();
        UpdateChildEnabledState();
    }

    public event EventHandler<WidgetValueChangedEventArgs<double>>? ValueChanged;

    public double Minimum
    {
        get => _minimum;
        set
        {
            if (Math.Abs(_minimum - value) <= ValueEpsilon)
            {
                return;
            }

            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            SetValueInternal(_value, raise: false);
            UpdateSpinDirections();
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            if (Math.Abs(_maximum - value) <= ValueEpsilon)
            {
                return;
            }

            _maximum = value;
            if (_maximum < _minimum)
            {
                _minimum = _maximum;
            }

            SetValueInternal(_value, raise: false);
            UpdateSpinDirections();
        }
    }

    public double Increment
    {
        get => _increment;
        set
        {
            var increment = Math.Abs(value);
            if (increment <= ValueEpsilon)
            {
                increment = 1;
            }

            if (Math.Abs(_increment - increment) <= ValueEpsilon)
            {
                return;
            }

            _increment = increment;
            UpdateSpinDirections();
        }
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            var places = Math.Max(0, value);
            if (_decimalPlaces == places)
            {
                return;
            }

            _decimalPlaces = places;
            UpdateTextFromValue(force: true);
        }
    }

    public string? FormatString
    {
        get => _formatString;
        set
        {
            if (string.Equals(_formatString, value, StringComparison.Ordinal))
            {
                return;
            }

            _formatString = value;
            UpdateTextFromValue(force: true);
        }
    }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? CultureInfo.CurrentCulture;
            UpdateTextFromValue(force: true);
        }
    }

    public double Value
    {
        get => _value;
        set => SetValueInternal(value, raise: true);
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        var minimum = _minimum;
        var maximum = _maximum;
        var value = _value;
        var increment = _increment;
        var decimals = _decimalPlaces;
        var format = _formatString;
        var culture = _culture;
        var enabled = IsEnabled;

        if (provider is not null && Key is not null)
        {
            var data = provider.GetValue(item, Key);
            if (ApplyValue(data, ref minimum, ref maximum, ref value, ref increment, ref decimals, ref format, ref culture, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref minimum, ref maximum, ref value, ref increment, ref decimals, ref format, ref culture, ref enabled))
        {
            goto Apply;
        }

Apply:
        _culture = culture ?? CultureInfo.CurrentCulture;
        DecimalPlaces = decimals;
        Increment = increment;
        Minimum = minimum;
        Maximum = maximum;
        IsEnabled = enabled;
        SetValueInternal(value, raise: false);

        if (!string.IsNullOrWhiteSpace(format))
        {
            FormatString = format;
        }

        UpdateChildEnabledState();
        UpdateSpinDirections();
        UpdateTextFromValue(force: true);
    }

    private bool ApplyValue(
        object? source,
        ref double minimum,
        ref double maximum,
        ref double value,
        ref double increment,
        ref int decimals,
        ref string? format,
        ref CultureInfo? culture,
        ref bool enabled)
    {
        switch (source)
        {
            case NumericUpDownWidgetValue numeric:
                value = numeric.Value;
                minimum = numeric.Minimum;
                maximum = numeric.Maximum;
                increment = numeric.Increment;
                decimals = Math.Max(0, numeric.DecimalPlaces);
                format = numeric.FormatString ?? format;
                culture = numeric.Culture ?? culture;
                enabled = numeric.IsEnabled;
                if (numeric.Interaction is { } interaction)
                {
                    enabled = interaction.IsEnabled;
                }

                return true;
            case double doubleValue:
                value = doubleValue;
                return true;
            case float floatValue:
                value = floatValue;
                return true;
            case decimal decimalValue:
                value = (double)decimalValue;
                return true;
            case int intValue:
                value = intValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            default:
                return false;
        }
    }

    private void SetValueInternal(double value, bool raise)
    {
        var clamped = Math.Clamp(value, _minimum, _maximum);
        if (Math.Abs(clamped - _value) <= ValueEpsilon)
        {
            UpdateTextFromValue();
            return;
        }

        var oldValue = _value;
        _value = clamped;
        UpdateTextFromValue();
        UpdateSpinDirections();

        if (raise)
        {
            ValueChanged?.Invoke(this, new WidgetValueChangedEventArgs<double>(this, oldValue, _value));
        }
    }

    private void UpdateTextFromValue(bool force = false)
    {
        if (_updatingText && !force)
        {
            return;
        }

        var formatted = FormatCurrentValue();

        try
        {
            _updatingText = true;
            if (!string.Equals(_textInput.Text, formatted, StringComparison.Ordinal))
            {
                _textInput.Text = formatted;
            }
        }
        finally
        {
            _updatingText = false;
        }
    }

    private string FormatCurrentValue()
    {
        if (!string.IsNullOrWhiteSpace(_formatString))
        {
            return _value.ToString(_formatString, _culture);
        }

        return _value.ToString($"F{_decimalPlaces}", _culture);
    }

    private void UpdateSpinDirections()
    {
        var directions = WidgetValidSpinDirections.None;
        if (_maximum - _value > ValueEpsilon)
        {
            directions |= WidgetValidSpinDirections.Increase;
        }

        if (_value - _minimum > ValueEpsilon)
        {
            directions |= WidgetValidSpinDirections.Decrease;
        }

        ValidSpinDirections = directions;
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette, WidgetVisualState state)
    {
        var picker = palette.Picker;

        if (_textInput is not null)
        {
            var background = picker.ButtonBackground.Get(state) ?? picker.ButtonBackground.Normal;
            var border = picker.ButtonBorder.Get(state) ?? picker.ButtonBorder.Normal;
            var foreground = picker.ButtonForeground.Get(state) ?? picker.ButtonForeground.Normal;

            if (background is not null)
            {
                _textInput.Background = background;
            }

            if (border is not null)
            {
                _textInput.BorderBrush = border;
            }

            if (foreground is not null)
            {
                _textInput.Foreground = foreground;
            }
        }

        UpdateChildEnabledState();
    }

    private void OnSpin(object? sender, WidgetSpinEventArgs e)
    {
        var delta = e.Direction == WidgetSpinDirection.Increase ? _increment : -_increment;
        SetValueInternal(_value + delta, raise: true);
    }

    private void OnTextChanged(object? sender, WidgetValueChangedEventArgs<string> e)
    {
        if (_updatingText)
        {
            return;
        }

        if (TryParseText(e.NewValue, out var parsed))
        {
            SetValueInternal(parsed, raise: true);
        }
    }

    private void OnSubmitted(object? sender, EventArgs e)
    {
        if (!TryParseText(_textInput.Text, out var parsed))
        {
            UpdateTextFromValue(force: true);
            return;
        }

        SetValueInternal(parsed, raise: true);
        UpdateTextFromValue(force: true);
    }

    private bool TryParseText(string? text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = _minimum;
            return true;
        }

        text = text.Trim();

        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, _culture, out value))
        {
            value = Math.Round(value, _decimalPlaces);
            return true;
        }

        return false;
    }

    private void UpdateChildEnabledState()
    {
        if (_textInput is not null)
        {
            _textInput.IsEnabled = IsEnabled;
        }
    }
}
