using System;
using System.Collections.Generic;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

internal abstract class CryptoNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public abstract bool IsGroup { get; }

    public abstract IReadOnlyList<CryptoNode> Children { get; }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public abstract object? GetValue(object? item, string key);

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }
}
