namespace ExchangeServiceWebAPI.Tests
{
    public class CacheExchangeRateTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestLoadCache()
        {
            var cache = Utils.CacheExchangeRate.CachedRates;

            Assert.That(cache?.result, Is.EqualTo("success") );
            Assert.That(cache?.base_code, Is.EqualTo("AUD"));
        }
    }
}