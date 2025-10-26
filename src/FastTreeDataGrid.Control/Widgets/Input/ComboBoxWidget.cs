using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ComboBoxWidget : TemplatedWidget
{
    private BorderWidget? _rootBorder;
    private ButtonWidget? _displayButton;
    private SurfaceWidget? _popupHost;
    private BorderWidget? _popupBorder;
    private StackLayoutWidget? _itemPanel;
    private ImmutableArray<object?> _items = ImmutableArray<object?>.Empty;
    private object? _selectedItem;
    private string? _displayMember;
    private bool _isDropDownOpen;
    private readonly List<ComboBoxItemWidget> _itemWidgets = new();
    private IFastTreeDataGridValueProvider? _currentProvider;
    private object? _currentSourceItem;

    public event EventHandler<WidgetValueChangedEventArgs<object?>>? SelectionChanged;
    public event EventHandler<WidgetValueChangedEventArgs<bool>>? DropDownOpenChanged;

    static ComboBoxWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(ComboBoxWidget),
                WidgetVisualState.Normal,
                (widget, theme) =>
                {
                    if (widget is ComboBoxWidget combo)
                    {
                        combo.ApplyPalette(theme.Palette);
                    }
                }));
    }

    public ImmutableArray<object?> Items
    {
        get => _items;
        set
        {
            _items = value;
            BuildItems();
        }
    }

    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetSelectedItem(value, true);
    }

    public string? DisplayMember
    {
        get => _displayMember;
        set
        {
            if (string.Equals(_displayMember, value, StringComparison.Ordinal))
            {
                return;
            }

            _displayMember = value;
            RefreshDisplayText();
        }
    }

    public bool IsDropDownOpen => _isDropDownOpen;

    protected override Widget? CreateDefaultTemplate()
    {
        _rootBorder = new BorderWidget
        {
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(4)
        };

        var root = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        _displayButton = new ButtonWidget
        {
            DesiredHeight = 32,
            DesiredWidth = double.NaN,
            Automation = { Name = "Combo selection" }
        };
        _displayButton.Click += (_, __) => ToggleDropDown();

        _popupHost = new SurfaceWidget
        {
            ClipToBounds = true
        };

        _popupBorder = new BorderWidget
        {
            ClipToBounds = true,
            Padding = new Thickness(0)
        };

        _itemPanel = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };
        _popupBorder.Child = _itemPanel;

        root.Children.Add(_displayButton);
        root.Children.Add(_popupHost);
        _rootBorder.Child = root;

        SetDropDownState(false);
        RefreshDisplayText();
        BuildItems();

        return _rootBorder;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        _currentProvider = provider;
        _currentSourceItem = item;

        ImmutableArray<object?> items = _items;
        object? selected = _selectedItem;
        var displayMember = _displayMember;
        var enabled = IsEnabled;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref items, ref selected, ref displayMember, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref items, ref selected, ref displayMember, ref enabled))
        {
            goto Apply;
        }

Apply:
        IsEnabled = enabled;
        DisplayMember = displayMember;
        Items = items;
        SetSelectedItem(selected, false);
    }

    private bool ApplyValue(object? value, ref ImmutableArray<object?> items, ref object? selected, ref string? displayMember, ref bool enabled)
    {
        switch (value)
        {
            case ComboBoxWidgetValue combo:
                items = combo.Items is { } list ? list.ToImmutableArray<object?>() : items;
                selected = combo.SelectedItem ?? selected;
                displayMember = combo.DisplayMemberPath ?? displayMember;
                enabled = combo.IsEnabled;
                if (combo.Interaction is { } interaction)
                {
                    enabled = interaction.IsEnabled;
                }

                if (combo.ItemsProvider is { } provider)
                {
                    items = provider().ToImmutableArray<object?>();
                }

                return true;
            case object[] array:
                items = ImmutableArray.Create<object?>(array);
                return true;
            case IEnumerable<object?> enumerable:
                items = enumerable.ToImmutableArray();
                return true;
            default:
                return false;
        }
    }

    private void ToggleDropDown() => SetDropDownState(!_isDropDownOpen);

    private void SetDropDownState(bool open)
    {
        if (_isDropDownOpen == open)
        {
            return;
        }

        var old = _isDropDownOpen;
        _isDropDownOpen = open;

        if (!open)
        {
            ClearPopup();
        }
        else
        {
            BuildItems();

            if (_popupHost is not null && _popupBorder is not null && !_popupHost.Children.Contains(_popupBorder))
            {
                _popupHost.Children.Add(_popupBorder);
            }
        }

        RefreshItemSelection();

        DropDownOpenChanged?.Invoke(this, new WidgetValueChangedEventArgs<bool>(this, old, open));
    }

    private void BuildItems()
    {
        if (_itemPanel is null)
        {
            return;
        }

        foreach (var widget in _itemWidgets)
        {
            widget.Clicked -= OnComboItemClicked;
        }

        _itemWidgets.Clear();
        _itemPanel.Children.Clear();

        foreach (var item in _items)
        {
            var comboItem = CreateItemWidget(item);
            _itemWidgets.Add(comboItem);
            _itemPanel.Children.Add(comboItem);
        }

        RefreshItemSelection();
    }

    private void OnItemClicked(object? item)
    {
        SetSelectedItem(item, true);
        SetDropDownState(false);
    }

    private void OnComboItemClicked(object? sender, WidgetEventArgs e)
    {
        if (sender is ComboBoxItemWidget itemWidget)
        {
            OnItemClicked(itemWidget.Item);
        }
    }

    private void ClearPopup()
    {
        if (_popupHost is not null)
        {
            _popupHost.Children.Clear();
        }
    }

    private void SetSelectedItem(object? item, bool raise)
    {
        if (Equals(_selectedItem, item))
        {
            RefreshDisplayText();
            return;
        }

        var previous = _selectedItem;
        _selectedItem = item;
        RefreshDisplayText();
        RefreshItemSelection();

        if (raise)
        {
            SelectionChanged?.Invoke(this, new WidgetValueChangedEventArgs<object?>(this, previous, item));
        }
    }

    private string FormatItem(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(_displayMember))
        {
            return item.ToString() ?? string.Empty;
        }

        var property = item.GetType().GetProperty(_displayMember!);
        if (property is null)
        {
            return item.ToString() ?? string.Empty;
        }

        var value = property.GetValue(item);
        return value?.ToString() ?? string.Empty;
    }

    private void RefreshDisplayText()
    {
        if (_displayButton is null)
        {
            return;
        }

        var text = FormatItem(_selectedItem);
        if (string.IsNullOrEmpty(text))
        {
            text = "Select";
        }

        _displayButton.SetText(text + " ▼");
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        if (_displayButton is not null)
        {
            _displayButton.Background = palette.Picker.ButtonBackground.Normal ?? _displayButton.Background;
            _displayButton.BorderBrush = palette.Picker.ButtonBorder.Normal ?? _displayButton.BorderBrush;
        }

        if (_rootBorder is not null)
        {
            _rootBorder.BorderBrush = palette.Picker.ButtonBorder.Normal ?? _rootBorder.BorderBrush;
            _rootBorder.Background = palette.Flyout.Background ?? _rootBorder.Background;
        }

        if (_popupBorder is not null)
        {
            _popupBorder.Background = palette.Picker.FlyoutBackground ?? _popupBorder.Background;
            _popupBorder.BorderBrush = palette.Picker.FlyoutBorder ?? _popupBorder.BorderBrush;
            _popupBorder.BorderThickness = (_popupBorder.BorderBrush is null) ? default : new Thickness(1);
        }
    }

    private ComboBoxItemWidget CreateItemWidget(object? item)
    {
        var comboItem = new ComboBoxItemWidget
        {
            DesiredHeight = 28,
            DesiredWidth = double.NaN,
            Automation = { Name = "Combo item" },
            Item = item
        };

        var content = BuildItemContent(_currentProvider, item);
        comboItem.ContentWidget = content;
        comboItem.Clicked += OnComboItemClicked;
        return comboItem;
    }

    private void RefreshItemSelection()
    {
        foreach (var widget in _itemWidgets)
        {
            widget.IsSelected = Equals(widget.Item, _selectedItem);
        }
    }

    private Widget BuildItemContent(IFastTreeDataGridValueProvider? provider, object? item)
    {
        var widget = ListBoxWidget.CreateTextContent(item);
        widget.UpdateValue(provider, item);
        return widget;
    }
}
