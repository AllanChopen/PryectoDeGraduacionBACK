using System.Text.Json.Serialization;

namespace Proyecto.Models.Recurrente;

public class RecurrenteConnectedAccountResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("account_type")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string? TaxId { get; set; }

    [JsonPropertyName("tax_name")]
    public string? TaxName { get; set; }

    [JsonPropertyName("onboarding_completed")]
    public bool OnboardingCompleted { get; set; }
}
