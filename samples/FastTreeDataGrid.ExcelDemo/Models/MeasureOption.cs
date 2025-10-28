using System;
using System.Globalization;

namespace FastTreeDataGrid.ExcelDemo.Models;

public sealed class MeasureOption
{
    public MeasureOption(string key, string displayName, Func<SalesRecord, double> valueSelector, Func<double, string> formatter, string description)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ValueSelector = valueSelector ?? throw new ArgumentNullException(nameof(valueSelector));
        Formatter = formatter ?? DefaultFormatter;
        Description = description ?? string.Empty;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public Func<SalesRecord, double> ValueSelector { get; }

    public Func<double, string> Formatter { get; }

    public string Format(double value) => Formatter(value);

    private static string DefaultFormatter(double value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
