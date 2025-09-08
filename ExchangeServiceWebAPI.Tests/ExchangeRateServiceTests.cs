using ExchangeServiceWebAPI.Models;
using ExchangeServiceWebAPI.Services;
using ExchangeServiceWebAPI.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Collections.Concurrent;
using System.Net;

namespace ExchangeServiceWebAPI.Tests
{
    [TestFixture]
    public class ExchangeRateServiceTests
    {
        private Mock<ICacheExchangeRate> cacheMock;
        private Mock<ILogger<ExchangeRateService>> loggerMock;
        private Mock<IConfiguration> configMock;
        private HttpClient httpClient;
        private Mock<HttpMessageHandler> httpHandlerMock;
        private ExchangeRateService service;

        private const uint retryIntervalMinutes = 20;

        [SetUp]
        public void SetUp()
        {
            cacheMock = new Mock<ICacheExchangeRate>();
            loggerMock = new Mock<ILogger<ExchangeRateService>>();
            configMock = new Mock<IConfiguration>();  

            httpHandlerMock = new Mock<HttpMessageHandler>();
            httpClient = new HttpClient(httpHandlerMock.Object);

            service = new ExchangeRateService(httpClient, configMock.Object, cacheMock.Object, loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            httpClient?.Dispose();
        }

        [Test]
        public async Task TestConvertAsync_WhenCacheIsValid()
        {
            var request = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "USD" };
            var rates = new ConcurrentDictionary<string, decimal> { ["USD"] = 0.7m };
            var cachedRates = new ExchangeRatesResponse { Rates = rates };

            cacheMock.Setup(c => c.IsCacheValid()).Returns(true);
            cacheMock.SetupGet(c => c.CachedRates).Returns(cachedRates);

            var (success, value) = await service.ConvertAsync(request);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(value, Is.EqualTo(7.00m));
            });

            httpHandlerMock.Protected().Verify(
                "SendAsync", Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task TestConvertAsync_WhenCacheInvalid_ApiLimitReached()
        {
            var request = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "USD" };
            var cachedRates = new ExchangeRatesResponse
            {
                Rates = new ConcurrentDictionary<string, decimal>(),
                TimeCanRetryUtc = DateTime.UtcNow.AddMinutes(10)
            };

            cacheMock.Setup(c => c.IsCacheValid()).Returns(false);
            cacheMock.SetupGet(c => c.CachedRates).Returns(cachedRates);

            var (success, value) = await service.ConvertAsync(request);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(value, Is.Null);
            });

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API call limit reached")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task TestConvertAsync_429TooManyRequests_SetsTimeCanRetryUtc()
        {
            var request = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "USD" };
            var cachedRates = new ExchangeRatesResponse
            {
                Rates = new ConcurrentDictionary<string, decimal>(),
                TimeCanRetryUtc = null
            };

            cacheMock.Setup(c => c.IsCacheValid()).Returns(false);
            cacheMock.SetupGet(c => c.CachedRates).Returns(cachedRates);

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.TooManyRequests,
                    Content = new StringContent("")
                });

            var (success, value) = await service.ConvertAsync(request);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(value, Is.Null);
                Assert.That(cachedRates.TimeCanRetryUtc, Is.GreaterThan(DateTime.UtcNow.AddMinutes(retryIntervalMinutes - 1)));
            });

            cacheMock.Verify(c => c.SaveCache(It.IsAny<ExchangeRatesResponse>()), Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API call limit reached")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task TestConvertAsync_WhenApiReturnValidRates()
        {
            var request = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "USD" };
            var rates = new ConcurrentDictionary<string, decimal> { ["USD"] = 0.8M };
            var responseObj = new ExchangeRatesResponse { Rates = rates };
            var json = System.Text.Json.JsonSerializer.Serialize(responseObj);

            cacheMock.Setup(c => c.IsCacheValid()).Returns(false);
            cacheMock.SetupGet(c => c.CachedRates).Returns((ExchangeRatesResponse)null);

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            cacheMock.Setup(c => c.SaveCache(It.IsAny<ExchangeRatesResponse>()))
                .Callback<ExchangeRatesResponse>(r => cacheMock.SetupGet(c => c.CachedRates).Returns(r));

            var (success, value) = await service.ConvertAsync(request);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(value, Is.EqualTo(8));
            });

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Currency USD rate is")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}