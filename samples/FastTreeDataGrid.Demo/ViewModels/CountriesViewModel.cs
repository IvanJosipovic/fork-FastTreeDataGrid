using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class CountriesViewModel : INotifyPropertyChanged
{
    private const string AllRegionsOption = "All regions";

    private readonly FastTreeDataGridFlatSource<CountryNode> _source;
    private readonly IReadOnlyList<CountryNode> _rootNodes;

    private string _searchText = string.Empty;
    private string _selectedRegion = AllRegionsOption;
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private IReadOnlyList<CountryNode> _selectedCountries = Array.Empty<CountryNode>();
    private IReadOnlyList<string> _selectedCountryNames = Array.Empty<string>();
    private string _selectionSummary = "No rows selected";

    internal CountriesViewModel(IReadOnlyList<CountryNode> rootNodes)
    {
        _rootNodes = rootNodes ?? throw new ArgumentNullException(nameof(rootNodes));
        _source = new FastTreeDataGridFlatSource<CountryNode>(_rootNodes, node => node.Children);
        Regions = BuildRegions(rootNodes);
        ExpandAllGroups();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static Func<FastTreeDataGridRow, string?> CountryNameSelector { get; } = row =>
    {
        if (row.Item is CountryNode node)
        {
            return node.IsGroup ? node.Title : node.DisplayName;
        }

        return row.Item?.ToString();
    };

    public IFastTreeDataGridSource Source => _source;

    public IReadOnlyList<string> Regions { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _searchText, normalized, nameof(SearchText)))
            {
                UpdateFilter();
            }
        }
    }

    public string SelectedRegion
    {
        get => _selectedRegion;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? AllRegionsOption : value;
            if (SetProperty(ref _selectedRegion, normalized, nameof(SelectedRegion)))
            {
                UpdateFilter();
            }
        }
    }

    public IReadOnlyList<int> SelectedIndices
    {
        get => _selectedIndices;
        set => SetSelectedIndices(value);
    }

    internal IReadOnlyList<CountryNode> SelectedCountries => _selectedCountries;

    public IReadOnlyList<string> SelectedCountryNames => _selectedCountryNames;

    public string SelectionSummary
    {
        get => _selectionSummary;
        private set => SetProperty(ref _selectionSummary, value, nameof(SelectionSummary));
    }

    public bool ApplySort(IReadOnlyList<FastTreeDataGridSortDescription> descriptions)
    {
        if (descriptions is null || descriptions.Count == 0)
        {
            _source.Sort(null);
            return true;
        }

        Comparison<FastTreeDataGridRow>? combined = null;

        foreach (var description in descriptions)
        {
            var column = description.Column;
            if (string.IsNullOrEmpty(column.ValueKey))
            {
                continue;
            }

            var comparison = GetComparison(column.ValueKey);
            if (comparison is null)
            {
                continue;
            }

            if (description.Direction == FastTreeDataGridSortDirection.Descending)
            {
                var baseComparison = comparison;
                comparison = (left, right) => baseComparison(right, left);
            }

            if (combined is null)
            {
                combined = comparison;
            }
            else
            {
                var previous = combined;
                var current = comparison;
                combined = (left, right) =>
                {
                    var result = previous(left, right);
                    return result != 0 ? result : current(left, right);
                };
            }
        }

        if (combined is null)
        {
            _source.Sort(null);
            return false;
        }

        _source.Sort(combined);
        return true;
    }

    private static IReadOnlyList<string> BuildRegions(IReadOnlyList<CountryNode> rootNodes)
    {
        var result = new List<string> { AllRegionsOption };
        var regions = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var node in rootNodes)
        {
            regions.Add(node.Title);
        }

        result.AddRange(regions);
        return result;
    }

    private void ExpandAllGroups()
    {
        for (var i = 0; i < _source.RowCount; i++)
        {
            var row = _source.GetRow(i);
            if (row.HasChildren && !row.IsExpanded)
            {
                _source.ToggleExpansion(i);
            }
        }
    }

    private void UpdateFilter()
    {
        var regionFilter = GetRegionFilter();
        var hasSearch = !string.IsNullOrWhiteSpace(_searchText);

        if (!hasSearch && regionFilter is null)
        {
            _source.SetFilter(null);
            return;
        }

        Predicate<FastTreeDataGridRow> predicate = row =>
        {
            if (row.Item is not CountryNode node)
            {
                return true;
            }

            if (node.IsGroup)
            {
                if (regionFilter is not null && !string.Equals(node.Title, regionFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return !hasSearch || node.ContainsQuery(_searchText);
            }

            if (regionFilter is not null && !string.Equals(node.Region, regionFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !hasSearch || node.ContainsQuery(_searchText);
        };

        _source.SetFilter(predicate, expandMatches: true);
    }

    private void SetSelectedIndices(IReadOnlyList<int>? value)
    {
        var incoming = value ?? Array.Empty<int>();

        if (ReferenceEquals(incoming, _selectedIndices))
        {
            return;
        }

        if (_selectedIndices.Count == incoming.Count && _selectedIndices.SequenceEqual(incoming))
        {
            return;
        }

        _selectedIndices = incoming;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndices)));
        UpdateSelectedCountries();
    }

    private void UpdateSelectedCountries()
    {
        if (_selectedIndices.Count == 0)
        {
            _selectedCountries = Array.Empty<CountryNode>();
            _selectedCountryNames = Array.Empty<string>();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCountries)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCountryNames)));
            SelectionSummary = "No rows selected";
            return;
        }

        var items = new List<CountryNode>();
        var names = new List<string>();
        foreach (var index in _selectedIndices)
        {
            if (index < 0 || index >= _source.RowCount)
            {
                continue;
            }

            var row = _source.GetRow(index);
            if (row.Item is CountryNode node)
            {
                items.Add(node);
                names.Add(node.IsGroup ? node.Title : node.DisplayName ?? node.Title);
            }
        }

        _selectedCountries = items;
        _selectedCountryNames = names;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCountries)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCountryNames)));
        SelectionSummary = BuildSelectionSummary(items);
    }

    private static string BuildSelectionSummary(IReadOnlyList<CountryNode> items)
    {
        if (items.Count == 0)
        {
            return "No rows selected";
        }

        if (items.Count == 1)
        {
            var node = items[0];
            return node.IsGroup
                ? $"Selected group: {node.Title}"
                : $"Selected country: {node.DisplayName}";
        }

        var preview = items
            .Where(node => !node.IsGroup)
            .Take(3)
            .Select(node => node.DisplayName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (preview.Count == 0)
        {
            preview = items.Take(3).Select(node => node.Title).ToList();
        }

        var summary = string.Join(", ", preview);
        if (items.Count > preview.Count)
        {
            summary = string.IsNullOrEmpty(summary)
                ? $"{items.Count} items selected"
                : $"{summary}, +{items.Count - preview.Count} more";
        }
        else if (!string.IsNullOrEmpty(summary))
        {
            summary = preview.Count == 1 ? summary : $"{summary}";
        }

        return string.IsNullOrEmpty(summary)
            ? $"{items.Count} items selected"
            : $"Selected ({items.Count}): {summary}";
    }

    private string? GetRegionFilter()
    {
        return string.Equals(_selectedRegion, AllRegionsOption, StringComparison.OrdinalIgnoreCase)
            ? null
            : _selectedRegion;
    }

    private Comparison<FastTreeDataGridRow>? GetComparison(string valueKey)
    {
        return valueKey switch
        {
            CountryNode.KeyName => CreateStringComparison(node => node.DisplayName),
            CountryNode.KeyRegion => CreateStringComparison(node => node.Region),
            CountryNode.KeyPopulation => CreateNumericComparison(node => node.Population),
            CountryNode.KeyArea => CreateNumericComparison(node => node.Area),
            CountryNode.KeyGdp => CreateNumericComparison(node => node.Gdp),
            _ => null,
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateStringComparison(Func<CountryNode, string?> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not CountryNode left || rightRow.Item is not CountryNode right)
            {
                return 0;
            }

            if (left.IsGroup && right.IsGroup)
            {
                return comparer.Compare(left.Title, right.Title);
            }

            if (left.IsGroup)
            {
                return -1;
            }

            if (right.IsGroup)
            {
                return 1;
            }

            return comparer.Compare(selector(left) ?? string.Empty, selector(right) ?? string.Empty);
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateNumericComparison(Func<CountryNode, long?> selector)
    {
        var tieBreaker = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not CountryNode left || rightRow.Item is not CountryNode right)
            {
                return 0;
            }

            if (left.IsGroup && right.IsGroup)
            {
                return tieBreaker.Compare(left.Title, right.Title);
            }

            if (left.IsGroup)
            {
                return -1;
            }

            if (right.IsGroup)
            {
                return 1;
            }

            var leftValue = selector(left);
            var rightValue = selector(right);
            var comparison = Nullable.Compare(leftValue, rightValue);
            if (comparison != 0)
            {
                return comparison;
            }

            return tieBreaker.Compare(left.DisplayName, right.DisplayName);
        };
    }

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
