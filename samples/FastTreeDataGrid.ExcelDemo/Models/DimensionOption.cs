using System;

namespace FastTreeDataGrid.ExcelDemo.Models;

public sealed class DimensionOption
{
    public DimensionOption(string key, string displayName, Func<SalesRecord, string> selector, string description)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        Description = description ?? string.Empty;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Description { get; }

    private Func<SalesRecord, string> Selector { get; }

    public string Select(SalesRecord record)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        return Selector(record);
    }
}
