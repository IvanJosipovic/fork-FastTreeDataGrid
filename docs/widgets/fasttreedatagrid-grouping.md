# FastTreeDataGrid Grouping Guide

FastTreeDataGrid ships with a pivot-style grouping UI that lets users drag column headers into a grouping band, reorder chips to change nesting, and view aggregate summaries per group level. This guide walks through enabling the feature, configuring descriptors, persisting layouts, and customizing the visual experience.

## Enabling Grouping

```csharp
var grid = new FastTreeDataGrid
{
    ItemsSource = new FastTreeDataGridFlatSource<MyRow>(rows, _ => Array.Empty<MyRow>()),
    CanUserGroup = true, // optional convenience helper
};
```

Grouping is opt-in per column. Set `FastTreeDataGridColumn.CanUserGroup` to `true` and provide either a `ValueKey` or a custom `IFastTreeDataGridGroupAdapter`.

```csharp
grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Region",
    ValueKey = "Region",
    CanUserGroup = true,
    GroupAdapter = new FastTreeDataGridValueGroupAdapter("Region"),
});
```

Users can now drag the “Region” header into the band to create the first grouping level.

## Programmatic Group Descriptors

You can define grouping levels up front by populating the grid’s `GroupDescriptors` collection.

```csharp
var descriptor = new FastTreeDataGridGroupDescriptor
{
    ColumnKey = "Region",
    Adapter = new FastTreeDataGridValueGroupAdapter("Region"),
    SortDirection = FastTreeDataGridSortDirection.Ascending,
    IsExpanded = true,
};

grid.GroupDescriptors.Add(descriptor);
```

Descriptors support aggregate definitions either globally (`grid.AggregateDescriptors`) or per level (`FastTreeDataGridGroupDescriptor.AggregateDescriptors`). The grouping pipeline caches aggregate results and will reuse them while groups remain expanded.

## Layout Persistence

The grid exposes helper APIs for saving and restoring grouping layouts:

```csharp
// Save
FastTreeDataGridGroupingLayout layout = grid.GetGroupingLayout();
string json = FastTreeDataGridGroupingLayoutSerializer.Serialize(layout);
File.WriteAllText("layout.json", json);

// Restore later
string restoredJson = File.ReadAllText("layout.json");
var restoredLayout = FastTreeDataGridGroupingLayoutSerializer.Deserialize(restoredJson);
if (restoredLayout is not null)
{
    grid.ApplyGroupingLayout(restoredLayout);
}
```

The `FastTreeDataGridGroupingLayout` payload includes the grouping order, per-level metadata, and expansion states. Layouts are versioned; older payloads default to version 1, while future major changes will surface a `NotSupportedException` with guidance to upgrade.

## Customizing Group Headers & Footers

Use the column-level `GroupHeader*` and `GroupFooter*` properties to provide bespoke visuals:

```csharp
regionColumn.GroupHeaderControlTemplate = new FuncDataTemplate<object>((item, _) =>
{
    return new Border
    {
        Background = Brushes.LightBlue,
        Padding = new Thickness(6, 4),
        Child = new TextBlock { Text = $"Region: {item}" }
    };
});

revenueColumn.GroupFooterWidgetFactory = (provider, _) =>
{
    var badge = new BadgeWidget { StyleKey = "RevenueBadge" };
    badge.Key = "FastTreeDataGrid.Group.Footer.Revenue";
    return badge;
};
```

The grouping band now uses Avalonia’s drag & drop infrastructure, so any custom controls must be `DragDrop.SetAllowDrop` friendly if they initiate drags.

## Custom Grouping Adapters

Implement `IFastTreeDataGridGroupAdapter` to control how rows map to grouping keys. The grouping demo includes a `RevenueBucketGroupAdapter` that bins revenue into three ranges:

```csharp
internal sealed class RevenueBucketGroupAdapter : IFastTreeDataGridGroupAdapter
{
    private static readonly IReadOnlyList<(decimal Threshold, string Label)> Buckets = new[]
    {
        (100_000m, "<$100K"),
        (250_000m, "$100K - $249K"),
        (decimal.MaxValue, "$250K+"),
    };

    public object? GetGroupKey(FastTreeDataGridRow row)
    {
        if (row.Item is not SalesRecord record)
        {
            return "Unknown";
        }

        foreach (var bucket in Buckets)
        {
            if (record.Revenue < bucket.Threshold)
            {
                return bucket.Label;
            }
        }

        return Buckets[^1].Label;
    }

    public string GetGroupLabel(object? key, int level, int itemCount) => $"{key} ({itemCount:N0} items)";

    public IComparer<object?>? Comparer => StringComparer.Ordinal;
}
```

Attach the adapter to a column and add a preset that references the column key:

```csharp
revenueColumn.GroupAdapter = new RevenueBucketGroupAdapter();

Presets = new[]
{
    new GroupPreset(
        "Region ▸ Revenue Band",
        "Group by region and revenue bucket",
        new[]
        {
            new GroupDescriptorSpec(SalesRecord.KeyRegion, "Region"),
            new GroupDescriptorSpec(SalesRecord.KeyRevenue, "Revenue Band"),
        }),
};
```

Running the demo shows “Region ▸ Revenue Band” instantly grouping rows using the custom bucket logic.

## Lazy Materialization & Prefetching

The `FastTreeDataGridFlatSource` only materializes child groups when their parent is expanded, reducing the work required for large hierarchies. When a user expands a group, the grid automatically requests a prefetch from the virtualization provider to hydrate the next rows. Prefetching is throttled to avoid repeated requests when users toggle rapidly.

### Monitoring Performance

Grouping emits counters and logs through `FastTreeDataGridVirtualizationDiagnostics` so you can track cache behavior:

- `fasttree_datagrid_cache_hits` / `fasttree_datagrid_cache_misses` expose aggregate cache effectiveness (hits increase when collapsed groups are expanded again).
- Prefetch operations log category `PrefetchAsync` with the request range, visible via `FastTreeDataGridVirtualizationDiagnostics.LogCallback`.

Attach your own listener or forward metrics to `System.Diagnostics.Metrics` to feed dashboards when tuning virtualization settings.

### Async Source Best Practices

- `FastTreeDataGridFlatSource` cancels pending sort/group operations before scheduling the latest request, ensuring user gestures (dragging chips repeatedly) do not enqueue long-running work. When implementing custom sources, adopt the same pattern—link cancellation tokens from UI commands and cancel/flush queued work before issuing a new regroup.
- Respect cancellation tokens in `ApplyGroupingAsync` / `ApplySortFilterAsync` implementations and avoid swallowing `OperationCanceledException` silently; cancellation should leave the data source in its previous state.
- Batch remote updates and raise `ResetRequested` once per logical change instead of per-row to keep virtualization responsive.

## Demo Walkthrough

`samples/FastTreeDataGrid.GroupingDemo` demonstrates:

- Applying presets that configure grouping descriptors.
- Saving and restoring layouts through the new JSON helpers.
- Viewing the persisted layout JSON.

Launch the sample, drag headers into the band, and use the “Save Layout” button to capture your current configuration. The “Restore Layout” button rehydrates the saved state, including expansion flags.

## Best Practices

- When building presets, clone aggregate descriptors so each descriptor owns its copy.
- Persist grouping layouts alongside other grid state (sort, filter, column widths) to offer full workspace restoration.
- Respect the layout version during deserialization and implement migration logic when bumping the schema.
- For high-volume datasets, ensure your virtualization provider honours `PrefetchAsync` requests to keep scrolling smooth after expansion changes.

## Styling and Template Overrides

The control exposes resource keys and template hooks so you can restyle grouping surfaces without rewriting control logic:

- `FastTreeDataGrid.GroupingBandBackground` / `FastTreeDataGrid.GroupingBandBorder` control the band host brush and border.
- `FastTreeDataGrid.GroupingChipStyle` re-templated chips inside the band (Style targeting `Border`).
- `FastTreeDataGrid.GroupingDropIndicatorBrush` controls the vertical drop indicator color.
- `FastTreeDataGrid.DefaultGroupRowTemplate` is a data template applied to group rows when you set `FastTreeDataGrid.GroupRowTemplate`.
- `FastTreeDataGrid.GroupSummaryBackground` sets the footer row background.

Declare overrides in your resource dictionary:

```xml
<ResourceDictionary>
  <SolidColorBrush x:Key="FastTreeDataGrid.GroupingBandBackground" Color="#FFEDEFFF" />
  <Style x:Key="FastTreeDataGrid.GroupingChipStyle" Selector="Border">
    <Setter Property="Background" Value="#FFF6F8FF" />
    <Setter Property="CornerRadius" Value="12" />
  </Style>
</ResourceDictionary>
```

To swap the default group row visuals:

```csharp
grid.Resources["FastTreeDataGrid.GroupRowTemplate"] = new FuncDataTemplate<object>((item, _) =>
{
    var context = (FastTreeDataGridGroupRow)item;
    return new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Children =
        {
            new TextBlock { Text = context.HeaderText, FontWeight = FontWeight.Bold },
            new TextBlock { Text = $" · {context.ItemCount:N0} records", Margin = new Thickness(6,0,0,0) },
        }
    };
});
```

Summaries follow the same pattern via `FastTreeDataGrid.GroupSummaryTemplate`.

When staying in code, the column’s `WidgetFactory` and the group-specific factories (`GroupHeaderWidgetFactory`, `GroupFooterWidgetFactory`) can coexist. The grid falls back to `WidgetFactory` (or `CellTemplate`) when header/footer factories aren’t supplied, so custom header chips or summaries don’t disrupt existing widget usage. The Grouping demo’s “Product” column uses a `WidgetFactory` that returns a `BadgeWidget`, demonstrating that grouping/interactions keep custom renderers intact.

### Column Metadata Interactions

Grouping is layered on top of the existing column metadata system, so the descriptors you already configure continue to drive rendering and behavior:

- **Value keys**: `FastTreeDataGridGroupingDescriptor` instances derive their `ColumnKey` from `FastTreeDataGridColumn.ValueKey`, ensuring the same value provider powers sorting, filtering, editing and group headers. If you materialise descriptors yourself, reuse the factory helper (`FastTreeDataGridGroupingDescriptorFactory.CreateFromColumn`) so keys and comparer metadata stay aligned.
- **Widget & control templates**: hierarchy columns fall back to `GroupRowTemplate`/`GroupSummaryTemplate`, while every other column keeps using its `WidgetFactory`, `CellTemplate`, or `EditTemplate`. This means inline editors, badge widgets, or validation decorations automatically appear inside grouped views without extra wiring.
- **Sorting & filtering**: column level `SortDirection`, custom comparers, and `FilterFactory` predicates are honoured when groups are applied; the grid simply replays the metadata into the grouping descriptors and the filter pipeline so active filters don’t disappear when a header is dragged into the band.
- **Aggregates**: column `AggregateDescriptors` are cloned into the matching group descriptors, so any per-column totals or averages you already surface will light up in summaries as soon as the column is grouped.

### Aggregates in Headers or Chips

`FastTreeDataGridGroupDescriptor.AggregateDescriptors` describes aggregates for header badges or summaries. Aggregates can be:

- **Global** via `grid.AggregateDescriptors`, applied to every group plus the grid footer.
- **Per-level** via `descriptor.AggregateDescriptors`, scoped to a specific grouping level.

When aggregates target the hierarchy column (i.e., `ColumnKey` is `null`), the result is rendered inline with the group header. Setting `ColumnKey` maps the value into that column instead.

Samples:

```csharp
var descriptor = new FastTreeDataGridGroupDescriptor
{
    ColumnKey = SalesRecord.KeyRegion,
    Adapter = new FastTreeDataGridValueGroupAdapter(SalesRecord.KeyRegion),
};

descriptor.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
{
    ColumnKey = SalesRecord.KeyRevenue,
    Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
    Aggregator = rows => rows.Sum(r => ((SalesRecord)r.Item!).Revenue),
    Formatter = SalesRecord.FormatCurrency,
});
```

Async aggregates can be supplied by setting `IFastTreeDataGridAggregateProvider` on the descriptor instead of `Aggregator`; the result appears once the asynchronous computation completes.

Grouping aggregates pass through the same widget factory/custom template hooks described earlier, so footer rows can host complex visuals via `GroupFooterWidgetFactory` or templates.
