using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Data;

public sealed class CatalogNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyName = "Catalog.Name";
    public const string KeyKind = "Catalog.Kind";
    public const string KeyPrice = "Catalog.Price";

    private readonly IReadOnlyList<CatalogNode> _children;

    private CatalogNode(string name, string kind, decimal price, string key, IReadOnlyList<CatalogNode> children)
    {
        Name = name;
        Kind = kind;
        Price = price;
        Key = key;
        _children = children;
    }

    public string Name { get; }

    public string Kind { get; }

    public decimal Price { get; }

    public string Key { get; }

    public IReadOnlyList<CatalogNode> Children => _children;

    public bool IsGroup => _children.Count > 0;

    public static CatalogNode CreateGroup(string key, string name, IReadOnlyList<CatalogNode> children) =>
        new(name, "Category", 0m, key, children);

    public static CatalogNode CreateItem(string key, string name, decimal price) =>
        new(name, "Item", price, key, Array.Empty<CatalogNode>());

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public object? GetValue(object? item, string key)
    {
        var culture = CultureInfo.CurrentCulture;
        return key switch
        {
            KeyName => Name,
            KeyKind => Kind,
            KeyPrice => IsGroup ? string.Empty : Price.ToString("C", culture),
            _ => string.Empty,
        };
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }
}
