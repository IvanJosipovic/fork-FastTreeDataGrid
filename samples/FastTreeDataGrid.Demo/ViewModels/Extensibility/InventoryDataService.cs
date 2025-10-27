using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Demo.ViewModels.Extensibility;

/// <summary>
/// In-memory repository that mimics an external service powering the extensibility demo.
/// Generates deterministic data, exposes asynchronous CRUD operations, and raises mutation
/// events so virtualization providers can react without polling.
/// </summary>
public sealed class InventoryDataService
{
    private readonly List<InventoryRecord> _records;
    private readonly Dictionary<int, int> _indexLookup;
    private readonly object _gate = new();
    private int _nextId;

    public InventoryDataService()
    {
        _records = SeedRecords();
        _indexLookup = new Dictionary<int, int>(_records.Count);
        for (var i = 0; i < _records.Count; i++)
        {
            _indexLookup[_records[i].Id] = i;
        }

        _nextId = _records.Count == 0 ? 1 : _records.Max(r => r.Id) + 1;
        Categories = new ReadOnlyCollection<string>(_records
            .Select(r => r.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    public event EventHandler<InventoryMutationEventArgs>? Mutated;

    public IReadOnlyList<string> Categories { get; }

    public ValueTask<int> GetCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return new ValueTask<int>(_records.Count);
        }
    }

    public async Task<IReadOnlyList<InventoryRecord>> GetPageAsync(int startIndex, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return Array.Empty<InventoryRecord>();
        }

        await Task.Delay(TimeSpan.FromMilliseconds(35), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (startIndex >= _records.Count)
            {
                return Array.Empty<InventoryRecord>();
            }

            var start = Math.Max(0, startIndex);
            var actualCount = Math.Min(count, _records.Count - start);
            if (actualCount <= 0)
            {
                return Array.Empty<InventoryRecord>();
            }

            var page = new InventoryRecord[actualCount];
            _records.CopyTo(start, page, arrayIndex: 0, count: actualCount);
            return page;
        }
    }

    public async Task<InventoryRecord> CreateAsync(InventoryRecord template, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(40), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        InventoryRecord created;
        InventoryMutationEventArgs? args;
        lock (_gate)
        {
            created = template with
            {
                Id = _nextId++,
                LastUpdated = DateTimeOffset.UtcNow,
            };

            _records.Add(created);
            _indexLookup[created.Id] = _records.Count - 1;

            args = new InventoryMutationEventArgs(
                InventoryMutationKind.Created,
                index: _records.Count - 1,
                record: created,
                previous: null,
                newCount: _records.Count);
        }

        Mutated?.Invoke(this, args);
        return created;
    }

    public async Task<InventoryRecord> UpdateAsync(InventoryRecord record, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(30), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        InventoryRecord updated;
        InventoryRecord previous;
        InventoryMutationEventArgs? args;
        lock (_gate)
        {
            if (!_indexLookup.TryGetValue(record.Id, out var index))
            {
                throw new InvalidOperationException($"Record with id {record.Id} was not found.");
            }

            previous = _records[index];
            updated = record with
            {
                LastUpdated = DateTimeOffset.UtcNow,
            };

            _records[index] = updated;
            args = new InventoryMutationEventArgs(
                InventoryMutationKind.Updated,
                index,
                updated,
                previous,
                _records.Count);
        }

        Mutated?.Invoke(this, args);
        return updated;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(35), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        InventoryMutationEventArgs? args = null;
        lock (_gate)
        {
            if (!_indexLookup.TryGetValue(id, out var index))
            {
                return;
            }

            var removed = _records[index];
            _records.RemoveAt(index);
            _indexLookup.Remove(id);

            for (var i = index; i < _records.Count; i++)
            {
                _indexLookup[_records[i].Id] = i;
            }

            args = new InventoryMutationEventArgs(
                InventoryMutationKind.Deleted,
                index,
                removed,
                previous: null,
                newCount: _records.Count);
        }

        if (args is not null)
        {
            Mutated?.Invoke(this, args);
        }
    }

    public bool TryGetRecord(int index, out InventoryRecord record)
    {
        lock (_gate)
        {
            if (index < 0 || index >= _records.Count)
            {
                record = default;
                return false;
            }

            record = _records[index];
            return true;
        }
    }

    public IReadOnlyList<int> GetIndicesForCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Array.Empty<int>();
        }

        lock (_gate)
        {
            var matches = new List<int>();
            for (var i = 0; i < _records.Count; i++)
            {
                if (string.Equals(_records[i].Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(i);
                }
            }

            return matches;
        }
    }

    public int LocateById(int id)
    {
        lock (_gate)
        {
            return _indexLookup.TryGetValue(id, out var index) ? index : -1;
        }
    }

    private static List<InventoryRecord> SeedRecords()
    {
        var categories = new[]
        {
            "Audio",
            "Compute",
            "Displays",
            "Networking",
            "Peripherals",
            "Storage",
        };

        var suppliers = new[]
        {
            "Blue Harbor Supply",
            "Contoso Logistics",
            "Northwind Components",
            "Pioneer Dynamics",
            "Red Rock Outfitters",
            "Silverline Wholesale",
        };

        var adjectives = new[]
        {
            "Adaptive",
            "Aurora",
            "Cloudline",
            "Element",
            "Fusion",
            "Helix",
            "Lumina",
            "Nimbus",
            "Quantum",
            "Vantage",
        };

        var products = new[]
        {
            "Amplifier",
            "Bridge",
            "Controller",
            "Dock",
            "Gateway",
            "Hub",
            "Interface",
            "Monitor",
            "Router",
            "Switch",
            "Transceiver",
        };

        var random = new Random(1729);
        var timestamp = DateTimeOffset.UtcNow;
        var records = new List<InventoryRecord>(240);

        for (var i = 0; i < 240; i++)
        {
            var category = categories[i % categories.Length];
            var supplier = suppliers[random.Next(suppliers.Length)];
            var name = $"{adjectives[random.Next(adjectives.Length)]} {products[random.Next(products.Length)]}";

            var price = Math.Round((decimal)(random.NextDouble() * 900) + 50m, 2);
            var stock = random.Next(0, 400);
            var rating = Math.Round((random.NextDouble() * 4.0) + 1.0, 1);
            var ageDays = random.Next(0, 120);

            records.Add(new InventoryRecord(
                Id: i + 1,
                Category: category,
                Name: name,
                Supplier: supplier,
                Price: price,
                Stock: stock,
                Rating: rating,
                LastUpdated: timestamp.AddDays(-ageDays)));
        }

        return records;
    }
}

public enum InventoryMutationKind
{
    Created,
    Updated,
    Deleted,
}

public sealed class InventoryMutationEventArgs : EventArgs
{
    public InventoryMutationEventArgs(
        InventoryMutationKind kind,
        int index,
        InventoryRecord record,
        InventoryRecord? previous,
        int newCount)
    {
        Kind = kind;
        Index = index;
        Record = record;
        Previous = previous;
        NewCount = newCount;
    }

    public InventoryMutationKind Kind { get; }

    public int Index { get; }

    public InventoryRecord Record { get; }

    public InventoryRecord? Previous { get; }

    public int NewCount { get; }
}

public readonly record struct InventoryRecord(
    int Id,
    string Category,
    string Name,
    string Supplier,
    decimal Price,
    int Stock,
    double Rating,
    DateTimeOffset LastUpdated);
