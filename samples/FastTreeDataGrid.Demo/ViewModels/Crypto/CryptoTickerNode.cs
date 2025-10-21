using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

internal sealed class CryptoTickerNode : CryptoNode
{
    public const string KeySymbol = "Crypto.Symbol";
    public const string KeyQuote = "Crypto.Quote";
    public const string KeyPrice = "Crypto.Price";
    public const string KeyChange = "Crypto.Change";
    public const string KeyChangeChart = "Crypto.ChangeChart";
    public const string KeyVolume = "Crypto.Volume";

    private const int MaxHistoryLength = 60;

    private static readonly ImmutableSolidColorBrush PositiveStroke = new(Color.FromRgb(0x38, 0xA1, 0x69));
    private static readonly ImmutableSolidColorBrush PositiveFill = new(Color.FromArgb(0x33, 0x38, 0xA1, 0x69));
    private static readonly ImmutableSolidColorBrush NegativeStroke = new(Color.FromRgb(0xD9, 0x47, 0x47));
    private static readonly ImmutableSolidColorBrush NegativeFill = new(Color.FromArgb(0x33, 0xD9, 0x47, 0x47));
    private static readonly ImmutableSolidColorBrush NeutralStroke = new(Color.FromRgb(0x80, 0x80, 0x80));
    private static readonly ImmutableSolidColorBrush NeutralFill = new(Color.FromArgb(0x26, 0x80, 0x80, 0x80));

    private readonly CultureInfo _formatCulture = CultureInfo.CurrentCulture;
    private readonly List<double> _changeHistory = new(MaxHistoryLength);

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

    public decimal LastPrice => _lastPrice;

    public decimal ChangePercent => _changePercent;

    public decimal QuoteVolume => _quoteVolume;

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
            KeyChangeChart => CreateChangeChartValue(),
            KeyVolume => _quoteVolume.ToString("N0", _formatCulture),
            _ => string.Empty,
        };
    }

    public void Update(decimal lastPrice, decimal changePercent, decimal volume)
    {
        var chartUpdated = TrackChangeSample(changePercent);

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

        if (chartUpdated)
        {
            NotifyValueChanged(KeyChangeChart);
        }
    }

    private bool TrackChangeSample(decimal changePercent)
    {
        var value = (double)changePercent;
        if (!double.IsFinite(value))
        {
            return false;
        }

        if (_changeHistory.Count == MaxHistoryLength)
        {
            _changeHistory.RemoveAt(0);
        }

        _changeHistory.Add(value);
        return true;
    }

    private ChartWidgetValue CreateChangeChartValue()
    {
        var stroke = _changePercent switch
        {
            > 0 => PositiveStroke,
            < 0 => NegativeStroke,
            _ => NeutralStroke,
        };

        var fill = _changePercent switch
        {
            > 0 => PositiveFill,
            < 0 => NegativeFill,
            _ => NeutralFill,
        };

        var series = _changeHistory.Count == 0
            ? Array.Empty<ChartSeriesValue>()
            : new[]
            {
                new ChartSeriesValue(
                    _changeHistory,
                    Stroke: stroke,
                    Fill: fill,
                    StrokeThickness: 1.5,
                    FillToBaseline: true),
            };

        return new ChartWidgetValue(
            series,
            Baseline: 0,
            ShowBaseline: true,
            BaselineBrush: NeutralStroke,
            BaselineThickness: 1);
    }
}
