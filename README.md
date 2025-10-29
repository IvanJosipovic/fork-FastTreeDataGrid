# FastTreeDataGrid

FastTreeDataGrid is a high-performance tree data grid for Avalonia UI that renders hierarchical datasets directly onto a canvas-backed surface. The control pairs a pluggable FlatTreeDataGrid engine with an immediate-mode widget system so large trees stay responsive while delivering rich cell visuals.

## Packages

FastTreeDataGrid is split into a few NuGet packages:

| Package | NuGet | Downloads | Description |
| --- | --- | --- | --- |
| `FastTreeDataGrid.Engine` | [![NuGet](https://img.shields.io/nuget/v/FastTreeDataGrid.Engine.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Engine) | [![Downloads](https://img.shields.io/nuget/dt/FastTreeDataGrid.Engine.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Engine) | Platform-neutral engine that flattens, groups, and virtualizes hierarchical data sources. |
| `FastTreeDataGrid.Control` | [![NuGet](https://img.shields.io/nuget/v/FastTreeDataGrid.Control.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Control) | [![Downloads](https://img.shields.io/nuget/dt/FastTreeDataGrid.Control.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Control) | Avalonia UI control that renders FastTreeDataGrid data with canvas-backed virtualization. |
| `FastTreeDataGrid.Widgets` | [![NuGet](https://img.shields.io/nuget/v/FastTreeDataGrid.Widgets.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Widgets) | [![Downloads](https://img.shields.io/nuget/dt/FastTreeDataGrid.Widgets.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Widgets) | Immediate-mode widget library used by the control templates and samples. |
| `FastTreeDataGrid.Analyzers` | [![NuGet](https://img.shields.io/nuget/v/FastTreeDataGrid.Analyzers.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Analyzers) | [![Downloads](https://img.shields.io/nuget/dt/FastTreeDataGrid.Analyzers.svg)](https://www.nuget.org/packages/FastTreeDataGrid.Analyzers) | Roslyn analyzers that surface diagnostics and fixes for FastTreeDataGrid usage. |

The legacy `FastTreeDataGrid.Core` package has been collapsed into the control. Existing `using FastTreeDataGrid.Control.Infrastructure` statements targeting datasource/virtualization types should now point to `FastTreeDataGrid.Engine.Infrastructure`.

## Quick Usage Guide

### Column templates with Avalonia controls

Reuse your existing Avalonia control templates by supplying `CellControlTemplate` or `CellTemplate` with an Avalonia `DataTemplate`. Cells receive a lightweight `FastTreeDataGridCell` that exposes the bound item (`Item`) and the computed value (`Value`), so you can combine visuals, icons, and formatted text in a single cell.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:YourApp.ViewModels">
    <FastTreeDataGrid ItemsSource="{Binding Countries}"
                      RowHeight="32">
        <FastTreeDataGrid.Columns>
            <FastTreeDataGridColumn Header="Country"
                                    ValueKey="Name"
                                    IsHierarchy="True">
                <FastTreeDataGridColumn.CellControlTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal"
                                    Spacing="8"
                                    VerticalAlignment="Center">
                            <Image Width="16"
                                   Height="12"
                                   Source="{Binding Item.Flag}" />
                            <TextBlock Text="{Binding Value}"
                                       FontWeight="SemiBold" />
                        </StackPanel>
                    </DataTemplate>
                </FastTreeDataGridColumn.CellControlTemplate>
            </FastTreeDataGridColumn>

            <FastTreeDataGridColumn Header="Population"
                                    ValueKey="Population"
                                    SizingMode="Pixel"
                                    PixelWidth="200">
                <FastTreeDataGridColumn.CellControlTemplate>
                    <DataTemplate>
                        <Grid ColumnDefinitions="Auto,*"
                              VerticalAlignment="Center"
                              Margin="0,2">
                            <TextBlock Text="{Binding Value,
                                                      StringFormat={}{0:N0},
                                                      TargetNullValue=-}"
                                       VerticalAlignment="Center" />
                            <ProgressBar Grid.Column="1"
                                         Margin="12,0,0,0"
                                         Minimum="0"
                                         Maximum="1500000000"
                                         Height="4"
                                         Value="{Binding Value, TargetNullValue=0}"
                                         IsHitTestVisible="False" />
                        </Grid>
                    </DataTemplate>
                </FastTreeDataGridColumn.CellControlTemplate>
            </FastTreeDataGridColumn>

            <FastTreeDataGridColumn Header="Region"
                                    ValueKey="Region">
                <FastTreeDataGridColumn.CellControlTemplate>
                    <DataTemplate>
                        <Border Background="{DynamicResource ThemeAccentBrush2}"
                                CornerRadius="4"
                                Padding="6,2"
                                VerticalAlignment="Center">
                            <TextBlock Text="{Binding Item.Region}"
                                       Foreground="{DynamicResource ThemeForegroundOnAccentBrush}" />
                        </Border>
                    </DataTemplate>
                </FastTreeDataGridColumn.CellControlTemplate>
            </FastTreeDataGridColumn>
        </FastTreeDataGrid.Columns>
    </FastTreeDataGrid>
</UserControl>
```

`YourApp.ViewModels.CountryRow` (referenced through `Countries`) exposes the backing data—use the `Item` field for raw domain objects and the `Value` property for the column’s resolved value. This approach keeps the familiar Avalonia control toolchain while benefiting from the grid’s virtualization pipeline.

```csharp
public sealed class CountriesViewModel
{
    public FastTreeDataGridFlatSource<CountryRow> Countries { get; } =
        new FastTreeDataGridFlatSource<CountryRow>(
            rootItems: CountryRow.CreateSampleData(),
            childrenSelector: row => row.Children,
            keySelector: row => row.Id);
}

public sealed class CountryRow : IFastTreeDataGridValueProvider
{
    public const string KeyName = "Name";
    public const string KeyPopulation = "Population";
    public const string KeyRegion = "Region";

    private readonly List<CountryRow> _children = new();

    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public long Population { get; init; }
    public string Region { get; init; } = string.Empty;
    public IBitmap? Flag { get; init; }

    public IReadOnlyList<CountryRow> Children => _children;

    public object? GetValue(object? _, string key) => key switch
    {
        KeyName => Name,
        KeyPopulation => Population,
        KeyRegion => Region,
        _ => null
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public CountryRow AddChild(CountryRow child)
    {
        _children.Add(child);
        return this;
    }

    public static IEnumerable<CountryRow> CreateSampleData()
    {
        yield return new CountryRow
        {
            Name = "Canada",
            Population = 38_000_000,
            Region = "North America"
        };

        yield return new CountryRow
        {
            Name = "Japan",
            Population = 125_000_000,
            Region = "Asia"
        };
    }
}
```

> Note: Add `using Avalonia.Media.Imaging;` (or whichever namespace exposes `IBitmap` in your project) for the `Flag` property if you display flag images or icons.

Each row implements `IFastTreeDataGridValueProvider`, so the templates gain access to both the strongly typed `Item` for complex visuals (flag images, tags) and the per-column `Value` for quick formatting.

### XAML-first: drop-in control

Reference `FastTreeDataGrid.Control` from your Avalonia project. The assembly’s `XmlnsDefinition` attributes surface the types on the default Avalonia namespace, so you can drop the control straight into XAML and bind `ItemsSource` to a `FastTreeDataGridFlatSource<T>` exposed from your view model.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <FastTreeDataGrid ItemsSource="{Binding FileSystem}"
                      RowHeight="28"
                      SelectionMode="Extended">
        <FastTreeDataGrid.Columns>
            <FastTreeDataGridColumn Header="Name"
                                    ValueKey="Name"
                                    SizingMode="Pixel"
                                    PixelWidth="240"
                                    IsHierarchy="True" />
            <FastTreeDataGridColumn Header="Type"
                                    ValueKey="Kind"
                                    SizingMode="Pixel"
                                    PixelWidth="140" />
            <FastTreeDataGridColumn Header="Size"
                                    ValueKey="Size"
                                    SizingMode="Star"
                                    StarValue="1" />
        </FastTreeDataGrid.Columns>
    </FastTreeDataGrid>
</UserControl>
```

Expose the source from your view model:

```csharp
public sealed class ExplorerViewModel
{
    public FastTreeDataGridFlatSource<FileNode> FileSystem { get; } =
        new FastTreeDataGridFlatSource<FileNode>(
            rootItems: FileNode.LoadRoots(),
            childrenSelector: node => node.Children,
            keySelector: node => node.Id);
}

public sealed class FileNode : IFastTreeDataGridValueProvider
{
    public const string KeyName = "Name";
    public const string KeyKind = "Kind";
    public const string KeySize = "Size";

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Size { get; init; } = string.Empty;
    public IReadOnlyList<FileNode> Children { get; init; } = Array.Empty<FileNode>();

    public object? GetValue(object? _, string key) => key switch
    {
        KeyName => Name,
        KeyKind => Kind,
        KeySize => Size,
        _ => null
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;
}
```

Only rows that implement `IFastTreeDataGridValueProvider` need to expose values; other view models can be wrapped with a lightweight adapter if preferred.

### XAML + widgets: templated cells

Add the `FastTreeDataGrid.Widgets` package to your project and use `WidgetTemplate` to render highly customised cells without touching control templates—the widgets share the same default XML namespace registration. You can also mix widget-backed columns with Avalonia control templates for scenarios that benefit from existing controls.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <FastTreeDataGrid ItemsSource="{Binding Metrics}"
                      RowHeight="32">
        <FastTreeDataGrid.Columns>
            <FastTreeDataGridColumn Header="Service"
                                    ValueKey="Service"
                                    SizingMode="Pixel"
                                    PixelWidth="200" />
            <FastTreeDataGridColumn Header="CPU"
                                    ValueKey="Cpu"
                                    SizingMode="Star">
                <FastTreeDataGridColumn.CellTemplate>
                    <WidgetTemplate>
                        <ProgressWidget Key="Cpu" />
                    </WidgetTemplate>
                </FastTreeDataGridColumn.CellTemplate>
            </FastTreeDataGridColumn>
            <FastTreeDataGridColumn Header="Alerts"
                                    ValueKey="Alerts"
                                    SizingMode="Pixel"
                                    PixelWidth="120">
                <FastTreeDataGridColumn.CellTemplate>
                    <WidgetTemplate>
                        <BadgeWidget Key="Alerts"
                                     CornerRadius="8" />
                    </WidgetTemplate>
                </FastTreeDataGridColumn.CellTemplate>
            </FastTreeDataGridColumn>
            <FastTreeDataGridColumn Header="Owner"
                                    ValueKey="Owner"
                                    SizingMode="Pixel"
                                    PixelWidth="180">
                <FastTreeDataGridColumn.CellControlTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal"
                                    Spacing="4">
                            <Border Background="{DynamicResource ThemeAccentBrush}"
                                    Width="6"
                                    CornerRadius="3" />
                            <TextBlock Text="{Binding Value,
                                                        RelativeSource={RelativeSource AncestorType=FastTreeDataGridCell}}"
                                       FontWeight="SemiBold"
                                       VerticalAlignment="Center" />
                            <TextBlock Text="{Binding Item.Team}"
                                       Foreground="{DynamicResource ThemeForegroundLowBrush}"
                                       VerticalAlignment="Center" />
                        </StackPanel>
                    </DataTemplate>
                </FastTreeDataGridColumn.CellControlTemplate>
            </FastTreeDataGridColumn>
        </FastTreeDataGrid.Columns>
    </FastTreeDataGrid>
</UserControl>
```

Widgets inherit from the same `Widget` base class used internally, so they react to virtualization, value updates, and pointer gestures automatically. The `CellControlTemplate` column uses the control-based pipeline—`FastTreeDataGrid` creates a lightweight `ContentControl` per cell and binds its `DataContext` to the `FastTreeDataGridCell` wrapper so standard Avalonia bindings (like `RelativeSource`) continue to work.

### Code-only: configure in view models or controls

You can construct the grid entirely in code (handy for dynamically generated UIs or integration tests). Import `FastTreeDataGrid.Control`, `FastTreeDataGrid.Engine.Infrastructure`, `FastTreeDataGrid.Engine.Models`, and `FastTreeDataGrid.Control.Widgets` namespaces, then build the control:

```csharp
var source = new FastTreeDataGridFlatSource<ProcessRow>(
    rootItems: ProcessRow.CreateSnapshot(),
    childrenSelector: row => row.Children,
    keySelector: row => row.Id);

var grid = new FastTreeDataGrid
{
    ItemsSource = source,
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 512,
        PrefetchRadius = 2
    }
};

grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Process",
    ValueKey = ProcessRow.KeyName,
    IsHierarchy = true,
    SizingMode = ColumnSizingMode.Pixel,
    PixelWidth = 260
});

grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "CPU %",
    ValueKey = ProcessRow.KeyCpu,
    SizingMode = ColumnSizingMode.Star,
    CellTemplate = new FuncWidgetTemplate(() => new ProgressWidget { Key = ProcessRow.KeyCpu })
});

grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Memory",
    ValueKey = ProcessRow.KeyMemory,
    SizingMode = ColumnSizingMode.Pixel,
    PixelWidth = 140
});
```

`ProcessRow` in this example is your view model type that exposes static key constants and implements `IFastTreeDataGridValueProvider` so the grid can fetch values on demand.

### Advanced features at a glance

- **Data virtualization** – Stream massive datasets without blocking the UI. Swap the in-memory source for `FastTreeDataGridAsyncSource<T>` (or your own `IFastTreeDataVirtualizationProvider`) and the grid automatically pages data, shows placeholders, and throttles fetches around the viewport.

  ```csharp
  var catalog = new FastTreeDataGridAsyncSource<CatalogItem>(
      loadItemsAsync: token => inventoryClient.LoadPageAsync(token),
      childrenSelector: item => item.Children,
      keySelector: item => item.Id);

  grid.ItemsSource = catalog;
  grid.VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
  {
      PageSize = 256,
      PrefetchRadius = 3,
      ShowPlaceholderSkeletons = true
  };

  grid.GetObservable(FastTreeDataGrid.IsLoadingProperty)
      .Subscribe(isLoading => loadingIndicator.IsVisible = isLoading);
  ```

  Bind the `IsLoading` property to a progress ring or status text so users always know when additional pages are streaming in.

  For bespoke transports (REST, SQL, cloud storage) expose prefetch hints and cancellation through a custom `IFastTreeDataVirtualizationProvider`; the grid reuses the same scheduling, placeholder, and caching logic.

- **Live updates & streaming** – Keep dashboards and monitoring views current without rebuilding the tree. `FastTreeDataGridStreamingSource<T>` snapshots the existing rows, preserves hierarchy expansion, and applies incremental updates from observables or async streams.

  ```csharp
  var stream = new FastTreeDataGridStreamingSource<MetricRow>(
      initialItems: metricsProvider.InitialRows,
      childrenSelector: row => row.Children,
      keySelector: row => row.Id);

  using var subscription = stream.Connect(metricsProvider.Updates);
  grid.ItemsSource = stream;
  stream.ApplyUpdate(rows => metricsProvider.PruneExpired(rows));
  ```

  Streaming sources support `IObservable<FastTreeDataGridStreamUpdate<T>>` and `IAsyncEnumerable`, so you can bridge Rx pipelines, gRPC streams, message queues, or background agents.

- **Grouping, aggregates, and pinned columns** – Layer pivot-style groupings on any column, calculate custom aggregates, and pin critical metrics to either edge of the grid.

  ```csharp
  // Requires using System.Linq;
  grid.GroupDescriptors.Add(new FastTreeDataGridGroupDescriptor
  {
      ColumnKey = OrdersRow.KeyRegion,
      DisplayName = "Region",
      GroupAdapter = new FastTreeDataGridValueGroupAdapter(OrdersRow.KeyRegion)
  });

  var revenueColumn = grid.Columns.First(column => column.ValueKey == OrdersRow.KeyRevenue);

  revenueColumn.AggregateDescriptors.Add(new FastTreeDataGridAggregateDescriptor
  {
      ColumnKey = OrdersRow.KeyRevenue,
      Placement = FastTreeDataGridAggregatePlacement.GroupFooter,
      Aggregator = rows =>
          rows.Sum(row => (decimal?)row.ValueProvider?.GetValue(row.Item, OrdersRow.KeyRevenue) ?? 0m),
      Formatter = total => total is decimal value ? value.ToString("C0") : "—",
      Label = "Total revenue"
  });

  revenueColumn.PinnedPosition = FastTreeDataGridPinnedPosition.Right;
  ```

  Combine `GroupDescriptors`, `AggregateDescriptors`, and custom adapters to persist layouts, sort nested groups, or surface subtotal bars inline.

- **Variable heights, overlays, and row adorners** – Mix dense lists with expandable detail panes. Implement `IFastTreeDataGridRowHeightAware` on view models or plug in `FastTreeDataGridDefaultVariableRowHeightProvider` to calculate heights per row, then use overlays for context.

  ```csharp
  grid.VirtualizationSettings.VariableRowHeightProvider =
      new FastTreeDataGridDefaultVariableRowHeightProvider(minHeight: 20, maxHeight: 160);

  public sealed class TimelineRow : IFastTreeDataGridValueProvider, IFastTreeDataGridRowHeightAware
  {
      public double GetRowHeight(FastTreeDataGridRow row, double defaultHeight) =>
          IsExpanded ? defaultHeight * 3 : defaultHeight;

      // ...IFastTreeDataGridValueProvider implementation...
  }
  ```

  Pair variable heights with `WidgetOverlayManager` to display notes, error badges, or drag indicators that float above the grid.

- **Inline editing & validation** – Reuse Avalonia controls for editing and tap into the full lifecycle (`CellEditStarting`, `CellEditCommitting`, etc.) for validation.

  ```csharp
  // using Avalonia.Controls;
  // using Avalonia.Controls.Templates;
  // using Avalonia.Data;
  var statusColumn = new FastTreeDataGridColumn
  {
      Header = "Status",
      ValueKey = WorkItemRow.KeyStatus,
      CellControlTemplate = new FuncDataTemplate<WorkItemRow>((row, _) =>
          new TextBlock { Text = row.Status, VerticalAlignment = VerticalAlignment.Center }),
      EditTemplate = new FuncDataTemplate<WorkItemRow>((_, _) =>
      {
          var combo = new ComboBox
          {
              ItemsSource = WorkItemRow.AllStatuses,
              MinWidth = 140
          };
          combo.Bind(ComboBox.SelectedItemProperty, new Binding("Item.Status")
          {
              Mode = BindingMode.TwoWay,
              UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
          });
          return combo;
      })
  };

  grid.Columns.Add(statusColumn);

  grid.CellEditCommitting += (_, e) =>
  {
      if (e.Column == statusColumn && e.Editor is ComboBox combo && combo.SelectedItem is null)
      {
          e.Cancel = true; // keep the editor open until a valid status is chosen
      }
  };
  ```

  `WorkItemRow` in this example is a domain model that exposes `KeyStatus`, `Status`, and a static `AllStatuses` collection while implementing `IFastTreeDataGridValueProvider`.

  The editing pipeline cooperates with data validation attributes and raises visual error adorners automatically when an edit is rejected.

- **Row reorder & drag/drop** – Let users reprioritise backlogs or reorder playlists with built-in drag/drop. Configure previews, drop indicators, and persistence through `FastTreeDataGridRowReorderSettings` and `IFastTreeDataGridRowReorderHandler`.

  ```csharp
  // using Avalonia.Media;
  grid.RowReorderSettings = new FastTreeDataGridRowReorderSettings
  {
      IsEnabled = true,
      ActivationThreshold = 4,
      DragPreviewBrush = Brushes.SteelBlue.ToImmutable(),
      UseSelection = true
  };

  public sealed class BacklogSource : FastTreeDataGridFlatSource<BacklogItem>, IFastTreeDataGridRowReorderHandler
  {
      public BacklogSource(IEnumerable<BacklogItem> items)
          : base(items, item => item.Children, item => item.Id) { }

      public bool CanReorder(FastTreeDataGridRowReorderRequest request) => request.SourceIndices.Count > 0;

      public Task<FastTreeDataGridRowReorderResult> ReorderAsync(
          FastTreeDataGridRowReorderRequest request,
          CancellationToken cancellationToken)
      {
          MoveItems(request.SourceIndices, request.InsertIndex);
          return Task.FromResult(FastTreeDataGridRowReorderResult.Successful());
      }
  }
  ```

  `BacklogItem` is your domain entity (often implementing `IFastTreeDataGridValueProvider`)—move items in your repository within `ReorderAsync` and return the static `FastTreeDataGridRowReorderResult.Successful()` helper.

  Handle `RowReordering`/`RowReordered` events to audit changes or veto moves when business rules require it.

- **Observability & diagnostics** – Track performance with zero-effort telemetry. `FastTreeDataGridVirtualizationDiagnostics` publishes counters/histograms via `System.Diagnostics.Metrics` and exposes a structured log callback.

  ```csharp
  // using System.Diagnostics.Metrics;
  // ILogger logger = ...
  var listener = new MeterListener
  {
      InstrumentPublished = (instrument, l) =>
      {
          if (instrument.Meter == FastTreeDataGridVirtualizationDiagnostics.Meter)
          {
              l.EnableMeasurementEvents(instrument);
          }
      }
  };

  listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
  {
      if (instrument == FastTreeDataGridVirtualizationDiagnostics.PageFetchDuration)
      {
          logger.LogDebug("Page fetched in {Duration:F1} ms", measurement);
      }
  });

  listener.Start();
  FastTreeDataGridVirtualizationDiagnostics.LogCallback = entry =>
      logger.LogInformation("[{Category}] {Message}", entry.Category, entry.Message);
  ```

  Coupling the built-in `Meter` with your logging/metrics pipeline gives you first-class visibility into fetch latency, cache efficiency, and reset frequency.

  Feed the metrics into OpenTelemetry, Prometheus, or Application Insights to spot bottlenecks and tune virtualization settings.


## Data source options

FastTreeDataGrid decouples rendering from data access using lightweight source abstractions. Pick the source that matches your data workflow or implement your own for bespoke scenarios.

### `FastTreeDataGridFlatSource<T>`

Ideal for in-memory trees or view models where data is already materialised. You supply the root items, child selector, and optional stable key; the grid handles flattening, sorting, filtering, grouping, and row reorder in memory.

```csharp
var fileSystem = new FastTreeDataGridFlatSource<FileNode>(
    rootItems: FileNode.LoadRoots(),
    childrenSelector: node => node.Children,
    keySelector: node => node.Id);

fileSystem.ToggleExpansion(index: 0);
fileSystem.InvalidateAsync(FastTreeDataGridInvalidationRequest.Full, CancellationToken.None);
grid.ItemsSource = fileSystem;
```

Flat sources implement `IFastTreeDataGridRowReorderHandler`, `IFastTreeDataGridGroupingController`, and `IFastTreeDataGridSortFilterHandler`, so the grid can delegate advanced behaviours without extra glue code.

### `FastTreeDataGridAsyncSource<T>`

Best when data lives elsewhere (REST, SQL, gRPC) or the dataset is too large to load eagerly. You provide async delegates for loading children; the grid orchestrates paging, placeholders, cancellation, and concurrency limits.

```csharp
var catalog = new FastTreeDataGridAsyncSource<CatalogItem>(
    loadItemsAsync: async token => await inventoryClient.LoadHierarchyAsync(token),
    childrenSelector: item => item.Children,
    keySelector: item => item.Id);

grid.ItemsSource = catalog;
```

Invalidate ranges or the full view when your backend notifies the client of changes—`FastTreeDataGridAsyncSource<T>` will recalc the visible rows while keeping placeholders onscreen.

### `FastTreeDataGridStreamingSource<T>`

Use this for live dashboards or collaborative boards that receive steady updates. Seed it with the initial snapshot, then connect observables/async streams to flow mutations through the grid without reloading everything.

```csharp
var stream = new FastTreeDataGridStreamingSource<MetricRow>(
    initialItems: metricsProvider.InitialRows,
    childrenSelector: row => row.Children,
    keySelector: row => row.Id);

using var subscription = stream.Connect(metricsProvider.Updates);
grid.ItemsSource = stream;

stream.ApplyUpdate(rows => metricsProvider.PruneExpired(rows));
```

Streaming sources preserve expansion, grouping, selection, and scroll position while diffing rows in place.

### `FastTreeDataGridHybridSource<T>`

Combine cached data with remote refresh. `FastTreeDataGridHybridSource<T>` loads an initial snapshot, applies streaming updates as they arrive, and falls back to async fetches when a region isn’t cached.

```csharp
var hybrid = new FastTreeDataGridHybridSource<MetricsNode>(
    loader: metricsClient.LoadInitialAsync,
    refresher: metricsClient.RefreshAsync,
    childrenSelector: node => node.Children,
    keySelector: node => node.Id);

grid.ItemsSource = hybrid;
```

Hybrid sources are great for dashboards that need immediate local responsiveness with periodic server reconciliation.

### Custom sources and providers

- Implement `IFastTreeDataGridSource` to plug in an existing virtualised tree representation.
- Implement `IFastTreeDataVirtualizationProvider` when you want full control over paging, caches, and placeholders (the grid will drive it through `FastTreeDataGridViewportScheduler`).
- Implement `IFastTreeDataGridValueProvider` on your row types so widgets and control templates can extract values on demand without reflection or standard Avalonia bindings.

Because sources are swappable, you can prototype with a flat in-memory source, then drop in an async provider as your dataset grows—all without rewriting view or column definitions.


## Widget Value Binding

Widgets bypass Avalonia’s `Binding` system and resolve values directly through `FastTreeDataGrid` row providers:

- `FastTreeDataGridColumn.ValueKey` identifies the value to pull for the column. The grid copies that key into the widget’s `Widget.Key` when it instantiates a cell.
- `FastTreeDataGridRow` caches the item’s `IFastTreeDataGridValueProvider`. When a cell is realised, the grid calls `Widget.UpdateValue(provider, item)` so the widget can synchronously fetch the value it needs.
- `IFastTreeDataGridValueProvider.GetValue(item, key)` returns the value to display. Widgets accept primitives, formatted strings, or rich records such as `BadgeWidgetValue` and `ProgressWidgetValue`.
- When a value changes, raise `ValueInvalidated` so the grid re-queries just the affected widgets instead of walking the Avalonia binding tree.
- Virtualisation keeps the update surface small—only realised rows invoke `UpdateValue`, and cached providers mean there is no reflection or property-path parsing during scrolling.

```csharp
public sealed class MetricRow : IFastTreeDataGridValueProvider, INotifyPropertyChanged
{
    public const string KeyService = "Service";
    public const string KeyCpu = "Cpu";
    public const string KeyAlerts = "Alerts";

    private double _cpu;
    private string _service = string.Empty;
    private int _alerts;

    public object? GetValue(object? _, string key) => key switch
    {
        KeyService => _service,
        KeyCpu => new ProgressWidgetValue { Progress = _cpu / 100 },
        KeyAlerts => _alerts == 0 ? "—" : _alerts.ToString(),
        _ => null
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;
    public event PropertyChangedEventHandler? PropertyChanged;

    public double Cpu
    {
        get => _cpu;
        set
        {
            if (Math.Abs(_cpu - value) <= double.Epsilon)
            {
                return;
            }

            _cpu = value;
            OnPropertyChanged(nameof(Cpu));
            ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, KeyCpu));
        }
    }

    // Other properties set _service/_alerts, call OnPropertyChanged, and raise ValueInvalidated likewise.

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

List-style widgets (`ItemsControlWidget`, `ListBoxWidget`, etc.) follow the same pattern. Set `ItemsSource` to an `IFastTreeDataGridSource`, pick a `ValueKey` inside your item template, and widgets will pull the correct value whenever the item is realised.

## Widget Catalog

Every widget draws straight to Avalonia’s `DrawingContext`, providing lightweight equivalents for familiar controls. The tables below list the public widgets shipped with the toolkit.

### Visual widgets

| Widget | Avalonia analogue | Notes |
| --- | --- | --- |
| `AccessTextWidget` | `AccessText` | Renders text with access-key underscores. |
| `ArcShapeWidget` | `Path`/`Arc` | Draws arc segments for gauges and charts. |
| `BadgeWidget` | `Badge` | Pill badge that supports counts, colours, and corner radius. |
| `BorderWidget` | `Border` | Single-child container with custom padding, stroke, and background. |
| `ChartWidget` | Chart controls | Sparkline-style chart rendering without templated controls. |
| `CustomDrawWidget` | `DrawingPresenter` | Exposes a delegate for bespoke immediate drawing. |
| `FormattedTextWidget` | `TextBlock` | Multi-line formatted text with trimming and wrapping. |
| `GeometryWidget` | `Path` | Paints arbitrary geometries provided at runtime. |
| `GlyphRunWidget` | `GlyphRun` | Low-level glyph rendering for perf-critical text. |
| `IconElementWidget` | `IconElement` | Hosts fluent icon elements with theme-aware colours. |
| `IconWidget` | `PathIcon`/`Image` | Displays vector or bitmap icons. |
| `ImageWidget` | `Image` | Draws bitmaps with stretch modes. |
| `LineShapeWidget` | `Line` | Straight line drawing with thickness and caps. |
| `PathShapeWidget` | `Path` | General-purpose path renderer with fill/stroke support. |
| `PolygonShapeWidget` | `Polygon` | Renders closed polygons. |
| `PolylineShapeWidget` | `Polyline` | Renders open polyline segments. |
| `ProgressWidget` | `ProgressBar` | Determinate/indeterminate progress indicator. |
| `RectangleShapeWidget` | `Rectangle` | Rounded rectangle drawing. |
| `SectorShapeWidget` | `Path` | Pie-slice renderer for gauges. |
| `TextBlockWidget` | `TextBlock` | Rich text layout with inline formatting. |
| `TextWidget` | `TextBlock` (lightweight) | Single-line text tuned for large data sets. |
| `ThumbWidget` | `Thumb` | Draggable thumb used by sliders and scrollbars. |
| `TrackWidget` | `Track` | Composes track and thumb visuals for sliders/scrollbars. |

### Input widgets

| Widget | Avalonia analogue | Notes |
| --- | --- | --- |
| `AutoCompleteBoxWidget` | `AutoCompleteBox` | Text input with suggestion flyout. |
| `ButtonWidget` | `Button` | Command-enabled push button with hover/press states. |
| `ButtonSpinnerWidget` | `ButtonSpinner` | Numeric spinner base with increment/decrement buttons. |
| `CalendarDatePickerWidget` | `CalendarDatePicker` | Text box + calendar drop-down for picking dates. |
| `CalendarWidget` | `Calendar` | Month calendar with selection support. |
| `CheckBoxWidget` | `CheckBox` | Two- or three-state checkbox. |
| `ComboBoxItemWidget` | `ComboBoxItem` | Item container used by combo boxes. |
| `ComboBoxWidget` | `ComboBox` | Drop-down selector backed by widget templates. |
| `DatePickerWidget` | `DatePicker` | Inline date selector with validation. |
| `DropDownButtonWidget` | `DropDownButton` | Button that opens a menu of widget content. |
| `HyperlinkButtonWidget` | `HyperlinkButton` | Button styled as a link with navigation events. |
| `MenuItemWidget` | `MenuItem` | Commandable entry for menus. |
| `MenuSeparatorWidget` | `Separator` | Visual divider within menus. |
| `NumericUpDownWidget` | `NumericUpDown` | Numeric input with repeat buttons and formatting. |
| `RadioButtonWidget` | `RadioButton` | Group-aware option button. |
| `RepeatButtonWidget` | `RepeatButton` | Auto-repeating button (used by sliders and scrollbars). |
| `ScrollBarWidget` | `ScrollBar` | Horizontal/vertical scrollbar with virtualization hooks. |
| `SliderWidget` | `Slider` | Continuous or stepped slider built from `TrackWidget`. |
| `SplitButtonWidget` | `SplitButton` | Primary button plus secondary drop-down action. |
| `SpinnerWidget` | `Spinner` | Indeterminate activity indicator. |
| `TextInputWidget` | `TextBox` | Single-line text input with caret management. |
| `TimePickerWidget` | `TimePicker` | Time selection with spinner-style editing. |
| `ToggleSwitchWidget` | `ToggleSwitch` | Animated on/off toggle. |

### Layout and container widgets

| Widget | Avalonia analogue | Notes |
| --- | --- | --- |
| `BorderVisualWidget` | `Border` | Optimised border surface for widget trees. |
| `CanvasLayoutWidget` | `Canvas` | Absolute positioning of child widgets. |
| `ContentControlWidget` | `ContentControl` | Single-child content host. |
| `DecoratorWidget` | `Decorator` | Wraps a child with additional visuals or transforms. |
| `DockLayoutWidget` | `DockPanel` | Dock-style layout with optional `LastChildFill`. |
| `ExpanderWidget` | `Expander` | Collapsible content region with toggle header. |
| `GridLayoutWidget` | `Grid` | Row/column grid supporting pixel and star sizing. |
| `GroupBoxWidget` | `GroupBox` | Headered border container. |
| `HeaderedContentControlWidget` | `HeaderedContentControl` | Content + header composition. |
| `LayoutTransformLayoutWidget` | `LayoutTransformControl` | Applies transforms while preserving desired size. |
| `RelativePanelLayoutWidget` | `RelativePanel` | Constraint-based relative positioning. |
| `ScrollViewerWidget` | `ScrollViewer` | Scrollable viewport with widget scrollbars. |
| `SplitViewLayoutWidget` | `SplitView` | Master/detail layout with collapsible pane. |
| `StackLayoutWidget` | `StackPanel` | Horizontal/vertical stacking with spacing support. |
| `TransitioningContentWidget` | `TransitioningContentControl` | Animated content transitions. |
| `UniformGridLayoutWidget` | `UniformGrid` | Evenly sized grid layout. |
| `ViewboxLayoutWidget` | `Viewbox` | Scales child content to available space. |
| `VirtualizingCarouselPanelWidget` | `CarouselPanel` | Virtualised looping layout for carousels. |
| `VirtualizingPanelWidget` | Virtualising panels | Base class that realises items via `IFastTreeDataGridSource`. |
| `VirtualizingStackPanelWidget` | `VirtualizingStackPanel` | Virtualised stack used by list-style widgets. |
| `WrapLayoutWidget` | `WrapPanel` | Flow layout that wraps to the next line/column. |

### Collections and navigation widgets

| Widget | Avalonia analogue | Notes |
| --- | --- | --- |
| `ContextMenuWidget` | `ContextMenu` | Right-click menu rendered with widgets. |
| `ItemsControlWidget` | `ItemsControl` | Base repeater with item factory/template support. |
| `ListBoxWidget` | `ListBox` | Selection-enabled list with keyboard navigation. |
| `MenuBarWidget` | `Menu` | Horizontal menu bar hosting drop-down menus. |
| `MenuWidget` | `Menu` | Vertical/horizontal command menu surface. |
| `SelectableItemsControlWidget` | `SelectingItemsControl` | Base class for selection-capable collections. |
| `TabControlWidget` | `TabControl` | Tabbed container with animated header strip. |
| `TabItemWidget` | `TabItem` | Tab header and content pairing. |
| `TabStripWidget` | `TabStrip` | Standalone tab header surface. |
| `TreeViewWidget` | `TreeView` | Hierarchical list backed by `IFastTreeDataGridSource`. |

### Data-grid presenters

| Widget | Avalonia analogue | Notes |
| --- | --- | --- |
| `FastTreeDataGridGroupRowPresenter` | `GroupItem` | Draws group headers inside the grid using widget styling. |
| `FastTreeDataGridGroupSummaryPresenter` | `GroupItem` footer | Renders aggregate rows with summary values. |

## Widget Architecture

The widget layer is an immediate-mode rendering system tuned for virtualised data:

- **Rendering pipeline** – `Widget.Draw` is called from `FastTreeDataGridPresenter` or `WidgetHostSurface`. Each widget receives the device `DrawingContext`, so scrolling simply repositions existing widgets without creating Avalonia controls.
- **Value flow** – `Widget.UpdateValue` works with `IFastTreeDataGridValueProvider` and `Widget.Key`, providing direct access to row data without dependency properties or bindings.
- **Layout** – Container widgets (`StackLayoutWidget`, `GridLayoutWidget`, `VirtualizingPanelWidget`, etc.) implement their own layout algorithms. Virtualising panels coordinate with `FastTreeDataGridViewportScheduler` to request pages, buffer placeholders, and recycle realised items.
- **Templating** – `WidgetTemplate` and `FuncWidgetTemplate` allow authoring in XAML or code. Higher-level widgets (`FastTreeDataGridColumn`, `ItemsControlWidget`) simply invoke the template to materialise cells on demand.
- **Styling & theming** – `WidgetStyleManager` applies palette data from `WidgetFluentPalette`. Switching themes or overriding a style key immediately repaints realised widgets without rebuilding the tree.
- **Input** – `WidgetPointerEvent` and `WidgetKeyboardEvent` deliver input directly to widgets. Interactive widgets update state and raise callbacks without relying on routed events.
- **Hosting** – `WidgetHost.Create` wraps any widget tree in a lightweight `WidgetHostSurface` so you can embed widgets inside traditional Avalonia layouts, dashboards, or dialogs.
- **Overlays** – `WidgetOverlayManager` tracks adorners, tooltips, and drag visuals, keeping them synchronised with the owning host’s coordinate space.

With lightweight widgets for cells, headers, and summary bars, most `FastTreeDataGrid` scenarios can avoid creating Avalonia controls entirely while still exposing familiar building blocks for layout, navigation, and input.

## Feature Overview
| Feature | Highlights | Primary APIs | More info |
| --- | --- | --- | --- |
| Canvas-backed virtualization | Reuses pooled canvas presenters for smooth scrolling and zero layout churn. | `FastTreeDataGrid`, `FastTreeDataGridVirtualizationSettings` | [Canvas-backed virtualization](#feature-canvas-backed-virtualization) |
| Flat source engine | Flattens hierarchical data, tracks expansion, sorting, and filtering. | `FastTreeDataGridFlatSource<T>`, `IFastTreeDataGridSource` | [Flat source engine](#feature-flat-source-engine) |
| Immediate-mode widgets | Draw text, icons, gauges, and inputs without templated controls. | `Widget`, `WidgetTemplate`, `IFastTreeDataGridValueProvider` | [Immediate-mode widgets](#feature-immediate-mode-widgets) |
| Items & navigation widgets | Drop-in ListBox/TreeView replacements on the widget renderer. | `ItemsControlWidget`, `ListBoxWidget`, `TreeViewWidget` | [Items & navigation widgets](#feature-items--navigation-widgets) |
| Column grouping & pivot | Drag headers into a grouping band, layer descriptors, and surface pivot-style aggregates with persistence. | `FastTreeDataGrid.GroupDescriptors`, `FastTreeDataGridGroupingBand`, `FastTreeDataGridGroupingLayout` | [Column grouping & pivot](#feature-column-grouping--pivot) |
| Flexible column system | Combine pixel/star sizing, templates, and selection hooks. | `FastTreeDataGridColumn`, `ColumnSizingMode` | [Flexible column system](#feature-flexible-column-system) |
| Row layouts & data sources | Mix uniform/variable heights with static or streaming feeds. | `IFastTreeDataGridRowLayout`, `FastTreeDataGridHybridSource<T>` | [Row layouts & data sources](#feature-row-layouts--data-sources) |
| Row reorder | Pointer-driven drag & drop with live preview, configurable visuals, and events. | `FastTreeDataGridRowReorderSettings`, `IFastTreeDataGridRowReorderHandler` | [Row reorder](#feature-row-reorder) |
| Provider-agnostic virtualization | Integrate REST/ModelFlow providers and capture diagnostics. | `FastTreeDataGridVirtualizationProviderRegistry`, `FastTreeDataGridVirtualizationDiagnostics` | [Provider-agnostic virtualization](#feature-provider-agnostic-virtualization) |

## Comprehensive Feature Matrix
| Area | Capability | What it does | Key APIs / Notes |
| --- | --- | --- | --- |
| Rendering & Virtualization | Canvas header/body surfaces | Hosts dedicated canvases for headers and rows so scrolling only repositions presenters. | `FastTreeDataGrid` template parts `PART_HeaderPresenter`, `PART_Presenter` |
| Rendering & Virtualization | Presenter pooling | Reuses header, row, and cell presenters instead of creating new controls per row. | `FastTreeDataGridPresenter`, `FastTreeDataGridHeaderPresenter` |
| Rendering & Virtualization | Viewport scheduler | Coordinates viewport changes with page requests, cancellation, and throttling. | `FastTreeDataGridViewportScheduler` |
| Rendering & Virtualization | Invalidation modes | Distinguishes full, range, and metadata-only redraws to minimise work. | `FastTreeDataGridInvalidationRequest`, `FastTreeDataGridInvalidatedEventArgs` |
| Rendering & Virtualization | Row materialization events | Raises callbacks whenever a provider materialises rows so hosts can warm caches. | `IFastTreeDataVirtualizationProvider.RowMaterialized`, `FastTreeDataGridRowMaterializedEventArgs` |
| Rendering & Virtualization | Row count updates | Notifies when total row counts change to keep pagers and summaries in sync. | `IFastTreeDataVirtualizationProvider.CountChanged`, `FastTreeDataGridCountChangedEventArgs` |
| Rendering & Virtualization | Materialised row cache lookup | Allows reuse of already materialised rows without re-fetching. | `IFastTreeDataVirtualizationProvider.TryGetMaterializedRow` |
| Rendering & Virtualization | Prefetch hook | Exposes a prefetch path so providers can warm pages without showing placeholders. | `IFastTreeDataVirtualizationProvider.PrefetchAsync` |
| Rendering & Virtualization | Loading overlay | Shows an optional overlay with progress during long running fetches. | `FastTreeDataGridVirtualizationSettings.ShowLoadingOverlay`, template part `PART_LoadingOverlay` |
| Rendering & Virtualization | Placeholder skeletons | Renders lightweight skeleton bars for placeholder rows when enabled. | `FastTreeDataGridPresenter`, `FastTreeDataGridVirtualizationSettings.ShowPlaceholderSkeletons` |
| Virtualization Settings | Page size (`PageSize`) | Controls how many rows are requested per page and enforces a minimum of one. | `FastTreeDataGridVirtualizationSettings.PageSize` |
| Virtualization Settings | Prefetch radius (`PrefetchRadius`) | Determines how many neighbour pages the scheduler requests around the viewport. | `FastTreeDataGridVirtualizationSettings.PrefetchRadius` |
| Virtualization Settings | Max cached pages (`MaxPages`) | Caps the number of cached pages to protect memory usage. | `FastTreeDataGridVirtualizationSettings.MaxPages` |
| Virtualization Settings | Max concurrent loads (`MaxConcurrentLoads`) | Limits parallel fetches so providers are not overwhelmed. | `FastTreeDataGridVirtualizationSettings.MaxConcurrentLoads` |
| Virtualization Settings | Reset throttle (`ResetThrottleDelayMilliseconds`) | Coalesces rapid invalidations to a single refresh. | `FastTreeDataGridVirtualizationSettings.ResetThrottleDelayMilliseconds`, `FastTreeDataGridThrottleDispatcher` |
| Virtualization Settings | Dispatcher priority | Lets hosts choose the dispatcher priority used for virtualization work. | `FastTreeDataGridVirtualizationSettings.DispatcherPriority` |
| Virtualization Settings | Loading overlay toggle | Enables or suppresses the built-in loading overlay. | `FastTreeDataGridVirtualizationSettings.ShowLoadingOverlay` |
| Virtualization Settings | Skeleton toggle | Enables or suppresses placeholder skeleton rendering. | `FastTreeDataGridVirtualizationSettings.ShowPlaceholderSkeletons` |
| Layout Customization | Row height property | Sets the baseline row height for uniform layouts. | `FastTreeDataGrid.RowHeight` |
| Layout Customization | Indent width property | Controls tree indentation spacing per level. | `FastTreeDataGrid.IndentWidth` |
| Layout Customization | Header height property | Adjusts header canvas height for custom chrome. | `FastTreeDataGrid.HeaderHeight` |
| Layout Customization | Column collection | Offers an observable list for constructing column definitions in code. | `FastTreeDataGrid.Columns` |
| Data Shaping & Engine | Hierarchy flattening | Flattens arbitrary trees into stable rows with level tracking. | `FastTreeDataGridFlatSource<T>`, `FastTreeDataGridRow.Level` |
| Data Shaping & Engine | Stable keys | Tracks nodes by key to keep expansion and selection across resets. | `FastTreeDataGridFlatSource<T>` `keySelector` |
| Data Shaping & Engine | Per-row expansion toggle | Exposes expansion per row index for both data nodes and group rows. | `FastTreeDataGridFlatSource<T>.ToggleExpansion` |
| Data Shaping & Engine | Custom sort pipeline | Reorders rows using caller-supplied comparisons and restores insertion order when cleared. | `FastTreeDataGridFlatSource<T>.Sort` |
| Data Shaping & Engine | Predicate filtering | Filters visible rows with a predicate while tracking matches. | `FastTreeDataGridFlatSource<T>.SetFilter` |
| Data Shaping & Engine | Auto-expand filtered matches | Automatically expands ancestors when filters match descendants. | `FastTreeDataGridFlatSource<T>.SetFilter(expandMatches)` |
| Data Shaping & Engine | Reset with expansion preservation | Reloads data while restoring expansion based on keys. | `FastTreeDataGridFlatSource<T>.Reset` |
| Data Shaping & Engine | Sort/filter/group aggregation requests | Applies batched sort, filter, group, and aggregate descriptors asynchronously. | `FastTreeDataGridFlatSource<T>.ApplySortFilterAsync`, `FastTreeDataGridSortFilterRequest` |
| Data Shaping & Engine | Group descriptor collection | Exposes group descriptors for binding or direct manipulation. | `FastTreeDataGrid.GroupDescriptors` |
| Data Shaping & Engine | Group expansion helpers | Expands or collapses all groups with a single call. | `FastTreeDataGridFlatSource<T>.ExpandAllGroups`, `.CollapseAllGroups` |
| Data Shaping & Engine | Aggregate descriptor collection | Publishes aggregate descriptors for summaries. | `FastTreeDataGrid.AggregateDescriptors` |
| Data Shaping & Engine | Value provider invalidation | Propagates fine-grained value changes to widgets without bindings. | `IFastTreeDataGridValueProvider.ValueInvalidated`, `FastTreeDataGridRow` |
| Data Shaping & Engine | Row measure callbacks | Widgets can request row remeasurements for dynamic layouts. | `FastTreeDataGridRow.RequestMeasure`, `FastTreeDataGridFlatSource<T>.OnNodeRequestMeasure` |
| Data Sources | Flat in-memory source | Eagerly flattens collections for deterministic in-memory browsing. | `FastTreeDataGridFlatSource<T>` |
| Data Sources | Async loader source | Wraps async factories so initial loads happen off the UI thread. | `FastTreeDataGridAsyncSource<T>` |
| Data Sources | Streaming source | Listens to live feeds and applies inserts/removes to the flattened list. | `FastTreeDataGridStreamingSource<T>` |
| Data Sources | Hybrid source | Combines a snapshot load with real-time deltas for dashboards. | `FastTreeDataGridHybridSource<T>` |
| Data Sources | Dynamic source base | Base class for custom dynamic/virtual sources with reuse of flat engine logic. | `FastTreeDataGridDynamicSource<T>` |
| Data Sources | Source factory helpers | Creates flat sources for common patterns with consistent key handling. | `FastTreeDataGridSourceFactory` |
| Row Layouts | Uniform row layout | Provides constant-height rows for maximal throughput. | `FastTreeDataGridUniformRowLayout` |
| Row Layouts | Variable row layout | Lets providers compute per-row heights during virtualization. | `FastTreeDataGridVariableRowLayout` |
| Row Layouts | Func-based height provider | Simplifies variable height scenarios with a delegate helper. | `FastTreeDataGridFuncVariableRowHeightProvider` |
| Row Layouts | Adaptive row layout | Samples heights in chunks to estimate scroll extents for mixed-height sets. | `FastTreeDataGridAdaptiveRowLayout` |
| Row Layouts | Custom layout contract | Developers can author bespoke layouts for domain-specific positioning. | `IFastTreeDataGridRowLayout` |
| Columns & Cells | Header content | Supplies arbitrary header objects or templates. | `FastTreeDataGridColumn.Header` |
| Columns & Cells | Sizing mode selection | Chooses between pixel, star, and auto sizing per column. | `FastTreeDataGridColumn.SizingMode` (`ColumnSizingMode`) |
| Columns & Cells | Fixed pixel width | Locks a column to an explicit pixel width with min clamping. | `FastTreeDataGridColumn.PixelWidth` |
| Columns & Cells | Star sizing weights | Distributes remaining space proportionally across columns. | `FastTreeDataGridColumn.StarValue` |
| Columns & Cells | Minimum width guard | Prevents columns from shrinking below a threshold. | `FastTreeDataGridColumn.MinWidth` |
| Columns & Cells | Maximum width guard | Caps column width or allows unlimited expansion. | `FastTreeDataGridColumn.MaxWidth` |
| Columns & Cells | Value key mapping | Binds widgets to values without using data bindings. | `FastTreeDataGridColumn.ValueKey` |
| Columns & Cells | Hierarchy indentation flag | Marks which column should show tree indentation and expanders. | `FastTreeDataGridColumn.IsHierarchy` |
| Columns & Cells | Widget factory hook | Creates per-cell widgets on demand for pooling-friendly rendering. | `FastTreeDataGridColumn.WidgetFactory` |
| Columns & Cells | Widget template support | Reuses declarative widget templates per column. | `FastTreeDataGridColumn.CellTemplate` |
| Columns & Cells | Control template support | Falls back to Avalonia controls when needed. | `FastTreeDataGridColumn.CellControlTemplate` |
| Columns & Cells | Edit template | Supplies an edit-time Avalonia template. | `FastTreeDataGridColumn.EditTemplate` |
| Columns & Cells | Edit template selector | Chooses edit template dynamically per row. | `FastTreeDataGridColumn.EditTemplateSelector` |
| Columns & Cells | Read-only toggle | Locks a column against edits. | `FastTreeDataGridColumn.IsReadOnly` |
| Columns & Cells | Resize toggle | Enables or disables end-user resizing. | `FastTreeDataGridColumn.CanUserResize` |
| Columns & Cells | Sort toggle | Opts columns into header sorting gestures. | `FastTreeDataGridColumn.CanUserSort` |
| Columns & Cells | Reorder toggle | Allows drag/drop column reordering. | `FastTreeDataGridColumn.CanUserReorder` |
| Columns & Cells | Pin toggle | Opts columns into pin/unpin interactions. | `FastTreeDataGridColumn.CanUserPin` |
| Columns & Cells | Auto-size toggle | Controls if auto measuring can resize the column. | `FastTreeDataGridColumn.CanAutoSize` |
| Columns & Cells | Filter toggle | Enables per-column filtering UX. | `FastTreeDataGridColumn.CanUserFilter` |
| Columns & Cells | Filter placeholder text | Customises the inline filter prompt. | `FastTreeDataGridColumn.FilterPlaceholder` |
| Columns & Cells | Filter descriptor factory | Builds complex filter descriptors from user input. | `FastTreeDataGridColumn.FilterFactory` |
| Columns & Cells | Pinned position | Pins a column to the left or right rail. | `FastTreeDataGridColumn.PinnedPosition` (`FastTreeDataGridPinnedPosition`) |
| Columns & Cells | Sort direction tracking | Stores the active sort direction for visual adorners. | `FastTreeDataGridColumn.SortDirection` |
| Columns & Cells | Sort order index | Tracks multi-column sort order. | `FastTreeDataGridColumn.SortOrder` |
| Columns & Cells | Custom comparison | Supplies a bespoke row comparison. | `FastTreeDataGridColumn.SortComparison` |
| Columns & Cells | Validation key | Links cells to validation metadata. | `FastTreeDataGridColumn.ValidationKey` |
| Columns & Cells | Control pooling | Reuses editing controls internally to avoid allocations. | `FastTreeDataGridColumn` internal pooling |
| Columns & Cells | Text widget pooling | Reuses formatted text widgets per column for efficiency. | `FastTreeDataGridColumn` text widget pool |
| Widget Toolkit | Immediate-mode widget templates | Renders cells via lightweight widget descriptors instead of controls. | `Widget`, `WidgetTemplate`, `IWidgetTemplate` |
| Widget Toolkit | Animation scheduler | Runs widget animations on a shared frame scheduler. | `WidgetAnimationFrameScheduler` |
| Widget Toolkit | Overlay manager | Manages floating overlays for widgets (tooltips, flyouts). | `WidgetOverlayManager`, `IWidgetOverlayHost` |
| Widget Toolkit | Style manager | Applies palette resources to widgets without Avalonia styles. | `WidgetStyleManager` |
| Widget Toolkit | Scroll viewer widget | Adds scrollable surfaces that participate in virtualization. | `ScrollViewerWidget` |
| Widget Toolkit | Virtualizing stack panel | Provides a vertical virtualizing items host. | `VirtualizingStackPanelWidget` |
| Widget Toolkit | Virtualizing carousel panel | Provides a horizontal virtualizing panel for cards/carousels. | `VirtualizingCarouselPanelWidget` |
| Widget Toolkit | Items control widget | Virtualizes item collections on the widget pipeline. | `ItemsControlWidget` |
| Widget Toolkit | ListBox widget | Adds single-selection gestures and Fluent visuals. | `ListBoxWidget` |
| Widget Toolkit | TreeView widget | Adds indentation, expanders, and helper APIs like `ExpandToLevel`. | `TreeViewWidget` |
| Widget Toolkit | Tab control widget | Renders tab headers/content without templated controls. | `TabControlWidget`, `TabStripWidget` |
| Widget Toolkit | Menu widgets | Builds menu bars and flyout menus using pooled widgets. | `MenuBarWidget`, `MenuWidget` |
| Widget Toolkit | Transitioning content | Animates between widget states with fades or slides. | `TransitioningContentWidget`, `WidgetTransitionDescriptor` |
| Widget Toolkit | Text rendering widget | Delivers wrapped, selectable formatted text without Avalonia `TextBlock`. | `FormattedTextWidget` |
| Widget Toolkit | Gauge/progress widget | Renders progress bars directly on the canvas. | `ProgressWidget` |
| Interaction & UX | Selection model injection | Allows custom selection strategies. | `FastTreeDataGrid.SelectionModel`, `IFastTreeDataGridSelectionModel` |
| Interaction & UX | Selection mode | Switches between single and extended selection semantics. | `FastTreeDataGrid.SelectionMode`, `FastTreeDataGridSelectionMode` |
| Interaction & UX | Selected index | Exposes the currently selected row index. | `FastTreeDataGrid.SelectedIndex` |
| Interaction & UX | Selected item | Mirrors the selected row's item for binding. | `FastTreeDataGrid.SelectedItem` |
| Interaction & UX | Selected indices | Publishes a list of selected row indices. | `FastTreeDataGrid.SelectedIndices` |
| Interaction & UX | Type search selector | Configures how type-to-search derives display text. | `FastTreeDataGrid.TypeSearchSelector` |
| Interaction & UX | Type search events | Raises events when buffered type-to-search queries update. | `FastTreeDataGrid.TypeSearchRequested`, `FastTreeDataGridTypeSearchEventArgs` |
| Interaction & UX | Type search auto reset | Clears the search buffer after a short idle interval. | `FastTreeDataGrid` buffered search timeout |
| Interaction & UX | Row drag & drop reorder | Enables live drag handles, preview overlays, and cancellable hooks. | `FastTreeDataGridRowReorderSettings`, `FastTreeDataGrid.RowReordering`, `FastTreeDataGrid.RowReordered` |
| Interaction & UX | Sort request events | Notifies when header gestures request a sort direction. | `FastTreeDataGrid.SortRequested`, `FastTreeDataGridSortEventArgs` |
| Interaction & UX | Filter row visibility | Shows or hides the filter row globally. | `FastTreeDataGrid.IsFilterRowVisible` |
| Interaction & UX | Column filter flyout | Provides a flyout UI for advanced column filtering. | `FastTreeDataGridColumnFilterFlyout` |
| Interaction & UX | Loading state flag | Exposes a boolean flag when virtualization is loading. | `FastTreeDataGrid.IsLoading` |
| Interaction & UX | Loading progress value | Exposes load progress (0–1 or NaN) for binding. | `FastTreeDataGrid.LoadingProgress` |
| Diagnostics & Telemetry | Reset counters | Emits metrics each time the grid resets. | `FastTreeDataGridVirtualizationDiagnostics.ResetCount` |
| Diagnostics & Telemetry | Viewport render metrics | Records rows rendered and placeholder counts per frame. | `FastTreeDataGridVirtualizationDiagnostics.ViewportRowsRendered`, `.PlaceholderRowsRendered` |
| Diagnostics & Telemetry | Page request metrics | Tracks page request counts, concurrency, and durations. | `FastTreeDataGridVirtualizationDiagnostics.PageRequests`, `.PageFetchDuration`, `.InFlightRequests` |
| Diagnostics & Telemetry | Viewport timing metrics | Measures viewport update durations for profiling. | `FastTreeDataGridVirtualizationDiagnostics.ViewportUpdateDuration` |
| Diagnostics & Telemetry | Logging callback | Surfaces scheduler diagnostics via pluggable logging. | `FastTreeDataGridVirtualizationDiagnostics.Log`, `.LogCallback` |
| Diagnostics & Telemetry | Meter integration | Integrates with `System.Diagnostics.Metrics` for OTEL export. | `FastTreeDataGridVirtualizationDiagnostics.Meter` |
| Diagnostics & Telemetry | Benchmark suite | Ships BenchmarkDotNet scenarios for performance validation. | `benchmarks/FastTreeDataGrid.Benchmarks` |
| Integration & Extensibility | Virtualization provider registry | Lets apps register custom providers and dispose them later. | `FastTreeDataGridVirtualizationProviderRegistry.Register`, `FastTreeDataGridVirtualizationProviderRegistration` |
| Integration & Extensibility | Default provider adapter | Automatically wraps `IFastTreeDataGridSource` into a provider. | `FastTreeDataGridVirtualizationProviderRegistry` default factory, `FastTreeDataGridSourceVirtualizationProvider` |
| Integration & Extensibility | Dependency injection friendly | Supports DI by registering provider factories during startup. | See `docs/virtualization/providers.md` DI sample |
| Integration & Extensibility | Virtualization settings injection | Exposes virtualization settings as a mutable struct property. | `FastTreeDataGrid.VirtualizationSettings` |
| Integration & Extensibility | Value provider contract | Lets domain models expose values without bindings. | `IFastTreeDataGridValueProvider`, `ValueInvalidatedEventArgs` |

## Feature: Canvas-backed Virtualization
FastTreeDataGrid hosts a header canvas and a body canvas and reuses a compact pool of presenters to cover the viewport. Every scroll operation simply repositions the existing header, row, and cell presenters; offsets are computed from column widths and the active row layout instead of Avalonia's layout system. Selection, sorting, and expansion state live on the control and flow through a small set of events, keeping large hierarchies responsive.

### Quick usage
- Place `FastTreeDataGrid` in your view (XAML or code) and bind `ItemsSource` to an `IFastTreeDataGridSource`.
- Choose a row layout (uniform or variable) so the control can compute offsets without Avalonia measure passes.
- Tune `VirtualizationSettings` (page size, prefetch radius, concurrency) to align fetching with your data provider.

```csharp
var grid = new FastTreeDataGrid
{
    ItemsSource = flatSource, // e.g., new FastTreeDataGridFlatSource<T>(...)
    RowLayout = new FastTreeDataGridUniformRowLayout(),
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 256,
        PrefetchRadius = 2,
        Concurrency = 2
    }
};
```

## Feature: Flat Source Engine
The FlatTreeDataGrid engine flattens arbitrary hierarchies into a stable list of `FastTreeDataGridRow` instances while tracking expansion, preserving keys across refreshes, and issuing a single `ResetRequested` notification when data changes. Sources expose `IFastTreeDataGridValueProvider` so widgets can read values without bindings and react to fine-grained invalidations.

### Quick usage
- Create a `FastTreeDataGridFlatSource<T>` with your root items and a `childrenSelector`.
- Optionally provide `keySelector` so the source can diff nodes across refreshes, plus `Sort` and `SetFilter` handlers.
- Assign the source to `FastTreeDataGrid.ItemsSource` and let the grid request pages through virtualization.

```csharp
var files = new FastTreeDataGridFlatSource<FileNode>(
    viewModel.RootNodes,
    node => node.Children,
    keySelector: node => node.Id);

files.Sort((left, right) =>
{
    var leftName = left.ValueProvider?.GetValue(left.Item, FileNode.KeyName) as string ?? string.Empty;
    var rightName = right.ValueProvider?.GetValue(right.Item, FileNode.KeyName) as string ?? string.Empty;
    return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
});

var searchText = "log";
files.SetFilter(row =>
{
    var name = row.ValueProvider?.GetValue(row.Item, FileNode.KeyName) as string;
    return string.IsNullOrWhiteSpace(searchText) ||
           (name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
});

grid.ItemsSource = files;
```

## Feature: Immediate-mode Widgets
Cells render `Widget` instances defined by `WidgetTemplate` so drawing happens directly on the canvas without templated controls, routed events, or bindings. Widgets stream values from the row's `IFastTreeDataGridValueProvider`, style themselves via `WidgetStyleManager`, and support wrapped labels, selectable text, icons, badges, sliders, and other affordances.

### Quick usage
- Define a reusable `IWidgetTemplate` (or `WidgetFactory`) that builds the cell visual.
- Emit value lookups through `ValueKey` and react to invalidations inside the widget.
- Apply palette or style overrides via `WidgetStyleManager` or by composing widgets.

```csharp
var nameTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
{
    ValueKey = FileNode.KeyName,
    EmSize = 13,
    Trimming = TextTrimming.CharacterEllipsis,
    IsSelectable = true
});

var gaugeTemplate = new FuncWidgetTemplate(() => new ProgressWidget
{
    Max = 100,
    ValueKey = MetricsNode.KeyCpu
});
```

Widgets can also capture pointer and keyboard input (`WidgetInputContext`) so `ButtonWidget`, `CheckBoxWidget`, `SliderWidget`, `BadgeWidget`, and custom widgets stay interactive without leaving the canvas pipeline.

## Feature: Items & Navigation Widgets
The widget layer mirrors Avalonia's items controls so you can migrate views without leaving the pooled canvas surface. `ItemsControlWidget` virtualizes arbitrary item lists, `ListBoxWidget` layers in single-selection gestures that follow Fluent brushes, and `TreeViewWidget` adds indentation and expander glyphs while reusing the same value providers. The demo's **Widgets Gallery** tab showcases these wrappers with live boards and migration tips.

### Quick usage
- Choose the wrapper (`ItemsControlWidget`, `ListBoxWidget`, or `TreeViewWidget`) that matches the interaction model you need.
- Bind `ItemsSource` and `ItemChildrenSelector` (for hierarchies) so the widget can walk your data.
- Provide a `WidgetTemplate` that renders each item; call helpers like `ExpandToLevel` to bootstrap expansion.

```csharp
var tree = new TreeViewWidget
{
    ItemsSource = viewModel.RootNodes,
    ItemChildrenSelector = item => item is ProjectNode node ? node.Children : Array.Empty<ProjectNode>(),
    ItemTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
    {
        EmSize = 13,
        Trimming = TextTrimming.CharacterEllipsis
    }),
    DesiredWidth = 260,
    DesiredHeight = 200,
};

tree.ExpandToLevel(1);
```

**Wrapper selection quick guide**
- `ItemsControlWidget` – reach for this when you need a pooled, read-only list and plan to handle selection upstream.
- `ListBoxWidget` – keeps Avalonia's single-selection gestures and Fluent brushes while staying on the widget renderer.
- `TreeViewWidget` – handles hierarchical data with pooled expander glyphs, indentation, and lazy loading helpers.
- `TabControlWidget` + `TabStripWidget` – delivers tab navigation with Alt/arrow/Home/End keys and indicator styling sourced from the widget palette.
- `MenuBarWidget` + `MenuWidget` – builds command surfaces with access keys, accelerators, and overlay hosting without templated controls.

## Feature: Column Grouping & Pivot
The grid now supports a pivot-style grouping band, so users can drag headers above the grid to reshape datasets without writing code. Grouping works with both flat and hierarchical sources and uses the same value-provider pipeline that powers body cells.

### Quick usage
- Enable grouping by populating `FastTreeDataGrid.GroupDescriptors` or by letting users drag headers into the built-in grouping band (`PART_GroupingBand`).
- Reorder grouping levels via drag, Alt+Ctrl+Up/Down, or the grouping chip context menu; removal and "clear all" share the same gestures as the header menu.
- Attach aggregates by populating `FastTreeDataGrid.AggregateDescriptors` or per-column `AggregateDescriptors`; summaries render as footer rows out of the box.

### Customization hooks
- Persist layouts with `GetGroupingLayout()`/`ApplyGroupingLayout(layout)`—column order, sort direction, and expansion state are captured in a compact JSON contract (`FastTreeDataGridGroupingLayout`).
- Swap visuals by overriding `FastTreeDataGrid.GroupingBandBackground`, chip styles, or by supplying `GroupHeaderTemplate` / `GroupFooterTemplate` on individual columns.
- Implement `IFastTreeDataGridGroupAdapter` for custom key projection (e.g., date bucketing) or `IFastTreeDataGridAggregateProvider` for complex or async aggregates.

> **Migration notes**: The grouping band is opt-in; existing layouts continue to work until descriptors are supplied. If you provide a custom control template for `FastTreeDataGrid`, ensure it includes the new `PART_GroupingBandHost` placeholder to surface the band.

## Feature: Flexible Column System
Columns support pixel, star, and auto sizing, hierarchical indentation, selection hooks, and custom editing templates. You can pin columns, opt in to sorting or filtering, or pool widgets by supplying a `WidgetFactory`, and all sizing is computed analytically to avoid Avalonia layout passes.

### Quick usage
- Define each column with `SizingMode` (`Pixel`, `Star`, or `Auto`) plus optional min/max constraints.
- Provide `ValueKey`, `CellTemplate`, or `WidgetFactory` so the column knows how to render and sort values.
- Toggle behaviors such as `CanUserSort`, `CanUserResize`, or `PinnedPosition` to match your UX.

```csharp
grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Name",
    ValueKey = FileNode.KeyName,
    IsHierarchy = true,
    SizingMode = ColumnSizingMode.Star,
    StarValue = 2,
    CellTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
    {
        EmSize = 13,
        Trimming = TextTrimming.CharacterEllipsis
    })
});

grid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "Modified",
    ValueKey = FileNode.KeyModified,
    SizingMode = ColumnSizingMode.Pixel,
    PixelWidth = 140,
    CellTemplate = new FuncWidgetTemplate(() => new FormattedTextWidget
    {
        ValueKey = FileNode.KeyModified,
        Format = "g"
    }),
    CanUserSort = true,
    CanUserFilter = true
});
```

XAML projections follow the same pattern—define columns, pick sizing modes, and attach widget templates so values flow through `IFastTreeDataGridValueProvider`.

## Feature: Row Layouts & Data Sources
Row positioning is delegated to implementations of `IFastTreeDataGridRowLayout`. Uniform layouts keep rows the same height for maximum throughput, while variable layouts ask providers for per-row heights so dashboards and grouped summaries get the real estate they need. Sources can be static lists, `FastTreeDataGridFlatSource<T>`, hybrid sources that mix snapshots with live updates, or fully custom providers that emit rows asynchronously.

### Quick usage
- Pick a row layout (`FastTreeDataGridUniformRowLayout`, `FastTreeDataGridVariableRowLayout`, or `FastTreeDataGridAdaptiveRowLayout`) that matches your density and affordances.
- If you need variable heights, provide an `IFastTreeDataGridVariableRowHeightProvider` or the func-based helper to compute per-row values.
- Combine the layout with the right source type (flat, async, streaming, hybrid) so virtualization knows how to request pages.

```csharp
var filesSource = new FastTreeDataGridFlatSource<FileNode>(
    rootFolders,
    node => node.Children,
    keySelector: node => node.Path);

var layout = new FastTreeDataGridVariableRowLayout(
    new FastTreeDataGridFuncVariableRowHeightProvider((row, _, defaultHeight) =>
        row.IsGroup ? defaultHeight * 1.5 : defaultHeight));

var filesGrid = new FastTreeDataGrid
{
    ItemsSource = filesSource,
    RowLayout = layout
};
```

**Row layout options**
- `FastTreeDataGridUniformRowLayout` – default constant-height layout for dense tabular data.
- `FastTreeDataGridVariableRowLayout` – derives heights from providers for scenarios with tall summary rows or group headers.
- `FastTreeDataGridAdaptiveRowLayout` – samples row heights in blocks to estimate scroll extents for very large sets with mixed heights.
- Custom layouts – implement `IFastTreeDataGridRowLayout` to integrate domain-specific sizing (timeline grids, calendar rows, etc.).

**Available sources**
- `FastTreeDataGridFlatSource<T>` – deterministic flattening of in-memory hierarchies; great for file systems, configuration trees, or cached API responses.
- `FastTreeDataGridAsyncSource<T>` – wraps async factories so initial load happens off the UI thread while providing the same flat-tree API.
- `FastTreeDataGridStreamingSource<T>` – listens to live feeds (`IObservable`, channels, async enumerables) and applies inserts/removes to the flat list.
- `FastTreeDataGridHybridSource<T>` – combines a snapshot load with real-time updates; ideal for dashboards that hydrate once then listen for deltas.
- `FastTreeDataGridDynamicSource<T>` – base class for bespoke dynamic sources when you need custom change tracking or background processing.

## Feature: Row Reorder
`FastTreeDataGrid` includes a drag & drop pipeline for reordering rows with professional visuals. Enable it by toggling `RowReorderSettings.IsEnabled`, adjust the indicator and preview brushes, and provide an `IFastTreeDataGridRowReorderHandler` (the flat source already implements one) so the grid can persist the new order.

### Quick usage
- Set behavioural and visual knobs through `RowReorderSettings` (activation threshold, preview opacity, indicator brush, etc.).
- Handle `RowReordering` to cancel or redirect operations and `RowReordered` for telemetry.
- Plug in a custom handler when you need to forward reorders to a backend or enforce domain-specific rules.

```csharp
var grid = new FastTreeDataGrid
{
    ItemsSource = new FastTreeDataGridFlatSource<Node>(nodes, node => node.Children),
    RowReorderSettings = new FastTreeDataGridRowReorderSettings
    {
        IsEnabled = true,
        ActivationThreshold = 4,
        ShowDragPreview = true,
        DropIndicatorBrush = new SolidColorBrush(Colors.DeepSkyBlue),
        DragPreviewOpacity = 0.7,
    }
};

grid.RowReordering += (_, e) =>
{
    if (e.Request.SourceIndices.Any(IsLockedRow))
    {
        e.Cancel = true;
    }
};

grid.RowReordered += (_, e) =>
    Logger.Info($"Rows moved to {string.Join(",", e.Result.NewIndices)}");
```

> ℹ️  See [docs/rows/reordering.md](docs/rows/reordering.md) for a full settings reference, UX guidance, and examples of custom reorder handlers.

## Feature: Provider-agnostic Virtualization
FastTreeDataGrid virtualization is provider-agnostic: register factories with `FastTreeDataGridVirtualizationProviderRegistry` so the grid can discover the right `IFastTreeDataVirtualizationProvider` at runtime. Diagnostics via `FastTreeDataGridVirtualizationDiagnostics` surface fetch latency, placeholder density, and reset frequency so you can harden remote data sources before shipping.

### Quick usage
- Register a provider factory that adapts your data engine to `IFastTreeDataVirtualizationProvider`.
- Configure control-level `VirtualizationSettings` to describe page size, prefetch radius, and concurrency for the provider.
- Feed the grid a streaming-friendly source (e.g., `FastTreeDataGridHybridSource<T>`) and subscribe to diagnostics to monitor the pipeline.

```csharp
using var registration = FastTreeDataGridVirtualizationProviderRegistry.Register((source, settings) =>
{
    if (source is MetricsClient client)
    {
        // MetricsVirtualizationProvider implements IFastTreeDataVirtualizationProvider.
        return new MetricsVirtualizationProvider(client, settings);
    }

    return null;
});

var metricsGrid = new FastTreeDataGrid
{
    ItemsSource = new FastTreeDataGridHybridSource<MetricsNode>(
        loader: LoadInitialMetricsAsync,
        updates: metricsChannel.Reader,
        childrenSelector: node => node.Children,
        keySelector: node => node.Id),
    RowLayout = new FastTreeDataGridVariableRowLayout(
        new FastTreeDataGridFuncVariableRowHeightProvider((row, _, defaultHeight) =>
            row.IsGroup ? defaultHeight * 1.5 : defaultHeight)),
    VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
    {
        PageSize = 512,
        PrefetchRadius = 3
    }
};

metricsGrid.Columns.Add(new FastTreeDataGridColumn
{
    Header = "CPU",
    ValueKey = MetricsNode.KeyCpu,
    SizingMode = ColumnSizingMode.Star,
    CellTemplate = new FuncWidgetTemplate(() => new ProgressWidget { Max = 100 })
});
```

**Integration tips**
- Register your data engine (ModelFlow, REST, gRPC, etc.) with `FastTreeDataGridVirtualizationProviderRegistry` so the grid auto-discovers the correct provider at runtime.
- Configure `FastTreeDataGrid.VirtualizationSettings` per control to tune page size, prefetch radius, concurrency, and dispatcher priority.
- Emit metrics via `FastTreeDataGridVirtualizationDiagnostics` (MeterListener/OpenTelemetry) to watch fetch latency, placeholder density, and reset frequency.
- Run the BenchmarkDotNet suite (`benchmarks/FastTreeDataGrid.Benchmarks`) against large test sets to validate provider throughput before shipping.
- Keep row value providers lightweight—avoid synchronous network calls from `IFastTreeDataGridValueProvider` implementations.
- Prefer placeholder-aware widgets to avoid accessing null data while virtualization is inflight.

## Performance strategy

FastTreeDataGrid prioritises frame time predictability:

- No Avalonia layout: row and cell coordinates are computed analytically, so scrolling only moves existing presenters.
- No templated-control system: cells are widgets, not controls; they skip template instantiation, styling lookups, and routed events.
- No standard Avalonia controls: widgets draw straight to the canvas with retained assets and pooled brushes.
- No bindings: data flows through `IFastTreeDataGridValueProvider` and explicit value keys, eliminating binding allocations and change propagation costs.
- Batched measure: column widths and row heights are recomputed incrementally, and flat sources reuse nodes across resets via stable keys.

## Documentation

- [Layout & Virtualizing Widgets](docs/widgets/layout-widgets.md)
- [Text Widgets](docs/widgets/text-widgets.md)
- [Media & Icon Widgets](docs/widgets/media-widgets.md)
- [Menu Widgets](docs/widgets/menu-widgets.md)
- [Providers & Virtualization Integration](docs/virtualization/providers.md)
- [Metrics & Diagnostics](docs/virtualization/metrics.md)
- [Benchmarks](docs/virtualization/benchmarks.md)
- [Virtualization Migration Guide](docs/virtualization/migration.md)
- [Changelog](docs/changelog.md)

## Getting started

1. Restore and build the solution:

   ```bash
   dotnet build
   ```

2. Run the demo application:

   ```bash
   dotnet run --project samples/FastTreeDataGrid.Demo
   ```

3. Explore the sample tabs—including the new **Widgets Gallery** scenario explorer—to see flat tree rendering, streaming updates, widget boards, and variable-height rows in action.

## Samples

`samples/FastTreeDataGrid.Demo` showcases:
- File-system and country browsers backed by `FastTreeDataGridFlatSource<T>`.
- Live dashboards using `FastTreeDataGridStreamingSource<T>` and `FastTreeDataGridHybridSource<T>`.
- A widget gallery highlighting text, icon, badge, checkbox, slider, and custom draw widgets.
- A Widgets Gallery scenario explorer that maps each widget family to sample boards, palette notes, and migration tips.
- Variable-height and adaptive row layouts alongside uniform grids.
- Virtualization tab featuring a 1B-row pseudo-random provider and a REST-backed Hacker News provider that showcase the virtualization stack.

Additional sample applications:
- `samples/FastTreeDataGrid.VirtualizationDemo` dives deeper into virtualization strategies (adaptive rows, SQLite-backed paging, REST providers, and extensibility hooks).
- `samples/FastTreeDataGrid.DataSourcesDemo` compares async, streaming, and hybrid providers with live mutation pipelines.
- `samples/FastTreeDataGrid.ControlsDemo` mirrors Avalonia DataGrid/ListBox/TreeView/ItemsControl APIs using MVVM-first page view models, control-based column templates, and ItemsSource adapter samples.
- `samples/FastTreeDataGrid.WidgetsDemo` is a widget gallery explorer with interactive controls.
- `samples/FastTreeDataGrid.ExcelDemo` delivers an Excel-style pivot grid with row/column virtualization, Power Fx formulas, and financial cell styling for analytics scenarios.

## Validation

- Run `dotnet test tests/FastTreeDataGrid.Control.Tests/FastTreeDataGrid.Control.Tests.csproj` to exercise widget layout regressions (expander toggles, scroll viewer viewport notifications, and existing picker coverage).
- Run `dotnet run --project benchmarks/FastTreeDataGrid.Benchmarks -c Release` to execute the BenchmarkDotNet suite, including the new `WidgetInteractionBenchmarks` that stress tab switching, menu refreshes, expander toggles, and scroll viewport updates.

## License

FastTreeDataGrid is licensed under the [MIT License](LICENSE.TXT).
