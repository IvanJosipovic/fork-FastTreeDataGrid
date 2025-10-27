using System;
using System.Collections.Generic;
using System.Globalization;

namespace FastTreeDataGrid.Control.Infrastructure;

internal sealed class FastTreeDataGridGeneratedGroupRow : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly string _headerKey;
    private readonly string _headerText;

    public FastTreeDataGridGeneratedGroupRow(
        string? columnKey,
        string headerText,
        object? key,
        int itemCount,
        int level)
    {
        Key = key;
        ItemCount = itemCount;
        Level = level;
        _headerKey = columnKey ?? string.Empty;
        _headerText = headerText;

        if (!string.IsNullOrEmpty(_headerKey))
        {
            _values[_headerKey] = headerText;
        }
        else
        {
            _values[string.Empty] = headerText;
        }
    }

    public object? Key { get; }

    public int ItemCount { get; }

    public int Level { get; }

    public bool IsGroup => true;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public object? GetValue(object? item, string key)
    {
        if (_values.TryGetValue(key ?? string.Empty, out var value))
        {
            return value;
        }

        if (key == "FastTreeDataGrid.Group.Header")
        {
            return _headerText;
        }

        return string.Empty;
    }

    public bool ContainsQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return _headerText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    public void NotifyValueChanged(string? key = null)
    {
        var handler = ValueInvalidated;
        if (handler is null)
        {
            return;
        }

        handler(this, new ValueInvalidatedEventArgs(this, key));
    }
}

internal sealed class FastTreeDataGridGeneratedSummaryRow : IFastTreeDataGridValueProvider, IFastTreeDataGridSummaryRow
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly string _labelKey;

    public FastTreeDataGridGeneratedSummaryRow(int level, string? labelKey, string? label)
    {
        Level = level;
        _labelKey = labelKey ?? string.Empty;
        if (!string.IsNullOrEmpty(label))
        {
            _values[_labelKey] = label;
        }
    }

    public int Level { get; }

    public bool IsSummary => true;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public void SetValue(string? columnKey, object? value, Func<object?, string?>? formatter)
    {
        var key = columnKey ?? string.Empty;
        var formatted = formatter is null
            ? value switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
                _ => value.ToString() ?? string.Empty,
            }
            : formatter(value) ?? string.Empty;

        _values[key] = formatted;
    }

    public object? GetValue(object? item, string key)
    {
        if (_values.TryGetValue(key ?? string.Empty, out var value))
        {
            return value;
        }

        if (string.IsNullOrEmpty(key) && _values.TryGetValue(string.Empty, out value))
        {
            return value;
        }

        return string.Empty;
    }

    public bool ContainsQuery(string query) => string.IsNullOrWhiteSpace(query);

    public void NotifyValueChanged(string? key = null)
    {
        var handler = ValueInvalidated;
        if (handler is null)
        {
            return;
        }

        handler(this, new ValueInvalidatedEventArgs(this, key));
    }
}

