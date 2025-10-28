using System.Collections.Generic;
using System.Globalization;

namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Default grouping adapter that extracts values using the column key.
/// </summary>
public sealed class FastTreeDataGridValueGroupAdapter : IFastTreeDataGridGroupAdapter
{
    private readonly string? _columnKey;
    private readonly IComparer<object?>? _comparer;

    public FastTreeDataGridValueGroupAdapter(string? columnKey, IComparer<object?>? comparer = null)
    {
        _columnKey = columnKey;
        _comparer = comparer;
    }

    public IComparer<object?>? Comparer => _comparer;

    public object? GetGroupKey(FastTreeDataGridRow row)
    {
        if (!string.IsNullOrEmpty(_columnKey) && row.ValueProvider is { } provider)
        {
            return provider.GetValue(row.Item, _columnKey);
        }

        return row.Item;
    }

    public string GetGroupLabel(object? key, int level, int itemCount)
    {
        _ = level;
        var keyText = key switch
        {
            null => "(None)",
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
            _ => key?.ToString() ?? string.Empty,
        };

        return string.Format(CultureInfo.CurrentCulture, "{0} ({1:N0})", keyText, itemCount);
    }
}
