using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ExchangeServiceWebAPI.Models
{
    public class ExchangeRatesResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTime TimeNextUpdateUtc => DateTime.Parse(TimeNextUpdateUtcStr);

        [JsonPropertyName("time_next_update_utc")]
        public string TimeNextUpdateUtcStr { get; set; } = string.Empty;

        [JsonPropertyName("base_code")] 
        public string BaseCurrencyCode { get; set; } = string.Empty;

        [JsonPropertyName("rates")]
        public ConcurrentDictionary<string, decimal> Rates { get; set; } = new();

        public DateTime? TimeCanRetryUtc { get; set; }
    }
}
