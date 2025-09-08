using ExchangeServiceWebAPI.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExchangeServiceWebAPI.Tests
{
    public class CacheExchangeRateTests
    {
        private CacheExchangeRate cacheExchangeRate;

        [SetUp]
        public void Setup()
        {
            var mockLogger = new Mock<ILogger<CacheExchangeRate>>();
            cacheExchangeRate = new CacheExchangeRate(mockLogger.Object);
        }

        [Test]
        public void TestLoadCache()
        {
            var cache = cacheExchangeRate.CachedRates;

            Assert.Multiple(() =>
            {
                Assert.That(cache?.Result, Is.EqualTo("success"));
                Assert.That(cache?.BaseCurrencyCode, Is.EqualTo("AUD"));
            });
        }
    }
}