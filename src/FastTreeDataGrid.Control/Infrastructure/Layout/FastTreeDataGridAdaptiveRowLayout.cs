using System;
using System.Collections.Generic;

using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridAdaptiveRowLayout : IFastTreeDataGridRowLayout
{
    private readonly IFastTreeDataGridVariableRowHeightProvider _heightProvider;
    private readonly Dictionary<int, Block> _blocks = new();
    private ControlsFastTreeDataGrid? _owner;
    private IFastTreeDataGridSource? _source;
    private double _lastDefaultRowHeight = 28d;
    private int _latestTotalRows;
    private double _lastEstimatedTotalHeight;
    private int _chunkSize = 512;
    private int _samplesPerBlock = 3;

    public FastTreeDataGridAdaptiveRowLayout()
        : this(new FastTreeDataGridDefaultVariableRowHeightProvider())
    {
    }

    public FastTreeDataGridAdaptiveRowLayout(IFastTreeDataGridVariableRowHeightProvider heightProvider)
    {
        _heightProvider = heightProvider ?? throw new ArgumentNullException(nameof(heightProvider));
    }

    public int ChunkSize
    {
        get => _chunkSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == _chunkSize)
            {
                return;
            }

            _chunkSize = value;
            Reset();
        }
    }

    public int SamplesPerBlock
    {
        get => _samplesPerBlock;
        set
        {
            var clamped = Math.Clamp(value, 0, 16);
            if (clamped == _samplesPerBlock)
            {
                return;
            }

            _samplesPerBlock = clamped;
            Reset();
        }
    }

    public void Attach(ControlsFastTreeDataGrid owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Detach()
    {
        _owner = null;
        _source = null;
        Reset();
    }

    public void Bind(IFastTreeDataGridSource? source)
    {
        _source = source;
        Reset();
    }

    public void Reset()
    {
        _blocks.Clear();
        _latestTotalRows = 0;
        _lastEstimatedTotalHeight = 0;
    }

    public RowLayoutViewport GetVisibleRange(double verticalOffset, double viewportHeight, double defaultRowHeight, int totalRows, int buffer)
    {
        if (_source is null || totalRows <= 0)
        {
            return RowLayoutViewport.Empty;
        }

        _latestTotalRows = totalRows;
        _lastDefaultRowHeight = Math.Max(1d, defaultRowHeight);

        var totalHeight = _lastEstimatedTotalHeight > 0
            ? _lastEstimatedTotalHeight
            : GetTotalHeightInternal(totalRows, _lastDefaultRowHeight);

        var clampedOffset = Math.Clamp(verticalOffset, 0, Math.Max(0, totalHeight - _lastDefaultRowHeight));

        var chunkCount = GetChunkCount(totalRows);
        var accumulated = 0d;
        var firstIndex = 0;
        var firstRowTop = 0d;
        var found = false;

        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            var block = GetOrCreateBlock(chunkIndex, totalRows, _lastDefaultRowHeight);
            EnsureBlockSamples(block, totalRows);

            var chunkHeight = block.TotalHeight;
            if (accumulated + chunkHeight <= clampedOffset)
            {
                accumulated += chunkHeight;
                continue;
            }

            var localOffset = clampedOffset - accumulated;
            var localIndex = 0;
            var rowTop = accumulated;
            var globalIndex = block.StartIndex;

            while (localIndex < block.Count && globalIndex < totalRows)
            {
                var height = GetOrMeasure(block, localIndex, globalIndex, _lastDefaultRowHeight);
                if (localOffset < height)
                {
                    firstIndex = globalIndex;
                    firstRowTop = rowTop;
                    found = true;
                    break;
                }

                localOffset -= height;
                rowTop += height;
                localIndex++;
                globalIndex++;
            }

            if (!found)
            {
                var fallbackIndex = Math.Min(block.StartIndex + block.Count, Math.Max(0, totalRows - 1));
                firstIndex = fallbackIndex;
                firstRowTop = accumulated + chunkHeight;
                found = true;
            }

            break;
        }

        if (!found)
        {
            firstIndex = Math.Max(0, totalRows - 1);
            firstRowTop = Math.Max(0, totalHeight - _lastDefaultRowHeight);
        }

        var lastIndexExclusive = firstIndex;
        var cursorTop = firstRowTop;
        var bottomTarget = clampedOffset + viewportHeight;

        while (lastIndexExclusive < totalRows && cursorTop < bottomTarget)
        {
            var block = GetOrCreateBlock(lastIndexExclusive / _chunkSize, totalRows, _lastDefaultRowHeight);
            var localIndex = lastIndexExclusive - block.StartIndex;
            var height = GetOrMeasure(block, localIndex, lastIndexExclusive, _lastDefaultRowHeight);
            cursorTop += height;
            lastIndexExclusive++;
        }

        lastIndexExclusive = Math.Min(totalRows, lastIndexExclusive + buffer);
        return new RowLayoutViewport(firstIndex, lastIndexExclusive, firstRowTop);
    }

    public double GetRowHeight(int rowIndex, FastTreeDataGridRow row, double defaultRowHeight)
    {
        if (_source is null)
        {
            return Math.Max(1d, defaultRowHeight);
        }

        _latestTotalRows = Math.Max(_source.RowCount, _latestTotalRows);
        _lastDefaultRowHeight = Math.Max(1d, defaultRowHeight);

        var block = GetOrCreateBlock(rowIndex / _chunkSize, _source.RowCount, _lastDefaultRowHeight);
        var localIndex = rowIndex - block.StartIndex;
        if (block.TryGetMeasured(localIndex, out var measured))
        {
            return measured;
        }

        var height = Math.Max(1d, _heightProvider.GetRowHeight(row, rowIndex, _lastDefaultRowHeight));
        block.SetMeasuredHeight(localIndex, height);
        _lastEstimatedTotalHeight = 0;
        return height;
    }

    public double GetRowTop(int rowIndex)
    {
        if (rowIndex <= 0)
        {
            return 0;
        }

        var totalRows = _source?.RowCount ?? _latestTotalRows;
        if (totalRows <= 0)
        {
            return rowIndex * _lastDefaultRowHeight;
        }

        var defaultHeight = _lastDefaultRowHeight > 0 ? _lastDefaultRowHeight : 28d;
        var chunkIndex = rowIndex / _chunkSize;
        var top = 0d;

        for (var i = 0; i < chunkIndex; i++)
        {
            var block = GetOrCreateBlock(i, totalRows, defaultHeight);
            top += block.TotalHeight;
        }

        var targetBlock = GetOrCreateBlock(chunkIndex, totalRows, defaultHeight);
        var withinBlock = Math.Clamp(rowIndex - targetBlock.StartIndex, 0, targetBlock.Count);
        top += targetBlock.SumBefore(withinBlock);
        return top;
    }

    public double GetTotalHeight(double viewportHeight, double defaultRowHeight, int totalRows)
    {
        if (_source is null || totalRows <= 0)
        {
            _lastEstimatedTotalHeight = 0;
            return Math.Max(0, viewportHeight);
        }

        _latestTotalRows = totalRows;
        _lastDefaultRowHeight = Math.Max(1d, defaultRowHeight);

        var total = GetTotalHeightInternal(totalRows, _lastDefaultRowHeight);
        _lastEstimatedTotalHeight = total;
        return Math.Max(total, viewportHeight);
    }

    public void InvalidateRow(int rowIndex)
    {
        if (_chunkSize <= 0)
        {
            return;
        }

        var chunkIndex = rowIndex / _chunkSize;
        if (!_blocks.TryGetValue(chunkIndex, out var block))
        {
            return;
        }

        var localIndex = rowIndex - block.StartIndex;
        if (block.RemoveMeasurement(localIndex))
        {
            _lastEstimatedTotalHeight = 0;
        }
    }

    private int GetChunkCount(int totalRows)
    {
        if (totalRows <= 0 || _chunkSize <= 0)
        {
            return 0;
        }

        return (totalRows + _chunkSize - 1) / _chunkSize;
    }

    private double GetTotalHeightInternal(int totalRows, double defaultRowHeight)
    {
        var chunkCount = GetChunkCount(totalRows);
        double total = 0;

        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            var block = GetOrCreateBlock(chunkIndex, totalRows, defaultRowHeight);
            EnsureBlockSamples(block, totalRows);
            total += block.TotalHeight;
        }

        return total;
    }

    private Block GetOrCreateBlock(int chunkIndex, int totalRows, double defaultRowHeight)
    {
        if (_chunkSize <= 0)
        {
            _chunkSize = 512;
        }

        var start = chunkIndex * _chunkSize;
        var count = Math.Max(0, Math.Min(_chunkSize, totalRows - start));
        if (!_blocks.TryGetValue(chunkIndex, out var block))
        {
            block = new Block(start, count, defaultRowHeight);
            _blocks[chunkIndex] = block;
            return block;
        }

        block.Update(defaultRowHeight, count);
        return block;
    }

    private void EnsureBlockSamples(Block block, int totalRows)
    {
        if (_samplesPerBlock <= 0 || _source is null || block.Count == 0)
        {
            return;
        }

        block.EnsureSamples(_samplesPerBlock, localIndex =>
        {
            var globalIndex = block.StartIndex + localIndex;
            if (globalIndex < 0 || globalIndex >= totalRows)
            {
                return block.DefaultHeight;
            }

            return GetMeasuredHeight(block, localIndex, globalIndex, _lastDefaultRowHeight);
        });
    }

    private double GetOrMeasure(Block block, int localIndex, int globalIndex, double defaultRowHeight)
    {
        if (block.TryGetMeasured(localIndex, out var measured))
        {
            return measured;
        }

        return GetMeasuredHeight(block, localIndex, globalIndex, defaultRowHeight);
    }

    private double GetMeasuredHeight(Block block, int localIndex, int globalIndex, double defaultRowHeight)
    {
        if (_source is null || globalIndex < 0 || globalIndex >= _source.RowCount)
        {
            return defaultRowHeight;
        }

        var row = _source.GetRow(globalIndex);
        var height = Math.Max(1d, _heightProvider.GetRowHeight(row, globalIndex, defaultRowHeight));
        block.SetMeasuredHeight(localIndex, height);
        _lastEstimatedTotalHeight = 0;
        return height;
    }

    private sealed class Block
    {
        private readonly SortedDictionary<int, double> _measured = new();
        private bool _samplesTaken;

        public Block(int startIndex, int count, double defaultHeight)
        {
            StartIndex = startIndex;
            Count = Math.Max(0, count);
            DefaultHeight = Math.Max(1d, defaultHeight);
            TotalHeight = DefaultHeight * Count;
        }

        public int StartIndex { get; }

        public int Count { get; private set; }

        public double DefaultHeight { get; private set; }

        public double TotalHeight { get; private set; }

        public void Update(double defaultHeight, int count)
        {
            var previousCount = Count;
            var sanitizedDefault = Math.Max(1d, defaultHeight);
            if (count < previousCount)
            {
                List<int>? toRemove = null;
                foreach (var kv in _measured)
                {
                    if (kv.Key < count)
                    {
                        continue;
                    }

                    toRemove ??= new List<int>();
                    toRemove.Add(kv.Key);
                }

                if (toRemove is not null)
                {
                    foreach (var key in toRemove)
                    {
                        var value = _measured[key];
                        TotalHeight += DefaultHeight - value;
                        _measured.Remove(key);
                    }
                }

                var removed = previousCount - count;
                if (removed > 0)
                {
                    TotalHeight -= removed * DefaultHeight;
                }
            }
            else if (count > previousCount)
            {
                TotalHeight += (count - previousCount) * DefaultHeight;
            }

            Count = Math.Max(0, count);
            ApplyNewDefault(sanitizedDefault);

            if (Count <= 0 || previousCount != Count)
            {
                _samplesTaken = false;
            }
        }

        public bool TryGetMeasured(int localIndex, out double height)
        {
            return _measured.TryGetValue(localIndex, out height);
        }

        public void SetMeasuredHeight(int localIndex, double height)
        {
            if (localIndex < 0 || localIndex >= Count)
            {
                return;
            }

            var previous = _measured.TryGetValue(localIndex, out var existing)
                ? existing
                : DefaultHeight;

            _measured[localIndex] = height;

            if (Math.Abs(height - previous) > 0.001)
            {
                TotalHeight += height - previous;
            }
        }

        public double SumBefore(int localIndex)
        {
            if (localIndex <= 0)
            {
                return 0;
            }

            var limited = Math.Min(localIndex, Count);
            var sum = DefaultHeight * limited;
            foreach (var kv in _measured)
            {
                if (kv.Key >= limited)
                {
                    break;
                }

                sum += kv.Value - DefaultHeight;
            }

            return sum;
        }

        public void EnsureSamples(int sampleCount, Func<int, double> sampler)
        {
            if (_samplesTaken || Count <= 0 || sampleCount <= 0)
            {
                return;
            }

            foreach (var localIndex in BuildSampleIndices(sampleCount))
            {
                if (localIndex < 0 || localIndex >= Count)
                {
                    continue;
                }

                if (_measured.ContainsKey(localIndex))
                {
                    continue;
                }

                var height = sampler(localIndex);
                SetMeasuredHeight(localIndex, height);
            }

            _samplesTaken = true;
        }

        public bool RemoveMeasurement(int localIndex)
        {
            if (!_measured.TryGetValue(localIndex, out var existing))
            {
                return false;
            }

            _measured.Remove(localIndex);
            TotalHeight += DefaultHeight - existing;
            _samplesTaken = false;
            return true;
        }

        private IEnumerable<int> BuildSampleIndices(int sampleCount)
        {
            if (Count <= sampleCount)
            {
                for (var i = 0; i < Count; i++)
                {
                    yield return i;
                }

                yield break;
            }

            yield return 0;
            if (sampleCount == 1)
            {
                yield break;
            }

            if (sampleCount == 2)
            {
                yield return Count - 1;
                yield break;
            }

            var step = (Count - 1) / (double)(sampleCount - 1);
            for (var i = 1; i < sampleCount - 1; i++)
            {
                var index = (int)Math.Round(i * step);
                if (index <= 0 || index >= Count - 1)
                {
                    continue;
                }

                yield return index;
            }

            yield return Count - 1;
        }

        private void ApplyNewDefault(double newDefault)
        {
            if (Math.Abs(newDefault - DefaultHeight) < 0.001)
            {
                return;
            }

            var unmeasured = Count - _measured.Count;
            if (unmeasured < 0)
            {
                unmeasured = 0;
            }

            TotalHeight += (newDefault - DefaultHeight) * unmeasured;
            DefaultHeight = newDefault;
            _samplesTaken = false;
        }
    }
}
