using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridRowReorderingEventArgs : EventArgs
{
    public FastTreeDataGridRowReorderingEventArgs(FastTreeDataGridRowReorderRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public FastTreeDataGridRowReorderRequest Request { get; }

    public bool Cancel { get; set; }
}

public sealed class FastTreeDataGridRowReorderedEventArgs : EventArgs
{
    public FastTreeDataGridRowReorderedEventArgs(FastTreeDataGridRowReorderRequest request, FastTreeDataGridRowReorderResult result)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public FastTreeDataGridRowReorderRequest Request { get; }

    public FastTreeDataGridRowReorderResult Result { get; }
}
