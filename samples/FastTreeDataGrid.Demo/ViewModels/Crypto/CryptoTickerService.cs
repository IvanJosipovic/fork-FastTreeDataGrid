using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastTreeDataGrid.Demo.ViewModels.Crypto;

internal static class CryptoTickerService
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.binance.com"),
        Timeout = TimeSpan.FromSeconds(10),
    };

    public static async Task<IReadOnlyList<CryptoTickerRecord>> GetTickersAsync(CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync("/api/v3/ticker/24hr", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var records = await JsonSerializer.DeserializeAsync<List<CryptoTickerRecord>>(stream, options, cancellationToken);
        return records is null ? Array.Empty<CryptoTickerRecord>() : records;
    }
}
