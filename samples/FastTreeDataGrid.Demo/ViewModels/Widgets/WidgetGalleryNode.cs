using System;
using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Widgets;

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
    public const string KeyRadio = "Widget.Radio";
    public const string KeyBadge = "Widget.Badge";

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

    public object? RadioValue { get; set; }

    public object? BadgeValue { get; set; }

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
        KeyRadio => RadioValue,
        KeyBadge => BadgeValue,
        _ => _additionalValues.TryGetValue(key, out var value) ? value : null
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }
}
