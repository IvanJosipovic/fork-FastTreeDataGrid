using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Stores runtime grouping state, aggregates, and expansion flags.
/// </summary>
public sealed class FastTreeDataGridGroupingStateStore : IFastTreeDataGridGroupStateProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, GroupEntry> _groups = new(StringComparer.Ordinal);
    private readonly ObservableCollection<FastTreeDataGridGroupDescriptor> _descriptors = new();

    public event EventHandler<FastTreeDataGridGroupingStateChangedEventArgs>? GroupingStateChanged;

    public IReadOnlyList<FastTreeDataGridGroupDescriptor> Descriptors => _descriptors;

    public void SetDescriptors(IEnumerable<FastTreeDataGridGroupDescriptor> descriptors)
    {
        _lock.EnterWriteLock();
        try
        {
            _descriptors.Clear();
            foreach (var descriptor in descriptors)
            {
                _descriptors.Add(descriptor);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        GroupingStateChanged?.Invoke(
            this,
            new FastTreeDataGridGroupingStateChangedEventArgs(FastTreeDataGridGroupingChangeKind.DescriptorsChanged));
    }

    public void UpdateGroup(string path, FastTreeDataGridGroupState state)
    {
        _lock.EnterWriteLock();
        try
        {
            _groups[path] = new GroupEntry(state);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        GroupingStateChanged?.Invoke(
            this,
            new FastTreeDataGridGroupingStateChangedEventArgs(FastTreeDataGridGroupingChangeKind.GroupStateChanged, path));
    }

    public FastTreeDataGridGroupingSnapshot CreateSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            var descriptors = _descriptors.ToList();
            var groups = _groups.Values.Select(entry => entry.State).ToList();
            return new FastTreeDataGridGroupingSnapshot(descriptors, groups);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public FastTreeDataGridGroupState GetState(string path)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_groups.TryGetValue(path, out var entry))
            {
                return entry.State;
            }

            var descriptor = new FastTreeDataGridGroupDescriptor();
            var state = new FastTreeDataGridGroupState(
                descriptor,
                path,
                level: 0,
                key: null,
                isExpanded: true);

            _lock.EnterWriteLock();
            try
            {
                _groups[path] = new GroupEntry(state);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return state;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public void SetExpanded(string path, bool isExpanded)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_groups.TryGetValue(path, out var entry))
            {
                entry.State.IsExpanded = isExpanded;
            }
            else
            {
                var descriptor = new FastTreeDataGridGroupDescriptor();
                var state = new FastTreeDataGridGroupState(
                    descriptor,
                    path,
                    level: 0,
                    key: null,
                    isExpanded: isExpanded);
                _groups[path] = new GroupEntry(state);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        GroupingStateChanged?.Invoke(
            this,
            new FastTreeDataGridGroupingStateChangedEventArgs(FastTreeDataGridGroupingChangeKind.GroupStateChanged, path));
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _groups.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        GroupingStateChanged?.Invoke(
            this,
            new FastTreeDataGridGroupingStateChangedEventArgs(FastTreeDataGridGroupingChangeKind.Reset));
    }

    private sealed class GroupEntry
    {
        public GroupEntry(FastTreeDataGridGroupState state)
        {
            State = state;
        }

        public FastTreeDataGridGroupState State { get; }
    }
}
