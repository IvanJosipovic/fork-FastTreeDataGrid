using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class DataGridAdapterPageViewModel
{
    private readonly Random _random = new(51);

    public DataGridAdapterPageViewModel()
    {
        Products = new ObservableCollection<InventoryProduct>(CreateProducts());
    }

    public ObservableCollection<InventoryProduct> Products { get; }

    public IReadOnlyList<string> StatusOptions => DataGridPageViewModel.StatusOptions;

    public IReadOnlyList<string> CategoryOptions => DataGridPageViewModel.CategoryOptions;

    private IEnumerable<InventoryProduct> CreateProducts()
    {
        return Enumerable.Range(1, 18)
            .Select(index =>
            {
                var category = CategoryOptions[(index + 3) % CategoryOptions.Count];
                var status = StatusOptions[index % StatusOptions.Count];
                var price = Math.Round(49.99m + (decimal)_random.NextDouble() * 350m, 2);
                var stock = _random.Next(0, 175);
                var rating = Math.Round(2.5 + _random.NextDouble() * 2.5, 1);
                return new InventoryProduct(
                    id: 1000 + index,
                    name: $"Catalog item {index:D2}",
                    category,
                    price,
                    stock,
                    status,
                    rating);
            });
    }
}

public sealed class InventoryProduct : INotifyPropertyChanged
{
    public const string KeyId = nameof(Id);
    public const string KeyName = nameof(Name);
    public const string KeyCategory = nameof(Category);
    public const string KeyPrice = nameof(Price);
    public const string KeyStock = nameof(Stock);
    public const string KeyStatus = nameof(Status);
    public const string KeyRating = nameof(Rating);

    private string _name;
    private string _category;
    private decimal _price;
    private int _stock;
    private string _status;
    private double _rating;

    public InventoryProduct(int id, string name, string category, decimal price, int stock, string status, double rating)
    {
        Id = id;
        _name = name;
        _category = category;
        _price = price;
        _stock = stock;
        _status = status;
        _rating = rating;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private void SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
