using System;
using System.Globalization;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class TextWidget : Widget
{
    public string? Text { get; protected set; }

    public double EmSize { get; set; }

    public void SetText(string? text)
    {
        Text = text ?? string.Empty;
        Invalidate();
    }

    public TextTrimming Trimming { get; set; } = TextTrimming.None;

    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

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
