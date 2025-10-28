using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Control.Widgets.Samples;

namespace FastTreeDataGrid.DataSourcesDemo.ViewModels;

public sealed class DataSourceSamplesViewModel : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly UpdateStreamBroker _updates = new();
    private readonly object _updateLock = new();
    private Timer? _updateTimer;
    private IDisposable? _streamSubscription;
    private IDisposable? _hybridSubscription;
    private bool _disposed;

    public DataSourceSamplesViewModel()
    {
        AsyncWidgets = new FastTreeDataGridAsyncSource<WidgetGalleryNode>(
            LoadWidgetsAsync,
            node => node.Children,
            node => node.Name);

        StreamingWidgets = new FastTreeDataGridStreamingSource<WidgetGalleryNode>(
            WidgetSamplesFactory.Create(),
            node => node.Children,
            node => node.Name);

        HybridWidgets = new FastTreeDataGridHybridSource<WidgetGalleryNode>(
            LoadWidgetsAsync,
            node => node.Children,
            node => node.Name);

        _streamSubscription = StreamingWidgets.Connect(_updates, preserveExpansion: true);
        _hybridSubscription = HybridWidgets.Connect(_updates, preserveExpansion: true);

        _ = InitializeAsync();
    }

    public FastTreeDataGridAsyncSource<WidgetGalleryNode> AsyncWidgets { get; }

    public FastTreeDataGridStreamingSource<WidgetGalleryNode> StreamingWidgets { get; }

    public FastTreeDataGridHybridSource<WidgetGalleryNode> HybridWidgets { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _updateTimer?.Dispose();
        _streamSubscription?.Dispose();
        _hybridSubscription?.Dispose();
        AsyncWidgets.Dispose();
        StreamingWidgets.Dispose();
        HybridWidgets.Dispose();
        _updates.Complete();
        _cts.Dispose();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await AsyncWidgets.LoadAsync(_cts.Token).ConfigureAwait(false);
            await HybridWidgets.InitializeAsync(_cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // Swallow errors for demo purposes.
        }

        lock (_updateLock)
        {
            if (_disposed)
            {
                return;
            }

            _updateTimer = new Timer(OnUpdateTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }
    }

    private async Task<IEnumerable<WidgetGalleryNode>> LoadWidgetsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(350), cancellationToken).ConfigureAwait(false);

        var nodes = await Dispatcher.UIThread.InvokeAsync(
            () => (IEnumerable<WidgetGalleryNode>)WidgetSamplesFactory.Create(),
            DispatcherPriority.Background);

        cancellationToken.ThrowIfCancellationRequested();
        return nodes;
    }

    private void OnUpdateTick(object? state)
    {
        if (_disposed)
        {
            return;
        }

        var update = CreateRandomUpdate();
        if (update is null)
        {
            return;
        }

        _updates.Publish(update);
    }

    private FastTreeDataGridStreamUpdate<WidgetGalleryNode>? CreateRandomUpdate()
    {
        return new FastTreeDataGridStreamUpdate<WidgetGalleryNode>(list =>
        {
            var leaf = TryPickRandomLeaf(list);
            if (leaf is null)
            {
                return;
            }

            var progress = Math.Round(Random.Shared.NextDouble(), 2);
            var level = Math.Clamp(progress, 0.05, 0.95);
            leaf.ProgressValue = new ProgressWidgetValue(level);

            var brush = CreateBadgeBrush(progress);
            leaf.BadgeValue = new BadgeWidgetValue($"{progress:P0}", brush, new ImmutableSolidColorBrush(Colors.White), CornerRadius: 10, Padding: 8);
        });
    }

    private static ImmutableSolidColorBrush CreateBadgeBrush(double value)
    {
        var clamped = Math.Clamp(value, 0d, 1d);
        var red = (byte)(255 * (1d - clamped));
        var green = (byte)(200 * clamped + 25);
        var blue = (byte)(120 + (80 * (1d - clamped)));
        var color = Color.FromRgb(red, green, blue);
        return new ImmutableSolidColorBrush(color);
    }

    private static WidgetGalleryNode? TryPickRandomLeaf(IList<WidgetGalleryNode> roots)
    {
        if (roots.Count == 0)
        {
            return null;
        }

        var leaves = new List<WidgetGalleryNode>();
        CollectLeaves(roots, leaves);
        if (leaves.Count == 0)
        {
            return null;
        }

        var index = Random.Shared.Next(leaves.Count);
        return leaves[index];
    }

    private static void CollectLeaves(IEnumerable<WidgetGalleryNode> nodes, ICollection<WidgetGalleryNode> accumulator)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Count == 0)
            {
                accumulator.Add(node);
            }
            else
            {
                CollectLeaves(node.Children, accumulator);
            }
        }
    }

    private sealed class UpdateStreamBroker : IObservable<FastTreeDataGridStreamUpdate<WidgetGalleryNode>>
    {
        private readonly List<IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>>> _observers = new();

        public IDisposable Subscribe(IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>> observer)
        {
            if (observer is null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (_observers)
            {
                _observers.Add(observer);
            }

            return new Subscription(this, observer);
        }

        public void Publish(FastTreeDataGridStreamUpdate<WidgetGalleryNode> update)
        {
            if (update is null)
            {
                return;
            }

            List<IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>>> snapshot;
            lock (_observers)
            {
                snapshot = _observers.ToList();
            }

            foreach (var observer in snapshot)
            {
                observer.OnNext(update);
            }
        }

        public void Complete()
        {
            List<IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>>> snapshot;
            lock (_observers)
            {
                snapshot = _observers.ToList();
                _observers.Clear();
            }

            foreach (var observer in snapshot)
            {
                observer.OnCompleted();
            }
        }

        private void Unsubscribe(IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>> observer)
        {
            lock (_observers)
            {
                _observers.Remove(observer);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly UpdateStreamBroker _owner;
            private IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>>? _observer;

            public Subscription(UpdateStreamBroker owner, IObserver<FastTreeDataGridStreamUpdate<WidgetGalleryNode>> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Dispose()
            {
                var observer = Interlocked.Exchange(ref _observer, null);
                if (observer is null)
                {
                    return;
                }

                _owner.Unsubscribe(observer);
            }
        }
    }
}
