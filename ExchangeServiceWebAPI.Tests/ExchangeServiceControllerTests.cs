using ExchangeServiceWebAPI.Controllers;
using ExchangeServiceWebAPI.Models;
using ExchangeServiceWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExchangeServiceWebAPI.Tests
{
    [TestFixture]
    public class ExchangeServiceControllerTests
    {
        private Mock<ILogger<ExchangeServiceController>> loggerMock;
        private Mock<IExchangeRateService> exchangeRateServiceMock;
        private ExchangeServiceController controller;

        [SetUp]
        public void SetUp()
        {
            loggerMock = new Mock<ILogger<ExchangeServiceController>>();
            exchangeRateServiceMock = new Mock<IExchangeRateService>();
            controller = new ExchangeServiceController(loggerMock.Object, exchangeRateServiceMock.Object);
        }

        [Test]
        public async Task TestPost_ConvertAsyncSuccees()
        {
            var request = new ExchangeRequest { Amount = 100, InputCurrency = "AUD", OutputCurrency = "USD" };
            decimal expectedValue = 65m;
            exchangeRateServiceMock
                .Setup(x => x.ConvertAsync(It.IsAny<ExchangeRequest>()))
                .ReturnsAsync((true, expectedValue));

            var result = await controller.Post(request);

            var okResult = result as OkObjectResult;
            var response = okResult?.Value as ExchangeResponse;

            Assert.Multiple(() =>
            {
                Assert.That(response?.Amount, Is.EqualTo(request?.Amount));
                Assert.That(response?.InputCurrency, Is.EqualTo("AUD"));
                Assert.That(response?.OutputCurrency, Is.EqualTo("USD"));
                Assert.That(response?.Value, Is.EqualTo(expectedValue));
            });
        }

        [Test]
        public async Task TestPost_InvalidModelState()
        {
            var request = new ExchangeRequest { Amount = 0, InputCurrency = "AUD", OutputCurrency = "USD" };
            controller.ModelState.AddModelError("Amount", "Required");

            var result = await controller.Post(request);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task TestPost_InputCurrencyNotAUD()
        {
            var request = new ExchangeRequest { Amount = 100, InputCurrency = "usd", OutputCurrency = "EUR" };

            var result = await controller.Post(request);

            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest, Is.Not.Null);
            Assert.That(badRequest.Value, Is.EqualTo("Only support AUD to other currency."));
        }

        [Test]
        public async Task TestPost_ConvertAsyncFail()
        {
            var request = new ExchangeRequest { Amount = 100, InputCurrency = "AUD", OutputCurrency = "USD" };
            exchangeRateServiceMock
                .Setup(x => x.ConvertAsync(It.IsAny<ExchangeRequest>()))
                .ReturnsAsync((false, (decimal?)null));

            var result = await controller.Post(request);

            var statusResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(statusResult?.StatusCode, Is.EqualTo(503));
                Assert.That(statusResult?.Value, Is.EqualTo("Please retry later."));
            });
        }

        [Test]
        public async Task TestPost_WhenExceptionThrown()
        {
            var request = new ExchangeRequest { Amount = 100, InputCurrency = "AUD", OutputCurrency = "USD" };
            exchangeRateServiceMock
                .Setup(x => x.ConvertAsync(It.IsAny<ExchangeRequest>()))
                .ThrowsAsync(new Exception("API error"));

            var result = await controller.Post(request);

            var statusResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(statusResult?.StatusCode, Is.EqualTo(500));
                Assert.That(statusResult?.Value, Is.EqualTo("Internal server error."));
            });
        }
    }
}