using System.Text.Json;
using ExchangeServiceWebAPI.Models;
using ExchangeServiceWebAPI.Utils;

namespace ExchangeServiceWebAPI.Services
{
    public class ExchangeRateService(HttpClient httpClient) : IExchangeRateService
    {
        private const string ApiUrl = "https://open.er-api.com/v6/latest/AUD";

        public async Task<(bool success, decimal? value)> ConvertAsync(ExchangeRequest request)
        {
            decimal? rate;
            if (CacheExchangeRate.IsCacheValid())
            {
                rate = CacheExchangeRate.CachedRates?.rates[request.OutputCurrency];
                if (rate.HasValue)
                {
                    return (true, Math.Round(request.Amount * rate.Value, 2));
                }
            }

            if (CacheExchangeRate.CachedRates?.TimeCanRetryUtc > DateTime.UtcNow)
            {
                return (false, null);
            }

            var response = await httpClient.GetAsync(ApiUrl);
            if ((int)response.StatusCode == 429)
            {
                // process 429，set TimeCanRetryUtc after 20 Minutes
                if (CacheExchangeRate.CachedRates != null)
                {
                    CacheExchangeRate.CachedRates.TimeCanRetryUtc = DateTime.UtcNow.AddMinutes(20);
                    CacheExchangeRate.SaveCache(CacheExchangeRate.CachedRates);
                }

                return (false, null);
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var ratesResponse = JsonSerializer.Deserialize<ExchangeRatesResponse>(content);

            if (ratesResponse == null)
            {
                return (false, null);
            }

            ratesResponse.TimeCanRetryUtc = DateTime.UtcNow.AddMinutes(20);
            CacheExchangeRate.SaveCache(ratesResponse);

            rate = CacheExchangeRate.CachedRates?.rates[request.OutputCurrency];
            if (rate.HasValue)
            {
                return (true, Math.Round(request.Amount * rate.Value, 2));
            }
            else
            {
                return (false, null);
            }
        }
    }
}
