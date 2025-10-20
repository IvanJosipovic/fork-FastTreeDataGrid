using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

public sealed class CryptoTickersViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly string[] QuotePriority = { "USDT", "USDC", "BUSD", "BTC", "ETH", "BNB" };
    private static readonly string[] QuoteCandidates =
    {
        "USDT", "USDC", "BUSD", "BTC", "ETH", "BNB", "EUR", "TRY", "GBP", "AUD", "DAI", "RUB", "ZAR", "JPY"
    };
    private const int MaxTickersPerGroup = 15;

    private readonly List<CryptoNode> _rootNodes = new();
    private readonly Dictionary<string, CryptoGroupNode> _groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CryptoTickerNode> _tickers = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    private IFastTreeDataGridSource _cryptoSource;
    private FastTreeDataGridFlatSource<CryptoNode>? _flatSource;

    public CryptoTickersViewModel()
    {
        _cryptoSource = new FastTreeDataGridFlatSource<CryptoNode>(_rootNodes, node => node.Children);
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, async (_, _) => await RefreshAsync());
        _ = InitializeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource CryptoSource
    {
        get => _cryptoSource;
        private set
        {
            if (!ReferenceEquals(_cryptoSource, value))
            {
                _cryptoSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CryptoSource)));
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer.Stop();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var records = await CryptoTickerService.GetTickersAsync(_cts.Token);
            await Dispatcher.UIThread.InvokeAsync(() => BuildInitialNodes(records));
            _timer.Start();
        }
        catch
        {
            // Swallow for demo purposes
        }
    }

    private async Task RefreshAsync()
    {
        if (_tickers.Count == 0)
        {
            return;
        }

        try
        {
            var records = await CryptoTickerService.GetTickersAsync(_cts.Token);
            await Dispatcher.UIThread.InvokeAsync(() => UpdateTickers(records));
        }
        catch
        {
            // ignore transient errors
        }
    }

    private void BuildInitialNodes(IReadOnlyList<CryptoTickerRecord> records)
    {
        _rootNodes.Clear();
        _groups.Clear();
        _tickers.Clear();

        foreach (var (quote, tickers) in GroupTickers(records))
        {
            var groupNode = new CryptoGroupNode(quote);
            _groups[quote] = groupNode;
            _rootNodes.Add(groupNode);

            foreach (var tickerRecord in tickers)
            {
                var leaf = CreateTickerNode(tickerRecord, quote);
                _tickers[tickerRecord.Symbol] = leaf;
                groupNode.AddChild(leaf);
            }
        }

        _flatSource = new FastTreeDataGridFlatSource<CryptoNode>(_rootNodes, node => node.Children);
        CryptoSource = _flatSource;
        ExpandAllGroups();
    }

    private IEnumerable<(string Quote, List<CryptoTickerRecord> Records)> GroupTickers(IReadOnlyList<CryptoTickerRecord> records)
    {
        return records
            .Select(r => (Record: r, Quote: DetermineQuoteAsset(r.Symbol)))
            .Where(tuple => !string.IsNullOrEmpty(tuple.Quote))
            .GroupBy(tuple => tuple.Quote!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => GetQuotePriority(g.Key))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (
                Quote: g.Key,
                Records: g.OrderByDescending(tuple => ParseDecimal(tuple.Record.QuoteVolume))
                    .Take(MaxTickersPerGroup)
                    .Select(tuple => tuple.Record)
                    .ToList()));
    }

    private void UpdateTickers(IReadOnlyList<CryptoTickerRecord> records)
    {
        foreach (var record in records)
        {
            if (!_tickers.TryGetValue(record.Symbol, out var node))
            {
                continue;
            }

            var lastPrice = ParseDecimal(record.LastPrice);
            var changePercent = ParseDecimal(record.PriceChangePercent) / 100m;
            var volume = ParseDecimal(record.QuoteVolume);

            node.Update(lastPrice, changePercent, volume);
        }
    }

    private CryptoTickerNode CreateTickerNode(CryptoTickerRecord record, string quoteAsset)
    {
        var node = new CryptoTickerNode(record.Symbol, quoteAsset);
        var lastPrice = ParseDecimal(record.LastPrice);
        var changePercent = ParseDecimal(record.PriceChangePercent) / 100m;
        var volume = ParseDecimal(record.QuoteVolume);
        node.Update(lastPrice, changePercent, volume);
        return node;
    }

    private void ExpandAllGroups()
    {
        if (_flatSource is null)
        {
            return;
        }

        var index = 0;
        foreach (var group in _rootNodes)
        {
            if (group.Children.Count == 0)
            {
                index++;
                continue;
            }

            _flatSource.ToggleExpansion(index);
            index += group.Children.Count + 1;
        }
    }

    private static string? DetermineQuoteAsset(string symbol)
    {
        foreach (var quote in QuoteCandidates.OrderByDescending(q => q.Length))
        {
            if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase))
            {
                return quote;
            }
        }

        return null;
    }

    private static int GetQuotePriority(string quote)
    {
        var index = Array.IndexOf(QuotePriority, quote);
        return index >= 0 ? index : QuotePriority.Length;
    }

    private static decimal ParseDecimal(string text)
    {
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return 0m;
    }
}
