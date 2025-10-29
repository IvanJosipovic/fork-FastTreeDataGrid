using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Represents a lightweight, UI-agnostic description of a logical column.
/// </summary>
public sealed class FastTreeDataGridColumnDescriptor
{
    private static readonly IReadOnlyDictionary<string, object?> s_emptyProperties = new Dictionary<string, object?>();

    public FastTreeDataGridColumnDescriptor(
        string key,
        string? header = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        object? payload = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Header = header;
        Properties = properties ?? s_emptyProperties;
        Payload = payload;
    }

    /// <summary>
    /// Gets a unique identifier for the column across virtualization refreshes.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets an optional, display-friendly header.
    /// </summary>
    public string? Header { get; }

    /// <summary>
    /// Gets an arbitrary bag of metadata required by higher layers (value keys, sizing hints, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets an optional provider-defined payload that can carry additional state.
    /// </summary>
    public object? Payload { get; }
}
