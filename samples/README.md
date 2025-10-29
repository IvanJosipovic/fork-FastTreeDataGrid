# FastTreeDataGrid Samples

The repository now ships focused sample applications:

- **`FastTreeDataGrid.Demo`** – core scenarios (file system explorer, country browsing, crypto tickers, chart gallery).
- **`FastTreeDataGrid.DataSourcesDemo`** – compares async, streaming, and hybrid sources with live mutation pipelines and a drag-and-drop row reorder showcase.
- **`FastTreeDataGrid.VirtualizationDemo`** – showcases variable row heights, adaptive layouts, adapters, and custom virtualization providers.
- **`FastTreeDataGrid.ControlsDemo`** – mirrors Avalonia DataGrid/ListBox/TreeView/ItemsControl APIs with MVVM-friendly templates, control-based cells, and ItemsSource adapter walk-throughs.
- **`FastTreeDataGrid.ExcelDemo`** – Excel-inspired pivot analytics with row & column virtualization, Power Fx formulas, and financial styling.
- **`FastTreeDataGrid.ExcelLikeDemo`** – workbook-style editor with adaptive row heights, cell editing, worksheet tabs, and XLSX loading via DocumentFormat.OpenXml.
- **`FastTreeDataGrid.WidgetsDemo`** – hosts the widget gallery explorer, virtualizing widget layouts, and board compositions.

## Running the Demos

```bash
dotnet run --project samples/FastTreeDataGrid.Demo
dotnet run --project samples/FastTreeDataGrid.DataSourcesDemo
dotnet run --project samples/FastTreeDataGrid.VirtualizationDemo
dotnet run --project samples/FastTreeDataGrid.ControlsDemo
dotnet run --project samples/FastTreeDataGrid.ExcelDemo
dotnet run --project samples/FastTreeDataGrid.ExcelLikeDemo
dotnet run --project samples/FastTreeDataGrid.WidgetsDemo
```

Switch between tabs inside each app to explore the dedicated scenarios. Refer to the [virtualization provider guide](../docs/virtualization/providers.md) for integration details.
