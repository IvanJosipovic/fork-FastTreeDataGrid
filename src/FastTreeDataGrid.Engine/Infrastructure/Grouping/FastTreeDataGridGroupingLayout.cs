using System;
using System.Collections.Generic;
using FastTreeDataGrid.Engine.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Represents a serializable snapshot of grouping descriptors and expansion state.
/// </summary>
public sealed class FastTreeDataGridGroupingLayout
{
    public int Version { get; set; } = 1;

    public List<FastTreeDataGridGroupingLayoutDescriptor> Groups { get; set; } = new();

    public List<FastTreeDataGridGroupingExpansionState> ExpansionStates { get; set; } = new();
}

/// <summary>
/// Serializable descriptor for a grouping level.
/// </summary>
public sealed class FastTreeDataGridGroupingLayoutDescriptor
{
    public string? ColumnKey { get; set; }

    public FastTreeDataGridSortDirection SortDirection { get; set; } = FastTreeDataGridSortDirection.Ascending;

    public bool IsExpanded { get; set; } = true;

    public IDictionary<string, string?> Metadata { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);
}

/// <summary>
/// Serializable expansion state for a specific grouping path.
/// </summary>
public sealed class FastTreeDataGridGroupingExpansionState
{
    public string Path { get; set; } = string.Empty;

    public bool IsExpanded { get; set; }
}

public static class FastTreeDataGridGroupingLayoutSerializer
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(FastTreeDataGridGroupingLayout layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        if (layout.Version <= 0)
        {
            layout.Version = 1;
        }

        return JsonSerializer.Serialize(layout, s_options);
    }

    public static FastTreeDataGridGroupingLayout? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var layout = JsonSerializer.Deserialize<FastTreeDataGridGroupingLayout>(json, s_options);
        if (layout is not null && layout.Version <= 0)
        {
            layout.Version = 1;
        }

        return layout;
    }
}
