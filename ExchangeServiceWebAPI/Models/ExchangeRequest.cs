namespace ExchangeServiceWebAPI.Models
{
    public class ExchangeRequest
    {
        public decimal Amount { get; set; }
        public required string InputCurrency { get; set; }
        public required string OutputCurrency { get; set; }
    }
}
