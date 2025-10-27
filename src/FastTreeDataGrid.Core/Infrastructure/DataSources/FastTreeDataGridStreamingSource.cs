using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Control.Infrastructure;

public class FastTreeDataGridStreamingSource<T> : FastTreeDataGridDynamicSource<T>
{
    private readonly List<T> _items;
    private readonly object _syncRoot = new();
    private readonly List<IDisposable> _subscriptions = new();
    private readonly List<Task> _runningTasks = new();
    private bool _disposed;

    public FastTreeDataGridStreamingSource(
        IEnumerable<T>? initialItems,
        Func<T, IEnumerable<T>> childrenSelector,
        Func<T, string>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
        : base(initialItems ?? Array.Empty<T>(), childrenSelector, keySelector, keyComparer)
    {
        _items = initialItems?.ToList() ?? new List<T>();
    }

    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_syncRoot)
            {
                return _items.ToArray();
            }
        }
    }

    public void ApplyUpdate(Action<IList<T>> updateAction, bool preserveExpansion = true)
    {
        if (updateAction is null)
        {
            throw new ArgumentNullException(nameof(updateAction));
        }

        List<T> snapshot;
        lock (_syncRoot)
        {
            updateAction(_items);
            snapshot = _items.ToList();
        }

        ResetWithSnapshot(snapshot, preserveExpansion);
    }

    public IDisposable Connect(IObservable<FastTreeDataGridStreamUpdate<T>> updates, bool preserveExpansion = true)
    {
        if (updates is null)
        {
            throw new ArgumentNullException(nameof(updates));
        }

        var observer = new UpdateObserver(this, preserveExpansion);
        var subscription = updates.Subscribe(observer);

        RegisterSubscription(subscription);
        return new Subscription(this, subscription);
    }

    public Task ConnectAsync(
        IAsyncEnumerable<FastTreeDataGridStreamUpdate<T>> updates,
        CancellationToken cancellationToken = default,
        bool preserveExpansion = true)
    {
        if (updates is null)
        {
            throw new ArgumentNullException(nameof(updates));
        }

        var task = Task.Run(async () =>
        {
            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ApplyUpdate(update.Apply, preserveExpansion);
            }
        }, cancellationToken);

        RegisterTask(task);
        return task;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            base.Dispose(disposing);
            return;
        }

        lock (_syncRoot)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        base.Dispose(disposing);
        _disposed = true;
    }

    private void RegisterSubscription(IDisposable subscription)
    {
        lock (_syncRoot)
        {
            _subscriptions.Add(subscription);
        }
    }

    private void UnregisterSubscription(IDisposable subscription)
    {
        lock (_syncRoot)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private void RegisterTask(Task task)
    {
        lock (_syncRoot)
        {
            _runningTasks.Add(task);
        }

        _ = task.ContinueWith(_ => CleanupTask(task), TaskScheduler.Default);
    }

    private void CleanupTask(Task task)
    {
        lock (_syncRoot)
        {
            _runningTasks.Remove(task);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly FastTreeDataGridStreamingSource<T> _owner;
        private IDisposable? _subscription;

        public Subscription(FastTreeDataGridStreamingSource<T> owner, IDisposable subscription)
        {
            _owner = owner;
            _subscription = subscription;
        }

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _subscription, null);
            if (current is null)
            {
                return;
            }

            current.Dispose();
            _owner.UnregisterSubscription(current);
        }
    }

    private sealed class UpdateObserver : IObserver<FastTreeDataGridStreamUpdate<T>>
    {
        private readonly FastTreeDataGridStreamingSource<T> _owner;
        private readonly bool _preserveExpansion;

        public UpdateObserver(FastTreeDataGridStreamingSource<T> owner, bool preserveExpansion)
        {
            _owner = owner;
            _preserveExpansion = preserveExpansion;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(FastTreeDataGridStreamUpdate<T> value)
        {
            if (value is null)
            {
                return;
            }

            _owner.ApplyUpdate(value.Apply, _preserveExpansion);
        }
    }
}
