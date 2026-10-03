using System.Text.Json.Serialization;

namespace Proyecto.Models.Recurrente;

public class RecurrenteBankDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
