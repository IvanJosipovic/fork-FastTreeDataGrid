using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Represents the outcome of an aggregate calculation.
/// </summary>
public sealed class FastTreeDataGridAggregateResult
{
    public FastTreeDataGridAggregateResult(string? columnKey, object? value, string? formatted)
    {
        ColumnKey = columnKey;
        Value = value;
        FormattedText = formatted;
    }

    /// <summary>
    /// Gets the column key associated with this aggregate. <c>null</c> reserves the hierarchy column.
    /// </summary>
    public string? ColumnKey { get; }

    /// <summary>
    /// Gets the raw value produced by the aggregate provider.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the formatted text representation.
    /// </summary>
    public string? FormattedText { get; }

    /// <summary>
    /// Gets additional metadata to inform rendering.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();
}
