using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Engine.Infrastructure;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public sealed class FastTreeDataGridViewportSchedulerTests
{
    [Fact]
    public void SchedulerRaisesLoadingEvents()
    {
        var provider = new StubVirtualizationProvider();
        var settings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 2,
            PrefetchRadius = 0,
            MaxConcurrentLoads = 4,
            MaxPages = 16,
        };

        using var scheduler = new FastTreeDataGridViewportScheduler(provider, settings);

        var events = new List<FastTreeDataGridLoadingStateEventArgs>();
        using var completedSignal = new ManualResetEventSlim(false);

        scheduler.LoadingStateChanged += (_, args) =>
        {
            lock (events)
            {
                events.Add(args);
                if (!args.IsLoading)
                {
                    completedSignal.Set();
                }
            }
        };

        scheduler.Request(new FastTreeDataGridViewportRequest(0, 8, 0));

        var completed = completedSignal.Wait(TimeSpan.FromSeconds(2));
        Assert.True(completed);

        lock (events)
        {
            Assert.NotEmpty(events);
            Assert.Contains(events, e => e.IsLoading);
            Assert.Contains(events, e => !e.IsLoading);
        }
    }

    private sealed class StubVirtualizationProvider : IFastTreeDataVirtualizationProvider
    {
        public event EventHandler<FastTreeDataGridInvalidatedEventArgs>? Invalidated
        {
            add { }
            remove { }
        }

        public event EventHandler<FastTreeDataGridRowMaterializedEventArgs>? RowMaterialized
        {
            add { }
            remove { }
        }

        public event EventHandler<FastTreeDataGridCountChangedEventArgs>? CountChanged
        {
            add { }
            remove { }
        }

        public bool SupportsMutations => false;

        public bool IsInitialized => true;

        public ValueTask InitializeAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask<int> GetRowCountAsync(CancellationToken cancellationToken) => new(0);

        public async ValueTask<FastTreeDataGridPageResult> GetPageAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return FastTreeDataGridPageResult.Empty;
        }

        public async ValueTask PrefetchAsync(FastTreeDataGridPageRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }

        public Task InvalidateAsync(FastTreeDataGridInvalidationRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

        public bool TryGetMaterializedRow(int index, out FastTreeDataGridRow row)
        {
            row = default!;
            return false;
        }

        public bool IsPlaceholder(int index) => false;

        public Task<int> LocateRowIndexAsync(object? item, CancellationToken cancellationToken) => Task.FromResult(-1);

        public Task CreateAsync(object viewModel, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(object viewModel, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(object viewModel, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ApplySortFilterAsync(FastTreeDataGridSortFilterRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
