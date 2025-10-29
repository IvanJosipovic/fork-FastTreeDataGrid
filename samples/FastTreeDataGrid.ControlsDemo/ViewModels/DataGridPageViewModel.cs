using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class DataGridPageViewModel : IDisposable
{
    private readonly FastTreeDataGridFlatSource<ProductRow> _source;

    public DataGridPageViewModel()
    {
        var rows = CreateRows();
        _source = new FastTreeDataGridFlatSource<ProductRow>(rows, _ => Array.Empty<ProductRow>());
    }

    public static IReadOnlyList<string> StatusOptions { get; } = new[]
    {
        "New",
        "Active",
        "Backordered",
        "Discontinued",
    };

    public static IReadOnlyList<string> CategoryOptions { get; } = new[]
    {
        "Accessories",
        "Audio",
        "Computing",
        "Gaming",
        "Smart Home",
    };

    public IFastTreeDataGridSource Source => _source;

    public void Dispose()
    {
    }

    private static IReadOnlyList<ProductRow> CreateRows()
    {
        var random = new Random(17);
        return Enumerable.Range(1, 32)
            .Select(index =>
            {
                var category = CategoryOptions[index % CategoryOptions.Count];
                var price = Math.Round(29.99m + (decimal)random.NextDouble() * 470m, 2);
                var stock = random.Next(0, 250);
                var status = StatusOptions[(index + 1) % StatusOptions.Count];
                var rating = Math.Round(3.0 + random.NextDouble() * 2.0, 1);
                return new ProductRow(
                    id: index,
                    name: $"Product {index:D2}",
                    category,
                    price,
                    stock,
                    isActive: stock > 0,
                    status,
                    rating);
            })
            .ToArray();
    }
}

public sealed class ProductRow : IFastTreeDataGridValueProvider, INotifyPropertyChanged
{
    public const string KeyId = nameof(Id);
    public const string KeyName = nameof(Name);
    public const string KeyCategory = nameof(Category);
    public const string KeyPrice = nameof(Price);
    public const string KeyStock = nameof(Stock);
    public const string KeyIsActive = nameof(IsActive);
    public const string KeyStatus = nameof(Status);
    public const string KeyRating = nameof(Rating);

    private string _name;
    private string _category;
    private decimal _price;
    private int _stock;
    private bool _isActive;
    private string _status;
    private double _rating;

    public ProductRow(int id, string name, string category, decimal price, int stock, bool isActive, string status, double rating)
    {
        Id = id;
        _name = name;
        _category = category;
        _price = price;
        _stock = stock;
        _isActive = isActive;
        _status = status;
        _rating = rating;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public int Id { get; }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value, nameof(Name));
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value, nameof(Category));
    }

    public decimal Price
    {
        get => _price;
        set => SetField(ref _price, value, nameof(Price));
    }

    public int Stock
    {
        get => _stock;
        set => SetField(ref _stock, value, nameof(Stock));
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value, nameof(IsActive));
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value, nameof(Status));
    }

    public double Rating
    {
        get => _rating;
        set => SetField(ref _rating, value, nameof(Rating));
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyId => Id,
            KeyName => Name,
            KeyCategory => Category,
            KeyPrice => Price,
            KeyStock => Stock,
            KeyIsActive => IsActive,
            KeyStatus => Status,
            KeyRating => Rating,
            _ => null,
        };
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, propertyName));
    }
}
