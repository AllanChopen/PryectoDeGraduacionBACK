using System.Text.Json.Serialization;

namespace Proyecto.Models.Recurrente;

public class RecurrenteSubscriptionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("current_period_end")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [JsonPropertyName("checkout")]
    public RecurrenteSubscriptionCheckout? Checkout { get; set; }
}

public class RecurrenteSubscriptionCheckout
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
