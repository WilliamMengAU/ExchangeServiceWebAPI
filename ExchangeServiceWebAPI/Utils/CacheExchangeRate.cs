using System.Text.Json;
using ExchangeServiceWebAPI.Models;

namespace ExchangeServiceWebAPI.Utils
{
    public class CacheExchangeRate(ILogger<CacheExchangeRate> logger) : ICacheExchangeRate
    {
        private const string CacheFile = "exchange_cache.json";
        private readonly object _lock = new();

        private ExchangeRatesResponse? _cachedRates;
        public ExchangeRatesResponse? CachedRates => _cachedRates ?? LoadCache();

        public void SaveCache(ExchangeRatesResponse? rates)
        {
            _cachedRates = rates;
            var jsonStr = JsonSerializer.Serialize(rates, new JsonSerializerOptions { WriteIndented = true });

            lock (_lock)
            {
                File.WriteAllText(CacheFile, jsonStr);
            }

            logger.LogInformation("Cache saved.");
        }

        public bool IsCacheValid()
        {
            if (CachedRates == null) return false;
            return DateTime.UtcNow < CachedRates.TimeNextUpdateUtc;
        }

        private ExchangeRatesResponse? LoadCache()
        {
            if (!File.Exists(CacheFile))
            {
                logger.LogWarning("Cache file does not exist.");
                return null;
            }

            var jsonStr = File.ReadAllText(CacheFile);

            _cachedRates = JsonSerializer.Deserialize<ExchangeRatesResponse>(jsonStr);
            return _cachedRates;
        }
    }
}
