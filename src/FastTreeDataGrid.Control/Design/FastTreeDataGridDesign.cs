using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeGridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Design;

/// <summary>
/// Provides lightweight design-time helpers so FastTreeDataGrid renders sample data in the XAML previewer.
/// Apply with <c>d:ftdg.UseSampleData="True"</c> when authoring layouts.
/// </summary>
public static class FastTreeDataGridDesign
{
    /// <summary>
    /// Attached property toggling design-time sample data.
    /// </summary>
    public static readonly AttachedProperty<bool> UseSampleDataProperty =
        AvaloniaProperty.RegisterAttached<FastTreeGridControl, bool>("UseSampleData", typeof(FastTreeDataGridDesign));

    private static readonly AttachedProperty<SampleSubscription?> SampleSubscriptionProperty =
        AvaloniaProperty.RegisterAttached<FastTreeGridControl, SampleSubscription?>("SampleSubscription", typeof(FastTreeDataGridDesign));

    static FastTreeDataGridDesign()
    {
        UseSampleDataProperty.Changed.AddClassHandler<FastTreeGridControl>(OnUseSampleDataChanged);
    }

    public static bool GetUseSampleData(FastTreeGridControl grid) =>
        grid.GetValue(UseSampleDataProperty);

    public static void SetUseSampleData(FastTreeGridControl grid, bool value) =>
        grid.SetValue(UseSampleDataProperty, value);

    private static void OnUseSampleDataChanged(FastTreeGridControl grid, AvaloniaPropertyChangedEventArgs e)
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            return;
        }

        if (e.NewValue is true)
        {
            ActivateSampleData(grid);
        }
        else
        {
            DeactivateSampleData(grid);
        }
    }

    private static void ActivateSampleData(FastTreeGridControl grid)
    {
        if (grid.GetValue(SampleSubscriptionProperty) is SampleSubscription existing)
        {
            existing.Dispose();
            grid.SetValue(SampleSubscriptionProperty, null);
        }

        if (grid.ItemsSource is not null && grid.ItemsSource is not SampleSource)
        {
            return;
        }

        var subscription = new SampleSubscription(grid);
        grid.SetValue(SampleSubscriptionProperty, subscription);
    }

    private static void DeactivateSampleData(FastTreeGridControl grid)
    {
        if (grid.GetValue(SampleSubscriptionProperty) is SampleSubscription subscription)
        {
            subscription.Dispose();
            grid.SetValue(SampleSubscriptionProperty, null);
        }
        else if (grid.ItemsSource is SampleSource)
        {
            grid.ItemsSource = null;
        }
    }

    private sealed class SampleSubscription : IDisposable
    {
        private readonly FastTreeGridControl _grid;
        private SampleSource? _source;

        public SampleSubscription(FastTreeGridControl grid)
        {
            _grid = grid;
            _grid.Columns.CollectionChanged += OnColumnsChanged;
            ApplySampleSource();
        }

        public void Dispose()
        {
            _grid.Columns.CollectionChanged -= OnColumnsChanged;
            if (_grid.ItemsSource is SampleSource && ReferenceEquals(_grid.ItemsSource, _source))
            {
                _grid.ItemsSource = null;
            }

            _source = null;
        }

        private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Avalonia.Controls.Design.IsDesignMode || !FastTreeDataGridDesign.GetUseSampleData(_grid))
            {
                return;
            }

            ApplySampleSource();
        }

        private void ApplySampleSource()
        {
            _source = BuildSampleSource(_grid);
            if (_grid.ItemsSource is null || _grid.ItemsSource is SampleSource)
            {
                _grid.ItemsSource = _source;
            }
        }
    }

    private static SampleSource BuildSampleSource(FastTreeGridControl grid)
    {
        var rows = new List<FastTreeDataGridRow>();
        var columns = grid.Columns ?? new AvaloniaList<FastTreeDataGridColumn>();
        if (columns.Count == 0)
        {
            columns = new AvaloniaList<FastTreeDataGridColumn>
            {
                new FastTreeDataGridColumn { Header = "Name", ValueKey = "Design.Name" },
                new FastTreeDataGridColumn { Header = "Category", ValueKey = "Design.Category" },
                new FastTreeDataGridColumn { Header = "Status", ValueKey = "Design.Status" },
            };
        }

        var samples = SampleValueCatalog.Build(columns.Select(c => c.ValueKey).Where(k => !string.IsNullOrWhiteSpace(k)).Cast<string>().ToArray());
        for (var i = 0; i < samples.Count; i++)
        {
            var provider = new SampleValueProvider(i, samples[i]);
            rows.Add(new FastTreeDataGridRow(provider, level: 0, hasChildren: false, isExpanded: false, requestMeasureCallback: null));
        }

        return new SampleSource(rows);
    }

    private sealed class SampleSource : IFastTreeDataGridSource
    {
        private readonly IReadOnlyList<FastTreeDataGridRow> _rows;

        public SampleSource(IReadOnlyList<FastTreeDataGridRow> rows)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }

        public event EventHandler? ResetRequested;
        public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
        public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized;

        public int RowCount => _rows.Count;

        public bool SupportsPlaceholders => false;

        public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) =>
            new(RowCount);

        public ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var start = Math.Max(0, request.StartIndex);
            var end = Math.Min(_rows.Count, start + request.Count);
            if (end <= start)
            {
                return new ValueTask<FastTreeDataGridPageResult>(FastTreeDataGridPageResult.Empty);
            }

            var page = new List<FastTreeDataGridRow>(end - start);
            for (var i = start; i < end; i++)
            {
                var row = _rows[i];
                page.Add(row);
                RowMaterialized?.Invoke(this, new FastTreeDataGridRowMaterializedEventArgs(i, row));
            }

            return new ValueTask<FastTreeDataGridPageResult>(
                new FastTreeDataGridPageResult(page, Array.Empty<int>(), completion: null, cancellation: null));
        }

        public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
        {
            if ((uint)index < (uint)_rows.Count)
            {
                row = _rows[index];
                return true;
            }

            row = default!;
            return false;
        }

        public bool IsPlaceholder(int index) => false;

        public FastTreeDataGridRow GetRow(int index)
        {
            if ((uint)index >= (uint)_rows.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _rows[index];
        }

        public void ToggleExpansion(int index)
        {
            _ = index;
        }
    }

    private sealed class SampleValueProvider : IFastTreeDataGridValueProvider
    {
        private readonly IReadOnlyDictionary<string, object?> _values;
        private readonly int _index;

        public SampleValueProvider(int index, IReadOnlyDictionary<string, object?> values)
        {
            _index = index;
            _values = values;
        }

        public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

        public object? GetValue(object? item, string key)
        {
            if (string.Equals(key, "Design.Index", StringComparison.Ordinal))
            {
                return _index + 1;
            }

            if (_values.TryGetValue(key, out var value))
            {
                return value;
            }

            return $"Sample {key} {_index + 1}";
        }
    }

    private static class SampleValueCatalog
    {
        private static readonly string[] Statuses = { "Active", "Draft", "Pending", "Complete", "Disabled" };
        private static readonly string[] Categories = { "Alpha", "Beta", "Gamma", "Delta" };

        public static IReadOnlyList<IReadOnlyDictionary<string, object?>> Build(IReadOnlyList<string> keys)
        {
            var count = Math.Max(1, keys.Count);
            var rows = new List<IReadOnlyDictionary<string, object?>>(Math.Max(4, count));

            for (var i = 0; i < 6; i++)
            {
                var values = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var key in keys)
                {
                    values[key] = CreateValueForKey(key, i);
                }

                if (values.Count == 0)
                {
                    values["Design.Name"] = $"Sample Row {i + 1}";
                    values["Design.Status"] = Statuses[i % Statuses.Length];
                }

                values["Design.Index"] = i + 1;
                rows.Add(values);
            }

            return rows;
        }

        private static object? CreateValueForKey(string key, int index)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return $"Sample {index + 1}";
            }

            var keyName = key.AsSpan();
            if (keyName.Contains("price", StringComparison.OrdinalIgnoreCase) ||
                keyName.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
                keyName.Contains("amount", StringComparison.OrdinalIgnoreCase))
            {
                return (index + 1) * 19.95m;
            }

            if (keyName.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                keyName.Contains("time", StringComparison.OrdinalIgnoreCase))
            {
                return DateTimeOffset.Now.AddDays(-index).ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
            }

            if (keyName.Contains("status", StringComparison.OrdinalIgnoreCase))
            {
                return Statuses[index % Statuses.Length];
            }

            if (keyName.Contains("category", StringComparison.OrdinalIgnoreCase))
            {
                return Categories[index % Categories.Length];
            }

            if (keyName.Contains("percent", StringComparison.OrdinalIgnoreCase) ||
                keyName.Contains("ratio", StringComparison.OrdinalIgnoreCase))
            {
                return $"{10 + index * 7}%";
            }

            if (keyName.Contains("score", StringComparison.OrdinalIgnoreCase) ||
                keyName.Contains("value", StringComparison.OrdinalIgnoreCase))
            {
                return (index + 1) * 42;
            }

            return $"Sample {key} {index + 1}";
        }
    }
}
