using System;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class DropDownButtonWidget : ButtonWidget
{
    private Widget? _dropDownContent;
    private IWidgetTemplate? _dropDownTemplate;
    private Func<Widget?>? _dropDownFactory;
    private bool _executePrimaryCommand;

    public event EventHandler<WidgetDropDownEventArgs>? DropDownRequested;

    public string? DropDownKey { get; set; }

    public Widget? DropDownContent
    {
        get => _dropDownContent;
        set
        {
            _dropDownContent = value;
            _dropDownTemplate = null;
            _dropDownFactory = null;
        }
    }

    public IWidgetTemplate? DropDownContentTemplate
    {
        get => _dropDownTemplate;
        set
        {
            _dropDownTemplate = value;
            if (value is not null)
            {
                _dropDownContent = null;
                _dropDownFactory = null;
            }
        }
    }

    public Func<Widget?>? DropDownContentFactory
    {
        get => _dropDownFactory;
        set
        {
            _dropDownFactory = value;
            if (value is not null)
            {
                _dropDownContent = null;
                _dropDownTemplate = null;
            }
        }
    }

    public bool ExecutePrimaryCommand
    {
        get => _executePrimaryCommand;
        set => _executePrimaryCommand = value;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        DropDownButtonWidgetValue? dropDownValue = null;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (value is DropDownButtonWidgetValue drop)
            {
                dropDownValue = drop;
            }
        }

        base.UpdateValue(provider, item);

        if (dropDownValue is null)
        {
            switch (item)
            {
                case DropDownButtonWidgetValue dropItem:
                    dropDownValue = dropItem;
                    break;
            }
        }

        if (dropDownValue is not null)
        {
            ApplyDropDownValue(dropDownValue);
        }
        else if (provider is not null && DropDownKey is not null)
        {
            var value = provider.GetValue(item, DropDownKey);
            ApplyDropDownValue(value);
        }
    }

    protected override void OnClick(WidgetEventArgs args)
    {
        var content = BuildDropDownContent();
        var eventArgs = new WidgetDropDownEventArgs(this, content, Bounds);
        DropDownRequested?.Invoke(this, eventArgs);

        if (_executePrimaryCommand)
        {
            base.OnClick(args);
        }
    }

    private Widget? BuildDropDownContent()
    {
        if (_dropDownTemplate is not null)
        {
            return _dropDownTemplate.Build();
        }

        if (_dropDownFactory is not null)
        {
            return _dropDownFactory();
        }

        return _dropDownContent;
    }

    internal void ApplyDropDownDescriptor(DropDownButtonWidgetValue value)
    {
        ApplyDropDownValue(value);
    }

    private void ApplyDropDownValue(DropDownButtonWidgetValue value)
    {
        DropDownContent = value.DropDownContent;
        DropDownContentTemplate = value.DropDownContentTemplate;
        DropDownContentFactory = value.DropDownContentFactory;
        _executePrimaryCommand = value.ExecutePrimaryCommand;
    }

    private void ApplyDropDownValue(object? value)
    {
        switch (value)
        {
            case DropDownButtonWidgetValue dropDown:
                ApplyDropDownValue(dropDown);
                break;
            case Widget widget:
                DropDownContent = widget;
                break;
            case IWidgetTemplate template:
                DropDownContentTemplate = template;
                break;
            case Func<Widget?> factory:
                DropDownContentFactory = factory;
                break;
            case null:
                DropDownContent = null;
                DropDownContentTemplate = null;
                DropDownContentFactory = null;
                _executePrimaryCommand = false;
                break;
        }
    }
}
