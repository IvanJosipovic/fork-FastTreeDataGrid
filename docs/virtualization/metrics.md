# FastTreeDataGrid Metrics & Diagnostics

FastTreeDataGrid exposes performance telemetry via the .NET `System.Diagnostics.Metrics` infrastructure. Use
these metrics to monitor paging throughput, cache behavior, and rendering impact.

## Meter

All metrics are published from the meter `FastTreeDataGrid.Virtualization` (version `1.0.0`). Attach a
`MeterListener`, OpenTelemetry exporter, or Application Insights exporter to this meter to collect data.

## Metrics Summary

| Name | Type | Tags | Description |
| ---- | ---- | ---- | ----------- |
| `fasttree_datagrid_page_requests` | Counter<long> | `start_index`, `count` | Number of virtualization page requests issued. |
| `fasttree_datagrid_inflight_requests` | UpDownCounter<long> | `start_index`, `count` | Current number of in-flight page requests. |
| `fasttree_datagrid_page_fetch_duration_ms` | Histogram<double> | `start_index`, `count` | Duration (ms) of virtualization page fetches. |
| `fasttree_datagrid_viewport_update_duration_ms` | Histogram<double> | `control_hash` | Duration (ms) of viewport update passes. |
| `fasttree_datagrid_viewport_rows_rendered` | Counter<long> | `control_hash` | Number of rows processed during a viewport update. |
| `fasttree_datagrid_placeholder_rows_rendered` | Counter<long> | `control_hash` | Number of placeholder rows rendered during a viewport update. |
| `fasttree_datagrid_resets` | Counter<long> | `control_hash` | Number of datasource resets processed by the grid. |

## Enabling Metrics Publishing

FastTreeDataGrid starts emitting metrics as soon as `System.Diagnostics.Metrics` listeners subscribe to the
`FastTreeDataGrid.Virtualization` meter. No additional configuration is required inside the control. In
your application, register a `MeterListener`, OpenTelemetry exporter, or Application Insights meter to consume
the metrics.

### MeterListener Example

```csharp
using System.Diagnostics.Metrics;
using FastTreeDataGrid.Control.Infrastructure;

var listener = new MeterListener();
listener.InstrumentPublished = (instrument, l) =>
{
    if (instrument.Meter == FastTreeDataGridVirtualizationDiagnostics.Meter)
    {
        l.EnableMeasurementEvents(instrument);
    }
};

listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
{
    Console.WriteLine($"{instrument.Name}: {measurement} {string.Join(",", tags)}");
});

listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
{
    Console.WriteLine($"{instrument.Name}: {measurement} {string.Join(",", tags)}");
});

listener.Start();
```

When using OpenTelemetry, add a `MeterProviderBuilder` that includes the meter name:

```csharp
builder.AddMeter("FastTreeDataGrid.Virtualization");
```

### ASP.NET Core/OpenTelemetry Example

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("FastTreeDataGrid.Virtualization")
        .AddPrometheusExporter());
```

If you are using Application Insights, ensure the Application Insights meter listener is enabled and add the
meter name to the collection settings.

### Avalonia Application Example

In a desktop Avalonia app, register the listener during application startup (e.g., in `App.axaml.cs`):

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == FastTreeDataGridVirtualizationDiagnostics.Meter)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            Debug.WriteLine($"{instrument.Name}: {measurement}");
        });

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            Debug.WriteLine($"{instrument.Name}: {measurement}");
        });

        listener.Start();
    }

    base.OnFrameworkInitializationCompleted();
}
```

You can route the measurements into Serilog, OpenTelemetry, or any other logging/telemetry sink used by your
Avalonia application.

## Logging Hook

`FastTreeDataGridVirtualizationDiagnostics.LogCallback` captures scheduler events (cancellations, failures,
etc.). Assign your own delegate to route messages to Serilog, Microsoft.Extensions.Logging, or another sink:

```csharp
FastTreeDataGridVirtualizationDiagnostics.LogCallback = entry =>
    logger.LogInformation("[{Category}] {Message}", entry.Category, entry.Message);
```

## Debug Overlay (Optional)

For quick debugging, you can expose metrics in a HUD by sampling the counters and drawing overlays in the
presenter. This repo does not include a baked-in overlay, but the metrics above provide the necessary inputs
to visualize fetch rates and placeholder density.
