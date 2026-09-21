using System.Text.Json.Serialization;

namespace Proyecto.Models.Recurrente
{
    public class RecurrenteCheckoutResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("checkout_url")]
        public string CheckoutUrl { get; set; } = string.Empty;

        [JsonPropertyName("total_in_cents")]
        public int TotalInCents { get; set; }
    }
}
