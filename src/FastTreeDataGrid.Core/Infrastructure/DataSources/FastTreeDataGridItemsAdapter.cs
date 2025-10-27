using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.Templates;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Adapter that converts hierarchical data described by <see cref="IEnumerable"/> sources,
/// property paths, or <see cref="ITreeDataTemplate"/> instances into an <see cref="IFastTreeDataGridSource"/>.
/// </summary>
public sealed class FastTreeDataGridItemsAdapter : AvaloniaObject
{
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<FastTreeDataGridItemsAdapter, IEnumerable?>(nameof(Items));

    public static readonly StyledProperty<ITreeDataTemplate?> TreeTemplateProperty =
        AvaloniaProperty.Register<FastTreeDataGridItemsAdapter, ITreeDataTemplate?>(nameof(TreeTemplate));

    public static readonly StyledProperty<string?> ChildrenMemberPathProperty =
        AvaloniaProperty.Register<FastTreeDataGridItemsAdapter, string?>(nameof(ChildrenMemberPath));

    public static readonly StyledProperty<Func<object?, IEnumerable?>?> ChildrenSelectorProperty =
        AvaloniaProperty.Register<FastTreeDataGridItemsAdapter, Func<object?, IEnumerable?>?>(nameof(ChildrenSelector));

    public static readonly StyledProperty<string?> KeyMemberPathProperty =
        AvaloniaProperty.Register<FastTreeDataGridItemsAdapter, string?>(nameof(KeyMemberPath));

    public static readonly StyledProperty<Func<object?, string?>?> KeySelectorProperty =
        AvaloniaProperty.Register<FastTreeDataGridItemsAdapter, Func<object?, string?>?>(nameof(KeySelector));

    public static readonly DirectProperty<FastTreeDataGridItemsAdapter, IFastTreeDataGridSource?> SourceProperty =
        AvaloniaProperty.RegisterDirect<FastTreeDataGridItemsAdapter, IFastTreeDataGridSource?>(
            nameof(Source),
            o => o.Source,
            (o, v) => o.Source = v);

    private readonly Dictionary<(Type type, string path), Func<object?, IEnumerable?>> _childrenSelectorCache = new();
    private readonly Dictionary<(Type type, string path), Func<object?, string?>> _keySelectorCache = new();
    private IFastTreeDataGridSource? _source;

    static FastTreeDataGridItemsAdapter()
    {
        ItemsProperty.Changed.AddClassHandler<FastTreeDataGridItemsAdapter>((adapter, _) => adapter.Rebuild());
        TreeTemplateProperty.Changed.AddClassHandler<FastTreeDataGridItemsAdapter>((adapter, _) => adapter.Rebuild());
        ChildrenMemberPathProperty.Changed.AddClassHandler<FastTreeDataGridItemsAdapter>((adapter, _) => adapter.Rebuild());
        ChildrenSelectorProperty.Changed.AddClassHandler<FastTreeDataGridItemsAdapter>((adapter, _) => adapter.Rebuild());
        KeyMemberPathProperty.Changed.AddClassHandler<FastTreeDataGridItemsAdapter>((adapter, _) => adapter.Rebuild());
        KeySelectorProperty.Changed.AddClassHandler<FastTreeDataGridItemsAdapter>((adapter, _) => adapter.Rebuild());
    }

    /// <summary>
    /// Gets or sets the enumerable providing root items.
    /// </summary>
    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the tree data template that describes how to retrieve children for each item.
    /// </summary>
    public ITreeDataTemplate? TreeTemplate
    {
        get => GetValue(TreeTemplateProperty);
        set => SetValue(TreeTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the member path that exposes child collections for a given item.
    /// </summary>
    public string? ChildrenMemberPath
    {
        get => GetValue(ChildrenMemberPathProperty);
        set => SetValue(ChildrenMemberPathProperty, value);
    }

    /// <summary>
    /// Gets or sets a delegate that returns the child collection for the provided item.
    /// </summary>
    public Func<object?, IEnumerable?>? ChildrenSelector
    {
        get => GetValue(ChildrenSelectorProperty);
        set => SetValue(ChildrenSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the member path used to produce stable item keys.
    /// </summary>
    public string? KeyMemberPath
    {
        get => GetValue(KeyMemberPathProperty);
        set => SetValue(KeyMemberPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the delegate that produces stable item keys.
    /// </summary>
    public Func<object?, string?>? KeySelector
    {
        get => GetValue(KeySelectorProperty);
        set => SetValue(KeySelectorProperty, value);
    }

    /// <summary>
    /// Gets the generated data grid source.
    /// </summary>
    public IFastTreeDataGridSource? Source
    {
        get => _source;
        private set => SetAndRaise(SourceProperty, ref _source, value);
    }

    private void Rebuild()
    {
        var items = Items;
        if (items is null)
        {
            Source = null;
            return;
        }

        try
        {
            var keySelector = BuildKeySelector();
            if (TreeTemplate is ITreeDataTemplate template)
            {
                Source = FastTreeDataGridSourceFactory.FromTreeDataTemplate(
                    items,
                    template,
                    BuildChildrenSelector(),
                    keySelector);
            }
            else
            {
                var selector = BuildChildrenSelector();
                if (selector is null)
                {
                    Source = FastTreeDataGridSourceFactory.FromEnumerable(
                        items,
                        _ => Array.Empty<object?>(),
                        keySelector);
                }
                else
                {
                    Source = FastTreeDataGridSourceFactory.FromEnumerable(
                        items,
                        selector,
                        keySelector);
                }
            }
        }
        catch
        {
            Source = null;
        }
    }

    private Func<object?, IEnumerable?>? BuildChildrenSelector()
    {
        if (ChildrenSelector is { } selector)
        {
            return selector;
        }

        var path = ChildrenMemberPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return ResolveChildrenSelector(path!);
    }

    private Func<object?, string?>? BuildKeySelector()
    {
        if (KeySelector is { } selector)
        {
            return selector;
        }

        var path = KeyMemberPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return ResolveKeySelector(path!);
    }

    private Func<object?, IEnumerable?> ResolveChildrenSelector(string memberPath)
    {
        return value =>
        {
            if (value is null)
            {
                return Array.Empty<object?>();
            }

            var type = value.GetType();
            if (!_childrenSelectorCache.TryGetValue((type, memberPath), out var selector))
            {
                selector = CreateChildrenSelector(type, memberPath);
                _childrenSelectorCache[(type, memberPath)] = selector;
            }

            return selector(value);
        };
    }

    private Func<object?, string?> ResolveKeySelector(string memberPath)
    {
        return value =>
        {
            if (value is null)
            {
                return null;
            }

            var type = value.GetType();
            if (!_keySelectorCache.TryGetValue((type, memberPath), out var selector))
            {
                selector = CreateKeySelector(type, memberPath);
                _keySelectorCache[(type, memberPath)] = selector;
            }

            return selector(value);
        };
    }

    private static Func<object?, IEnumerable?> CreateChildrenSelector(Type type, string memberPath)
    {
        var property = type.GetProperty(memberPath, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
        {
            return _ => Array.Empty<object?>();
        }

        return obj =>
        {
            if (obj is null)
            {
                return Array.Empty<object?>();
            }

            var result = property.GetValue(obj);
            return result as IEnumerable ?? Array.Empty<object?>();
        };
    }

    private static Func<object?, string?> CreateKeySelector(Type type, string memberPath)
    {
        var property = type.GetProperty(memberPath, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
        {
            return _ => null;
        }

        return obj =>
        {
            if (obj is null)
            {
                return null;
            }

            var result = property.GetValue(obj);
            return result?.ToString();
        };
    }
}
