using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Inline column source that mirrors the grid's column collection while providing placeholder-aware,
/// asynchronous materialization so the horizontal viewport can yield work to background scheduling.
/// </summary>
internal sealed class FastTreeDataGridInlineColumnSource : IFastTreeDataGridColumnSource
{
    private readonly GridControl _owner;
    private readonly object _gate = new();
    private readonly List<ColumnEntry> _entries = new();
    private static readonly int[] s_emptyIndices = Array.Empty<int>();

    private readonly struct ColumnSnapshotUpdate
    {
        public ColumnSnapshotUpdate(bool requiresReset, IReadOnlyList<int> changedIndices)
        {
            RequiresReset = requiresReset;
            ChangedIndices = changedIndices ?? s_emptyIndices;
        }

        public bool RequiresReset { get; }

        public IReadOnlyList<int> ChangedIndices { get; }

        public bool HasChanges => RequiresReset || ChangedIndices.Count > 0;

        public static ColumnSnapshotUpdate None => new(false, s_emptyIndices);
    }

    private sealed class ColumnEntry
    {
        public ColumnEntry(FastTreeDataGridColumn column)
        {
            Column = column;
        }

        public FastTreeDataGridColumn Column { get; set; }

        public FastTreeDataGridColumnDescriptor? CurrentDescriptor { get; set; }

        public ColumnSnapshot? PendingSnapshot { get; set; }

        public Task? MaterializationTask { get; set; }

        public CancellationTokenSource? Cancellation { get; set; }

        public int Version { get; set; }

        public long MaterializationStartTimestamp { get; set; }

        public bool HasPendingSnapshot => PendingSnapshot is not null;
    }

    private sealed class ColumnSnapshot
    {
        public ColumnSnapshot(
            FastTreeDataGridColumn column,
            string key,
            string? header,
            string? valueKey,
            ColumnSizingMode sizingMode,
            double pixelWidth,
            double minWidth,
            double maxWidth,
            bool isHierarchy,
            FastTreeDataGridPinnedPosition pinnedPosition)
        {
            Column = column;
            Key = key;
            Header = header;
            ValueKey = valueKey;
            SizingMode = sizingMode;
            PixelWidth = pixelWidth;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
            IsHierarchy = isHierarchy;
            PinnedPosition = pinnedPosition;
        }

        public FastTreeDataGridColumn Column { get; }

        public string Key { get; }

        public string? Header { get; }

        public string? ValueKey { get; }

        public ColumnSizingMode SizingMode { get; }

        public double PixelWidth { get; }

        public double MinWidth { get; }

        public double MaxWidth { get; }

        public bool IsHierarchy { get; }

        public FastTreeDataGridPinnedPosition PinnedPosition { get; }
    }

    public FastTreeDataGridInlineColumnSource(GridControl owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        ApplyStructuralSnapshot();
    }

    public event EventHandler? ResetRequested;
    public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated;
    public event EventHandler<FastTreeDataGridColumnMaterializedEventArgs>? ColumnMaterialized;

    public int ColumnCount
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    public bool SupportsPlaceholders => true;

    public ValueTask<int> GetColumnCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ColumnCount);
    }

    public ValueTask<FastTreeDataGridColumnPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var columnCount = ColumnCount;
        if (columnCount == 0 || request.Count == 0 || request.StartIndex >= columnCount)
        {
            return ValueTask.FromResult(FastTreeDataGridColumnPageResult.Empty);
        }

        var endExclusive = Math.Min(request.StartIndex + request.Count, columnCount);
        var descriptors = new List<FastTreeDataGridColumnDescriptor>(endExclusive - request.StartIndex);
        var placeholderIndices = new List<int>();
        var pendingTasks = new List<Task>();

        for (var index = request.StartIndex; index < endExclusive; index++)
        {
            var task = EnsureMaterializationStarted(index, cancellationToken);
            if (!task.IsCompleted)
            {
                pendingTasks.Add(task);
            }

            if (TryGetMaterializedColumn(index, out var descriptor))
            {
                descriptors.Add(descriptor);
            }
            else
            {
                placeholderIndices.Add(index);
            }
        }

        Task? completion = null;
        if (pendingTasks.Count > 0)
        {
            completion = Task.WhenAll(pendingTasks);
        }

        var result = new FastTreeDataGridColumnPageResult(
            descriptors,
            placeholderIndices,
            completion,
            cancellation: null);

        return ValueTask.FromResult(result);
    }

    public ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var columnCount = ColumnCount;
        if (columnCount == 0 || request.Count == 0 || request.StartIndex >= columnCount)
        {
            return ValueTask.CompletedTask;
        }

        var endExclusive = Math.Min(request.StartIndex + request.Count, columnCount);
        for (var index = request.StartIndex; index < endExclusive; index++)
        {
            _ = EnsureMaterializationStarted(index, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        ColumnSnapshotUpdate update = request.Kind switch
        {
            FastTreeDataGridInvalidationKind.Full => ApplyStructuralSnapshot(),
            FastTreeDataGridInvalidationKind.Range when request.HasRange => ApplyIncrementalSnapshot(
                CreateRangeIndices(
                    Math.Max(0, request.StartIndex),
                    Math.Max(0, Math.Min(request.Count, ColumnCount - request.StartIndex)))),
            _ => ApplyIncrementalSnapshot(null),
        };

        if (!update.HasChanges)
        {
            return Task.CompletedTask;
        }

        if (update.RequiresReset)
        {
            RaiseReset();
        }
        else
        {
            RaiseRangeInvalidations(update.ChangedIndices);
        }

        return Task.CompletedTask;
    }

    public bool TryGetMaterializedColumn(int index, out FastTreeDataGridColumnDescriptor column)
    {
        lock (_gate)
        {
            if ((uint)index < (uint)_entries.Count && _entries[index].CurrentDescriptor is { } descriptor)
            {
                column = descriptor;
                return true;
            }
        }

        column = default!;
        return false;
    }

    public bool IsPlaceholder(int index)
    {
        lock (_gate)
        {
            return (uint)index < (uint)_entries.Count && _entries[index].CurrentDescriptor is null;
        }
    }

    public FastTreeDataGridColumnDescriptor GetColumn(int index)
    {
        lock (_gate)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (_entries[index].CurrentDescriptor is { } descriptor)
            {
                return descriptor;
            }
        }

        throw new InvalidOperationException($"Column at index {index} has not been materialized.");
    }

    internal void RaiseColumnsChanged(int? columnIndex = null, bool structuralChange = false)
    {
        ColumnSnapshotUpdate update = structuralChange
            ? ApplyStructuralSnapshot()
            : ApplyIncrementalSnapshot(columnIndex.HasValue ? new[] { columnIndex.Value } : null);

        if (!update.HasChanges && !structuralChange)
        {
            return;
        }

        if (structuralChange || update.RequiresReset)
        {
            RaiseReset();
        }
        else
        {
            RaiseRangeInvalidations(update.ChangedIndices);
        }
    }

    private ColumnSnapshotUpdate ApplyStructuralSnapshot()
    {
        return RunOnUiThread(() =>
        {
            var columns = _owner.Columns;
            return ApplyStructuralSnapshot(columns);
        });
    }

    private ColumnSnapshotUpdate ApplyStructuralSnapshot(IReadOnlyList<FastTreeDataGridColumn> columns)
    {
        var cancellations = new List<CancellationTokenSource>();

        lock (_gate)
        {
            foreach (var entry in _entries)
            {
                if (entry.Cancellation is { } cts)
                {
                    cancellations.Add(cts);
                }
            }

            _entries.Clear();
            for (var i = 0; i < columns.Count; i++)
            {
                _entries.Add(new ColumnEntry(columns[i]));
            }
        }

        foreach (var cts in cancellations)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
            }

            cts.Dispose();
        }

        return columns.Count == 0
            ? new ColumnSnapshotUpdate(true, s_emptyIndices)
            : new ColumnSnapshotUpdate(true, CreateSequentialIndices(columns.Count));
    }

    private ColumnSnapshotUpdate ApplyIncrementalSnapshot(IReadOnlyList<int>? indices)
    {
        return RunOnUiThread(() =>
        {
            var columns = _owner.Columns;
            var columnCount = columns.Count;
            var targetIndices = indices ?? CreateSequentialIndices(columnCount);
            if (targetIndices.Count == 0)
            {
                return ColumnSnapshotUpdate.None;
            }

            var changed = new List<int>(targetIndices.Count);
            var cancellations = new List<CancellationTokenSource>();
            var requiresStructuralReset = false;

            lock (_gate)
            {
                if (_entries.Count != columnCount)
                {
                    requiresStructuralReset = true;
                }
                else
                {
                    foreach (var index in targetIndices)
                    {
                        if ((uint)index >= (uint)columnCount)
                        {
                            continue;
                        }

                        var entry = _entries[index];
                        if (!ReferenceEquals(entry.Column, columns[index]))
                        {
                            requiresStructuralReset = true;
                            break;
                        }

                        if (entry.Cancellation is { } existingCts)
                        {
                            cancellations.Add(existingCts);
                            entry.Cancellation = null;
                        }

                        entry.Column = columns[index];
                        entry.PendingSnapshot = CaptureSnapshot(columns[index]);
                        entry.MaterializationTask = null;
                        entry.MaterializationStartTimestamp = 0;
                        changed.Add(index);
                    }
                }
            }

            foreach (var cts in cancellations)
            {
                try
                {
                    cts.Cancel();
                }
                catch
                {
                }

                cts.Dispose();
            }

            if (requiresStructuralReset)
            {
                return ApplyStructuralSnapshot(columns);
            }

            return changed.Count == 0
                ? ColumnSnapshotUpdate.None
                : new ColumnSnapshotUpdate(false, changed);
        });
    }

    private static IReadOnlyList<int> CreateSequentialIndices(int count)
    {
        if (count <= 0)
        {
            return s_emptyIndices;
        }

        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = i;
        }

        return result;
    }

    private static IReadOnlyList<int> CreateRangeIndices(int start, int count)
    {
        if (count <= 0)
        {
            return s_emptyIndices;
        }

        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = start + i;
        }

        return result;
    }

    private Task EnsureMaterializationStarted(int index, CancellationToken cancellationToken)
    {
        ColumnSnapshot? snapshot = null;
        bool needsSnapshotCapture = false;
        FastTreeDataGridColumn? columnReference = null;

        lock (_gate)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                return Task.CompletedTask;
            }

            var entry = _entries[index];
            if (entry.CurrentDescriptor is not null && entry.PendingSnapshot is null)
            {
                return Task.CompletedTask;
            }

            if (entry.PendingSnapshot is null)
            {
                needsSnapshotCapture = true;
                columnReference = entry.Column;
            }
            else
            {
                snapshot = entry.PendingSnapshot;
            }

            if (entry.MaterializationTask is { } existingTask && !existingTask.IsCompleted)
            {
                return existingTask;
            }
        }

        if (needsSnapshotCapture)
        {
            snapshot = CaptureColumnSnapshot(columnReference ?? throw new InvalidOperationException());
        }

        CancellationTokenSource? linkedCts = null;
        int version;

        lock (_gate)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                return Task.CompletedTask;
            }

            var entry = _entries[index];
            if (snapshot is null)
            {
                snapshot = CaptureColumnSnapshot(entry.Column);
            }

            entry.PendingSnapshot ??= snapshot;
            snapshot = entry.PendingSnapshot;

            if (entry.MaterializationTask is { } existingTask && !existingTask.IsCompleted)
            {
                return existingTask;
            }

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            entry.Cancellation = linkedCts;
            version = ++entry.Version;
            entry.MaterializationStartTimestamp = Stopwatch.GetTimestamp();

            var task = MaterializeAsync(index, snapshot!, version, linkedCts.Token);
            entry.MaterializationTask = task;
            return task;
        }
    }

    private async Task MaterializeAsync(int index, ColumnSnapshot snapshot, int version, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var descriptor = await Task.Run(() => CreateDescriptor(snapshot), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            CompleteMaterialization(index, version, descriptor);
        }
        catch (OperationCanceledException)
        {
            MaterializationCancelled(index, version);
        }
        catch (Exception ex)
        {
            FastTreeDataGridVirtualizationDiagnostics.Log(
                "ColumnSource",
                ex.Message,
                ex,
                new KeyValuePair<string, object?>("index", index));
            MaterializationCancelled(index, version);
        }
    }

    private void CompleteMaterialization(int index, int version, FastTreeDataGridColumnDescriptor descriptor)
    {
        double latencyMs = 0;
        bool shouldRaise = false;

        lock (_gate)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                return;
            }

            var entry = _entries[index];
            if (entry.Version != version)
            {
                return;
            }

            entry.CurrentDescriptor = descriptor;
            entry.PendingSnapshot = null;
            entry.MaterializationTask = null;

            if (entry.Cancellation is { } cts)
            {
                entry.Cancellation = null;
                cts.Dispose();
            }

            if (entry.MaterializationStartTimestamp != 0)
            {
                latencyMs = Stopwatch.GetElapsedTime(entry.MaterializationStartTimestamp).TotalMilliseconds;
                entry.MaterializationStartTimestamp = 0;
            }

            shouldRaise = true;
        }

        if (latencyMs > 0)
        {
            FastTreeDataGridVirtualizationDiagnostics.ColumnPrefetchLatency.Record(
                latencyMs,
                new KeyValuePair<string, object?>("column_index", index));
        }

        if (!shouldRaise)
        {
            return;
        }

        void Raise()
        {
            ColumnMaterialized?.Invoke(this, new FastTreeDataGridColumnMaterializedEventArgs(index, descriptor));
            Invalidated?.Invoke(
                this,
                new FastTreeDataGridInvalidatedEventArgs(
                    new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, index, 1)));
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Raise();
        }
        else
        {
            Dispatcher.UIThread.Post(Raise, DispatcherPriority.Render);
        }
    }

    private void MaterializationCancelled(int index, int version)
    {
        CancellationTokenSource? cts = null;

        lock (_gate)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                return;
            }

            var entry = _entries[index];
            if (entry.Version != version)
            {
                return;
            }

            entry.MaterializationTask = null;
            entry.MaterializationStartTimestamp = 0;

            if (entry.Cancellation is { } existing)
            {
                cts = existing;
                entry.Cancellation = null;
            }
        }

        cts?.Dispose();
    }

    private void RaiseRangeInvalidations(IReadOnlyList<int> indices)
    {
        if (indices.Count == 0)
        {
            return;
        }

        void Raise()
        {
            var start = indices[0];
            var count = 1;

            for (var i = 1; i < indices.Count; i++)
            {
                var current = indices[i];
                if (current == start + count)
                {
                    count++;
                    continue;
                }

                Invalidated?.Invoke(
                    this,
                    new FastTreeDataGridInvalidatedEventArgs(
                        new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, start, count)));

                start = current;
                count = 1;
            }

            Invalidated?.Invoke(
                this,
                new FastTreeDataGridInvalidatedEventArgs(
                    new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Range, start, count)));
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Raise();
        }
        else
        {
            Dispatcher.UIThread.Post(Raise, DispatcherPriority.Render);
        }
    }

    private void RaiseReset()
    {
        void Raise()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
            Invalidated?.Invoke(
                this,
                new FastTreeDataGridInvalidatedEventArgs(
                    new FastTreeDataGridInvalidationRequest(FastTreeDataGridInvalidationKind.Full)));
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Raise();
        }
        else
        {
            Dispatcher.UIThread.Post(Raise, DispatcherPriority.Render);
        }
    }

    private ColumnSnapshotUpdate RunOnUiThread(Func<ColumnSnapshotUpdate> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return action();
        }

        return Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Render).GetAwaiter().GetResult();
    }

    private ColumnSnapshot CaptureColumnSnapshot(FastTreeDataGridColumn column)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return CaptureSnapshot(column);
        }

        return Dispatcher.UIThread.InvokeAsync(() => CaptureSnapshot(column), DispatcherPriority.Background).GetAwaiter().GetResult();
    }

    private static ColumnSnapshot CaptureSnapshot(FastTreeDataGridColumn column)
    {
        var headerText = column.Header switch
        {
            string s => s,
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.CurrentCulture),
            _ => column.Header?.ToString(),
        };

        var valueKey = column.ValueKey;
        var key = valueKey ?? column.GetHashCode().ToString();

        return new ColumnSnapshot(
            column,
            key,
            headerText,
            valueKey,
            column.SizingMode,
            column.PixelWidth,
            column.MinWidth,
            column.MaxWidth,
            column.IsHierarchy,
            column.PinnedPosition);
    }

    private static FastTreeDataGridColumnDescriptor CreateDescriptor(ColumnSnapshot snapshot)
    {
        var properties = new Dictionary<string, object?>
        {
            ["valueKey"] = snapshot.ValueKey,
            ["sizingMode"] = snapshot.SizingMode,
            ["pixelWidth"] = snapshot.PixelWidth,
            ["minWidth"] = snapshot.MinWidth,
            ["maxWidth"] = snapshot.MaxWidth,
            ["isHierarchy"] = snapshot.IsHierarchy,
            ["pinnedPosition"] = snapshot.PinnedPosition,
        };

        return new FastTreeDataGridColumnDescriptor(
            snapshot.Key,
            snapshot.Header,
            properties,
            payload: snapshot.Column);
    }
}
