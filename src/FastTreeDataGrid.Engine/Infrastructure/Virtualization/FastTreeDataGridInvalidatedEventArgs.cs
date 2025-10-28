using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridInvalidatedEventArgs : EventArgs
{
    public FastTreeDataGridInvalidatedEventArgs(FastTreeDataGridInvalidationRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public FastTreeDataGridInvalidationRequest Request { get; }
}
