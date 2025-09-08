


# ExchangeService API

A simple ASP.NET Web API to convert Australian Dollars (AUD) to US Dollars (USD) and also other currencies by using the open public API from ExchangeRate-API https://open.er-api.com/v6/latest/AUD.

Document reference to https://www.exchangerate-api.com/docs/free

## Implementation
1. AUD to USD and also other currencies convert are supported.
2. Cache the response data from https://open.er-api.com/v6/latest/AUD both in memory and in file.
3. Check the cacheExchangeRate.IsCacheValid() to reduce the API calls. (DateTime.UtcNow < CachedRates.TimeNextUpdateUtc)
4. If API rate limiting (HTTP 429) occurs, the next fetch will wait for 20 minutes before retrying. The next retry time is stored in the cache file.

## Improvements ideas.
1. Add retry policy when call the external API. Such as use Polly.
2. Add more unit tests and integration tests.
3. Move cache to Database or Redis for System Extensibility.
4. Add authentication and authorization.
5. Add more logging and monitoring.
6. Implement a health check (status) API for system monitor.

## How to run
Download the project, and run it in Visual Studio. It will open a swagger window in browser. Then you can put some test data.

You can write code to call http://localhost:5067/ExchangeService or use some WebAPI test tools, such as Postman, to test.

An example test data and response as:

http://localhost:5067/swagger/index.html

```
curl -X 'POST' \
  'http://localhost:5067/ExchangeService' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "amount": 10,
  "inputCurrency": "AUD",
  "outputCurrency": "USD"
}'
```

Request URL
http://localhost:5067/ExchangeService

```
Server response
Code	Details
200	
Response body
{
  "value": 6.46,
  "amount": 10,
  "inputCurrency": "AUD",
  "outputCurrency": "USD"
}
```

