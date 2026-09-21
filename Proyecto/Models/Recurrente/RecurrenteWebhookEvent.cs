using System.Text.Json.Serialization;

namespace Proyecto.Models.Recurrente
{
    public class RecurrenteWebhookEvent
    {
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("amount_in_cents")]
        public int AmountInCents { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("checkout")]
        public RecurrenteWebhookCheckout? Checkout { get; set; }
    }

    public class RecurrenteWebhookCheckout
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("total_in_cents")]
        public int TotalInCents { get; set; }
    }
}
