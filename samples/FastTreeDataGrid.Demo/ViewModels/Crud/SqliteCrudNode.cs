using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class SqliteCrudNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyId = "SqliteCrud.Id";
    public const string KeyName = "SqliteCrud.Name";
    public const string KeyType = "SqliteCrud.Type";
    public const string KeyPrice = "SqliteCrud.Price";

    private readonly List<SqliteCrudNode> _children;
    private decimal _price;
    private string _name;

    private SqliteCrudNode(
        int id,
        string name,
        SqliteCrudNodeKind kind,
        int? categoryId,
        decimal price,
        IReadOnlyList<SqliteCrudNode> children)
    {
        Id = id;
        _name = name;
        Kind = kind;
        CategoryId = categoryId;
        _price = price;
        _children = new List<SqliteCrudNode>(children);
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public int Id { get; }

    public string Name
    {
        get => _name;
        private set
        {
            if (!string.Equals(_name, value, StringComparison.CurrentCulture))
            {
                _name = value;
                NotifyValueChanged(KeyName);
            }
        }
    }

    public SqliteCrudNodeKind Kind { get; }

    public int? CategoryId { get; }

    public IReadOnlyList<SqliteCrudNode> Children => _children;

    public bool IsGroup => Kind == SqliteCrudNodeKind.Category;

    public decimal Price
    {
        get => _price;
        private set
        {
            if (_price != value)
            {
                _price = value;
                NotifyValueChanged(KeyPrice);
            }
        }
    }

    public static SqliteCrudNode CreateCategory(int id, string name, IReadOnlyList<SqliteCrudNode> children) =>
        new(id, name, SqliteCrudNodeKind.Category, categoryId: null, price: 0m, children);

    public static SqliteCrudNode CreateProduct(int id, int categoryId, string name, decimal price) =>
        new(id, name, SqliteCrudNodeKind.Product, categoryId, price, Array.Empty<SqliteCrudNode>());

    public void Update(string name, decimal? price = null)
    {
        Name = name;
        if (price is { } value)
        {
            Price = value;
        }
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyId => Id,
            KeyName => Name,
            KeyType => Kind == SqliteCrudNodeKind.Category ? "Category" : "Product",
            KeyPrice => Kind == SqliteCrudNodeKind.Product ? Price.ToString("C", CultureInfo.CurrentCulture) : string.Empty,
            _ => string.Empty,
        };
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }
}

public enum SqliteCrudNodeKind
{
    Category,
    Product,
}
