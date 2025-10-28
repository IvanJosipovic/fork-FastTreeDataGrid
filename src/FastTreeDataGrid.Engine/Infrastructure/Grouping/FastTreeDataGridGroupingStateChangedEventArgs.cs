using System;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Provides details about grouping state changes.
/// </summary>
public sealed class FastTreeDataGridGroupingStateChangedEventArgs : EventArgs
{
    public FastTreeDataGridGroupingStateChangedEventArgs(
        FastTreeDataGridGroupingChangeKind kind,
        string? path = null)
    {
        Kind = kind;
        Path = path;
    }

    public FastTreeDataGridGroupingChangeKind Kind { get; }

    public string? Path { get; }
}
