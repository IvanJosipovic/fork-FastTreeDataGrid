namespace FastTreeDataGrid.Control.Infrastructure;

using ControlsFastTreeDataGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

public interface IFastTreeDataGridRowLayout
{
    void Attach(ControlsFastTreeDataGrid owner);

    void Detach();

    void Bind(IFastTreeDataGridSource? source);

    void Reset();

    RowLayoutViewport GetVisibleRange(
        double verticalOffset,
        double viewportHeight,
        double defaultRowHeight,
        int totalRows,
        int buffer);

    double GetRowHeight(int rowIndex, FastTreeDataGridRow row, double defaultRowHeight);

    double GetRowTop(int rowIndex);

    double GetTotalHeight(double viewportHeight, double defaultRowHeight, int totalRows);

    void InvalidateRow(int rowIndex);
}

public readonly struct RowLayoutViewport
{
    public RowLayoutViewport(int firstIndex, int lastIndexExclusive, double firstRowTop)
    {
        FirstIndex = firstIndex;
        LastIndexExclusive = lastIndexExclusive;
        FirstRowTop = firstRowTop;
    }

    public int FirstIndex { get; }

    public int LastIndexExclusive { get; }

    public double FirstRowTop { get; }

    public bool IsEmpty => LastIndexExclusive <= FirstIndex;

    public static RowLayoutViewport Empty => default;
}
