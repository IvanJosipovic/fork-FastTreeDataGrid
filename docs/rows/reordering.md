# Row Reordering

FastTreeDataGrid exposes pointer-driven drag & drop reordering with live preview overlays, indicator lines, and cancellation hooks. This page covers how to enable the feature, tune the visuals, and plug in custom handlers.

## Enabling the controller

1. Set `RowReorderSettings.IsEnabled = true` on the grid.
2. Ensure the active data source or virtualization provider implements `IFastTreeDataGridRowReorderHandler`.
   - `FastTreeDataGridFlatSource<T>` ships with a handler, so in-memory hierarchies work out of the box.
   - Custom providers can opt in by implementing the interface and returning `FastTreeDataGridRowReorderResult.Successful(...)` when they commit a reorder.
3. (Optional) Subscribe to `RowReordering`/`RowReordered` to validate or log operations.

```csharp
var grid = new FastTreeDataGrid
{
    ItemsSource = new FastTreeDataGridFlatSource<FileNode>(files, node => node.Children),
    RowReorderSettings = new FastTreeDataGridRowReorderSettings
    {
        IsEnabled = true,
        ShowDragPreview = true,
        DropIndicatorBrush = new SolidColorBrush(Colors.Orange),
        DragPreviewOpacity = 0.65,
    }
};

grid.RowReordering += (_, e) =>
{
    if (IsReadOnlyFolder(e.Request.SourceIndices))
    {
        e.Cancel = true;
    }
};

grid.RowReordered += (_, e) =>
    Metrics.TrackReorder(e.Request.SourceIndices, e.Result.NewIndices);
```

## Settings reference

`FastTreeDataGridRowReorderSettings` controls both behaviour and visuals. All properties raise `SettingsChanged`, so updates reflect immediately.

| Property | Description |
| --- | --- |
| `IsEnabled` | Master switch. When `false`, the controller ignores drag gestures. |
| `ActivationThreshold` | Minimum pointer movement (in DIPs) before a press converts into a drag. |
| `ShowDragPreview` | Toggles the translucent “ghost” rectangle that follows the pointer. |
| `ShowDropIndicator` | Controls whether a horizontal indicator line is drawn at the potential drop slot. |
| `DropIndicatorBrush` / `DropIndicatorThickness` | Brush and thickness for the indicator line. |
| `DragPreviewBrush`, `DragPreviewOpacity`, `DragPreviewCornerRadius` | Styling for the floating preview block. |
| `UseSelection` | When `true`, dragging any selected row moves the entire selection block. When `false`, only the pressed row moves. |

## Handler interface

`IFastTreeDataGridRowReorderHandler` exposes two members:

- `bool CanReorder(FastTreeDataGridRowReorderRequest request)` lets handlers accept or reject a request synchronously.
- `Task<FastTreeDataGridRowReorderResult> ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken)` performs the mutation.

The request describes the zero-based indices of the rows being moved (`SourceIndices`) and the target insert index (`InsertIndex`) **after** the sources are removed. Handlers may attach arbitrary metadata to `FastTreeDataGridRowReorderRequest.Context` if they need additional state.

On success return `FastTreeDataGridRowReorderResult.Successful(newIndices)` where `newIndices` lists the rows’ new positions. The controller updates selection based on this information.

```csharp
public sealed class BackendReorderHandler : IFastTreeDataGridRowReorderHandler
{
    private readonly MyApiClient _client;

    public BackendReorderHandler(MyApiClient client) => _client = client;

    public bool CanReorder(FastTreeDataGridRowReorderRequest request) => request.SourceIndices.Count <= 50;

    public async Task<FastTreeDataGridRowReorderResult> ReorderAsync(FastTreeDataGridRowReorderRequest request, CancellationToken cancellationToken)
    {
        var response = await _client.MoveAsync(request.SourceIndices, request.InsertIndex, cancellationToken);
        return response.Success
            ? FastTreeDataGridRowReorderResult.Successful(response.NewIndices)
            : FastTreeDataGridRowReorderResult.Cancelled;
    }
}
```

Assign a custom handler by setting `FastTreeDataGrid.RowReorderHandlerProperty` or exposing it via a derived control.

## Provider support matrix

| Provider | Built-in handler | Notes |
| --- | --- | --- |
| `FastTreeDataGridFlatSource<T>` | ✅ | Supports in-memory reordering inside the same parent branch. Cross-parent moves require a custom handler that updates the underlying tree. |
| `FastTreeDataGridSourceVirtualizationProvider` | ✅ | Forwards to the wrapped source. Remote adapters should implement `IFastTreeDataGridRowReorderHandler` so updates flow to the backend. |
| `FastTreeDataGridDynamicSource<T>` / `FastTreeDataGridAsyncSource<T>` / `FastTreeDataGridStreamingSource<T>` / `FastTreeDataGridHybridSource<T>` | ⚠️ | The dynamic wrapper does not expose reordering today. If you want async, streaming, or hybrid feeds to support drag/drop, surface a bespoke handler that coordinates with your snapshot and persistence layer. |
| ModelFlow / REST adapters | ⚠️ | Implement a handler that calls the remote service (e.g., reorder mutation endpoint) and refreshes the viewport on success. |

When implementing a bespoke handler for cross-parent moves, resolve the target parent from `InsertIndex`, perform hierarchy validation, and update any domain identifiers before returning `FastTreeDataGridRowReorderResult.Successful(...)`.

## Events

- `RowReordering` fires before the handler runs. Set `Cancel = true` to abort the gesture while keeping the selection intact.
- `RowReordered` fires after the handler completes (successful or not). Inspect `Result.Success` to determine the outcome.

## Current behaviour & limitations

- The built-in flat source handler moves rows within the same parent. Groups and summary rows are not reorderable.
- Multi-row drags preserve the relative order of the selection and move as a contiguous block.
- If `CanReorder` or `ReorderAsync` return `false`, the controller snaps the preview back to its original location and clears the overlay.

Use the events or a custom handler to relax or extend these rules (e.g., cross-parent drops, server-side validation, or undo stacks).
