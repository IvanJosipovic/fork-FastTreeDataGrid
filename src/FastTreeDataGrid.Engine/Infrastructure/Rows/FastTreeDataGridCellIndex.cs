using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public readonly struct FastTreeDataGridCellIndex : IEquatable<FastTreeDataGridCellIndex>, IComparable<FastTreeDataGridCellIndex>
{
    public static FastTreeDataGridCellIndex Invalid { get; } = new(-1, -1);

    public FastTreeDataGridCellIndex(int rowIndex, int columnIndex)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
    }

    public int RowIndex { get; }

    public int ColumnIndex { get; }

    public bool IsValid => RowIndex >= 0 && ColumnIndex >= 0;

    public bool Equals(FastTreeDataGridCellIndex other) =>
        RowIndex == other.RowIndex && ColumnIndex == other.ColumnIndex;

    public override bool Equals(object? obj) =>
        obj is FastTreeDataGridCellIndex other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(RowIndex, ColumnIndex);

    public int CompareTo(FastTreeDataGridCellIndex other)
    {
        var rowCompare = RowIndex.CompareTo(other.RowIndex);
        if (rowCompare != 0)
        {
            return rowCompare;
        }

        return ColumnIndex.CompareTo(other.ColumnIndex);
    }

    public static bool operator ==(FastTreeDataGridCellIndex left, FastTreeDataGridCellIndex right) => left.Equals(right);

    public static bool operator !=(FastTreeDataGridCellIndex left, FastTreeDataGridCellIndex right) => !left.Equals(right);
}
