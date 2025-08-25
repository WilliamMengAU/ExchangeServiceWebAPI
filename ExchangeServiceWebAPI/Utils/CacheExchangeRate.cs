using System.Text.Json;
using ExchangeServiceWebAPI.Models;

namespace ExchangeServiceWebAPI.Utils
{
    public static class CacheExchangeRate
    {
        private const string CacheFile = "www.exchangerate-api.com docs free_cache.json";

        private static ExchangeRatesResponse? _cachedRates;
        public static ExchangeRatesResponse? CachedRates => _cachedRates?? LoadCache();

        static CacheExchangeRate()
        {
            _cachedRates = LoadCache();
        }

        public static void SaveCache(ExchangeRatesResponse? rates)
        {
            _cachedRates = rates;
            var jsonStr = JsonSerializer.Serialize(rates, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CacheFile, jsonStr);
        }

        public static bool IsCacheValid()
        {
            if (CachedRates == null) return false;
            return DateTime.UtcNow < CachedRates.TimeNextUpdateUtc;
        }

        private static ExchangeRatesResponse? LoadCache()
        {
            if (!File.Exists(CacheFile)) return null;
            var jsonStr = File.ReadAllText(CacheFile);

            _cachedRates = JsonSerializer.Deserialize<ExchangeRatesResponse>(jsonStr);
            return _cachedRates;
        }
    }
}
