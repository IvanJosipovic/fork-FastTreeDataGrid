using System;
using System.Globalization;

namespace FastTreeDataGrid.ExcelDemo.Models;

public sealed class FormulaDefinition
{
    public FormulaDefinition(string key, string displayName, string expression, Func<double, string> formatter, string description)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Formatter = formatter ?? DefaultFormatter;
        Description = description ?? string.Empty;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Expression { get; }

    public string Description { get; }

    public Func<double, string> Formatter { get; }

    public string Format(double? value)
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        return Formatter(value.Value);
    }

    private static string DefaultFormatter(double value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
