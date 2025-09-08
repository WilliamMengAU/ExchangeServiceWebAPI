using System.ComponentModel.DataAnnotations;

namespace ExchangeServiceWebAPI.Models
{
    public class ExchangeRequest
    {
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required, StringLength(3, MinimumLength = 3)]
        public required string InputCurrency { get; set; }

        [Required, StringLength(3, MinimumLength = 3)]
        public required string OutputCurrency { get; set; }
    }
}
