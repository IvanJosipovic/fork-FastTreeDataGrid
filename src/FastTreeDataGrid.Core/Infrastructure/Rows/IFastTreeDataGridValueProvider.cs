using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridValueProvider
{
    event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    object? GetValue(object? item, string key);
}
