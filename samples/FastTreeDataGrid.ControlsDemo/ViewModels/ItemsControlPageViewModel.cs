using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class ItemsControlPageViewModel : IDisposable
{
    private readonly FastTreeDataGridFlatSource<DashboardCard> _source;

    public ItemsControlPageViewModel()
    {
        var cards = CreateCards();
        _source = new FastTreeDataGridFlatSource<DashboardCard>(cards, _ => Array.Empty<DashboardCard>());
    }

    public IFastTreeDataGridSource Source => _source;

    public void Dispose()
    {
    }

    private static IReadOnlyList<DashboardCard> CreateCards()
    {
        var random = new Random(32);
        var metrics = new[] { "Sessions", "Conversion", "Retention", "Activation", "MRR", "Latency", "Crash Rate", "Net Promoter Score" };

        return metrics
            .Select((title, index) =>
            {
                var trend = Math.Round(random.NextDouble() * 0.2 - 0.05, 3);
                var value = title switch
                {
                    "Conversion" => 0.182,
                    "Retention" => 0.763,
                    "MRR" => 92035,
                    "Latency" => 180,
                    "Crash Rate" => 0.014,
                    "Net Promoter Score" => 41,
                    _ => random.Next(1200, 6500),
                };

                var tags = title switch
                {
                    "Conversion" => new[] { "Marketing", "Funnels" },
                    "Retention" => new[] { "Cohorts", "Lifecycle" },
                    "MRR" => new[] { "Finance", "Billing" },
                    "Latency" => new[] { "Platform", "SLO" },
                    "Crash Rate" => new[] { "Quality", "Alerts" },
                    "Net Promoter Score" => new[] { "Feedback" },
                    _ => new[] { "Product" },
                };

                return new DashboardCard(
                    title,
                    $"Live insight #{index + 1}",
                    value,
                    trend,
                    trend >= 0 ? "Up" : "Down",
                    tags);
            })
            .ToArray();
    }
}

public sealed class DashboardCard : IFastTreeDataGridValueProvider, INotifyPropertyChanged
{
    public const string KeyTitle = nameof(Title);
    public const string KeySubtitle = nameof(Subtitle);
    public const string KeyValue = nameof(Value);
    public const string KeyTrend = nameof(Trend);
    public const string KeyTrendDirection = nameof(TrendDirection);

    private static readonly IBrush PositiveTrendBackground = new ImmutableSolidColorBrush(Color.FromArgb(0x1F, 0x2E, 0x7D, 0x32));
    private static readonly IBrush NegativeTrendBackground = new ImmutableSolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0x70, 0x43));
    private static readonly IBrush PositiveTrendBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly IBrush NegativeTrendBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0x70, 0x43));

    private string _subtitle;
    private double _value;
    private double _trend;
    private string _trendDirection;

    public DashboardCard(string title, string subtitle, double value, double trend, string trendDirection, IReadOnlyList<string> tags)
    {
        Title = title;
        _subtitle = subtitle;
        _value = value;
        _trend = trend;
        _trendDirection = trendDirection;
        Tags = tags;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public string Title { get; }

    public string Subtitle
    {
        get => _subtitle;
        set => SetField(ref _subtitle, value, nameof(Subtitle));
    }

    public double Value
    {
        get => _value;
        set => SetField(ref _value, value, nameof(Value));
    }

    public double Trend
    {
        get => _trend;
        set => SetField(ref _trend, value, nameof(Trend));
    }

    public string TrendDirection
    {
        get => _trendDirection;
        set => SetField(ref _trendDirection, value, nameof(TrendDirection));
    }

    public IReadOnlyList<string> Tags { get; }

    public bool IsTrendPositive => Trend >= 0;

    public IBrush TrendBackground => IsTrendPositive ? PositiveTrendBackground : NegativeTrendBackground;

    public IBrush TrendBorder => IsTrendPositive ? PositiveTrendBorder : NegativeTrendBorder;

    public string DisplayTrend => Trend.ToString("P1", CultureInfo.CurrentCulture);

    public string DisplayValue =>
        Title switch
        {
            "Conversion" or "Retention" or "Crash Rate" => Value.ToString("P1", CultureInfo.CurrentCulture),
            "Net Promoter Score" => Value.ToString("N0", CultureInfo.CurrentCulture),
            "MRR" => Value.ToString("C0", CultureInfo.CurrentCulture),
            "Latency" => $"{Value:N0} ms",
            _ => Value.ToString("N0", CultureInfo.CurrentCulture),
        };

    public object? GetValue(object? item, string key) =>
        key switch
        {
            KeyTitle => Title,
            KeySubtitle => Subtitle,
            KeyValue => Value,
            KeyTrend => Trend,
            KeyTrendDirection => TrendDirection,
            _ => null,
        };

    private void SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, propertyName));
        if (propertyName is nameof(Value))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayValue)));
        }

        if (propertyName is nameof(Trend))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayTrend)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTrendPositive)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrendBackground)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrendBorder)));
        }
    }
}
