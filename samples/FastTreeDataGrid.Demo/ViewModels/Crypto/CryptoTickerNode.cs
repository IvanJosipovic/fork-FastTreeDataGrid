using System;
using System.Collections.Generic;
using System.Globalization;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

internal sealed class CryptoTickerNode : CryptoNode
{
    public const string KeySymbol = "Crypto.Symbol";
    public const string KeyQuote = "Crypto.Quote";
    public const string KeyPrice = "Crypto.Price";
    public const string KeyChange = "Crypto.Change";
    public const string KeyVolume = "Crypto.Volume";

    private readonly CultureInfo _formatCulture = CultureInfo.CurrentCulture;

    private decimal _lastPrice;
    private decimal _changePercent;
    private decimal _quoteVolume;

    public CryptoTickerNode(string symbol, string quoteAsset)
    {
        Symbol = symbol;
        QuoteAsset = quoteAsset;
    }

    public string Symbol { get; }

    public string QuoteAsset { get; }

    public override bool IsGroup => false;

    public override IReadOnlyList<CryptoNode> Children => Array.Empty<CryptoNode>();

    public override object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeySymbol => Symbol,
            KeyQuote => QuoteAsset,
            KeyPrice => _lastPrice.ToString("0.0000", _formatCulture),
            KeyChange => _changePercent.ToString("0.00%", _formatCulture),
            KeyVolume => _quoteVolume.ToString("N0", _formatCulture),
            _ => string.Empty,
        };
    }

    public void Update(decimal lastPrice, decimal changePercent, decimal volume)
    {
        if (_lastPrice != lastPrice)
        {
            _lastPrice = lastPrice;
            NotifyValueChanged(KeyPrice);
        }

        if (_changePercent != changePercent)
        {
            _changePercent = changePercent;
            NotifyValueChanged(KeyChange);
        }

        if (_quoteVolume != volume)
        {
            _quoteVolume = volume;
            NotifyValueChanged(KeyVolume);
        }
    }
}
