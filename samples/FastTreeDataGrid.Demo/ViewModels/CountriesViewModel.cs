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

    internal CountriesViewModel(IReadOnlyList<CountryNode> rootNodes)
    {
        _rootNodes = rootNodes ?? throw new ArgumentNullException(nameof(rootNodes));
        _source = new FastTreeDataGridFlatSource<CountryNode>(_rootNodes, node => node.Children);
        Regions = BuildRegions(rootNodes);
        ExpandAllGroups();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public bool ApplySort(FastTreeDataGridColumn column, FastTreeDataGridSortDirection direction)
    {
        if (column is null)
        {
            return false;
        }

        if (direction == FastTreeDataGridSortDirection.None)
        {
            _source.Sort(null);
            return true;
        }

        if (string.IsNullOrEmpty(column.ValueKey))
        {
            _source.Sort(null);
            return false;
        }

        var comparison = GetComparison(column.ValueKey);
        if (comparison is null)
        {
            _source.Sort(null);
            return false;
        }

        if (direction == FastTreeDataGridSortDirection.Descending)
        {
            var baseComparison = comparison;
            comparison = (left, right) => baseComparison(right, left);
        }

        _source.Sort(comparison);
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
