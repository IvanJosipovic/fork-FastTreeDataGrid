using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

internal sealed class CryptoGroupNode : CryptoNode
{
    private readonly List<CryptoNode> _children = new();

    public CryptoGroupNode(string quoteAsset)
    {
        QuoteAsset = quoteAsset;
    }

    public string QuoteAsset { get; }

    public override bool IsGroup => true;

    public override IReadOnlyList<CryptoNode> Children => _children;

    public override object? GetValue(object? item, string key)
    {
        return key switch
        {
            CryptoTickerNode.KeySymbol => $"{QuoteAsset} Markets",
            CryptoTickerNode.KeyQuote => QuoteAsset,
            _ => string.Empty,
        };
    }

    public void AddChild(CryptoTickerNode node)
    {
        _children.Add(node);
        NotifyValueChanged();
    }
}
