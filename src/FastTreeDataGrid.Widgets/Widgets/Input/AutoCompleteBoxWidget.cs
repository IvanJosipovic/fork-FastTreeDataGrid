using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class AutoCompleteBoxWidget : TemplatedWidget
{
    private StackLayoutWidget? _root;
    private TextInputWidget? _input;
    private SurfaceWidget? _popupHost;
    private List<ButtonWidget>? _suggestionButtons;
    private ImmutableArray<string> _items = ImmutableArray<string>.Empty;
    private ImmutableArray<string> _filtered = ImmutableArray<string>.Empty;
    private string _text = string.Empty;
    private bool _isDropDownOpen;

    public event EventHandler<WidgetValueChangedEventArgs<string?>>? TextChanged;
    public event EventHandler<WidgetValueChangedEventArgs<string?>>? SelectionChanged;

    static AutoCompleteBoxWidget()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(AutoCompleteBoxWidget),
                WidgetVisualState.Normal,
                (widget, theme) =>
                {
                    if (widget is AutoCompleteBoxWidget auto)
                    {
                        auto.ApplyPalette(theme.Palette);
                    }
                }));
    }

    public ImmutableArray<string> Items
    {
        get => _items;
        set
        {
            _items = value;
            FilterItems();
        }
    }

    public string? Text
    {
        get => _text;
        set => SetText(value ?? string.Empty, true);
    }

    public bool IsDropDownOpen => _isDropDownOpen;

    protected override Widget? CreateDefaultTemplate()
    {
        _root = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        _input = new TextInputWidget
        {
            DesiredHeight = 32,
            Placeholder = "Type to search"
        };
        _input.TextChanged += (_, args) => OnTextInputChanged(args.NewValue);

        _popupHost = new SurfaceWidget
        {
            ClipToBounds = true
        };

        _suggestionButtons = new List<ButtonWidget>();

        _root.Children.Add(_input);
        _root.Children.Add(_popupHost);

        ClearSuggestions();
        _isDropDownOpen = false;
        return _root;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        var text = _text;
        ImmutableArray<string> items = _items;
        var enabled = IsEnabled;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref text, ref items, ref enabled))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref text, ref items, ref enabled))
        {
            goto Apply;
        }

Apply:
        IsEnabled = enabled;
        Items = items;
        SetText(text, false);
    }

    private bool ApplyValue(object? value, ref string text, ref ImmutableArray<string> items, ref bool enabled)
    {
        switch (value)
        {
            case AutoCompleteBoxWidgetValue composite:
                text = composite.Text ?? text;
                if (composite.Suggestions is { } suggestions)
                {
                    items = suggestions.ToImmutableArray();
                }
                enabled = composite.IsEnabled;
                if (composite.Interaction is { } interaction)
                {
                    enabled = interaction.IsEnabled;
                }
                return true;
            case string str:
                text = str;
                return true;
            case string[] array:
                items = ImmutableArray.Create(array);
                return true;
            default:
                return false;
        }
    }

    private void OnTextInputChanged(string text)
    {
        SetText(text, true);
    }

    private void SetText(string text, bool raise)
    {
        if (string.Equals(_text, text, StringComparison.Ordinal))
        {
            return;
        }

        var old = _text;
        _text = text;
        if (_input is not null && !string.Equals(_input.Text, text, StringComparison.Ordinal))
        {
            _input.Text = text;
        }

        FilterItems();

        if (raise)
        {
            TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string?>(this, old, text));
        }
    }

    private void FilterItems()
    {
        if (_items.IsDefaultOrEmpty)
        {
            _filtered = ImmutableArray<string>.Empty;
            ClearSuggestions();
            SetDropDownState(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(_text))
        {
            _filtered = _items;
            UpdateSuggestions();
            return;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var filtered = _items.Where(i => i?.IndexOf(_text, comparison) >= 0).Take(10).ToImmutableArray();
        _filtered = filtered;
        UpdateSuggestions();
    }

    private void UpdateSuggestions()
    {
        if (_popupHost is null)
        {
            return;
        }

        ClearSuggestions();

        if (_filtered.IsDefaultOrEmpty)
        {
            ClearSuggestions();
            SetDropDownState(false);
            return;
        }

        var stack = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };

        foreach (var suggestion in _filtered)
        {
            var button = new ButtonWidget
            {
                DesiredHeight = 28,
                DesiredWidth = double.NaN
            };
            button.SetText(suggestion);
            var item = suggestion;
            button.Click += (_, __) => OnSuggestionClicked(item);
            stack.Children.Add(button);
            _suggestionButtons?.Add(button);
        }

        var container = new BorderWidget
        {
            Child = stack,
            Background = new ImmutableSolidColorBrush(Color.FromRgb(245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(220, 220, 220))
        };

        _popupHost.Children.Add(container);
        SetDropDownState(true);
    }

    private void OnSuggestionClicked(string suggestion)
    {
        var previous = _text;
        _text = suggestion;
        if (_input is not null)
        {
            _input.Text = suggestion;
        }

        SetDropDownState(false);
        SelectionChanged?.Invoke(this, new WidgetValueChangedEventArgs<string?>(this, previous, suggestion));
        TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string?>(this, previous, suggestion));
    }

    private void SetDropDownState(bool open)
    {
        if (_isDropDownOpen == open)
        {
            return;
        }

        _isDropDownOpen = open;
        if (!open)
        {
            ClearSuggestions();
        }
    }

    private void ClearSuggestions()
    {
        _popupHost?.Children.Clear();
        _suggestionButtons?.Clear();
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        if (_input is not null && palette.Picker.ButtonForeground.Normal is not null)
        {
            _input.Foreground = palette.Picker.ButtonForeground.Normal;
        }
    }
}
