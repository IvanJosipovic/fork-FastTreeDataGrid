using System;
using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Control.Widgets.Samples;

public sealed class WidgetGalleryNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyName = "Widget.Name";
    public const string KeyDescription = "Widget.Description";
    public const string KeyIcon = "Widget.Icon";
    public const string KeyGeometry = "Widget.Geometry";
    public const string KeyButton = "Widget.Button";
    public const string KeyCheckBox = "Widget.CheckBox";
    public const string KeyProgress = "Widget.Progress";
    public const string KeyCustom = "Widget.Custom";
    public const string KeyLayout = "Widget.Layout";
    public const string KeyToggle = "Widget.Toggle";
    public const string KeySlider = "Widget.Slider";
    public const string KeyScrollBar = "Widget.ScrollBar";
    public const string KeyNumeric = "Widget.NumericUpDown";
    public const string KeyCalendar = "Widget.Calendar";
    public const string KeyCalendarDatePicker = "Widget.CalendarDatePicker";
    public const string KeyTimePicker = "Widget.TimePicker";
    public const string KeyAutoComplete = "Widget.AutoComplete";
    public const string KeyComboBox = "Widget.ComboBox";
    public const string KeyDatePicker = "Widget.DatePicker";
    public const string KeyRadio = "Widget.Radio";
    public const string KeyBadge = "Widget.Badge";
    public const string KeyPrimaryAction = "Widget.PrimaryAction";
    public const string KeySecondaryAction = "Widget.SecondaryAction";
    public const string KeyImage = "Widget.Image";
    public const string KeyIconElement = "Widget.IconElement";
    public const string KeyPathIcon = "Widget.PathIcon";

    private readonly List<WidgetGalleryNode> _children = new();
    private readonly Dictionary<string, object?> _additionalValues = new();

    public WidgetGalleryNode(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string Description { get; }

    public object? IconValue { get; set; }

    public object? GeometryValue { get; set; }

    public object? ButtonValue { get; set; }

    public object? CheckBoxValue { get; set; }

    public object? ProgressValue { get; set; }

    public object? CustomValue { get; set; }

    public object? ToggleValue { get; set; }

    public object? SliderValue { get; set; }

    public object? ScrollBarValue { get; set; }

    public object? NumericValue { get; set; }

    public object? CalendarValue { get; set; }

    public object? DatePickerValue { get; set; }

    public object? CalendarDatePickerValue { get; set; }

    public object? TimePickerValue { get; set; }

    public object? AutoCompleteValue { get; set; }

    public object? ComboBoxValue { get; set; }

    public object? RadioValue { get; set; }

    public object? BadgeValue { get; set; }

    public object? PrimaryActionValue { get; set; }

    public object? SecondaryActionValue { get; set; }

    public Func<Widget>? LayoutFactory { get; set; }

    public IReadOnlyList<WidgetGalleryNode> Children => _children;

    public bool IsGroup => _children.Count > 0;

    public void AddChild(WidgetGalleryNode node)
    {
        _children.Add(node);
    }

    public void SetValue(string key, object? value)
    {
        _additionalValues[key] = value;
    }

    public object? GetValue(object? item, string key) => key switch
    {
        KeyName => Name,
        KeyDescription => Description,
        KeyIcon => IconValue,
        KeyGeometry => GeometryValue,
        KeyButton => ButtonValue,
        KeyCheckBox => CheckBoxValue,
        KeyProgress => ProgressValue,
        KeyCustom => CustomValue,
        KeyLayout => LayoutFactory,
        KeyToggle => ToggleValue,
        KeySlider => SliderValue,
        KeyScrollBar => ScrollBarValue,
        KeyNumeric => NumericValue,
        KeyCalendar => CalendarValue,
        KeyDatePicker => DatePickerValue,
        KeyCalendarDatePicker => CalendarDatePickerValue,
        KeyTimePicker => TimePickerValue,
        KeyAutoComplete => AutoCompleteValue,
        KeyComboBox => ComboBoxValue,
        KeyRadio => RadioValue,
        KeyBadge => BadgeValue,
        KeyPrimaryAction => PrimaryActionValue,
        KeySecondaryAction => SecondaryActionValue,
        KeyImage => _additionalValues.TryGetValue(KeyImage, out var image) ? image : null,
        KeyIconElement => _additionalValues.TryGetValue(KeyIconElement, out var icon) ? icon : null,
        KeyPathIcon => _additionalValues.TryGetValue(KeyPathIcon, out var pathIcon) ? pathIcon : null,
        _ => _additionalValues.TryGetValue(key, out var value) ? value : null
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }
}
