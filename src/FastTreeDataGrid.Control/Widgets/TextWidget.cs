using System;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class TextWidget : Widget
{
    public string? Text { get; protected set; }

    public double EmSize { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (provider is null || Key is null)
        {
            Text = item?.ToString() ?? string.Empty;
            return;
        }

        var value = provider.GetValue(item, Key);

        Text = value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    public abstract void Invalidate();
}
