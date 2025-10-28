using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using Avalonia.Collections;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.GroupingDemo.Adapters;

namespace FastTreeDataGrid.GroupingDemo.ViewModels;

public sealed class GroupingShowcaseViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<SalesRecord> _records;
    private readonly FastTreeDataGridFlatSource<SalesRecord> _source;
    private FastTreeDataGridGroupingLayout? _savedLayout;
    private string? _savedLayoutJson;

    public GroupingShowcaseViewModel()
    {
        _records = SeedData();
        _source = new FastTreeDataGridFlatSource<SalesRecord>(_records, _ => Array.Empty<SalesRecord>());

        Presets = new ReadOnlyCollection<GroupPreset>(new[]
        {
            new GroupPreset(
                "Region ▸ Category",
                "Organize sales by region then category.",
                new[]
                {
                    new GroupDescriptorSpec(SalesRecord.KeyRegion, "Region"),
                    new GroupDescriptorSpec(SalesRecord.KeyCategory, "Category"),
                }),
            new GroupPreset(
                "Category ▸ Quarter",
                "Group by category and quarter to compare seasonal trends.",
                new[]
                {
                    new GroupDescriptorSpec(SalesRecord.KeyCategory, "Category"),
                    new GroupDescriptorSpec(SalesRecord.KeyQuarter, "Quarter"),
                }),
            new GroupPreset(
                "Region ▸ Category ▸ Product",
                "Full drill-down across the dataset.",
                new[]
                {
                    new GroupDescriptorSpec(SalesRecord.KeyRegion, "Region"),
                    new GroupDescriptorSpec(SalesRecord.KeyCategory, "Category"),
                    new GroupDescriptorSpec(SalesRecord.KeyProduct, "Product"),
                }),
            new GroupPreset(
                "Region ▸ Revenue Band",
                "Group by sales region and a custom revenue bucket adapter to highlight major deals.",
                new[]
                {
                    new GroupDescriptorSpec(SalesRecord.KeyRegion, "Region"),
                    new GroupDescriptorSpec(SalesRecord.KeyRevenue, "Revenue Band"),
                }),
        });

        GroupDescriptors = new AvaloniaList<FastTreeDataGridGroupDescriptor>();
        AggregateDescriptors = new AvaloniaList<FastTreeDataGridAggregateDescriptor>();

        AggregateDescriptors.Add(SalesRecord.CreateSumDescriptor(SalesRecord.KeyUnitsSold, FastTreeDataGridAggregatePlacement.GroupAndGrid, value => value?.ToString() ?? string.Empty));
        AggregateDescriptors.Add(SalesRecord.CreateSumDescriptor(SalesRecord.KeyRevenue, FastTreeDataGridAggregatePlacement.GroupAndGrid, SalesRecord.FormatCurrency));
        SelectedPreset = Presets.Count > 0 ? Presets[0] : null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public IReadOnlyList<GroupPreset> Presets { get; }

    public AvaloniaList<FastTreeDataGridGroupDescriptor> GroupDescriptors { get; }

    public AvaloniaList<FastTreeDataGridAggregateDescriptor> AggregateDescriptors { get; }

    public bool HasSavedLayout => _savedLayout is not null;

    public string? SavedLayoutJson
    {
        get => _savedLayoutJson;
        private set
        {
            if (!string.Equals(_savedLayoutJson, value, StringComparison.Ordinal))
            {
                _savedLayoutJson = value;
                OnPropertyChanged(nameof(SavedLayoutJson));
            }
        }
    }

    private GroupPreset? _selectedPreset;
    public GroupPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!Equals(_selectedPreset, value))
            {
                _selectedPreset = value;
                OnPropertyChanged(nameof(SelectedPreset));
            }
        }
    }

    public void SaveLayout(FastTreeDataGridGroupingLayout layout)
    {
        _savedLayout = layout ?? throw new ArgumentNullException(nameof(layout));
        SavedLayoutJson = FastTreeDataGridGroupingLayoutSerializer.Serialize(layout);
        OnPropertyChanged(nameof(HasSavedLayout));
    }

    public FastTreeDataGridGroupingLayout? GetSavedLayout() => _savedLayout;

    public void ClearSavedLayout()
    {
        _savedLayout = null;
        SavedLayoutJson = null;
        OnPropertyChanged(nameof(HasSavedLayout));
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static IReadOnlyList<SalesRecord> SeedData()
    {
        return new List<SalesRecord>
        {
            new("North America", "Electronics", "Audio", "Noise-Cancelling Headphones", "Q1", 820, 196000m),
            new("North America", "Electronics", "Audio", "Bluetooth Speaker", "Q1", 540, 81000m),
            new("North America", "Electronics", "Computers", "Ultrabook 14\"", "Q1", 310, 279000m),
            new("North America", "Electronics", "Computers", "Convertible Laptop", "Q2", 265, 238500m),
            new("North America", "Home", "Kitchen", "Smart Blender", "Q2", 420, 58800m),
            new("North America", "Home", "Cleaning", "Robot Vacuum", "Q3", 350, 157500m),
            new("North America", "Home", "Outdoor", "Weather Station", "Q4", 180, 32400m),
            new("Europe", "Electronics", "Audio", "Noise-Cancelling Headphones", "Q1", 740, 176000m),
            new("Europe", "Electronics", "Audio", "Bluetooth Speaker", "Q2", 620, 93000m),
            new("Europe", "Electronics", "Computers", "Ultrabook 14\"", "Q3", 290, 261000m),
            new("Europe", "Electronics", "Computers", "Convertible Laptop", "Q4", 240, 216000m),
            new("Europe", "Home", "Kitchen", "Smart Blender", "Q2", 360, 50400m),
            new("Europe", "Home", "Outdoor", "Weather Station", "Q3", 220, 39600m),
            new("Europe", "Home", "Cleaning", "Robot Vacuum", "Q4", 310, 139500m),
            new("Asia-Pacific", "Electronics", "Audio", "Noise-Cancelling Headphones", "Q1", 1020, 241000m),
            new("Asia-Pacific", "Electronics", "Audio", "Bluetooth Speaker", "Q2", 890, 133500m),
            new("Asia-Pacific", "Electronics", "Computers", "Ultrabook 14\"", "Q2", 530, 477000m),
            new("Asia-Pacific", "Electronics", "Computers", "Convertible Laptop", "Q3", 470, 423000m),
            new("Asia-Pacific", "Home", "Kitchen", "Smart Blender", "Q3", 610, 85400m),
            new("Asia-Pacific", "Home", "Outdoor", "Weather Station", "Q4", 340, 61200m),
            new("Asia-Pacific", "Home", "Cleaning", "Robot Vacuum", "Q4", 520, 234000m),
        };
    }

}

public sealed record GroupPreset(string Name, string Description, IReadOnlyList<GroupDescriptorSpec> Levels);

public sealed record GroupDescriptorSpec(string ColumnKey, string? Header, FastTreeDataGridSortDirection Direction = FastTreeDataGridSortDirection.Ascending);

public sealed record SalesRecord(
    string Region,
    string Category,
    string Subcategory,
    string Product,
    string Quarter,
    int UnitsSold,
    decimal Revenue) : IFastTreeDataGridValueProvider
{
    public const string KeyRegion = nameof(Region);
    public const string KeyCategory = nameof(Category);
    public const string KeySubcategory = nameof(Subcategory);
    public const string KeyProduct = nameof(Product);
    public const string KeyQuarter = nameof(Quarter);
    public const string KeyUnitsSold = nameof(UnitsSold);
    public const string KeyRevenue = nameof(Revenue);

    public static string FormatCurrency(object? value) =>
        value switch
        {
            decimal dec => dec.ToString("C0", CultureInfo.CurrentCulture),
            double dbl => dbl.ToString("C0", CultureInfo.CurrentCulture),
            float fl => fl.ToString("C0", CultureInfo.CurrentCulture),
            _ => value?.ToString() ?? string.Empty,
        };

    public static FastTreeDataGridAggregateDescriptor CreateSumDescriptor(
        string columnKey,
        FastTreeDataGridAggregatePlacement placement,
        Func<object?, string?> formatter)
    {
        return new FastTreeDataGridAggregateDescriptor
        {
            ColumnKey = columnKey,
            Placement = placement,
            Aggregator = rows =>
            {
                decimal sum = 0;
                foreach (var row in rows)
                {
                    if (row.Item is not SalesRecord record)
                    {
                        continue;
                    }

                    sum += columnKey switch
                    {
                        KeyUnitsSold => record.UnitsSold,
                        KeyRevenue => record.Revenue,
                        _ => 0,
                    };
                }

                return columnKey == KeyUnitsSold ? (object)(int)sum : sum;
            },
            Formatter = formatter,
        };
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }

    public object? GetValue(object? item, string key)
    {
        var record = item as SalesRecord ?? this;

        return key switch
        {
            KeyRegion => record.Region,
            KeyCategory => record.Category,
            KeySubcategory => record.Subcategory,
            KeyProduct => record.Product,
            KeyQuarter => record.Quarter,
            KeyUnitsSold => record.UnitsSold,
            KeyRevenue => record.Revenue,
            _ => null,
        };
    }
}
