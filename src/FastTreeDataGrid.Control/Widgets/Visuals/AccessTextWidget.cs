using System;
using System.Globalization;
using System.Text;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Displays text with optional access key parsing similar to Avalonia's AccessText.
/// </summary>
public sealed class AccessTextWidget : FormattedTextWidget
{
    private string _rawText = string.Empty;
    private bool _showAccessKey = true;

    public char? AccessKey { get; private set; }

    public int AccessKeyIndex { get; private set; } = -1;

    public bool ShowAccessKey
    {
        get => _showAccessKey;
        set
        {
            if (_showAccessKey == value)
            {
                return;
            }

            _showAccessKey = value;
            SetAccessText(_rawText);
        }
    }

    public string AccessText
    {
        get => _rawText;
        set => SetAccessText(value);
    }

    public new void SetText(string? text) => SetAccessText(text);

    public void SetAccessText(string? text)
    {
        _rawText = text ?? string.Empty;
        var parsed = ParseAccessText(_rawText, out var accessKey, out var accessKeyIndex);

        AccessKey = accessKey;
        AccessKeyIndex = accessKeyIndex;

        base.SetText(parsed);
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        string resolved;

        if (provider is null || Key is null)
        {
            resolved = item?.ToString() ?? string.Empty;
        }
        else
        {
            var value = provider.GetValue(item, Key);
            resolved = value switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        SetAccessText(resolved);
    }

    internal static string ParseAccessText(string text, out char? accessKey, out int accessKeyIndex)
    {
        accessKey = null;
        accessKeyIndex = -1;

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (current == '_' && i + 1 < text.Length)
            {
                var next = text[i + 1];
                if (next == '_')
                {
                    builder.Append('_');
                    i++;
                    continue;
                }

                if (accessKeyIndex == -1 && !char.IsWhiteSpace(next))
                {
                    accessKeyIndex = builder.Length;
                    accessKey = char.ToUpperInvariant(next);
                }

                builder.Append(next);
                i++;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
