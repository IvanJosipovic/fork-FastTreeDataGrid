using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class SqliteCrudNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup, IEditableObject, INotifyPropertyChanged
{
    public const string KeyId = "SqliteCrud.Id";
    public const string KeyName = "SqliteCrud.Name";
    public const string KeyType = "SqliteCrud.Type";
    public const string KeyPrice = "SqliteCrud.Price";

    private readonly List<SqliteCrudNode> _children;
    private decimal _price;
    private string _name;
    private EditSnapshot? _snapshot;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value ?? string.Empty, KeyName, nameof(Name));
    }

    public SqliteCrudNodeKind Kind { get; }

    public int? CategoryId { get; }

    public IReadOnlyList<SqliteCrudNode> Children => _children;

    public bool IsGroup => Kind == SqliteCrudNodeKind.Category;

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value, KeyPrice, nameof(Price));
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

    public void BeginEdit()
    {
        _snapshot ??= new EditSnapshot(_name, _price);
    }

    public void EndEdit()
    {
        _snapshot = null;
    }

    public void CancelEdit()
    {
        if (_snapshot is not { } snapshot)
        {
            return;
        }

        SetProperty(ref _name, snapshot.Name, KeyName, nameof(Name));
        SetProperty(ref _price, snapshot.Price, KeyPrice, nameof(Price));
        _snapshot = null;
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }

    private bool SetProperty<T>(ref T storage, T value, string key, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        NotifyPropertyChanged(propertyName);
        NotifyValueChanged(key);
        return true;
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly struct EditSnapshot
    {
        public EditSnapshot(string name, decimal price)
        {
            Name = name;
            Price = price;
        }

        public string Name { get; }

        public decimal Price { get; }
    }
}

public enum SqliteCrudNodeKind
{
    Category,
    Product,
}
