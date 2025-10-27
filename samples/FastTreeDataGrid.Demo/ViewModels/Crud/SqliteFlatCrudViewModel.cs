using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Demo.ViewModels;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class SqliteFlatCrudViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SqliteCrudService _service;
    private readonly FastTreeDataGridFlatSource<SqliteProductRow> _source;
    private readonly AsyncCommand _addCategoryCommand;
    private readonly AsyncCommand _addProductCommand;
    private readonly AsyncCommand _saveProductCommand;
    private readonly AsyncCommand _deleteProductCommand;
    private readonly AsyncCommand _refreshCommand;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private List<SqliteProductRow> _rows = new();
    private IReadOnlyList<SqliteCategoryRecord> _categories = Array.Empty<SqliteCategoryRecord>();
    private SqliteProductRow? _selectedRow;
    private SqliteCategoryRecord? _newProductCategory;
    private SqliteCategoryRecord? _editProductCategory;
    private string _addCategoryName = string.Empty;
    private string _newProductName = string.Empty;
    private string _newProductPriceText = "29.00";
    private string _editProductName = string.Empty;
    private string _editProductPriceText = string.Empty;
    private string _status = "Ready";
    private bool _disposed;

    public SqliteFlatCrudViewModel(SqliteCrudService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _source = new FastTreeDataGridFlatSource<SqliteProductRow>(_rows, _ => Array.Empty<SqliteProductRow>());

        _addCategoryCommand = new AsyncCommand(_ => AddCategoryAsync(), _ => !string.IsNullOrWhiteSpace(AddCategoryName));
        _addProductCommand = new AsyncCommand(_ => AddProductAsync(), _ => CanAddProduct());
        _saveProductCommand = new AsyncCommand(_ => SaveProductAsync(), _ => CanSaveProduct());
        _deleteProductCommand = new AsyncCommand(_ => DeleteProductAsync(), _ => _selectedRow is not null);
        _refreshCommand = new AsyncCommand(_ => RefreshAsync(), _ => true);

        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public AsyncCommand AddCategoryCommand => _addCategoryCommand;

    public AsyncCommand AddProductCommand => _addProductCommand;

    public AsyncCommand SaveProductCommand => _saveProductCommand;

    public AsyncCommand DeleteProductCommand => _deleteProductCommand;

    public AsyncCommand RefreshCommand => _refreshCommand;

    public IReadOnlyList<int> SelectedIndices
    {
        get => _selectedIndices;
        set
        {
            var normalized = value ?? Array.Empty<int>();
            if (SetProperty(ref _selectedIndices, normalized, nameof(SelectedIndices)))
            {
                UpdateSelection();
            }
        }
    }

    public IReadOnlyList<SqliteCategoryRecord> Categories
    {
        get => _categories;
        private set => SetProperty(ref _categories, value, nameof(Categories));
    }

    public SqliteCategoryRecord? NewProductCategory
    {
        get => _newProductCategory;
        set
        {
            if (!Equals(_newProductCategory, value))
            {
                _newProductCategory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewProductCategory)));
                _addProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public SqliteCategoryRecord? EditProductCategory
    {
        get => _editProductCategory;
        set
        {
            if (!Equals(_editProductCategory, value))
            {
                _editProductCategory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditProductCategory)));
                _saveProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string AddCategoryName
    {
        get => _addCategoryName;
        set
        {
            if (SetProperty(ref _addCategoryName, value ?? string.Empty, nameof(AddCategoryName)))
            {
                _addCategoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewProductName
    {
        get => _newProductName;
        set
        {
            if (SetProperty(ref _newProductName, value ?? string.Empty, nameof(NewProductName)))
            {
                _addProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewProductPriceText
    {
        get => _newProductPriceText;
        set
        {
            if (SetProperty(ref _newProductPriceText, value ?? string.Empty, nameof(NewProductPriceText)))
            {
                _addProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditProductName
    {
        get => _editProductName;
        set
        {
            if (SetProperty(ref _editProductName, value ?? string.Empty, nameof(EditProductName)))
            {
                _saveProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditProductPriceText
    {
        get => _editProductPriceText;
        set
        {
            if (SetProperty(ref _editProductPriceText, value ?? string.Empty, nameof(EditProductPriceText)))
            {
                _saveProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value, nameof(Status));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
    }

    private async Task RefreshAsync()
    {
        try
        {
            Status = "Loading products...";
            var token = _lifetimeCts.Token;

            var categoriesTask = _service.GetCategoriesAsync(token);
            var productsTask = _service.GetProductsAsync(token);

            var categories = await categoriesTask.ConfigureAwait(false);
            var products = await productsTask.ConfigureAwait(false);

            Categories = categories;
            if (categories.Count > 0 && NewProductCategory is null)
            {
                NewProductCategory = categories[0];
            }

            var lookup = categories.ToDictionary(c => c.Id, c => c);
            var rows = new List<SqliteProductRow>();

            foreach (var product in products.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                lookup.TryGetValue(product.CategoryId, out var category);
                var categoryName = category.Name;
                rows.Add(new SqliteProductRow(product.Id, product.CategoryId, product.Name, categoryName, product.Price));
            }

            _rows = rows;
            _source.Reset(_rows, preserveExpansion: false);
            SelectedIndices = Array.Empty<int>();
            Status = $"Loaded {rows.Count} products.";
        }
        catch (OperationCanceledException)
        {
            Status = "Loading cancelled.";
        }
    }

    private async Task AddCategoryAsync()
    {
        var name = AddCategoryName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await _service.CreateCategoryAsync(name, _lifetimeCts.Token).ConfigureAwait(false);
        AddCategoryName = string.Empty;
        Status = $"Added category \"{name}\".";
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task AddProductAsync()
    {
        if (NewProductCategory is null)
        {
            Status = "Select a category for the new product.";
            return;
        }

        var name = NewProductName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Status = "Product name cannot be empty.";
            return;
        }

        if (!TryParsePrice(NewProductPriceText, out var price))
        {
            Status = "Enter a valid price.";
            return;
        }

        await _service.CreateProductAsync(NewProductCategory.Value.Id, name, price, _lifetimeCts.Token).ConfigureAwait(false);
        Status = $"Added product \"{name}\".";
        NewProductName = string.Empty;
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task SaveProductAsync()
    {
        var row = _selectedRow;
        if (row is null || EditProductCategory is null)
        {
            return;
        }

        var name = EditProductName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Status = "Product name cannot be empty.";
            return;
        }

        if (!TryParsePrice(EditProductPriceText, out var price))
        {
            Status = "Enter a valid price.";
            return;
        }

        await _service.UpdateProductAsync(row.Id, EditProductCategory.Value.Id, name, price, _lifetimeCts.Token).ConfigureAwait(false);
        Status = $"Updated product \"{name}\".";
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task DeleteProductAsync()
    {
        var row = _selectedRow;
        if (row is null)
        {
            return;
        }

        await _service.DeleteProductAsync(row.Id, _lifetimeCts.Token).ConfigureAwait(false);
        Status = $"Deleted product \"{row.Name}\".";
        await RefreshAsync().ConfigureAwait(false);
    }

    private bool CanAddProduct()
    {
        if (NewProductCategory is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(NewProductName))
        {
            return false;
        }

        return TryParsePrice(NewProductPriceText, out _);
    }

    private bool CanSaveProduct()
    {
        if (_selectedRow is null)
        {
            return false;
        }

        if (EditProductCategory is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditProductName))
        {
            return false;
        }

        return TryParsePrice(EditProductPriceText, out _);
    }

    private void UpdateSelection()
    {
        SqliteProductRow? selected = null;
        if (_selectedIndices.Count > 0)
        {
            var index = _selectedIndices[^1];
            if (_source.TryGetMaterializedRow(index, out var row) && row.Item is SqliteProductRow product)
            {
                selected = product;
            }
        }

        _selectedRow = selected;
        if (selected is null)
        {
            EditProductName = string.Empty;
            EditProductPriceText = string.Empty;
            EditProductCategory = null;
        }
        else
        {
            EditProductName = selected.Name;
            EditProductPriceText = selected.Price.ToString("F2", CultureInfo.CurrentCulture);
            EditProductCategory = Categories.FirstOrDefault(c => c.Id == selected.CategoryId);
        }

        _saveProductCommand.RaiseCanExecuteChanged();
        _deleteProductCommand.RaiseCanExecuteChanged();
    }

    private static bool TryParsePrice(string? text, out decimal price) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out price) && price >= 0m;

    private bool SetProperty<T>(ref T storage, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
