# Design-Time Metadata & Tooling Guidance

FastTreeDataGrid now ships with helper APIs that improve the Avalonia designer experience and calls out frequent configuration mistakes at compile time.

## Previewing Sample Data

The control exposes an attached property `design:FastTreeDataGridDesign.UseSampleData` that hydrates the grid with deterministic sample rows when the XAML previewer runs in design mode.

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:ftdg="clr-namespace:FastTreeDataGrid.Control.Controls;assembly=FastTreeDataGrid.Control"
    xmlns:design="clr-namespace:FastTreeDataGrid.Control.Design;assembly=FastTreeDataGrid.Control"
    mc:Ignorable="d">
  <ftdg:FastTreeDataGrid design:FastTreeDataGridDesign.UseSampleData="True">
    <ftdg:FastTreeDataGrid.Columns>
      <ftdg:FastTreeDataGridColumn Header="Name" ValueKey="Person.Name" />
      <ftdg:FastTreeDataGridColumn Header="Department" ValueKey="Person.Department" />
      <ftdg:FastTreeDataGridColumn Header="Status" ValueKey="Person.Status" />
    </ftdg:FastTreeDataGrid.Columns>
  </ftdg:FastTreeDataGrid>
</UserControl>
```

When the attached property is `True` and the designer has not provided a real `ItemsSource`, FastTreeDataGrid injects synthetic rows that respect the declared column keys. Designers can now reason about spacing, column widths, and templates without wiring a runtime data source. The helper keeps to design mode, so the grid remains empty at runtime unless a genuine source is assigned.

### Sample Data Heuristics

- The helper inspects the column `ValueKey` to populate meaningful placeholder values (e.g., price, date, status) where possible.
- Columns without keys still render placeholder text to highlight configuration gaps.
- Toggling `UseSampleData` off (or assigning a real `ItemsSource`) releases the synthetic source immediately.

## Analyzer Hints

Consumers automatically receive Roslyn diagnostics when referencing the FastTreeDataGrid package. The first rule focuses on a common source of blank cells:

| Rule | Message | Trigger |
| ---- | ------- | ------- |
| `FTDG0001` | `FastTreeDataGridColumn should specify ValueKey or provide a CellTemplate/WidgetFactory to render cell content` | Raised when a `FastTreeDataGridColumn` instance lacks `ValueKey`, `CellTemplate`, `CellTemplateSelector`, and `WidgetFactory` assignments inside object initialisers. |

The warning prompts developers to wire the column to data before running the application, saving a debug cycle. Future analyzer releases will build on this infrastructure to cover virtualization and editing pitfalls.

## Where to Look

- Attached property source: `src/FastTreeDataGrid.Control/Design/FastTreeDataGridDesign.cs`
- Analyzer implementation: `src/FastTreeDataGrid.Analyzers/Analyzers/FastTreeDataGridColumnAnalyzer.cs`

Attach the namespaces for the control (`xmlns:ftdg="clr-namespace:FastTreeDataGrid.Control.Controls;assembly=FastTreeDataGrid.Control"`) and the design helpers (`xmlns:design="clr-namespace:FastTreeDataGrid.Control.Design;assembly=FastTreeDataGrid.Control"`) and enable `UseSampleData` whenever you need a realistic preview in Avalonia XAML Studio, Rider, or Visual Studio.
