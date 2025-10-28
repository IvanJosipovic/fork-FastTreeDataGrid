# FastTreeDataGrid Column Grouping & Pivot Review

## Findings
- **High – Group expansion state never persists** *(Resolved)*  
  Expansion toggles now propagate through `FastTreeDataGrid.ToggleExpansionAt`, which captures the grouping path exposed by `IFastTreeDataGridGroupPathProvider` and updates `FastTreeDataGridGroupingStateStore`. `GetGroupingLayout()` therefore includes the expected expansion entries. See `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs:4136` and `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:192`.

- **High – Saved layouts ignore expansion states on restore** *(Resolved)*  
  Layout application now forwards expansion metadata to the virtualization provider, which in turn primes `FastTreeDataGridFlatSource` via the extended `IFastTreeDataGridGroupingController`. Restored views honour persisted open/closed state. See `src/FastTreeDataGrid.Control/Controls/FastTreeDataGrid.cs:215`, `src/FastTreeDataGrid.Core/Infrastructure/Virtualization/FastTreeDataGridSourceVirtualizationProvider.cs:125`, and `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:241`.

- **Medium – Descriptor default expansion flag is ignored** *(Resolved)*  
  Group creation now falls back to the originating descriptor's `IsExpanded` flag when no persisted state is available, enabling mixed default states. See `src/FastTreeDataGrid.Core/Infrastructure/DataSources/FastTreeDataGridFlatSource.cs:1128` and test coverage in `tests/FastTreeDataGrid.Control.Tests/FastTreeDataGridGroupingTests.cs`.

## Follow-ups
- Clarify how the grouping state store is meant to flow between the control and data sources so persisted expansion state can round-trip.
- Consider unit or integration coverage that exercises `GetGroupingLayout`/`ApplyGroupingLayout` end-to-end to lock regressions down.
