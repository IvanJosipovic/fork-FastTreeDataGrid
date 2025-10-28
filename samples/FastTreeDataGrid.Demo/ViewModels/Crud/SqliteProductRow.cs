using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class SqliteProductRow : IFastTreeDataGridValueProvider, IEditableObject, INotifyPropertyChanged
{
    public const string KeyId = "SqliteProduct.Id";
    public const string KeyName = "SqliteProduct.Name";
    public const string KeyCategory = "SqliteProduct.Category";
    public const string KeyPrice = "SqliteProduct.Price";

    private string _name;
    private string _categoryName;
    private decimal _price;
    private EditSnapshot? _snapshot;

    public SqliteProductRow(int id, int categoryId, string name, string categoryName, decimal price)
    {
        Id = id;
        CategoryId = categoryId;
        _name = name;
        _categoryName = categoryName;
        _price = price;
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }

    public int CategoryId { get; private set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value ?? string.Empty, KeyName, nameof(Name));
    }

    public string CategoryName
    {
        get => _categoryName;
        private set => SetProperty(ref _categoryName, value, KeyCategory, nameof(CategoryName));
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value, KeyPrice, nameof(Price));
    }

    public void Update(SqliteCategoryRecord category, string name, decimal price)
    {
        CategoryId = category.Id;
        CategoryName = category.Name;
        Name = name;
        Price = price;
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyId => Id,
            KeyName => Name,
            KeyCategory => CategoryName,
            KeyPrice => Price.ToString("C", CultureInfo.CurrentCulture),
            _ => string.Empty,
        };
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
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

    private bool SetProperty<T>(ref T storage, T value, string key, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        NotifyValueChanged(key);
        return true;
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
