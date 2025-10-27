namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataVirtualizationPage
{
    public FastTreeDataVirtualizationPage(int startIndex, int count)
    {
        StartIndex = startIndex;
        Count = count;
    }

    public int StartIndex { get; }

    public int Count { get; }
}
