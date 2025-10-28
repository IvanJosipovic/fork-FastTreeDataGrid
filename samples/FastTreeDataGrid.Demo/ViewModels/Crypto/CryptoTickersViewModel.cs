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

    private readonly List<CryptoTickerNode> _tickerNodes = new();
    private readonly Dictionary<string, CryptoTickerNode> _tickers = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly AvaloniaList<string> _quotes = new() { AllQuotesOption };
    private readonly FastTreeDataGridFlatSource<CryptoTickerNode> _flatSource;
    private Comparison<FastTreeDataGridRow>? _activeSortComparison;

    private string _searchText = string.Empty;
    private string _selectedQuote = AllQuotesOption;

    public CryptoTickersViewModel()
    {
        _flatSource = new FastTreeDataGridFlatSource<CryptoTickerNode>(
            _tickerNodes,
            static _ => Array.Empty<CryptoTickerNode>());

        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, async (_, _) => await RefreshAsync());
        _ = InitializeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource CryptoSource => _flatSource;

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

    public bool ApplySort(IReadOnlyList<FastTreeDataGridSortDescription> descriptions)
    {
        if (descriptions is null || descriptions.Count == 0)
        {
            _activeSortComparison = null;
            _flatSource.Sort(null);
            return true;
        }

        Comparison<FastTreeDataGridRow>? combined = null;

        foreach (var description in descriptions)
        {
            if (string.IsNullOrEmpty(description.Column.ValueKey))
            {
                continue;
            }

            var comparison = GetComparison(description.Column.ValueKey!);
            if (comparison is null)
            {
                continue;
            }

            if (description.Direction == FastTreeDataGridSortDirection.Descending)
            {
                var baseComparison = comparison;
                comparison = (left, right) => baseComparison(right, left);
            }

            combined = combined is null
                ? comparison
                : CombineComparisons(combined, comparison);
        }

        if (combined is null)
        {
            _activeSortComparison = null;
            _flatSource.Sort(null);
            return false;
        }

        _activeSortComparison = combined;
        _flatSource.Sort(combined);
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
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BuildInitialNodes(records);
                _timer.Start();
            });
        }
        catch
        {
            // Swallow errors for demo purposes.
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
            // Ignore transient failures.
        }
    }

    private void BuildInitialNodes(IReadOnlyList<CryptoTickerRecord> records)
    {
        _tickerNodes.Clear();
        _tickers.Clear();

        var desiredOrder = new List<CryptoTickerNode>();
        foreach (var (quote, tickers) in GroupTickers(records))
        {
            foreach (var tickerRecord in tickers)
            {
                var node = CreateOrUpdateNode(tickerRecord, quote);
                desiredOrder.Add(node);
            }
        }

        _tickerNodes.Clear();
        _tickerNodes.AddRange(desiredOrder);
        _tickers.Clear();
        foreach (var node in desiredOrder)
        {
            _tickers[node.Symbol] = node;
        }

        _flatSource.Reset(_tickerNodes, preserveExpansion: false);
        UpdateQuotes();
        UpdateFilter();
        ApplyActiveSort();
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
        var desired = new List<CryptoTickerNode>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (quote, tickers) in GroupTickers(records))
        {
            foreach (var tickerRecord in tickers)
            {
                var node = CreateOrUpdateNode(tickerRecord, quote);
                desired.Add(node);
                seen.Add(node.Symbol);
            }
        }

        foreach (var symbol in _tickers.Keys.ToList())
        {
            if (!seen.Contains(symbol))
            {
                _tickers.Remove(symbol);
            }
        }

        _tickerNodes.Clear();
        _tickerNodes.AddRange(desired);
        _flatSource.Reset(_tickerNodes, preserveExpansion: true);
        UpdateQuotes();
        UpdateFilter();

        ApplyActiveSort();
    }

    private CryptoTickerNode CreateOrUpdateNode(CryptoTickerRecord record, string quoteAsset)
    {
        if (!_tickers.TryGetValue(record.Symbol, out var node))
        {
            node = new CryptoTickerNode(record.Symbol, quoteAsset);
            _tickers[record.Symbol] = node;
        }

        var lastPrice = ParseDecimal(record.LastPrice);
        var changePercent = ParseDecimal(record.PriceChangePercent) / 100m;
        var volume = ParseDecimal(record.QuoteVolume);
        node.Update(lastPrice, changePercent, volume);
        return node;
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
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
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

    private static Comparison<FastTreeDataGridRow> CombineComparisons(
        Comparison<FastTreeDataGridRow> primary,
        Comparison<FastTreeDataGridRow> secondary)
    {
        return (left, right) =>
        {
            var result = primary(left, right);
            return result != 0 ? result : secondary(left, right);
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateStringComparison(Func<CryptoTickerNode, string?> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not CryptoTickerNode left || rightRow.Item is not CryptoTickerNode right)
            {
                return 0;
            }

            return comparer.Compare(selector(left) ?? string.Empty, selector(right) ?? string.Empty);
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateDecimalComparison(Func<CryptoTickerNode, decimal> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not CryptoTickerNode left || rightRow.Item is not CryptoTickerNode right)
            {
                return 0;
            }

            var comparison = selector(left).CompareTo(selector(right));
            if (comparison != 0)
            {
                return comparison;
            }

            return comparer.Compare(left.Symbol, right.Symbol);
        };
    }

    private void UpdateFilter()
    {
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
            if (row.Item is not CryptoTickerNode ticker)
            {
                return true;
            }

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
        }, expandMatches: true);
    }

    private void UpdateQuotes()
    {
        var ordered = _tickerNodes
            .Select(t => t.QuoteAsset)
            .Distinct(StringComparer.OrdinalIgnoreCase)
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

    private void ApplyActiveSort()
    {
        if (_activeSortComparison is not null)
        {
            _flatSource.Sort(_activeSortComparison);
        }
    }
}
