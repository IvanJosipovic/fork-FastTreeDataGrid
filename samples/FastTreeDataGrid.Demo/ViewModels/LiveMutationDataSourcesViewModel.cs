using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class LiveMutationDataSourcesViewModel : IDisposable
{
    private readonly List<DynamicDataNode> _roots;
    private readonly object _rootsLock = new();
    private readonly Random _random = new();
    private readonly UpdateStreamBroker _updates = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Timer _updateTimer;
    private IDisposable? _streamSubscription;
    private IDisposable? _hybridSubscription;
    private bool _disposed;
    private int _nextNodeId;
    private int _refreshCounter;

    public LiveMutationDataSourcesViewModel()
    {
        _roots = GenerateForest(regionCount: 5, facilitiesPerRegion: 7, sensorsPerFacility: 28);

        AsyncSource = new FastTreeDataGridAsyncSource<DynamicDataNode>(
            LoadSnapshotAsync,
            node => node.Children,
            node => node.Id);

        StreamingSource = new FastTreeDataGridStreamingSource<DynamicDataNode>(
            CloneForest(_roots),
            node => node.Children,
            node => node.Id);

        HybridSource = new FastTreeDataGridHybridSource<DynamicDataNode>(
            LoadSnapshotAsync,
            node => node.Children,
            node => node.Id);

        _streamSubscription = StreamingSource.Connect(_updates, preserveExpansion: true);
        _hybridSubscription = HybridSource.Connect(_updates, preserveExpansion: true);

        _ = InitializeAsync();

        _updateTimer = new Timer(OnUpdateTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public FastTreeDataGridAsyncSource<DynamicDataNode> AsyncSource { get; }

    public FastTreeDataGridStreamingSource<DynamicDataNode> StreamingSource { get; }

    public FastTreeDataGridHybridSource<DynamicDataNode> HybridSource { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _updateTimer.Dispose();
        _streamSubscription?.Dispose();
        _hybridSubscription?.Dispose();
        AsyncSource.Dispose();
        StreamingSource.Dispose();
        HybridSource.Dispose();
        _updates.Complete();
        _cts.Dispose();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await AsyncSource.LoadAsync(_cts.Token).ConfigureAwait(false);
            await HybridSource.InitializeAsync(_cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // Demo scenario – ignore initialization glitches.
        }
    }

    private Task<IEnumerable<DynamicDataNode>> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_rootsLock)
            {
                return (IEnumerable<DynamicDataNode>)CloneForest(_roots);
            }
        }, cancellationToken);
    }

    private void OnUpdateTick(object? state)
    {
        if (_disposed)
        {
            return;
        }

        List<Mutation> mutations;
        lock (_rootsLock)
        {
            mutations = BuildMutations();
            if (mutations.Count == 0)
            {
                return;
            }

            foreach (var mutation in mutations)
            {
                mutation.Apply(_roots);
            }
        }

        var update = new FastTreeDataGridStreamUpdate<DynamicDataNode>(list =>
        {
            foreach (var mutation in mutations)
            {
                mutation.Apply(list);
            }
        });

        _updates.Publish(update);

        if (Interlocked.Increment(ref _refreshCounter) % 4 == 0)
        {
            TriggerAsyncRefresh();
        }
    }

    private void TriggerAsyncRefresh()
    {
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await AsyncSource.RefreshAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }, CancellationToken.None);
    }

    private List<Mutation> BuildMutations()
    {
        var mutations = new List<Mutation>();
        var flat = DynamicDataNode.Flatten(_roots);
        var leaves = flat.Where(node => !node.IsGroup).ToList();

        if (leaves.Count == 0)
        {
            return mutations;
        }

        var updateCount = Math.Min(leaves.Count, _random.Next(20, 50));
        for (var i = 0; i < updateCount; i++)
        {
            var leaf = leaves[_random.Next(leaves.Count)];
            var units = Math.Max(0, leaf.Units + _random.Next(-80, 120));
            var volume = Math.Max(0, leaf.Volume + _random.NextDouble() * 2200 - 900);
            var load = Math.Clamp(leaf.Load + _random.NextDouble() * 0.4 - 0.2, 0, 1);
            var change = Math.Clamp(leaf.Change + _random.NextDouble() * 6 - 3, -20, 20);
            mutations.Add(new UpdateMetricsMutation(leaf.Id, units, volume, load, change));
        }

        if (_random.NextDouble() < 0.55)
        {
            var facilities = flat.Where(node => node.Kind == DynamicNodeKind.Facility).ToList();
            if (facilities.Count > 0 && _random.NextDouble() < 0.7)
            {
                var facility = facilities[_random.Next(facilities.Count)];
                var sensor = CreateSensor(facility.Name, facility.Children.Count + 1);
                mutations.Add(new AddNodeMutation(facility.Id, sensor.CloneDeep()));
            }
            else
            {
                var regions = _roots;
                if (regions.Count > 0)
                {
                    var region = regions[_random.Next(regions.Count)];
                    var facilityIndex = region.Children.Count + 1;
                    var facility = CreateFacility(region.Name, facilityIndex);
                    mutations.Add(new AddNodeMutation(region.Id, facility.CloneDeep()));
                }
            }
        }

        if (_random.NextDouble() < 0.35)
        {
            var sensors = flat.Where(node => node.Kind == DynamicNodeKind.Sensor).ToList();
            if (sensors.Count > 0)
            {
                var victim = sensors[_random.Next(sensors.Count)];
                mutations.Add(new RemoveNodeMutation(victim.Id, victim.Parent?.Id));
            }
        }

        mutations.Add(new RefreshAggregatesMutation());
        return mutations;
    }

    private List<DynamicDataNode> CloneForest(IEnumerable<DynamicDataNode> source)
    {
        return source.Select(node => node.CloneDeep()).ToList();
    }

    private List<DynamicDataNode> GenerateForest(int regionCount, int facilitiesPerRegion, int sensorsPerFacility)
    {
        var regions = new List<DynamicDataNode>(regionCount);
        for (var regionIndex = 0; regionIndex < regionCount; regionIndex++)
        {
            var region = new DynamicDataNode(NewNodeId(), $"Region {regionIndex + 1:00}", DynamicNodeKind.Region);
            for (var facilityIndex = 0; facilityIndex < facilitiesPerRegion; facilityIndex++)
            {
                var facility = CreateFacility(region.Name, facilityIndex + 1);
                region.AddChild(facility);
            }

            region.RefreshAggregates();
            regions.Add(region);
        }

        return regions;
    }

    private DynamicDataNode CreateFacility(string regionName, int facilityIndex)
    {
        var facility = new DynamicDataNode(NewNodeId(), $"{regionName}-Facility {facilityIndex:00}", DynamicNodeKind.Facility);
        var sensorCount = _random.Next(12, 24);
        for (var sensorIndex = 0; sensorIndex < sensorCount; sensorIndex++)
        {
            var sensor = CreateSensor(facility.Name, sensorIndex + 1);
            facility.AddChild(sensor);
        }

        facility.RefreshAggregates();
        return facility;
    }

    private DynamicDataNode CreateSensor(string facilityName, int sensorIndex)
    {
        var sensor = new DynamicDataNode(NewNodeId(), $"{facilityName}-Sensor {sensorIndex:000}", DynamicNodeKind.Sensor);
        sensor.SeedLeafMetrics(_random);
        return sensor;
    }

    private string NewNodeId()
    {
        var value = Interlocked.Increment(ref _nextNodeId);
        return $"live-{value}";
    }

    private abstract class Mutation
    {
        public abstract void Apply(IList<DynamicDataNode> roots);
    }

    private sealed class UpdateMetricsMutation : Mutation
    {
        private readonly string _nodeId;
        private readonly double _units;
        private readonly double _volume;
        private readonly double _load;
        private readonly double _change;

        public UpdateMetricsMutation(string nodeId, double units, double volume, double load, double change)
        {
            _nodeId = nodeId;
            _units = units;
            _volume = volume;
            _load = load;
            _change = change;
        }

        public override void Apply(IList<DynamicDataNode> roots)
        {
            var node = DynamicDataNode.FindById(roots, _nodeId);
            node?.SetMetrics(_units, _volume, _load, _change);
        }
    }

    private sealed class AddNodeMutation : Mutation
    {
        private readonly string? _parentId;
        private readonly DynamicDataNode _template;

        public AddNodeMutation(string? parentId, DynamicDataNode template)
        {
            _parentId = parentId;
            _template = template;
        }

        public override void Apply(IList<DynamicDataNode> roots)
        {
            if (string.IsNullOrEmpty(_parentId))
            {
                roots.Add(_template.CloneDeep());
                return;
            }

            var parent = DynamicDataNode.FindById(roots, _parentId);
            if (parent is null)
            {
                return;
            }

            parent.AddChild(_template.CloneDeep());
        }
    }

    private sealed class RemoveNodeMutation : Mutation
    {
        private readonly string _nodeId;
        private readonly string? _parentId;

        public RemoveNodeMutation(string nodeId, string? parentId)
        {
            _nodeId = nodeId;
            _parentId = parentId;
        }

        public override void Apply(IList<DynamicDataNode> roots)
        {
            if (string.IsNullOrEmpty(_parentId))
            {
                for (var i = 0; i < roots.Count; i++)
                {
                    if (string.Equals(roots[i].Id, _nodeId, StringComparison.Ordinal))
                    {
                        roots.RemoveAt(i);
                        break;
                    }
                }

                return;
            }

            var parent = DynamicDataNode.FindById(roots, _parentId);
            if (parent is null)
            {
                return;
            }

            parent.RemoveChild(_nodeId);
        }
    }

    private sealed class RefreshAggregatesMutation : Mutation
    {
        public override void Apply(IList<DynamicDataNode> roots)
        {
            foreach (var root in roots)
            {
                root.RefreshAggregates();
            }
        }
    }

    private sealed class UpdateStreamBroker : IObservable<FastTreeDataGridStreamUpdate<DynamicDataNode>>
    {
        private readonly List<IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>>> _observers = new();

        public IDisposable Subscribe(IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>> observer)
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

        public void Publish(FastTreeDataGridStreamUpdate<DynamicDataNode> update)
        {
            List<IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>>> snapshot;
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
            List<IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>>> snapshot;
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

        private void Unsubscribe(IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>> observer)
        {
            lock (_observers)
            {
                _observers.Remove(observer);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly UpdateStreamBroker _owner;
            private IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>>? _observer;

            public Subscription(UpdateStreamBroker owner, IObserver<FastTreeDataGridStreamUpdate<DynamicDataNode>> observer)
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
