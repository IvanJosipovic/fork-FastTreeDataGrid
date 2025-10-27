# FastTreeDataGrid Samples

The repository ships two sample applications:

- **`FastTreeDataGrid.Demo`** – highlights data-oriented scenarios such as virtualization diagnostics, flat tree browsing, streaming dashboards, and hybrid data feeds.
- **`FastTreeDataGrid.WidgetsDemo`** – showcases the widget gallery experience, virtualizing layouts, and board-based widget compositions without interfering with the main demo surface.

## Running the Demos

```bash
dotnet run --project samples/FastTreeDataGrid.Demo
dotnet run --project samples/FastTreeDataGrid.WidgetsDemo
```

Switch between tabs inside each app to explore the scenarios. The virtualization tab in the main demo includes on-screen controls for page size, prefetch radius, and metric logging hooks. Refer to the [virtualization provider guide](../docs/virtualization/providers.md) for integration details.
