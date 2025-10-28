using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

public interface IFastTreeDataGridValueProvider
{
    event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    object? GetValue(object? item, string key);
}
