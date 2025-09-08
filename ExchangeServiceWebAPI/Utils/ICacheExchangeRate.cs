using ExchangeServiceWebAPI.Models;

namespace ExchangeServiceWebAPI.Utils
{
    public interface ICacheExchangeRate
    {
        ExchangeRatesResponse? CachedRates { get; }

        bool IsCacheValid();
        void SaveCache(ExchangeRatesResponse? rates);
    }
}