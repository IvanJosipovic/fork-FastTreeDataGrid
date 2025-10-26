using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class TabControlWidget : TemplatedWidget
{
    private BorderWidget? _rootBorder;
    private TabStripWidget? _tabStrip;
    private ContentControlWidget? _contentPresenter;
    private ImmutableArray<object?> _items = ImmutableArray<object?>.Empty;
    private readonly List<TabEntry> _entries = new();
    private Func<IFastTreeDataGridValueProvider?, object?, Widget?>? _headerFactory;
    private Func<IFastTreeDataGridValueProvider?, object?, Widget?>? _contentFactory;
    private IWidgetTemplate? _headerTemplate;
    private IWidgetTemplate? _contentTemplate;
    private int _selectedIndex = -1;
    private object? _selectedItem;
    private IFastTreeDataGridValueProvider? _currentProvider;
    private object? _currentSourceItem;

    public event EventHandler<WidgetValueChangedEventArgs<object?>>? SelectionChanged;

    static TabControlWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(TabControlWidget),
                WidgetVisualState.Normal,
                static (widget, theme) =>
                {
                    if (widget is TabControlWidget tabControl)
                    {
                        tabControl.ApplyPalette(theme.Palette);
                    }
                }));
    }

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        var handled = base.HandleKeyboardEvent(e);

        if (!IsEnabled || e.Kind != WidgetKeyboardEventKind.KeyDown)
        {
            return handled;
        }

        var key = e.Args.Key;
        var result = false;

        switch (key)
        {
            case Avalonia.Input.Key.Left:
            case Avalonia.Input.Key.Up:
                result = MoveSelection(-1);
                break;
            case Avalonia.Input.Key.Right:
            case Avalonia.Input.Key.Down:
                result = MoveSelection(1);
                break;
            case Avalonia.Input.Key.Home:
                if (_items.Length > 0)
                {
                    var previous = _selectedIndex;
                    SetSelectedIndexInternal(0, raise: true);
                    result = _selectedIndex != previous;
                }
                break;
            case Avalonia.Input.Key.End:
                if (_items.Length > 0)
                {
                    var previous = _selectedIndex;
                    SetSelectedIndexInternal(_items.Length - 1, raise: true);
                    result = _selectedIndex != previous;
                }
                break;
        }

        if (result)
        {
            e.Args.Handled = true;
            return true;
        }

        return handled;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        _currentProvider = provider;
        _currentSourceItem = item;

        ImmutableArray<object?> items = _items;
        object? selectedItem = _selectedItem;
        var selectedIndex = _selectedIndex;
        var enabled = IsEnabled;
        var indexSet = false;
        var itemSet = false;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref items, ref selectedItem, ref selectedIndex, ref indexSet, ref itemSet, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref items, ref selectedItem, ref selectedIndex, ref indexSet, ref itemSet, ref enabled))
        {
            goto Apply;
        }

Apply:
        IsEnabled = enabled;
        SetItems(items, provider, item);

        if (indexSet)
        {
            SetSelectedIndexInternal(selectedIndex, raise: false);
        }
        else if (itemSet)
        {
            SetSelectedItemInternal(selectedItem, raise: false);
        }
        else
        {
            RefreshSelection();
        }

        UpdateTabEnabledState();
    }

    public void SetItems(ImmutableArray<object?> items)
    {
        SetItems(items, _currentProvider, _currentSourceItem);
    }

    public void SetSelectedIndex(int index)
    {
        SetSelectedIndexInternal(index, raise: true);
    }

    public void SetSelectedItem(object? item)
    {
        SetSelectedItemInternal(item, raise: true);
    }

    protected override Widget? CreateDefaultTemplate()
    {
        _rootBorder = new BorderWidget
        {
            ClipToBounds = true,
            Padding = new Thickness(0)
        };

        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            Padding = new Thickness(0)
        };

        _tabStrip = new TabStripWidget();
        _tabStrip.TabRequested += OnTabRequested;

        _contentPresenter = new ContentControlWidget
        {
            DesiredWidth = double.NaN,
            DesiredHeight = double.NaN
        };

        stack.Children.Add(_tabStrip);
        stack.Children.Add(_contentPresenter);
        _rootBorder.Child = stack;

        ApplyPalette(WidgetFluentPalette.Current);
        RefreshSelection();
        return _rootBorder;
    }

    private bool ApplyValue(
        object? value,
        ref ImmutableArray<object?> items,
        ref object? selectedItem,
        ref int selectedIndex,
        ref bool indexSet,
        ref bool itemSet,
        ref bool enabled)
    {
        switch (value)
        {
            case TabControlWidgetValue tab:
                if (tab.Items is not null)
                {
                    items = tab.Items.Cast<object?>().ToImmutableArray();
                }

                if (tab.SelectedIndex.HasValue)
                {
                    selectedIndex = tab.SelectedIndex.Value;
                    indexSet = true;
                }

                if (tab.SelectedItemSet)
                {
                    selectedItem = tab.SelectedItem;
                    itemSet = true;
                }

                if (tab.HeaderFactory is not null)
                {
                    _headerFactory = tab.HeaderFactory;
                    _headerTemplate = null;
                }
                else if (tab.HeaderTemplate is not null)
                {
                    _headerTemplate = tab.HeaderTemplate;
                    _headerFactory = null;
                }

                if (tab.ContentFactory is not null)
                {
                    _contentFactory = tab.ContentFactory;
                    _contentTemplate = null;
                }
                else if (tab.ContentTemplate is not null)
                {
                    _contentTemplate = tab.ContentTemplate;
                    _contentFactory = null;
                }

                enabled = tab.Interaction is { } interaction ? interaction.IsEnabled : enabled;
                return true;

            case IEnumerable enumerable and not string:
                items = enumerable.Cast<object?>().ToImmutableArray();
                return true;
            default:
                return false;
        }
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        if (_rootBorder is not null)
        {
            _rootBorder.Background = palette.Tab.StripBackground ?? palette.Flyout.Background ?? _rootBorder.Background;
            _rootBorder.BorderBrush = palette.Border.ControlBorder.Get(WidgetVisualState.Normal) ?? _rootBorder.BorderBrush;
            _rootBorder.BorderThickness = _rootBorder.BorderBrush is null ? default : new Thickness(1);
        }

        _tabStrip?.ApplyPalette();

        foreach (var entry in _entries)
        {
            entry.HeaderWidget.UpdateAppearance();
        }
    }

    private void SetItems(ImmutableArray<object?> items, IFastTreeDataGridValueProvider? provider, object? sourceItem)
    {
        if (_tabStrip is null)
        {
            _items = items;
            return;
        }

        foreach (var entry in _entries)
        {
            entry.HeaderWidget.Clicked -= OnHeaderClicked;
        }

        _entries.Clear();
        _items = items;

        if (items.Length == 0)
        {
            _tabStrip.SetTabs(Array.Empty<TabItemWidget>());
            var previous = _selectedItem;
            _selectedIndex = -1;
            _selectedItem = null;
            UpdateSelectionVisuals();
            if (previous is not null)
            {
                SelectionChanged?.Invoke(this, new WidgetValueChangedEventArgs<object?>(this, previous, null));
            }

            return;
        }

        var tabs = new List<TabItemWidget>(items.Length);

        for (var i = 0; i < items.Length; i++)
        {
            var dataItem = items[i];
            var tabItem = new TabItemWidget();
            tabItem.Clicked += OnHeaderClicked;
            var headerContent = BuildHeader(provider, dataItem);
            tabItem.SetHeader(headerContent);
            tabItem.Automation.Name = dataItem?.ToString() ?? $"Tab {i + 1}";
            tabs.Add(tabItem);
            _entries.Add(new TabEntry(dataItem, provider, tabItem));
        }

        _tabStrip.SetTabs(tabs);
        UpdateTabEnabledState();

        if (_selectedItem is not null)
        {
            var index = FindIndexForItem(_selectedItem);
            if (index >= 0)
            {
                SetSelectedIndexInternal(index, raise: false);
                return;
            }
        }

        if (_selectedIndex >= 0 && _selectedIndex < _items.Length)
        {
            SetSelectedIndexInternal(_selectedIndex, raise: false);
        }
        else
        {
            SetSelectedIndexInternal(0, raise: false);
        }
    }

    private void RefreshSelection()
    {
        SetSelectedIndexInternal(_selectedIndex, raise: false);
    }

    private bool MoveSelection(int delta)
    {
        if (_items.Length == 0)
        {
            return false;
        }

        var index = _selectedIndex;
        if (index < 0)
        {
            index = delta > 0 ? 0 : _items.Length - 1;
        }
        else
        {
            index = (index + delta + _items.Length) % _items.Length;
        }

        var previous = _selectedIndex;
        SetSelectedIndexInternal(index, raise: true);
        return _selectedIndex != previous;
    }

    private void SetSelectedIndexInternal(int index, bool raise)
    {
        if (_items.Length == 0)
        {
            index = -1;
        }
        else
        {
            if (index < -1)
            {
                index = -1;
            }
            else if (index >= _items.Length)
            {
                index = _items.Length - 1;
            }
        }

        var previous = _selectedItem;

        if (_selectedIndex == index && Equals(previous, index >= 0 ? _items[index] : null))
        {
            UpdateSelectionVisuals();
            return;
        }

        _selectedIndex = index;
        _selectedItem = index >= 0 ? _items[index] : null;
        UpdateSelectionVisuals();

        if (raise && !Equals(previous, _selectedItem))
        {
            SelectionChanged?.Invoke(this, new WidgetValueChangedEventArgs<object?>(this, previous, _selectedItem));
        }
    }

    private void SetSelectedItemInternal(object? item, bool raise)
    {
        if (Equals(_selectedItem, item))
        {
            UpdateSelectionVisuals();
            return;
        }

        var index = FindIndexForItem(item);
        SetSelectedIndexInternal(index, raise);
    }

    private int FindIndexForItem(object? item)
    {
        if (item is null)
        {
            return -1;
        }

        for (var i = 0; i < _items.Length; i++)
        {
            var candidate = _items[i];
            if (ReferenceEquals(candidate, item) || Equals(candidate, item))
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateTabEnabledState()
    {
        foreach (var entry in _entries)
        {
            entry.HeaderWidget.IsEnabled = IsEnabled;
        }
    }

    private Widget BuildHeader(IFastTreeDataGridValueProvider? provider, object? item)
    {
        Widget? header = null;

        if (_headerFactory is not null)
        {
            header = _headerFactory(provider, item);
        }
        else if (_headerTemplate is not null)
        {
            header = _headerTemplate.Build();
        }

        header ??= ListBoxWidget.CreateTextContent(item);
        header.UpdateValue(provider, item);
        return header;
    }

    private Widget BuildContent(IFastTreeDataGridValueProvider? provider, object? item)
    {
        Widget? content = null;

        if (_contentFactory is not null)
        {
            content = _contentFactory(provider, item);
        }
        else if (_contentTemplate is not null)
        {
            content = _contentTemplate.Build();
        }

        content ??= ListBoxWidget.CreateTextContent(item);
        content.UpdateValue(provider, item);
        return content;
    }

    private void UpdateSelectionVisuals()
    {
        if (_entries.Count == 0)
        {
            if (_contentPresenter is not null)
            {
                _contentPresenter.Content = null;
            }

            return;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var isSelected = i == _selectedIndex;
            entry.HeaderWidget.SetSelected(isSelected);

            if (isSelected && _contentPresenter is not null)
            {
                if (entry.Content is null)
                {
                    entry.Content = BuildContent(entry.Provider, entry.Item);
                }

                _contentPresenter.Content = entry.Content;
                entry.Content.UpdateValue(entry.Provider, entry.Item);
            }
        }

        if (_selectedIndex < 0 && _contentPresenter is not null)
        {
            _contentPresenter.Content = null;
        }
    }

    private void OnTabRequested(object? sender, int index)
    {
        SetSelectedIndexInternal(index, raise: true);
    }

    private void OnHeaderClicked(object? sender, WidgetEventArgs e)
    {
        if (sender is TabItemWidget tab)
        {
            SetSelectedIndexInternal(tab.Index, raise: true);
        }
    }

    private sealed class TabEntry
    {
        public TabEntry(object? item, IFastTreeDataGridValueProvider? provider, TabItemWidget header)
        {
            Item = item;
            Provider = provider;
            HeaderWidget = header;
        }

        public object? Item { get; }

        public IFastTreeDataGridValueProvider? Provider { get; }

        public TabItemWidget HeaderWidget { get; }

        public Widget? Content { get; set; }
    }
}

public sealed class TabStripWidget : SurfaceWidget
{
    private readonly BorderWidget _background;
    private readonly StackLayoutWidget _stack;
    private readonly List<TabItemWidget> _tabs = new();

    public event EventHandler<int>? TabRequested;

    public TabStripWidget()
    {
        ClipToBounds = true;

        _background = new BorderWidget
        {
            ClipToBounds = true,
            Padding = new Thickness(0)
        };

        _stack = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Padding = new Thickness(0)
        };

        _background.Child = _stack;
        Children.Add(_background);
    }

    public void SetTabs(IReadOnlyList<TabItemWidget> tabs)
    {
        foreach (var tab in _tabs)
        {
            tab.Clicked -= OnTabClicked;
        }

        _tabs.Clear();
        _stack.Children.Clear();

        for (var i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            tab.Index = i;
            tab.Clicked += OnTabClicked;
            _tabs.Add(tab);
            _stack.Children.Add(tab);
        }

        ApplyPalette();
    }

    public void ApplyPalette()
    {
        var palette = WidgetFluentPalette.Current.Tab;
        _background.Background = palette.StripBackground ?? _background.Background;
    }

    private void OnTabClicked(object? sender, WidgetEventArgs e)
    {
        if (sender is TabItemWidget tab)
        {
            TabRequested?.Invoke(this, tab.Index);
        }
    }
}

public sealed class TabItemWidget : SurfaceWidget
{
    private readonly SurfaceWidget _contentHost = new SurfaceWidget();
    private readonly BorderWidget _container = new BorderWidget();
    private readonly BorderWidget _indicator = new BorderWidget
    {
        ClipToBounds = true,
        Padding = new Thickness(0)
    };
    private Widget? _headerContent;
    private double _indicatorThickness;
    private bool _isSelected;

    static TabItemWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                string.Empty,
                new WidgetStyleRule(
                    typeof(TabItemWidget),
                    state,
                    static (widget, _) =>
                    {
                        if (widget is TabItemWidget tab)
                        {
                            tab.UpdateAppearance();
                        }
                    }));
        }
    }

    public TabItemWidget()
    {
        IsInteractive = true;
        ClipToBounds = true;

        _container.ClipToBounds = true;
        _container.Child = _contentHost;

        Children.Add(_container);
        Children.Add(_indicator);

        PointerInput += OnPointerInput;
    }

    public event EventHandler<WidgetEventArgs>? Clicked;

    internal int Index { get; set; }

    public void SetHeader(Widget? header)
    {
        if (ReferenceEquals(_headerContent, header))
        {
            return;
        }

        if (_headerContent is not null)
        {
            _contentHost.Children.Remove(_headerContent);
        }

        _headerContent = header;

        if (_headerContent is not null && !_contentHost.Children.Contains(_headerContent))
        {
            _contentHost.Children.Add(_headerContent);
        }

        UpdateAppearance();
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
        {
            UpdateAppearance();
            return;
        }

        _isSelected = selected;
        UpdateAppearance();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        var contentHeight = Math.Max(0, bounds.Height - _indicatorThickness);
        var contentRect = new Rect(bounds.X, bounds.Y, bounds.Width, contentHeight);
        _container.Arrange(contentRect);

        if (_indicatorThickness > 0)
        {
            var indicatorRect = new Rect(bounds.X, bounds.Y + contentHeight, bounds.Width, _indicatorThickness);
            _indicator.Arrange(indicatorRect);
        }
        else
        {
            _indicator.Arrange(new Rect(bounds.X, bounds.Y + contentHeight, bounds.Width, 0));
        }
    }

    private void OnPointerInput(WidgetPointerEvent e)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (e.Kind == WidgetPointerEventKind.Released)
        {
            Clicked?.Invoke(this, new WidgetEventArgs(this));
        }
    }

    internal void UpdateAppearance()
    {
        var palette = WidgetFluentPalette.Current.Tab;
        Margin = palette.HeaderMargin;
        _indicatorThickness = Math.Max(0, palette.IndicatorThickness);
        _container.Padding = palette.HeaderPadding;
        DesiredHeight = Math.Max(palette.MinHeight, DesiredHeight);

        var state = VisualState;

        var backgroundState = _isSelected ? palette.SelectedBackground : palette.UnselectedBackground;
        var foregroundState = _isSelected ? palette.SelectedForeground : palette.UnselectedForeground;

        _container.Background = backgroundState.Get(state) ?? palette.StripBackground ?? _container.Background;
        _container.BorderBrush = null;
        _container.BorderThickness = default;
        _container.CornerRadius = _isSelected ? palette.HeaderCornerRadius : default;

        _indicator.Background = _isSelected ? palette.IndicatorBrush : null;
        _indicator.BorderBrush = null;
        _indicator.BorderThickness = default;

        if (_headerContent is Widget header && foregroundState.Get(state) is { } foreground)
        {
            header.Foreground = foreground;
        }

        Automation.CommandLabel = _isSelected ? "Selected tab" : null;
    }
}
