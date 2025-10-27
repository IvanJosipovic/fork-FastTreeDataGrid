using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Demo.ViewModels.Data;

namespace FastTreeDataGrid.Demo.ViewModels;

internal sealed class CountryNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyName = "Country.Name";
    public const string KeyRegion = "Country.Region";
    public const string KeyPopulation = "Country.Population";
    public const string KeyArea = "Country.Area";
    public const string KeyGdp = "Country.GDP";

    private readonly Country? _country;
    private readonly IReadOnlyList<CountryNode> _children;

    private CountryNode(string title, Country? country, IReadOnlyList<CountryNode> children)
    {
        Title = title;
        _country = country;
        _children = children;
    }

    public string Title { get; }

    public string DisplayName => _country?.Name ?? Title;

    public string Region => _country?.Region ?? Title;

    public long? Population => _country?.Population;

    public long? Area => _country?.Area;

    public long? Gdp => _country?.GDP;

    public Country? Model => _country;

    public bool IsGroup => _country is null;

    public IReadOnlyList<CountryNode> Children => _children;

    public static CountryNode CreateGroup(string region, IReadOnlyList<CountryNode> children) =>
        new(region, null, children);

    public static CountryNode CreateLeaf(Country country) =>
        new(country.Name ?? string.Empty, country, Array.Empty<CountryNode>());

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public object? GetValue(object? item, string key)
    {
        var culture = CultureInfo.CurrentCulture;

        if (IsGroup)
        {
            return key == KeyName ? Title : string.Empty;
        }

        var country = _country!;
        return key switch
        {
            KeyName => country.Name ?? string.Empty,
            KeyRegion => country.Region,
            KeyPopulation => country.Population.ToString("N0", culture),
            KeyArea => country.Area.ToString("N0", culture),
            KeyGdp => country.GDP.ToString("N0", culture),
            _ => string.Empty,
        };
    }

    public bool ContainsQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var comparison = StringComparison.CurrentCultureIgnoreCase;

        if (Title.Contains(query, comparison))
        {
            return true;
        }

        if (_country is null)
        {
            return false;
        }

        return (_country.Name?.Contains(query, comparison) ?? false)
            || (_country.Region?.Contains(query, comparison) ?? false);
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }
}
