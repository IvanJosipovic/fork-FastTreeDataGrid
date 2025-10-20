using System.Text.Json.Serialization;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

internal sealed class CryptoTickerRecord
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("lastPrice")]
    public string LastPrice { get; set; } = string.Empty;

    [JsonPropertyName("priceChangePercent")]
    public string PriceChangePercent { get; set; } = string.Empty;

    [JsonPropertyName("quoteVolume")]
    public string QuoteVolume { get; set; } = string.Empty;
}
