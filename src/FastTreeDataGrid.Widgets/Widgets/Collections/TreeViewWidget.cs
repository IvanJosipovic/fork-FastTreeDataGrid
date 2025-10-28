using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class TreeViewWidget : SelectableItemsControlWidget
{
    internal static readonly StreamGeometry CollapsedGeometry = StreamGeometry.Parse("M 1,0 10,10 l -9,10 -1,-1 L 8,10 -0,1 Z");
    internal static readonly StreamGeometry ExpandedGeometry = StreamGeometry.Parse("M0,1 L10,10 20,1 19,0 10,8 1,0 Z");

    private double _indentWidth = 16;
    private double _glyphSize = 14;

    public TreeViewWidget()
    {
        base.ItemFactory = CreateContainer;
        ItemExtent = 28;
        Spacing = 1;
        var itemsPalette = WidgetFluentPalette.Current.Items;
        if (itemsPalette.TreeIndent > 0)
        {
            _indentWidth = itemsPalette.TreeIndent;
        }

        if (itemsPalette.TreeGlyphSize > 0)
        {
            _glyphSize = itemsPalette.TreeGlyphSize;
        }
    }

    public double IndentWidth
    {
        get => _indentWidth;
        set
        {
            var coerced = Math.Max(0, value);
            if (Math.Abs(_indentWidth - coerced) <= double.Epsilon)
            {
                return;
            }

            _indentWidth = coerced;
            OnItemPresentationChanged();
        }
    }

    public double GlyphSize
    {
        get => _glyphSize;
        set
        {
            var coerced = Math.Max(0, value);
            if (Math.Abs(_glyphSize - coerced) <= double.Epsilon)
            {
                return;
            }

            _glyphSize = coerced;
            OnItemPresentationChanged();
        }
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

    public Func<IFastTreeDataGridValueProvider?, object?, Widget?>? ItemContentFactory { get; set; }

    public void ExpandToLevel(int level)
    {
        if (level <= 0)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ExpandToLevel(level));
            return;
        }

        var index = 0;
        while (index < TotalItemCount)
        {
            if (!TryGetRow(index, out var row))
            {
                break;
            }

            if (row.Level < level && row.HasChildren && !row.IsExpanded)
            {
                ToggleExpansionAt(index);
            }

            index++;
        }
    }

    protected override void ApplyValue(object? value)
    {
        base.ApplyValue(value);
        base.ItemFactory = CreateContainer;

        if (value is TreeViewWidgetValue treeValue)
        {
            if (treeValue.ItemTemplate is not null && !ReferenceEquals(ItemTemplate, treeValue.ItemTemplate))
            {
                ItemTemplate = treeValue.ItemTemplate;
            }

            if (treeValue.ItemFactory is not null)
            {
                ItemContentFactory = treeValue.ItemFactory;
            }

            if (treeValue.IndentWidth.HasValue)
            {
                IndentWidth = treeValue.IndentWidth.Value;
            }

            if (treeValue.GlyphSize.HasValue)
            {
                GlyphSize = treeValue.GlyphSize.Value;
            }

            if (treeValue.SelectedIndex.HasValue)
            {
                SetSelectedIndexInternal(treeValue.SelectedIndex.Value, raiseEvents: true);
            }

            if (treeValue.SelectedItemSet)
            {
                SetSelectedItemInternal(treeValue.SelectedItem, raiseEvents: true);
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

        if (container is TreeViewItemContainer treeContainer && treeContainer.DisplayContent is null)
        {
            var content = BuildItemContent(row.ValueProvider, row.Item);
            treeContainer.DisplayContent = content;
        }
    }

    internal override void HandleContainerPointer(SelectableItemWidget container, WidgetPointerEvent e)
    {
        if (container is TreeViewItemContainer treeContainer && e.Kind == WidgetPointerEventKind.Pressed)
        {
            if (treeContainer.IsToggleHit(e.Position))
            {
                var index = GetContainerIndex(treeContainer);
                if (index >= 0)
                {
                    ToggleExpansionAt(index);
                }

                return;
            }
        }

        base.HandleContainerPointer(container, e);
    }

    private Widget? CreateContainer(IFastTreeDataGridValueProvider? provider, object? item)
    {
        var container = new TreeViewItemContainer(this)
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

        content ??= ListBoxWidget.CreateTextContent(item);
        content.UpdateValue(provider, item);
        return content;
    }
}

internal sealed class TreeViewItemContainer : ListBoxItemContainer
{
    private readonly TreeViewWidget _owner;
    private readonly SurfaceWidget _indentSpacer;
    private readonly SurfaceWidget _glyphHost;
    private readonly GeometryWidget _glyph;
    private readonly SurfaceWidget _contentHost;

    public TreeViewItemContainer(TreeViewWidget owner)
        : base(owner)
    {
        _owner = owner;

        _indentSpacer = new SurfaceWidget();
        _glyphHost = new SurfaceWidget
        {
            ClipToBounds = false
        };
        _glyph = new GeometryWidget
        {
            Stretch = Stretch.Uniform,
            ClipToBounds = false,
        };
        _glyphHost.Children.Add(_glyph);
        _contentHost = new SurfaceWidget();

        var layout = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        layout.Children.Add(_indentSpacer);
        layout.Children.Add(_glyphHost);
        layout.Children.Add(_contentHost);

        Children.Clear();
        Children.Add(layout);
    }

    internal bool IsToggleHit(Point position)
    {
        if (!HasChildren)
        {
            return false;
        }

        return _glyphHost.Bounds.Contains(position);
    }

    protected override IList<Widget> ContentHost => _contentHost.Children;

    protected override void OnRowBound(FastTreeDataGridRow row)
    {
        base.OnRowBound(row);

        _indentSpacer.DesiredWidth = Math.Max(0, _owner.IndentWidth * Math.Max(0, row.Level));
        _indentSpacer.DesiredHeight = double.NaN;

        _glyphHost.DesiredWidth = _owner.GlyphSize;
        _glyphHost.DesiredHeight = _owner.GlyphSize;

        if (row.HasChildren)
        {
            var geometry = row.IsExpanded ? TreeViewWidget.ExpandedGeometry : TreeViewWidget.CollapsedGeometry;
            var brush = WidgetFluentPalette.Current.Items?.TreeExpanderBrush ??
                        WidgetFluentPalette.Current.Selection.SelectedForeground ??
                        new ImmutableSolidColorBrush(Color.FromRgb(96, 96, 96));
            _glyph.SetGeometry(geometry, Stretch.Uniform, brush, stroke: null, padding: 0);
        }
        else
        {
            _glyph.SetGeometry(null);
        }
    }
}
