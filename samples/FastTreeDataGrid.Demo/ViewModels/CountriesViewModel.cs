using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels.Data;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class CountriesViewModel : INotifyPropertyChanged
{
    private const string AllRegionsOption = "All regions";
    private const string DefaultReorderStatus = "Drag countries within a region to reprioritize reports.";

    private readonly FastTreeDataGridFlatSource<CountryNode> _source;
    private List<RegionGroup> _groups;
    private List<CountryNode> _rootNodes;
    private IReadOnlyList<string> _regions;

    private string _searchText = string.Empty;
    private string _selectedRegion = AllRegionsOption;
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private IReadOnlyList<CountryNode> _selectedCountries = Array.Empty<CountryNode>();
    private IReadOnlyList<string> _selectedCountryNames = Array.Empty<string>();
    private string _selectionSummary = "No rows selected";
    private string _reorderStatus = DefaultReorderStatus;

    public CountriesViewModel()
        : this(BuildInitialNodes())
    {
    }

    internal CountriesViewModel(IReadOnlyList<CountryNode> initialNodes)
    {
        if (initialNodes is null)
        {
            throw new ArgumentNullException(nameof(initialNodes));
        }

        _groups = BuildGroups(initialNodes);
        _rootNodes = BuildNodes(_groups);
        _source = new FastTreeDataGridFlatSource<CountryNode>(_rootNodes, node => node.Children);
        _regions = BuildRegions(_groups);
        ExpandAllGroups();

        RowReorderSettings = new FastTreeDataGridRowReorderSettings
        {
            IsEnabled = true,
            ActivationThreshold = 6,
            ShowDragPreview = true,
            DragPreviewBrush = new ImmutableSolidColorBrush(Color.FromArgb(56, 25, 118, 210)),
            DragPreviewOpacity = 0.9,
            DragPreviewCornerRadius = 4,
            ShowDropIndicator = true,
            DropIndicatorBrush = new ImmutableSolidColorBrush(Color.FromRgb(25, 118, 210)),
            DropIndicatorThickness = 2,
            UseSelection = true,
            AllowGroupReorder = true,
        };

        RowReorderHandler = new CountriesRowReorderHandler(this);
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

    public IReadOnlyList<string> Regions => _regions;

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

    public FastTreeDataGridRowReorderSettings RowReorderSettings { get; }

    public IFastTreeDataGridRowReorderHandler RowReorderHandler { get; }

    public string ReorderStatus
    {
        get => _reorderStatus;
        private set => SetProperty(ref _reorderStatus, value, nameof(ReorderStatus));
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

    public bool HasMixedSelection(IReadOnlyList<int> indices)
    {
        if (indices is null || indices.Count == 0)
        {
            return false;
        }

        var hasGroup = false;
        var hasLeaf = false;

        foreach (var index in indices)
        {
            if (TryGetNode(index) is not { } node)
            {
                continue;
            }

            if (node.IsGroup)
            {
                hasGroup = true;
            }
            else
            {
                hasLeaf = true;
            }

            if (hasGroup && hasLeaf)
            {
                return true;
            }
        }

        return false;
    }

    public void NotifyReorderCancelled()
    {
        ReorderStatus = "Drag either regions or individual countries—mixed selections can't be moved together.";
    }

    public void NotifyReorderCompleted(IReadOnlyList<int> newIndices)
    {
        if (newIndices is null || newIndices.Count == 0)
        {
            ReorderStatus = DefaultReorderStatus;
            return;
        }

        var flat = BuildRowReferences(_groups);
        var summaries = new List<string>();
        var regions = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var index in newIndices)
        {
            if ((uint)index >= (uint)flat.Count)
            {
                continue;
            }

            var entry = flat[index];
            if (entry.IsGroup)
            {
                summaries.Add(entry.Group.Name);
                regions.Add(entry.Group.Name);
            }
            else if (entry.Country is { } country)
            {
                summaries.Add(country.Name ?? country.Region ?? "Country");
                regions.Add(entry.Group.Name);
            }
        }

        if (summaries.Count == 0)
        {
            ReorderStatus = DefaultReorderStatus;
            return;
        }

        var preview = string.Join(", ", summaries.Take(3));
        if (summaries.Count > 3)
        {
            preview = $"{preview}, +{summaries.Count - 3} more";
        }

        if (regions.Count == 1)
        {
            ReorderStatus = $"Moved {preview} within {regions.First()}.";
        }
        else if (regions.Count > 1)
        {
            ReorderStatus = $"Reassigned {preview} across {regions.Count} regions.";
        }
        else
        {
            ReorderStatus = $"Moved {preview}.";
        }
    }

    public void ResetReorderStatus()
    {
        ReorderStatus = DefaultReorderStatus;
    }

    internal bool CanReorder(FastTreeDataGridRowReorderRequest request) =>
        TryApplyReorder(request, apply: false, out _);

    internal Task<FastTreeDataGridRowReorderResult> ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryApplyReorder(request, apply: true, out var newIndices))
        {
            return Task.FromResult(FastTreeDataGridRowReorderResult.Cancelled);
        }

        return Task.FromResult(FastTreeDataGridRowReorderResult.Successful(newIndices));
    }

    private static IReadOnlyList<CountryNode> BuildInitialNodes() => DemoDataFactory.CreateCountries();

    private static List<RegionGroup> BuildGroups(IReadOnlyList<CountryNode> nodes)
    {
        var result = new List<RegionGroup>(nodes.Count);
        foreach (var groupNode in nodes)
        {
            var countries = new List<Country>();
            foreach (var child in groupNode.Children)
            {
                if (child.Model is { } country)
                {
                    countries.Add(country);
                }
            }

            result.Add(new RegionGroup(groupNode.Title, countries));
        }

        if (result.Count == 0)
        {
            var fallback = Countries.All
                .GroupBy(country => country.Region ?? "Unknown", StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);

            foreach (var region in fallback)
            {
                result.Add(new RegionGroup(region.Key, region.OrderBy(country => country.Name, StringComparer.CurrentCultureIgnoreCase).ToList()));
            }
        }

        return result;
    }

    private static List<CountryNode> BuildNodes(IReadOnlyList<RegionGroup> groups)
    {
        var result = new List<CountryNode>(groups.Count);
        foreach (var group in groups)
        {
            var children = group.Countries
                .Select(CountryNode.CreateLeaf)
                .ToList();

            result.Add(CountryNode.CreateGroup(group.Name, children));
        }

        return result;
    }

    private static IReadOnlyList<string> BuildRegions(IEnumerable<RegionGroup> groups)
    {
        var list = new List<string> { AllRegionsOption };
        list.AddRange(groups.Select(g => g.Name).Distinct(StringComparer.CurrentCultureIgnoreCase));
        return list;
    }

    private static List<RowReference> BuildRowReferences(IReadOnlyList<RegionGroup> groups)
    {
        var rows = new List<RowReference>();
        foreach (var group in groups)
        {
            rows.Add(RowReference.ForGroup(group));
            foreach (var country in group.Countries)
            {
                rows.Add(RowReference.ForCountry(group, country));
            }
        }

        return rows;
    }

    private static List<RegionGroup> CloneGroups(IReadOnlyList<RegionGroup> groups)
    {
        var clone = new List<RegionGroup>(groups.Count);
        foreach (var group in groups)
        {
            clone.Add(new RegionGroup(group.Name, new List<Country>(group.Countries)));
        }

        return clone;
    }

    private bool TryApplyReorder(FastTreeDataGridRowReorderRequest request, bool apply, out IReadOnlyList<int> newIndices)
    {
        newIndices = Array.Empty<int>();

        if (request is null || request.SourceIndices.Count == 0)
        {
            return false;
        }

        var workingGroups = apply ? _groups : CloneGroups(_groups);
        var references = BuildRowReferences(workingGroups);
        if (references.Count == 0)
        {
            return false;
        }

        var sortedIndices = request.SourceIndices
            .Where(index => index >= 0 && index < references.Count)
            .Distinct()
            .OrderBy(index => index)
            .ToList();

        if (sortedIndices.Count == 0)
        {
            return false;
        }

        var selectedRefs = sortedIndices.Select(index => references[index]).ToList();
        var movingGroups = selectedRefs.All(entry => entry.IsGroup);
        var movingLeaves = selectedRefs.All(entry => !entry.IsGroup);

        if (!movingGroups && !movingLeaves)
        {
            return false;
        }

        var insertIndex = Math.Clamp(request.InsertIndex, 0, references.Count - sortedIndices.Count);

        if (movingGroups)
        {
            var moving = selectedRefs.Select(entry => entry.Group).Distinct().ToList();
            workingGroups.RemoveAll(group => moving.Contains(group));

            var remainingRows = BuildRowReferences(workingGroups);
            insertIndex = Math.Clamp(insertIndex, 0, remainingRows.Count);

            var finalRows = new List<RowReference>(remainingRows);
            finalRows.InsertRange(insertIndex, selectedRefs);

            var reorderedGroups = new List<RegionGroup>();
            foreach (var entry in finalRows)
            {
                if (entry.IsGroup && !reorderedGroups.Contains(entry.Group))
                {
                    reorderedGroups.Add(entry.Group);
                }
            }

            workingGroups = reorderedGroups;

            var finalRefs = BuildRowReferences(workingGroups);
            var indices = new List<int>();
            foreach (var group in moving)
            {
                var index = finalRefs.FindIndex(entry => entry.IsGroup && ReferenceEquals(entry.Group, group));
                if (index >= 0)
                {
                    indices.Add(index);
                }
            }

            newIndices = indices;

            if (apply)
            {
                _groups = workingGroups;
                RebuildSourceFromGroups();
            }

            return indices.Count == moving.Count;
        }

        // Move leaf rows.
        var movingCountries = selectedRefs.Select(entry => entry.Country!).ToList();
        foreach (var entry in selectedRefs)
        {
            entry.Group.Countries.Remove(entry.Country!);
        }

        var remaining = BuildRowReferences(workingGroups);
        insertIndex = Math.Clamp(request.InsertIndex, 0, remaining.Count);

        var afterRef = insertIndex < remaining.Count ? remaining[insertIndex] : null;
        var beforeRef = insertIndex > 0 ? remaining[insertIndex - 1] : null;

        RegionGroup? targetGroup = null;
        var insertPosition = 0;

        if (afterRef is not null)
        {
            targetGroup = afterRef.Group;
            insertPosition = afterRef.IsGroup
                ? 0
                : afterRef.Group.Countries.IndexOf(afterRef.Country!);
        }
        else if (beforeRef is not null)
        {
            targetGroup = beforeRef.Group;
            insertPosition = beforeRef.IsGroup
                ? beforeRef.Group.Countries.Count
                : beforeRef.Group.Countries.IndexOf(beforeRef.Country!) + 1;
        }
        else if (workingGroups.Count > 0)
        {
            targetGroup = workingGroups[0];
            insertPosition = 0;
        }

        if (targetGroup is null)
        {
            return false;
        }

        for (var i = 0; i < movingCountries.Count; i++)
        {
            targetGroup.Countries.Insert(insertPosition + i, movingCountries[i]);
        }

        var finalReferences = BuildRowReferences(workingGroups);
        var newPositions = new List<int>();
        foreach (var country in movingCountries)
        {
            var index = finalReferences.FindIndex(entry => !entry.IsGroup && ReferenceEquals(entry.Country, country));
            if (index >= 0)
            {
                newPositions.Add(index);
            }
        }

        newIndices = newPositions;

        if (apply)
        {
            _groups = workingGroups;
            RebuildSourceFromGroups();
        }

        return newPositions.Count == movingCountries.Count;
    }

    private void RebuildSourceFromGroups()
    {
        _rootNodes = BuildNodes(_groups);
        _source.Reset(_rootNodes, preserveExpansion: true);

        var regions = BuildRegions(_groups);
        if (!_regions.SequenceEqual(regions))
        {
            _regions = regions;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Regions)));
        }
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
            if (TryGetNode(index) is { } node)
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
            summary = preview.Count == 1 ? summary : summary;
        }

        return string.IsNullOrEmpty(summary)
            ? $"{items.Count} items selected"
            : $"Selected ({items.Count}): {summary}";
    }

    private CountryNode? TryGetNode(int index)
    {
        if (index < 0 || index >= _source.RowCount)
        {
            return null;
        }

        if (_source.TryGetMaterializedRow(index, out var row))
        {
            return row.Item as CountryNode;
        }

        try
        {
            return _source.GetRow(index).Item as CountryNode;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private string? GetRegionFilter()
    {
        return string.Equals(_selectedRegion, AllRegionsOption, StringComparison.OrdinalIgnoreCase)
            ? null
            : _selectedRegion;
    }

    private Comparison<FastTreeDataGridRow>? GetComparison(string valueKey) => valueKey switch
    {
        CountryNode.KeyName => CreateStringComparison(node => node.DisplayName),
        CountryNode.KeyRegion => CreateStringComparison(node => node.Region),
        CountryNode.KeyPopulation => CreateNumericComparison(node => node.Population),
        CountryNode.KeyArea => CreateNumericComparison(node => node.Area),
        CountryNode.KeyGdp => CreateNumericComparison(node => node.Gdp),
        _ => null,
    };

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

            var leftValue = selector(left) ?? 0;
            var rightValue = selector(right) ?? 0;
            var comparison = leftValue.CompareTo(rightValue);
            return comparison != 0 ? comparison : tieBreaker.Compare(left.DisplayName, right.DisplayName);
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

    private sealed class RegionGroup
    {
        public RegionGroup(string name, List<Country> countries)
        {
            Name = name;
            Countries = countries;
        }

        public string Name { get; }

        public List<Country> Countries { get; }
    }

    private sealed class RowReference
    {
        private RowReference(RegionGroup group, Country? country)
        {
            Group = group;
            Country = country;
        }

        public RegionGroup Group { get; }

        public Country? Country { get; }

        public bool IsGroup => Country is null;

        public static RowReference ForGroup(RegionGroup group) => new(group, null);

        public static RowReference ForCountry(RegionGroup group, Country country) => new(group, country);
    }

    private sealed class CountriesRowReorderHandler : IFastTreeDataGridRowReorderHandler
    {
        private readonly CountriesViewModel _owner;

        public CountriesRowReorderHandler(CountriesViewModel owner)
        {
            _owner = owner;
        }

        public bool CanReorder(FastTreeDataGridRowReorderRequest request) => _owner.CanReorder(request);

        public Task<FastTreeDataGridRowReorderResult> ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken) =>
            _owner.ReorderAsync(request, cancellationToken);
    }
}
