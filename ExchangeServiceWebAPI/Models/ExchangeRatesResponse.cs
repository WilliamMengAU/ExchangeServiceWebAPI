using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ExchangeServiceWebAPI.Models
{
    public class ExchangeRatesResponse
    {
        public string result { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTime TimeNextUpdateUtc => DateTime.Parse(time_next_update_utc);

        public string time_next_update_utc { get; set; } = string.Empty;

        public string base_code { get; set; } = string.Empty;

        public ConcurrentDictionary<string, decimal> rates { get; set; } = new();

        public DateTime? TimeCanRetryUtc { get; set; }
    }
}
