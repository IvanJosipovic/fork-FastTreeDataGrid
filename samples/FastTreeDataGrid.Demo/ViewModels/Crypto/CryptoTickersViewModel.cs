using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

public sealed class CryptoTickersViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly string[] QuotePriority = { "USDT", "USDC", "BUSD", "BTC", "ETH", "BNB" };
    private static readonly string[] QuoteCandidates =
    {
        "USDT", "USDC", "BUSD", "BTC", "ETH", "BNB", "EUR", "TRY", "GBP", "AUD", "DAI", "RUB", "ZAR", "JPY"
    };
    private const int MaxTickersPerGroup = 15;
    private const string AllQuotesOption = "All quotes";

    private readonly List<CryptoNode> _rootNodes = new();
    private readonly Dictionary<string, CryptoGroupNode> _groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CryptoTickerNode> _tickers = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly AvaloniaList<string> _quotes = new() { AllQuotesOption };

    private IFastTreeDataGridSource _cryptoSource;
    private FastTreeDataGridFlatSource<CryptoNode>? _flatSource;

    private string _searchText = string.Empty;
    private string _selectedQuote = AllQuotesOption;

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

    public IReadOnlyList<string> Quotes => _quotes;

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (!string.Equals(_searchText, normalized, StringComparison.Ordinal))
            {
                _searchText = normalized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                UpdateFilter();
            }
        }
    }

    public string SelectedQuote
    {
        get => _selectedQuote;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? AllQuotesOption : value;
            if (!string.Equals(_selectedQuote, normalized, StringComparison.OrdinalIgnoreCase))
            {
                _selectedQuote = normalized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedQuote)));
                UpdateFilter();
            }
        }
    }

    public bool ApplySort(FastTreeDataGridColumn column, FastTreeDataGridSortDirection direction)
    {
        if (column is null || _flatSource is null)
        {
            return false;
        }

        if (direction == FastTreeDataGridSortDirection.None)
        {
            _flatSource.Sort(null);
            return true;
        }

        if (string.IsNullOrEmpty(column.ValueKey))
        {
            _flatSource.Sort(null);
            return false;
        }

        var comparison = GetComparison(column.ValueKey);
        if (comparison is null)
        {
            _flatSource.Sort(null);
            return false;
        }

        if (direction == FastTreeDataGridSortDirection.Descending)
        {
            var baseComparison = comparison;
            comparison = (left, right) => baseComparison(right, left);
        }

        _flatSource.Sort(comparison);
        return true;
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
        UpdateQuotes();
        UpdateFilter();
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

    private Comparison<FastTreeDataGridRow>? GetComparison(string valueKey)
    {
        return valueKey switch
        {
            CryptoTickerNode.KeySymbol => CreateStringComparison(node => node.Symbol),
            CryptoTickerNode.KeyQuote => CreateStringComparison(node => node.QuoteAsset),
            CryptoTickerNode.KeyPrice => CreateDecimalComparison(node => node.LastPrice),
            CryptoTickerNode.KeyChange => CreateDecimalComparison(node => node.ChangePercent),
            CryptoTickerNode.KeyVolume => CreateDecimalComparison(node => node.QuoteVolume),
            _ => null,
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateStringComparison(Func<CryptoTickerNode, string?> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            var order = CompareNodeOrder(leftRow.Item, rightRow.Item);
            if (order != 0)
            {
                return order;
            }

            if (leftRow.Item is CryptoGroupNode leftGroup && rightRow.Item is CryptoGroupNode rightGroup)
            {
                return comparer.Compare(leftGroup.QuoteAsset, rightGroup.QuoteAsset);
            }

            if (leftRow.Item is CryptoTickerNode leftTicker && rightRow.Item is CryptoTickerNode rightTicker)
            {
                return comparer.Compare(selector(leftTicker) ?? string.Empty, selector(rightTicker) ?? string.Empty);
            }

            return 0;
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateDecimalComparison(Func<CryptoTickerNode, decimal> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            var order = CompareNodeOrder(leftRow.Item, rightRow.Item);
            if (order != 0)
            {
                return order;
            }

            if (leftRow.Item is CryptoGroupNode leftGroup && rightRow.Item is CryptoGroupNode rightGroup)
            {
                return comparer.Compare(leftGroup.QuoteAsset, rightGroup.QuoteAsset);
            }

            if (leftRow.Item is CryptoTickerNode leftTicker && rightRow.Item is CryptoTickerNode rightTicker)
            {
                var comparison = selector(leftTicker).CompareTo(selector(rightTicker));
                if (comparison != 0)
                {
                    return comparison;
                }

                return comparer.Compare(leftTicker.Symbol, rightTicker.Symbol);
            }

            return 0;
        };
    }

    private static int CompareNodeOrder(object? left, object? right)
    {
        var leftIsGroup = left is CryptoGroupNode;
        var rightIsGroup = right is CryptoGroupNode;

        if (leftIsGroup && !rightIsGroup)
        {
            return -1;
        }

        if (!leftIsGroup && rightIsGroup)
        {
            return 1;
        }

        return 0;
    }

    private void UpdateFilter()
    {
        if (_flatSource is null)
        {
            return;
        }

        var hasSearch = !string.IsNullOrWhiteSpace(_searchText);
        var quote = _selectedQuote;
        var allQuotes = string.Equals(quote, AllQuotesOption, StringComparison.OrdinalIgnoreCase);

        if (!hasSearch && allQuotes)
        {
            _flatSource.SetFilter(null);
            return;
        }

        _flatSource.SetFilter(row =>
        {
            if (row.Item is CryptoGroupNode group)
            {
                var matchesQuote = allQuotes || string.Equals(group.QuoteAsset, quote, StringComparison.OrdinalIgnoreCase);
                if (!matchesQuote)
                {
                    return false;
                }

                if (!hasSearch)
                {
                    return true;
                }

                return group.QuoteAsset.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
            }

            if (row.Item is CryptoTickerNode ticker)
            {
                if (!allQuotes && !string.Equals(ticker.QuoteAsset, quote, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!hasSearch)
                {
                    return true;
                }

                return ticker.Symbol.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    || ticker.QuoteAsset.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }, expandMatches: true);
    }

    private void UpdateQuotes()
    {
        var ordered = _groups.Keys
            .OrderBy(GetQuotePriority)
            .ThenBy(q => q, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _quotes.Clear();
        _quotes.Add(AllQuotesOption);
        foreach (var quote in ordered)
        {
            _quotes.Add(quote);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quotes)));

        if (!_quotes.Any(q => string.Equals(q, _selectedQuote, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedQuote = AllQuotesOption;
        }
        else
        {
            UpdateFilter();
        }
    }
}
