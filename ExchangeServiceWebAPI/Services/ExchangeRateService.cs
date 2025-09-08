using ExchangeServiceWebAPI.Models;
using ExchangeServiceWebAPI.Utils;
using System.Net;
using System.Text.Json;

namespace ExchangeServiceWebAPI.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly string apiUrl = "https://open.er-api.com/v6/latest/AUD";
        private const uint retryIntervalMinutes = 20;

        private readonly HttpClient httpClient;
        private readonly ICacheExchangeRate cacheExchangeRate;
        private readonly ILogger<ExchangeRateService> logger;

        public ExchangeRateService(HttpClient httpClient, IConfiguration config, ICacheExchangeRate cacheExchangeRate, ILogger<ExchangeRateService> logger)
        {
            this.httpClient = httpClient;
            apiUrl = config["ExchangeRateApi:ApiUrl"] ?? apiUrl;
            this.cacheExchangeRate = cacheExchangeRate;
            this.logger = logger;
        }

        public async Task<(bool success, decimal? value)> ConvertAsync(ExchangeRequest request)
        {
            decimal? rate;
            if (cacheExchangeRate.IsCacheValid())
            {
                rate = cacheExchangeRate.CachedRates?.Rates[request.OutputCurrency];
                if (rate.HasValue)
                {
                    return (true, Math.Round(request.Amount * rate.Value, 2));
                }
            }

            if (cacheExchangeRate.CachedRates?.TimeCanRetryUtc > DateTime.UtcNow)
            {
                logger.LogWarning("API call limit reached. Please wait until {TimeCanRetryUtc}", cacheExchangeRate.CachedRates.TimeCanRetryUtc);
                return (false, null);
            }

            var response = await httpClient.GetAsync(apiUrl);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // process 429 TooManyRequests，set TimeCanRetryUtc after 20 Minutes
                if (cacheExchangeRate.CachedRates != null)
                {
                    cacheExchangeRate.CachedRates.TimeCanRetryUtc = DateTime.UtcNow.AddMinutes(retryIntervalMinutes);
                    cacheExchangeRate.SaveCache(cacheExchangeRate.CachedRates);
                }

                logger.LogWarning("API call limit reached. Please wait until {TimeCanRetryUtc}", cacheExchangeRate.CachedRates?.TimeCanRetryUtc);
                return (false, null);
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var ratesResponse = JsonSerializer.Deserialize<ExchangeRatesResponse>(content);

            if (ratesResponse == null)
            {
                logger.LogError("Failed to deserialize exchange rates response. content:[{content}]", content);
                return (false, null);
            }

            ratesResponse.TimeCanRetryUtc = DateTime.UtcNow.AddMinutes(20);
            cacheExchangeRate.SaveCache(ratesResponse);

            rate = cacheExchangeRate.CachedRates?.Rates[request.OutputCurrency];
            if (rate.HasValue)
            {
                logger.LogInformation("Currency {OutputCurrency} rate is {rate.Value}.", request.OutputCurrency, rate.Value);
                return (true, Math.Round(request.Amount * rate.Value, 2));
            }
            else
            {
                logger.LogError("Currency {OutputCurrency} not found in rates.", request.OutputCurrency);
                return (false, null);
            }
        }
    }
}
