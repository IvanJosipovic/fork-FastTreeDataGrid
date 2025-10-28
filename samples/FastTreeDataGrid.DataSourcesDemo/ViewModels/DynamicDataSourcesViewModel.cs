using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.DataSourcesDemo.ViewModels;

public sealed class DynamicDataSourcesViewModel : IDisposable
{
    private const int InitialRegionCount = 6;
    private const int InitialFacilitiesPerRegion = 8;
    private const int InitialSensorsPerFacility = 24;

    private readonly CancellationTokenSource _cts = new();
    private readonly UpdateStreamBroker _updates = new();
    private readonly Random _random = new();
    private readonly object _timerLock = new();
    private Timer? _updateTimer;
    private bool _disposed;
    private int _nextNodeId;
    private IDisposable? _streamSubscription;
    private IDisposable? _hybridSubscription;

    public DynamicDataSourcesViewModel()
    {
        AsyncSource = new FastTreeDataGridAsyncSource<DynamicDataNode>(
            LoadSnapshotAsync,
            node => node.Children,
            node => node.Id);

        var streamingRoots = GenerateForest(InitialRegionCount, InitialFacilitiesPerRegion, InitialSensorsPerFacility);
        StreamingSource = new FastTreeDataGridStreamingSource<DynamicDataNode>(
            streamingRoots,
            node => node.Children,
            node => node.Id);

        HybridSource = new FastTreeDataGridHybridSource<DynamicDataNode>(
            LoadSnapshotAsync,
            node => node.Children,
            node => node.Id);

        _streamSubscription = StreamingSource.Connect(_updates, preserveExpansion: true);
        _hybridSubscription = HybridSource.Connect(_updates, preserveExpansion: true);

        _ = InitializeAsync();

        lock (_timerLock)
        {
            _updateTimer = new Timer(OnUpdateTick, null, TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(1.5));
        }
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
        lock (_timerLock)
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
        }

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
            // Demo scenario – ignore transient initialization errors.
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(6), _cts.Token).ConfigureAwait(false);
                await AsyncSource.RefreshAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }, _cts.Token);
    }

    private Task<IEnumerable<DynamicDataNode>> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var regions = GenerateForest(
                regionCount: InitialRegionCount + 1,
                facilitiesPerRegion: InitialFacilitiesPerRegion + 2,
                sensorsPerFacility: InitialSensorsPerFacility + 6);
            return (IEnumerable<DynamicDataNode>)regions;
        }, cancellationToken);
    }

    private void OnUpdateTick(object? state)
    {
        if (_disposed)
        {
            return;
        }

        var update = CreateUpdate();
        _updates.Publish(update);
    }

    private FastTreeDataGridStreamUpdate<DynamicDataNode> CreateUpdate()
    {
        return new FastTreeDataGridStreamUpdate<DynamicDataNode>(roots =>
        {
            if (roots.Count == 0)
            {
                return;
            }

            var allNodes = DynamicDataNode.Flatten(roots);
            var leaves = allNodes.Where(node => !node.IsGroup).ToList();
            if (leaves.Count == 0)
            {
                return;
            }

            var updates = Math.Min(leaves.Count, _random.Next(40, 80));
            for (var i = 0; i < updates; i++)
            {
                var leaf = leaves[_random.Next(leaves.Count)];
                leaf.ApplyLeafUpdate(_random);
            }

            if (_random.NextDouble() < 0.35)
            {
                AddSensorOrFacility(roots);
            }

            if (_random.NextDouble() < 0.20)
            {
                RemoveRandomSensor(roots);
            }

            foreach (var region in roots)
            {
                region.RefreshAggregates();
            }
        });
    }

    private void AddSensorOrFacility(IList<DynamicDataNode> roots)
    {
        var facilities = DynamicDataNode.Flatten(roots)
            .Where(node => node.Kind == DynamicNodeKind.Facility)
            .ToList();

        if (facilities.Count == 0)
        {
            return;
        }

        if (_random.NextDouble() < 0.45)
        {
            var region = roots[_random.Next(roots.Count)];
            var newFacilityIndex = region.Children.Count + 1;
            var facility = CreateFacility(region.Name, $"Facility {newFacilityIndex:00}", sensors: _random.Next(8, 18));
            region.AddChild(facility);
        }
        else
        {
            var facility = facilities[_random.Next(facilities.Count)];
            var sensorIndex = facility.Children.Count + 1;
            var sensor = CreateSensor(facility.Name, $"Sensor {sensorIndex:000}");
            facility.AddChild(sensor);
        }
    }

    private void RemoveRandomSensor(IList<DynamicDataNode> roots)
    {
        var facilities = DynamicDataNode.Flatten(roots)
            .Where(node => node.Kind == DynamicNodeKind.Facility && node.Children.Count > 5)
            .ToList();

        if (facilities.Count == 0)
        {
            return;
        }

        var facility = facilities[_random.Next(facilities.Count)];
        if (facility.Children.Count == 0)
        {
            return;
        }

        var removeIndex = _random.Next(facility.Children.Count);
        facility.RemoveChildAt(removeIndex);
    }

    private List<DynamicDataNode> GenerateForest(int regionCount, int facilitiesPerRegion, int sensorsPerFacility)
    {
        var regions = new List<DynamicDataNode>(regionCount);
        for (var regionIndex = 0; regionIndex < regionCount; regionIndex++)
        {
            var region = new DynamicDataNode(NewNodeId(), $"Region {regionIndex + 1:00}", DynamicNodeKind.Region);
            for (var facilityIndex = 0; facilityIndex < facilitiesPerRegion; facilityIndex++)
            {
                var facility = new DynamicDataNode(NewNodeId(), $"Facility {regionIndex + 1:00}-{facilityIndex + 1:00}", DynamicNodeKind.Facility);
                var sensorCount = sensorsPerFacility + _random.Next(-4, 5);
                for (var sensorIndex = 0; sensorIndex < Math.Max(sensorCount, 6); sensorIndex++)
                {
                    var sensor = new DynamicDataNode(NewNodeId(), $"Sensor {regionIndex + 1:00}-{facilityIndex + 1:00}-{sensorIndex + 1:000}", DynamicNodeKind.Sensor);
                    sensor.SeedLeafMetrics(_random);
                    facility.AddChild(sensor);
                }
                facility.RefreshAggregates();
                region.AddChild(facility);
            }
            region.RefreshAggregates();
            regions.Add(region);
        }

        return regions;
    }

    private DynamicDataNode CreateFacility(string regionName, string facilityName, int sensors)
    {
        var facility = new DynamicDataNode(NewNodeId(), $"{regionName}-{facilityName}", DynamicNodeKind.Facility);
        var count = Math.Max(6, sensors);
        for (var i = 0; i < count; i++)
        {
            var sensor = new DynamicDataNode(NewNodeId(), $"{facilityName}-{i + 1:000}", DynamicNodeKind.Sensor);
            sensor.SeedLeafMetrics(_random);
            facility.AddChild(sensor);
        }
        facility.RefreshAggregates();
        return facility;
    }

    private DynamicDataNode CreateSensor(string facilityName, string sensorName)
    {
        var sensor = new DynamicDataNode(NewNodeId(), $"{facilityName}-{sensorName}", DynamicNodeKind.Sensor);
        sensor.SeedLeafMetrics(_random);
        return sensor;
    }

    private string NewNodeId()
    {
        var value = Interlocked.Increment(ref _nextNodeId);
        return $"dyn-{value}";
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
            if (update is null)
            {
                return;
            }

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

public enum DynamicNodeKind
{
    Region,
    Facility,
    Sensor
}

public sealed class DynamicDataNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyName = "Dynamic.Name";
    public const string KeyCategory = "Dynamic.Category";
    public const string KeyUnits = "Dynamic.Units";
    public const string KeyVolume = "Dynamic.Volume";
    public const string KeyChange = "Dynamic.Change";
    public const string KeyLoad = "Dynamic.Load";
    public const string KeyStatus = "Dynamic.Status";

    private readonly List<DynamicDataNode> _children = new();
    private readonly object _metricsLock = new();
    private double _units;
    private double _volume;
    private double _load;
    private double _change;
    private ImmutableSolidColorBrush _badgeBrush = new(Color.FromRgb(96, 165, 250));
    private string _badgeText = "Stable";
    private EventHandler<ValueInvalidatedEventArgs>? _valueInvalidated;

    public DynamicDataNode(string id, string name, DynamicNodeKind kind)
    {
        Id = id;
        Name = name;
        Kind = kind;
    }

    public string Id { get; }

    public string Name { get; }

    public DynamicNodeKind Kind { get; }

    public IReadOnlyList<DynamicDataNode> Children => _children;

    public DynamicDataNode? Parent { get; private set; }

    public bool IsGroup => _children.Count > 0;

    public double Units
    {
        get
        {
            lock (_metricsLock)
            {
                return _units;
            }
        }
    }

    public double Volume
    {
        get
        {
            lock (_metricsLock)
            {
                return _volume;
            }
        }
    }

    public double Load
    {
        get
        {
            lock (_metricsLock)
            {
                return _load;
            }
        }
    }

    public double Change
    {
        get
        {
            lock (_metricsLock)
            {
                return _change;
            }
        }
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add => _valueInvalidated += value;
        remove => _valueInvalidated -= value;
    }

    public DynamicDataNode CloneDeep()
    {
        var clone = new DynamicDataNode(Id, Name, Kind);
        lock (_metricsLock)
        {
            clone._units = _units;
            clone._volume = _volume;
            clone._load = _load;
            clone._change = _change;
            clone._badgeBrush = _badgeBrush;
            clone._badgeText = _badgeText;
        }

        foreach (var child in _children)
        {
            var childClone = child.CloneDeep();
            childClone.Parent = clone;
            clone._children.Add(childClone);
        }

        return clone;
    }

    public void AddChild(DynamicDataNode child)
    {
        if (child is null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        child.Parent = this;
        _children.Add(child);
        NotifyChanged();
    }

    public void RemoveChildAt(int index)
    {
        if ((uint)index >= (uint)_children.Count)
        {
            return;
        }

        _children.RemoveAt(index);
        NotifyChanged();
    }

    public bool RemoveChild(DynamicDataNode child)
    {
        if (child is null)
        {
            return false;
        }

        var index = _children.IndexOf(child);
        if (index < 0)
        {
            return false;
        }

        _children.RemoveAt(index);
        NotifyChanged();
        return true;
    }

    public bool RemoveChild(string id)
    {
        for (var i = 0; i < _children.Count; i++)
        {
            if (string.Equals(_children[i].Id, id, StringComparison.Ordinal))
            {
                _children.RemoveAt(i);
                NotifyChanged();
                return true;
            }
        }

        return false;
    }

    public void SeedLeafMetrics(Random random)
    {
        lock (_metricsLock)
        {
            _units = Math.Max(1, random.Next(120, 240) + random.NextDouble());
            _volume = Math.Max(10, random.NextDouble() * 14000 + 2500);
            _load = Math.Clamp(random.NextDouble(), 0.05, 0.95);
            _change = Math.Clamp(random.NextDouble() * 8 - 4, -15, 15);
            UpdateBadge();
        }
        NotifyChanged();
    }

    public void ApplyLeafUpdate(Random random)
    {
        if (IsGroup)
        {
            return;
        }

        lock (_metricsLock)
        {
            _units = Math.Max(0, _units + random.Next(-35, 55));
            _volume = Math.Max(0, _volume + random.NextDouble() * 1800 - 900);
            _load = Math.Clamp(_load + random.NextDouble() * 0.25 - 0.12, 0, 1);
            _change = Math.Clamp(_change + random.NextDouble() * 6 - 3, -20, 20);
            UpdateBadge();
        }

        NotifyChanged();
        Parent?.RefreshAggregates();
    }

    public void SetMetrics(double units, double volume, double load, double change)
    {
        lock (_metricsLock)
        {
            _units = units;
            _volume = volume;
            _load = Math.Clamp(load, 0, 1);
            _change = change;
            UpdateBadge();
        }

        NotifyChanged();
    }

    public void RefreshAggregates()
    {
        if (!IsGroup)
        {
            return;
        }

        lock (_metricsLock)
        {
            double units = 0;
            double volume = 0;
            double loadSum = 0;
            double changeSum = 0;
            var count = 0;

            foreach (var child in _children)
            {
                child.RefreshAggregates();
                units += child.Units;
                volume += child.Volume;
                loadSum += child.Load;
                changeSum += child.Change;
                count++;
            }

            _units = units;
            _volume = volume;
            _load = count > 0 ? Math.Clamp(loadSum / count, 0, 1) : 0;
            _change = count > 0 ? changeSum / count : 0;
            UpdateBadge();
        }

        NotifyChanged();
    }

    public object? GetValue(object? item, string key) =>
        key switch
        {
            KeyName => Name,
            KeyCategory => Kind switch
            {
                DynamicNodeKind.Region => "Region",
                DynamicNodeKind.Facility => "Facility",
                _ => "Sensor"
            },
            KeyUnits => Units.ToString("N0"),
            KeyVolume => Volume.ToString("N1"),
            KeyChange => Change >= 0 ? $"+{Change:F1}%" : $"{Change:F1}%",
            KeyLoad => new ProgressWidgetValue(Load, IsIndeterminate: false),
            KeyStatus => new BadgeWidgetValue(_badgeText, Background: _badgeBrush),
            _ => null
        };

    private void UpdateBadge()
    {
        var change = _change;
        if (change > 1.5)
        {
            _badgeText = "Rising";
            _badgeBrush = new ImmutableSolidColorBrush(Color.FromRgb(34, 197, 94));
        }
        else if (change < -1.5)
        {
            _badgeText = "Falling";
            _badgeBrush = new ImmutableSolidColorBrush(Color.FromRgb(239, 68, 68));
        }
        else
        {
            _badgeText = "Stable";
            _badgeBrush = new ImmutableSolidColorBrush(Color.FromRgb(96, 165, 250));
        }
    }

    private void NotifyChanged()
    {
        _valueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, null));
    }

    public static IReadOnlyList<DynamicDataNode> Flatten(IEnumerable<DynamicDataNode> roots)
    {
        var list = new List<DynamicDataNode>();
        foreach (var node in roots)
        {
            Append(node, list);
        }
        return list;
    }

    private static void Append(DynamicDataNode node, ICollection<DynamicDataNode> output)
    {
        output.Add(node);
        foreach (var child in node.Children)
        {
            Append(child, output);
        }
    }

    public static DynamicDataNode? FindById(IEnumerable<DynamicDataNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Id, id, StringComparison.Ordinal))
            {
                return node;
            }

            var match = FindById(node._children, id);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
