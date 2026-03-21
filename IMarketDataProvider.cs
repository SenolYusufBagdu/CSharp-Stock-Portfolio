// IMarketDataProvider.cs — interface for live price feeds
// swap Finnhub for AlphaVantage or any other provider without changing core logic

using System.Threading.Tasks;

namespace PortfolioEngine.Services
{
    public interface IMarketDataProvider
    {
        // fetch latest price for a symbol — returns 0 if unavailable
        Task<decimal> GetPriceAsync(string symbol);

        // provider name shown in UI
        string ProviderName { get; }
    }

    // manual price provider — fallback when no API key is set
    public class ManualPriceProvider : IMarketDataProvider
    {
        public string ProviderName => "Manual";

        public Task<decimal> GetPriceAsync(string symbol)
        {
            // manual mode: return 0 so the system prompts user to enter price
            return Task.FromResult(0m);
        }
    }

    // Finnhub integration — replace API_KEY with real key
    public class FinnhubProvider : IMarketDataProvider
    {
        private readonly string _apiKey;
        private readonly System.Net.Http.HttpClient _http = new();

        public string ProviderName => "Finnhub";

        public FinnhubProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<decimal> GetPriceAsync(string symbol)
        {
            try
            {
                string url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_apiKey}";
                string json = await _http.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                // Finnhub returns "c" as current price
                if (doc.RootElement.TryGetProperty("c", out var price))
                    return price.GetDecimal();
            }
            catch { }

            return 0m;
        }
    }

    // AlphaVantage integration — alternative provider
    public class AlphaVantageProvider : IMarketDataProvider
    {
        private readonly string _apiKey;
        private readonly System.Net.Http.HttpClient _http = new();

        public string ProviderName => "AlphaVantage";

        public AlphaVantageProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<decimal> GetPriceAsync(string symbol)
        {
            try
            {
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE" +
                             $"&symbol={symbol}&apikey={_apiKey}";
                string json = await _http.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                // AlphaVantage nests price under "Global Quote" → "05. price"
                if (doc.RootElement.TryGetProperty("Global Quote", out var quote) &&
                    quote.TryGetProperty("05. price", out var price))
                    return decimal.Parse(price.GetString() ?? "0");
            }
            catch { }

            return 0m;
        }
    }
}
