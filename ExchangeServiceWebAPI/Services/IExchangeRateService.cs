using ExchangeServiceWebAPI.Models;

namespace ExchangeServiceWebAPI.Services
{
    public interface IExchangeRateService
    {
        public Task<(bool success, decimal? value)> ConvertAsync(ExchangeRequest request);
    }
}
