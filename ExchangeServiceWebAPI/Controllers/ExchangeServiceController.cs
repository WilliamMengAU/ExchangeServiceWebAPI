using ExchangeServiceWebAPI.Models;
using ExchangeServiceWebAPI.Services;
using ExchangeServiceWebAPI.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeServiceWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExchangeServiceController(ILogger<ExchangeServiceController> logger, IExchangeRateService exchangeRateService) : ControllerBase
    {
        private readonly ILogger<ExchangeServiceController> _logger = logger;

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ExchangeRequest request)
        {
            if (request.InputCurrency != "AUD")
            {
                return BadRequest("Only support AUD to other currency");
            }

            try
            {
                var (success, value) = await exchangeRateService.ConvertAsync(request);
                if (!success)
                {
                    return StatusCode(503, $"Please retry later (after {CacheExchangeRate.CachedRates?.TimeCanRetryUtc}).");
                }

                var response = new ExchangeResponse
                {
                    Amount = request.Amount,
                    InputCurrency = request.InputCurrency,
                    OutputCurrency = request.OutputCurrency,
                    Value = value
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing the request.");

                return StatusCode(500, "Internal server error");
            }
        }
    }
}
