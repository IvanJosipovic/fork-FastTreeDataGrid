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

public sealed class SqliteHierarchicalCrudViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SqliteCrudService _service;
    private readonly FastTreeDataGridFlatSource<SqliteCrudNode> _source;
    private readonly AsyncCommand _addCategoryCommand;
    private readonly AsyncCommand _addProductCommand;
    private readonly AsyncCommand _saveCommand;
    private readonly AsyncCommand _deleteCommand;
    private readonly AsyncCommand _refreshCommand;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private List<SqliteCrudNode> _nodes = new();
    private SqliteCrudNode? _selectedNode;
    private string _addCategoryName = string.Empty;
    private string _addProductName = string.Empty;
    private string _addProductPriceText = "99.00";
    private string _editName = string.Empty;
    private string _editPriceText = string.Empty;
    private string _status = "Ready";
    private string _selectedSummary = "Select a category or product to edit.";
    private bool _disposed;

    public SqliteHierarchicalCrudViewModel(SqliteCrudService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _source = new FastTreeDataGridFlatSource<SqliteCrudNode>(_nodes, node => node.Children);
        _addCategoryCommand = new AsyncCommand(_ => AddCategoryAsync(), _ => !string.IsNullOrWhiteSpace(AddCategoryName));
        _addProductCommand = new AsyncCommand(_ => AddProductAsync(), _ => CanAddProduct());
        _saveCommand = new AsyncCommand(_ => SaveAsync(), _ => CanSave());
        _deleteCommand = new AsyncCommand(_ => DeleteAsync(), _ => _selectedNode is not null);
        _refreshCommand = new AsyncCommand(_ => RefreshAsync(), _ => true);

        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public AsyncCommand AddCategoryCommand => _addCategoryCommand;

    public AsyncCommand AddProductCommand => _addProductCommand;

    public AsyncCommand SaveCommand => _saveCommand;

    public AsyncCommand DeleteCommand => _deleteCommand;

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

    public string AddProductName
    {
        get => _addProductName;
        set
        {
            if (SetProperty(ref _addProductName, value ?? string.Empty, nameof(AddProductName)))
            {
                _addProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string AddProductPriceText
    {
        get => _addProductPriceText;
        set
        {
            if (SetProperty(ref _addProductPriceText, value ?? string.Empty, nameof(AddProductPriceText)))
            {
                _addProductCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditName
    {
        get => _editName;
        set
        {
            if (SetProperty(ref _editName, value ?? string.Empty, nameof(EditName)))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditPriceText
    {
        get => _editPriceText;
        set
        {
            if (SetProperty(ref _editPriceText, value ?? string.Empty, nameof(EditPriceText)))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value, nameof(Status));
    }

    public string SelectedSummary
    {
        get => _selectedSummary;
        private set => SetProperty(ref _selectedSummary, value, nameof(SelectedSummary));
    }

    public bool IsCategorySelected => _selectedNode?.Kind == SqliteCrudNodeKind.Category;

    public bool IsProductSelected => _selectedNode?.Kind == SqliteCrudNodeKind.Product;

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
            Status = "Loading data...";
            var token = _lifetimeCts.Token;

            var categoriesTask = _service.GetCategoriesAsync(token);
            var productsTask = _service.GetProductsAsync(token);

            var categories = await categoriesTask.ConfigureAwait(false);
            var products = await productsTask.ConfigureAwait(false);

            var lookup = products
                .GroupBy(p => p.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var nodes = new List<SqliteCrudNode>();
            var productCount = 0;

            foreach (var category in categories)
            {
                if (!lookup.TryGetValue(category.Id, out var entries))
                {
                    entries = new List<SqliteProductRecord>();
                }

                var childNodes = new List<SqliteCrudNode>();
                foreach (var product in entries.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    childNodes.Add(SqliteCrudNode.CreateProduct(product.Id, product.CategoryId, product.Name, product.Price));
                    productCount++;
                }

                nodes.Add(SqliteCrudNode.CreateCategory(category.Id, category.Name, childNodes));
            }

            _nodes = nodes;
            _source.Reset(_nodes, preserveExpansion: true);
            _source.ExpandAllGroups();
            SelectedIndices = Array.Empty<int>();
            Status = $"Loaded {categories.Count} categories with {productCount} products.";
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
        Status = $"Added category \"{name}\".";
        AddCategoryName = string.Empty;
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task AddProductAsync()
    {
        if (!TryGetSelectedCategoryId(out var categoryId))
        {
            return;
        }

        var name = AddProductName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (!TryParsePrice(AddProductPriceText, out var price))
        {
            Status = "Enter a valid product price.";
            return;
        }

        await _service.CreateProductAsync(categoryId, name, price, _lifetimeCts.Token).ConfigureAwait(false);
        Status = $"Added product \"{name}\".";
        AddProductName = string.Empty;
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task SaveAsync()
    {
        var node = _selectedNode;
        if (node is null)
        {
            return;
        }

        var name = EditName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Status = "Name cannot be empty.";
            return;
        }

        if (node.Kind == SqliteCrudNodeKind.Category)
        {
            await _service.UpdateCategoryAsync(node.Id, name, _lifetimeCts.Token).ConfigureAwait(false);
            Status = $"Updated category \"{name}\".";
        }
        else
        {
            if (!TryParsePrice(EditPriceText, out var price))
            {
                Status = "Enter a valid price for the product.";
                return;
            }

            if (node.CategoryId is not int categoryId)
            {
                Status = "Unable to determine product category.";
                return;
            }

            await _service.UpdateProductAsync(node.Id, categoryId, name, price, _lifetimeCts.Token).ConfigureAwait(false);
            Status = $"Updated product \"{name}\".";
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task DeleteAsync()
    {
        var node = _selectedNode;
        if (node is null)
        {
            return;
        }

        if (node.Kind == SqliteCrudNodeKind.Category)
        {
            await _service.DeleteCategoryAsync(node.Id, _lifetimeCts.Token).ConfigureAwait(false);
            Status = $"Deleted category \"{node.Name}\".";
        }
        else
        {
            await _service.DeleteProductAsync(node.Id, _lifetimeCts.Token).ConfigureAwait(false);
            Status = $"Deleted product \"{node.Name}\".";
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    public async Task CommitEditAsync(SqliteCrudNode node, CancellationToken cancellationToken)
    {
        if (node is null)
        {
            return;
        }

        var normalizedName = string.IsNullOrWhiteSpace(node.Name) ? "Untitled" : node.Name.Trim();
        if (!string.Equals(node.Name, normalizedName, StringComparison.CurrentCulture))
        {
            node.Name = normalizedName;
        }

        if (node.Kind == SqliteCrudNodeKind.Category)
        {
            await _service.UpdateCategoryAsync(node.Id, normalizedName, cancellationToken).ConfigureAwait(false);
            Status = $"Saved category \"{normalizedName}\".";
            return;
        }

        if (node.CategoryId is not { } categoryId)
        {
            Status = "Cannot update product without a category.";
            return;
        }

        var price = node.Price < 0 ? 0m : decimal.Round(node.Price, 2);
        if (node.Price != price)
        {
            node.Price = price;
        }

        await _service.UpdateProductAsync(node.Id, categoryId, normalizedName, price, cancellationToken).ConfigureAwait(false);
        Status = $"Saved product \"{normalizedName}\".";
    }

    private void UpdateSelection()
    {
        SqliteCrudNode? selected = null;

        if (_selectedIndices.Count > 0)
        {
            var index = _selectedIndices[^1];
            if (_source.TryGetMaterializedRow(index, out var row) && row.Item is SqliteCrudNode node)
            {
                selected = node;
            }
        }

        _selectedNode = selected;

        if (selected is null)
        {
            SelectedSummary = "Select a category to add products, or select a product to edit.";
            EditName = string.Empty;
            EditPriceText = string.Empty;
        }
        else
        {
            SelectedSummary = selected.Kind == SqliteCrudNodeKind.Category
                ? $"Category #{selected.Id}"
                : $"Product #{selected.Id}";
            EditName = selected.Name;
            EditPriceText = selected.Kind == SqliteCrudNodeKind.Product
                ? selected.Price.ToString("F2", CultureInfo.CurrentCulture)
                : string.Empty;
        }

        RaiseSelectionPropertiesChanged();
        _addProductCommand.RaiseCanExecuteChanged();
        _saveCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
    }

    private void RaiseSelectionPropertiesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCategorySelected)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProductSelected)));
    }

    private bool TryGetSelectedCategoryId(out int categoryId)
    {
        categoryId = 0;
        var node = _selectedNode;
        if (node is null)
        {
            return false;
        }

        if (node.Kind == SqliteCrudNodeKind.Category)
        {
            categoryId = node.Id;
            return true;
        }

        if (node.CategoryId is { } parentId)
        {
            categoryId = parentId;
            return true;
        }

        return false;
    }

    private bool CanAddProduct()
    {
        if (!TryGetSelectedCategoryId(out _))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(AddProductName))
        {
            return false;
        }

        return TryParsePrice(AddProductPriceText, out var price) && price >= 0m;
    }

    private bool CanSave()
    {
        var node = _selectedNode;
        if (node is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            return false;
        }

        if (node.Kind == SqliteCrudNodeKind.Product)
        {
            return TryParsePrice(EditPriceText, out _);
        }

        return true;
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
