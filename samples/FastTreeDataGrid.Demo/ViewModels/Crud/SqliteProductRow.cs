using System;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class SqliteProductRow : IFastTreeDataGridValueProvider
{
    public const string KeyId = "SqliteProduct.Id";
    public const string KeyName = "SqliteProduct.Name";
    public const string KeyCategory = "SqliteProduct.Category";
    public const string KeyPrice = "SqliteProduct.Price";

    private string _name;
    private string _categoryName;
    private decimal _price;

    public SqliteProductRow(int id, int categoryId, string name, string categoryName, decimal price)
    {
        Id = id;
        CategoryId = categoryId;
        _name = name;
        _categoryName = categoryName;
        _price = price;
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public int Id { get; }

    public int CategoryId { get; private set; }

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

    public string CategoryName
    {
        get => _categoryName;
        private set
        {
            if (!string.Equals(_categoryName, value, StringComparison.CurrentCulture))
            {
                _categoryName = value;
                NotifyValueChanged(KeyCategory);
            }
        }
    }

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
}
