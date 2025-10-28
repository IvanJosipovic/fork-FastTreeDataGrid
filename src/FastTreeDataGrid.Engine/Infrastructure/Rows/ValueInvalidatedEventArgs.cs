using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public class ValueInvalidatedEventArgs : EventArgs
{
    public ValueInvalidatedEventArgs(object? item, string? key)
    {
        Item = item;
        Key = key;
    }

    public object? Item { get; }

    public string? Key { get; }
}
