using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Extensibility;

/// <summary>
/// Custom virtualization provider used by the extensibility sample. It wraps <see cref="InventoryDataService"/>,
/// supplies lightweight placeholder rows, and eagerly updates cached rows when the backing service raises mutation events.
/// </summary>
public sealed class InventoryVirtualizationProvider : IFastTreeDataVirtualizationProvider
{
    private readonly InventoryDataService _service;
    private readonly FastTreeDataGridVirtualizationSettings _settings;
    private readonly Dictionary<int, InventoryEntry> _cache = new();
    private bool _disposed;
    private int _count;
    private bool _isInitialized;

    public InventoryVirtualizationProvider(InventoryDataService service, FastTreeDataGridVirtualizationSettings settings)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _service.Mutated += OnServiceMutated;
    }

    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;
    public event EventHandler<FastTreeDataGridCountChangedEventArgs>? CountChanged;

    public bool IsInitialized => _isInitialized;

    public bool SupportsMutations => true;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        _count = await _service.GetCountAsync(cancellationToken).ConfigureAwait(false);
        _isInitialized = true;
    }

    public async ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        return _count;
    }

    public async ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request.Count <= 0 || request.StartIndex >= _count)
        {
            return FastTreeDataGridPageResult.Empty;
        }

        var start = Math.Max(0, request.StartIndex);
        var endExclusive = Math.Min(_count, start + request.Count);
        var pageLength = Math.Max(0, endExclusive - start);
        if (pageLength == 0)
        {
            return FastTreeDataGridPageResult.Empty;
        }

        var rows = new List<FastTreeDataGridRow>(pageLength);
        var needsFetch = false;

        lock (_cache)
        {
            for (var index = start; index < endExclusive; index++)
            {
                if (!_cache.TryGetValue(index, out var entry))
                {
                    entry = CreateEntry(index);
                    _cache[index] = entry;
                    needsFetch = true;
                }

                rows.Add(entry.Row);
                needsFetch |= !entry.Provider.IsMaterialized;
            }
        }

        if (needsFetch)
        {
            var records = await _service.GetPageAsync(start, pageLength, cancellationToken).ConfigureAwait(false);
            ApplyPage(start, records);
        }

        return new FastTreeDataGridPageResult(rows, Array.Empty<int>(), completion: null, cancellation: null);
    }

    public async ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request.Count <= 0 || request.StartIndex >= _count)
        {
            return;
        }

        var start = Math.Max(0, request.StartIndex);
        var endExclusive = Math.Min(_count, start + request.Count);
        var pageLength = Math.Max(0, endExclusive - start);
        if (pageLength == 0)
        {
            return;
        }

        var records = await _service.GetPageAsync(start, pageLength, cancellationToken).ConfigureAwait(false);
        ApplyPage(start, records);
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        lock (_cache)
        {
            if (request.Kind == FastTreeDataGridInvalidationKind.Full)
            {
                _cache.Clear();
            }
            else if (request.HasRange)
            {
                for (var i = 0; i < request.Count; i++)
                {
                    _cache.Remove(request.StartIndex + i);
                }
            }
        }

        return Task.CompletedTask;
    }

    public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(index, out var entry) && entry.Provider.IsMaterialized)
            {
                row = entry.Row;
                return true;
            }
        }

        row = default!;
        return false;
    }

    public bool IsPlaceholder(int index)
    {
        lock (_cache)
        {
            return !_cache.TryGetValue(index, out var entry) || !entry.Provider.IsMaterialized;
        }
    }

    public async Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        await Task.CompletedTask;
    }

    public async Task<int> LocateRowIndexAsync(object? item, CancellationToken cancellationToken)
    {
        if (item is InventoryRowValueProvider provider)
        {
            return provider.Id > 0 ? provider.Id - 1 : await LocateByIdAsync(provider.Id, cancellationToken).ConfigureAwait(false);
        }

        if (item is InventoryRecord record)
        {
            return await LocateByIdAsync(record.Id, cancellationToken).ConfigureAwait(false);
        }

        return -1;
    }

    public async Task CreateAsync(object viewModel, CancellationToken cancellationToken)
    {
        var record = ExtractRecord(viewModel);
        await _service.CreateAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(object viewModel, CancellationToken cancellationToken)
    {
        var record = ExtractRecord(viewModel);
        await _service.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(object viewModel, CancellationToken cancellationToken)
    {
        var record = ExtractRecord(viewModel);
        if (record.Id <= 0)
        {
            return;
        }

        await _service.DeleteAsync(record.Id, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _service.Mutated -= OnServiceMutated;
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void ApplyPage(int startIndex, IReadOnlyList<InventoryRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        lock (_cache)
        {
            for (var i = 0; i < records.Count; i++)
            {
                var index = startIndex + i;
                if (index < 0)
                {
                    continue;
                }

                if (!_cache.TryGetValue(index, out var entry))
                {
                    entry = CreateEntry(index);
                    _cache[index] = entry;
                }

                var provider = entry.Provider;
                var wasMaterialized = provider.IsMaterialized;
                provider.Apply(records[i]);

                if (!wasMaterialized)
                {
                    RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(index, entry.Row));
                }
            }
        }
    }

    private InventoryEntry CreateEntry(int index)
    {
        var provider = new InventoryRowValueProvider(index);
        var row = new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null);
        return new InventoryEntry(row, provider);
    }

    private async Task<int> LocateByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return -1;
        }

        await Task.CompletedTask;
        return _service.LocateById(id);
    }

    private static InventoryRecord ExtractRecord(object viewModel)
    {
        return viewModel switch
        {
            InventoryRowValueProvider provider => provider.ToRecord(),
            InventoryRecord record => record,
            _ => throw new NotSupportedException($"Unsupported view-model type: {viewModel?.GetType().FullName ?? "null"}"),
        };
    }

    private void OnServiceMutated(object? sender, InventoryMutationEventArgs e)
    {
        _count = e.NewCount;

        if (e.Kind is InventoryMutationKind.Created or InventoryMutationKind.Deleted)
        {
            lock (_cache)
            {
                _cache.Clear();
            }

            CountChanged?.Invoke(this, new FastTreeDataGridCountChangedEventArgs(_count));
            Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(
                new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full)));
            return;
        }

        if (e.Kind == InventoryMutationKind.Updated)
        {
            InventoryRowValueProvider? provider = null;
            lock (_cache)
            {
                if (_cache.TryGetValue(e.Index, out var entry))
                {
                    provider = entry.Provider;
                }
            }

            provider?.Apply(e.Record);
            Invalidated?.Invoke(this, new FastTreeDataGridInvalidatedEventArgs(
                new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, e.Index, 1)));
        }
    }

    private readonly record struct InventoryEntry(FastTreeDataGridRow Row, InventoryRowValueProvider Provider);
}

public sealed class InventoryRowValueProvider : IFastTreeDataGridValueProvider, IEditableObject, INotifyPropertyChanged
{
    private readonly int _initialIndex;
    private InventorySnapshot? _snapshot;
    private int _id;
    private string _category;
    private string _name;
    private string _supplier;
    private decimal _price;
    private int _stock;
    private double _rating;
    private DateTimeOffset _lastUpdated;
    private bool _isMaterialized;

    public InventoryRowValueProvider(int index)
    {
        _initialIndex = index;
        _id = index + 1;
        _category = "Loading";
        _name = "Loading...";
        _supplier = string.Empty;
        _price = 0m;
        _stock = 0;
        _rating = 0;
        _lastUpdated = DateTimeOffset.MinValue;
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsMaterialized => _isMaterialized;

    public int Id
    {
        get => _id;
        private set => SetField(ref _id, value, InventoryVirtualizationColumns.KeyId);
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value, InventoryVirtualizationColumns.KeyCategory);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value, InventoryVirtualizationColumns.KeyName);
    }

    public string Supplier
    {
        get => _supplier;
        set => SetField(ref _supplier, value, InventoryVirtualizationColumns.KeySupplier);
    }

    public decimal Price
    {
        get => _price;
        set => SetField(ref _price, value, InventoryVirtualizationColumns.KeyPrice);
    }

    public int Stock
    {
        get => _stock;
        set
        {
            if (SetField(ref _stock, value, InventoryVirtualizationColumns.KeyStock))
            {
                RaiseStatusChanged();
            }
        }
    }

    public double Rating
    {
        get => _rating;
        set => SetField(ref _rating, value, InventoryVirtualizationColumns.KeyRating);
    }

    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        private set => SetField(ref _lastUpdated, value, InventoryVirtualizationColumns.KeyUpdated);
    }

    public string Status => !_isMaterialized
        ? "Loading..."
        : Stock <= 0
            ? "Backordered"
            : Stock < 25
                ? "Low"
                : "Available";

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            InventoryVirtualizationColumns.KeyId => Id,
            InventoryVirtualizationColumns.KeyCategory => Category,
            InventoryVirtualizationColumns.KeyName => Name,
            InventoryVirtualizationColumns.KeySupplier => Supplier,
            InventoryVirtualizationColumns.KeyPrice => Price.ToString("C2", CultureInfo.CurrentCulture),
            InventoryVirtualizationColumns.KeyStock => Stock,
            InventoryVirtualizationColumns.KeyRating => Rating.ToString("0.0", CultureInfo.CurrentCulture),
            InventoryVirtualizationColumns.KeyStatus => Status,
            InventoryVirtualizationColumns.KeyUpdated => FormatTimestamp(LastUpdated),
            _ => null,
        };
    }

    public InventoryRecord ToRecord()
    {
        return new InventoryRecord(Id, Category, Name, Supplier, Price, Stock, Rating, LastUpdated);
    }

    public void Apply(InventoryRecord record)
    {
        Id = record.Id;
        Category = record.Category;
        Name = record.Name;
        Supplier = record.Supplier;
        Price = record.Price;
        Stock = record.Stock;
        Rating = record.Rating;
        LastUpdated = record.LastUpdated;

        if (!_isMaterialized)
        {
            _isMaterialized = true;
            RaiseStatusChanged();
        }
    }

    public void BeginEdit()
    {
        _snapshot ??= new InventorySnapshot(ToRecord());
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

        Apply(snapshot.Record);
        _snapshot = null;
    }

    private bool SetField<T>(ref T storage, T value, string key, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        NotifyPropertyChanged(propertyName);
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
        return true;
    }

    private void NotifyPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RaiseStatusChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, InventoryVirtualizationColumns.KeyStatus));
    }

    private readonly struct InventorySnapshot
    {
        public InventorySnapshot(InventoryRecord record)
        {
            Record = record;
        }

        public InventoryRecord Record { get; }
    }

    private static string FormatTimestamp(DateTimeOffset value)
    {
        return value <= DateTimeOffset.MinValue
            ? "Pending"
            : value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
    }
}

public static class InventoryVirtualizationColumns
{
    public const string KeyId = "Inventory.Id";
    public const string KeyCategory = "Inventory.Category";
    public const string KeyName = "Inventory.Name";
    public const string KeySupplier = "Inventory.Supplier";
    public const string KeyPrice = "Inventory.Price";
    public const string KeyStock = "Inventory.Stock";
    public const string KeyRating = "Inventory.Rating";
    public const string KeyStatus = "Inventory.Status";
    public const string KeyUpdated = "Inventory.LastUpdated";
}
