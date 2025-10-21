using System;
using System.Collections.Generic;

using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridAdaptiveRowLayout : IFastTreeDataGridRowLayout
{
    private readonly IFastTreeDataGridVariableRowHeightProvider _heightProvider;
    private readonly Dictionary<int, Chunk> _chunks = new();
    private ControlsFastTreeDataGrid? _owner;
    private IFastTreeDataGridSource? _source;
    private double _lastDefaultRowHeight = 28d;
    private int _latestTotalRows;
    private double _lastEstimatedTotalHeight;
    private int _chunkSize = 256;

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

            if (_chunkSize == value)
            {
                return;
            }

            _chunkSize = value;
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
        _chunks.Clear();
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

        var estimatedTotal = _lastEstimatedTotalHeight > 0
            ? _lastEstimatedTotalHeight
            : EstimateTotalHeight(totalRows, _lastDefaultRowHeight);

        var clampedOffset = Math.Clamp(verticalOffset, 0, Math.Max(0, estimatedTotal - _lastDefaultRowHeight));

        var chunkCount = GetChunkCount(totalRows);
        if (chunkCount == 0)
        {
            return RowLayoutViewport.Empty;
        }

        var accumulated = 0d;
        var chunkIndex = 0;
        var chunkBaseTop = 0d;

        while (chunkIndex < chunkCount)
        {
            var chunkHeight = GetChunkEstimatedHeight(chunkIndex, totalRows, _lastDefaultRowHeight);
            if (accumulated + chunkHeight > clampedOffset)
            {
                chunkBaseTop = accumulated;
                break;
            }

            accumulated += chunkHeight;
            chunkIndex++;
        }

        if (chunkIndex >= chunkCount)
        {
            var lastRowIndex = totalRows - 1;
            var lastTop = Math.Max(0, estimatedTotal - _lastDefaultRowHeight);
            var lastExclusive = Math.Min(totalRows, lastRowIndex + 1 + buffer);
            return new RowLayoutViewport(lastRowIndex, lastExclusive, lastTop);
        }

        var chunkStart = chunkIndex * _chunkSize;
        var chunkEnd = Math.Min(chunkStart + _chunkSize, totalRows);
        var firstIndex = chunkStart;
        var firstRowTop = chunkBaseTop;
        var currentTop = chunkBaseTop;

        for (var rowIndex = chunkStart; rowIndex < chunkEnd; rowIndex++)
        {
            var row = _source.GetRow(rowIndex);
            var height = MeasureRow(rowIndex, row, _lastDefaultRowHeight);
            if (currentTop + height > clampedOffset)
            {
                firstIndex = rowIndex;
                firstRowTop = currentTop;
                break;
            }

            currentTop += height;
            firstIndex = rowIndex + 1;
            firstRowTop = currentTop;
        }

        if (firstIndex >= totalRows)
        {
            firstIndex = totalRows - 1;
            firstRowTop = Math.Max(0, estimatedTotal - MeasureRow(firstIndex, _source.GetRow(firstIndex), _lastDefaultRowHeight));
        }

        var lastIndexExclusive = firstIndex;
        var bottomTarget = clampedOffset + viewportHeight;
        currentTop = firstRowTop;

        while (lastIndexExclusive < totalRows && currentTop < bottomTarget)
        {
            var row = _source.GetRow(lastIndexExclusive);
            var height = MeasureRow(lastIndexExclusive, row, _lastDefaultRowHeight);
            currentTop += height;
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

        var totalRows = _source.RowCount;
        _latestTotalRows = Math.Max(totalRows, _latestTotalRows);
        _lastDefaultRowHeight = Math.Max(1d, defaultRowHeight);

        return MeasureRow(rowIndex, row, _lastDefaultRowHeight);
    }

    public double GetRowTop(int rowIndex)
    {
        if (rowIndex <= 0)
        {
            return 0;
        }

        var totalRows = _latestTotalRows > 0 ? _latestTotalRows : _source?.RowCount ?? 0;
        if (totalRows <= 0)
        {
            return rowIndex * _lastDefaultRowHeight;
        }

        var defaultRowHeight = _lastDefaultRowHeight > 0 ? _lastDefaultRowHeight : 28d;
        var chunkIndex = _chunkSize <= 0 ? 0 : rowIndex / _chunkSize;
        var top = 0d;

        for (var i = 0; i < chunkIndex; i++)
        {
            top += GetChunkEstimatedHeight(i, totalRows, defaultRowHeight);
        }

        var chunkStart = chunkIndex * _chunkSize;
        var chunkCount = Math.Max(0, Math.Min(_chunkSize, totalRows - chunkStart));
        var withinChunk = rowIndex - chunkStart;
        withinChunk = Math.Min(Math.Max(0, withinChunk), Math.Max(0, chunkCount));

        if (_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk.DefaultAverage = defaultRowHeight;
            chunk.UpdateCount(chunkCount);
            for (var i = 0; i < withinChunk; i++)
            {
                if (chunk.TryGetMeasured(i, out var height))
                {
                    top += height;
                }
                else
                {
                    top += chunk.EstimatedAverage;
                }
            }

            return top;
        }

        top += withinChunk * defaultRowHeight;
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

        var chunkCount = GetChunkCount(totalRows);
        double total = 0;

        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            total += GetChunkEstimatedHeight(chunkIndex, totalRows, _lastDefaultRowHeight);
        }

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
        if (!_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            return;
        }

        var localIndex = rowIndex - chunk.StartIndex;
        if (chunk.RemoveMeasurement(localIndex))
        {
            if (chunk.IsEmpty)
            {
                _chunks.Remove(chunkIndex);
            }
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

    private double EstimateTotalHeight(int totalRows, double defaultRowHeight)
    {
        var chunkCount = GetChunkCount(totalRows);
        double total = 0;
        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            total += GetChunkEstimatedHeight(chunkIndex, totalRows, defaultRowHeight);
        }

        return total;
    }

    private double GetChunkEstimatedHeight(int chunkIndex, int totalRows, double defaultRowHeight)
    {
        var start = chunkIndex * _chunkSize;
        var count = Math.Max(0, Math.Min(_chunkSize, totalRows - start));
        if (count <= 0)
        {
            return 0;
        }

        if (_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk.DefaultAverage = defaultRowHeight;
            chunk.UpdateCount(count);
            return chunk.EstimatedHeight;
        }

        return count * defaultRowHeight;
    }

    private double MeasureRow(int rowIndex, FastTreeDataGridRow row, double defaultRowHeight)
    {
        if (_source is null)
        {
            return Math.Max(1d, defaultRowHeight);
        }

        var totalRows = _latestTotalRows > 0 ? _latestTotalRows : _source.RowCount;
        var chunkIndex = _chunkSize <= 0 ? 0 : rowIndex / _chunkSize;
        var chunk = GetChunkForWrite(chunkIndex, defaultRowHeight, totalRows);
        var localIndex = rowIndex - chunk.StartIndex;

        if (localIndex >= chunk.Count)
        {
            chunk.UpdateCount(localIndex + 1);
        }

        if (chunk.TryGetMeasured(localIndex, out var cached))
        {
            return cached;
        }

        var height = Math.Max(1d, _heightProvider.GetRowHeight(row, rowIndex, defaultRowHeight));
        chunk.RecordMeasurement(localIndex, height);
        return height;
    }

    private Chunk GetChunkForWrite(int chunkIndex, double defaultRowHeight, int totalRows)
    {
        var start = chunkIndex * _chunkSize;
        var count = Math.Max(0, Math.Min(_chunkSize, totalRows - start));

        if (!_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk = new Chunk(start, count, defaultRowHeight);
            _chunks[chunkIndex] = chunk;
        }
        else
        {
            chunk.DefaultAverage = defaultRowHeight;
            chunk.UpdateCount(count);
        }

        return chunk;
    }

    private sealed class Chunk
    {
        private readonly Dictionary<int, double> _measured = new();

        public Chunk(int startIndex, int count, double defaultAverage)
        {
            StartIndex = startIndex;
            Count = count;
            _defaultAverage = Math.Max(1d, defaultAverage);
            EstimatedAverage = _defaultAverage;
        }

        public int StartIndex { get; }

        public int Count { get; private set; }

        public double DefaultAverage
        {
            get => _defaultAverage;
            set
            {
                var sanitized = Math.Max(1d, value);
                if (Math.Abs(_defaultAverage - sanitized) < 0.001)
                {
                    return;
                }

                _defaultAverage = sanitized;
                if (_measured.Count == 0)
                {
                    EstimatedAverage = _defaultAverage;
                }
            }
        }

        public double EstimatedAverage { get; private set; }

        public double MeasuredTotal { get; private set; }

        public bool IsEmpty => Count <= 0 || _measured.Count == 0;

        public double EstimatedHeight
        {
            get
            {
                if (Count <= 0)
                {
                    return 0;
                }

                if (_measured.Count >= Count)
                {
                    return MeasuredTotal;
                }

                var remaining = Count - _measured.Count;
                return MeasuredTotal + Math.Max(0, remaining) * EstimatedAverage;
            }
        }

        public void UpdateCount(int count)
        {
            if (count == Count)
            {
                return;
            }

            Count = count;

            if (_measured.Count == 0)
            {
                if (Count <= 0)
                {
                    MeasuredTotal = 0;
                }

                UpdateAverage();
                return;
            }

            if (Count <= 0)
            {
                _measured.Clear();
                MeasuredTotal = 0;
                UpdateAverage();
                return;
            }

            List<int>? removedKeys = null;
            foreach (var kvp in _measured)
            {
                if (kvp.Key >= Count)
                {
                    removedKeys ??= new List<int>();
                    removedKeys.Add(kvp.Key);
                }
            }

            if (removedKeys is not null)
            {
                var removedTotal = 0d;
                foreach (var key in removedKeys)
                {
                    removedTotal += _measured[key];
                    _measured.Remove(key);
                }

                MeasuredTotal -= removedTotal;
                if (MeasuredTotal < 0)
                {
                    MeasuredTotal = 0;
                }
            }

            UpdateAverage();
        }

        public bool TryGetMeasured(int localIndex, out double height)
        {
            return _measured.TryGetValue(localIndex, out height);
        }

        public void RecordMeasurement(int localIndex, double height)
        {
            if (_measured.TryGetValue(localIndex, out var existing))
            {
                if (Math.Abs(existing - height) < 0.001)
                {
                    return;
                }

                MeasuredTotal += height - existing;
                _measured[localIndex] = height;
            }
            else
            {
                _measured[localIndex] = height;
                MeasuredTotal += height;
            }

            UpdateAverage();
        }

        public bool RemoveMeasurement(int localIndex)
        {
            if (!_measured.TryGetValue(localIndex, out var existing))
            {
                return false;
            }

            _measured.Remove(localIndex);
            MeasuredTotal -= existing;
            if (MeasuredTotal < 0)
            {
                MeasuredTotal = 0;
            }

            UpdateAverage();
            return true;
        }

        private void UpdateAverage()
        {
            if (_measured.Count > 0)
            {
                EstimatedAverage = Math.Max(1d, MeasuredTotal / _measured.Count);
            }
            else
            {
                EstimatedAverage = Math.Max(1d, _defaultAverage);
            }
        }

        private double _defaultAverage;
    }
}
