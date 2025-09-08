using ExchangeServiceWebAPI.Models;
using ExchangeServiceWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeServiceWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExchangeServiceController(ILogger<ExchangeServiceController> logger, IExchangeRateService exchangeRateService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ExchangeRequest request)
        {
            logger.LogInformation("Received exchange request: {Amount} {InputCurrency} to {OutputCurrency}.",
                request.Amount, request.InputCurrency, request.OutputCurrency);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.InputCurrency = request.InputCurrency.ToUpperInvariant();
            request.OutputCurrency = request.OutputCurrency.ToUpperInvariant(); 

            if (request.InputCurrency != "AUD")
            {
                logger.LogWarning("Invalid InputCurrency. InputCurrency is [{InputCurrency}].", request.InputCurrency);
                return BadRequest("Only support AUD to other currency.");
            }

            try
            {
                var (success, value) = await exchangeRateService.ConvertAsync(request);
                if (!success)
                {
                    logger.LogWarning("Conversion failed due to external API issues or rate limits.");
                    return StatusCode(503, $"Please retry later.");
                }

                var response = new ExchangeResponse
                {
                    Amount = request.Amount,
                    InputCurrency = request.InputCurrency,
                    OutputCurrency = request.OutputCurrency,
                    Value = value
                };

                logger.LogInformation("Conversion successful: {Amount} {InputCurrency} to {Value} {OutputCurrency}.",
                    request.Amount, request.InputCurrency, value, request.OutputCurrency); 
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing the request.");

                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
