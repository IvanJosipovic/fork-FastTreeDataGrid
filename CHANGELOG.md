# Changelog

## Unreleased

### Added
- Multi-level **column grouping & pivot** experience with a grouping band (`FastTreeDataGridGroupingBand`), keyboard/automation gestures, and reorder/remove affordances.
- Aggregation pipeline with per-column (`FastTreeDataGridColumn.AggregateDescriptors`) and global aggregates (`FastTreeDataGrid.AggregateDescriptors`), including support for custom providers via `IFastTreeDataGridAggregateProvider`.
- Layout persistence APIs (`FastTreeDataGrid.GetGroupingLayout`, `FastTreeDataGrid.ApplyGroupingLayout`) plus JSON helpers to save/restore descriptor order and expansion state.
- Documentation updates covering grouping metadata interplay and new resource keys for theming the grouping band.

### Migration notes
- The default control template now contains `PART_GroupingBandHost`; custom templates should include an equivalent placeholder to display the grouping band when descriptors are present.
- Existing grids continue to behave as before unless group descriptors or aggregates are assigned. Styling resources fall back to their previous values if the new brushes are not overridden.

### Testing
- Added unit tests exercising grouping state snapshots, aggregate providers, layout serialization order, and grouping interoperability with value providers.
