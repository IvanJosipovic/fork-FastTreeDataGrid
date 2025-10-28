using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ListBoxWidget : SelectableItemsControlWidget
{
    private Func<IFastTreeDataGridValueProvider?, object?, Widget?>? _contentFactory;

    public ListBoxWidget()
    {
        base.ItemFactory = CreateContainer;
        ItemExtent = 36;
        Spacing = 2;
    }

    public new IWidgetTemplate? ItemTemplate
    {
        get => base.ItemTemplate;
        set
        {
            if (ReferenceEquals(base.ItemTemplate, value))
            {
                return;
            }

            base.ItemTemplate = value;
            OnItemPresentationChanged();
        }
    }

    public Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ItemContentFactory
    {
        get => _contentFactory;
        set
        {
            if (ReferenceEquals(_contentFactory, value))
            {
                return;
            }

            _contentFactory = value;
            OnItemPresentationChanged();
        }
    }

    protected override void ApplyValue(object? value)
    {
        base.ApplyValue(value);
        base.ItemFactory = CreateContainer;

        if (value is ListBoxWidgetValue listValue)
        {
            if (listValue.ItemTemplate is not null && !ReferenceEquals(ItemTemplate, listValue.ItemTemplate))
            {
                ItemTemplate = listValue.ItemTemplate;
            }

            if (listValue.ItemFactory is not null)
            {
                ItemContentFactory = listValue.ItemFactory;
            }

            if (listValue.SelectedIndex.HasValue)
            {
                SetSelectedIndexInternal(listValue.SelectedIndex.Value, raiseEvents: true);
            }

            if (listValue.SelectedItemSet)
            {
                SetSelectedItemInternal(listValue.SelectedItem, raiseEvents: true);
            }
        }
        else if (value is ItemsControlWidgetValue generic && generic.ItemFactory is not null)
        {
            ItemContentFactory = generic.ItemFactory;
        }
    }

    protected override void OnItemPresentationChanged()
    {
        base.OnItemPresentationChanged();
        base.ItemFactory = CreateContainer;
    }

    internal override void OnContainerPrepared(SelectableItemWidget container, FastTreeDataGridRow row, int index)
    {
        base.OnContainerPrepared(container, row, index);

        if (container is ListBoxItemContainer listContainer && listContainer.DisplayContent is null)
        {
            var content = BuildItemContent(row.ValueProvider, row.Item);
            listContainer.DisplayContent = content;
        }
    }

    private Widget? CreateContainer(IFastTreeDataGridValueProvider? provider, object? item)
    {
        var container = new ListBoxItemContainer(this)
        {
            DesiredWidth = double.NaN,
            DesiredHeight = ItemExtent,
        };

        var content = BuildItemContent(provider, item);
        container.DisplayContent = content;
        return container;
    }

    private Widget BuildItemContent(IFastTreeDataGridValueProvider? provider, object? item)
    {
        Widget? content = null;

        if (ItemContentFactory is { } factory)
        {
            content = factory(provider, item);
        }
        else if (base.ItemTemplate is { } template)
        {
            content = template.Build();
        }

        content ??= CreateTextContent(item);
        content.UpdateValue(provider, item);
        return content;
    }

    internal static Widget CreateTextContent(object? item)
    {
        var textPalette = WidgetFluentPalette.Current.Text;
        var typography = textPalette.Typography;
        var body = typography.Body;

        var widget = new FormattedTextWidget
        {
            EmSize = body.FontSize > 0 ? body.FontSize : 13,
            FontWeight = body.FontWeight,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        widget.FontFamily = body.FontFamily;

        if (textPalette.Foreground.Normal is { } normal)
        {
            widget.Foreground = normal;
        }

        widget.SetText(item?.ToString() ?? string.Empty);
        return widget;
    }
}

internal class ListBoxItemContainer : SelectableItemsControlWidget.SelectableItemWidget
{
    private Widget? _displayContent;

    public ListBoxItemContainer(SelectableItemsControlWidget host)
        : base(host)
    {
        ClipToBounds = true;

        var layout = WidgetFluentPalette.Current.Layout;
        Padding = layout.ContentPadding == default ? new Thickness(8, 4, 8, 4) : layout.ContentPadding;
        CornerRadius = layout.ControlCornerRadius == default ? new CornerRadius(4) : layout.ControlCornerRadius;
    }

    public Widget? DisplayContent
    {
        get => _displayContent;
        set
        {
            if (ReferenceEquals(_displayContent, value))
            {
                return;
            }

            _displayContent = value;
            SetDisplayContent(value);
        }
    }

    protected override void OnRowBound(FastTreeDataGridRow row)
    {
        if (_displayContent is Widget content)
        {
            content.UpdateValue(row.ValueProvider, row.Item);
        }
    }

    protected override void OnSelectionChanged(bool isSelected)
    {
        UpdateAppearance();
    }

    protected override void OnPointerOverChanged(bool isPointerOver)
    {
        UpdateAppearance();
    }

    protected virtual IList<Widget> ContentHost => Children;

    protected virtual void SetDisplayContent(Widget? content)
    {
        var host = ContentHost;
        host.Clear();
        if (content is not null)
        {
            host.Add(content);
        }
    }

    private void UpdateAppearance()
    {
        var palette = WidgetFluentPalette.Current;
        var items = palette.Items;
        var selection = palette.Selection;

        ImmutableSolidColorBrush? background = null;
        ImmutableSolidColorBrush? foreground = null;

        if (!IsEnabled)
        {
            background = items?.DisabledBackground ?? selection.InactiveBackground;
            foreground = items?.DisabledForeground ?? selection.InactiveForeground;
        }
        else if (IsSelected)
        {
            background = selection.SelectedBackground ?? items?.SelectedBackground ?? items?.ItemBackground;
            foreground = selection.SelectedForeground ?? items?.SelectedForeground ?? items?.ItemForeground;
        }
        else if (IsPointerOver)
        {
            background = selection.PointerOverBackground ?? items?.PointerOverBackground ?? items?.ItemBackground;
            foreground = selection.PointerOverForeground ?? items?.PointerOverForeground ?? items?.ItemForeground;
        }
        else
        {
            background = items?.ItemBackground;
            foreground = items?.ItemForeground;
        }

        Background = background;

        if (_displayContent is Widget content && foreground is not null)
        {
            content.Foreground = foreground;
        }
    }
}
