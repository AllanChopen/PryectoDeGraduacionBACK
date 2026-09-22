using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Proyecto.Models.Recurrente;
using Proyecto.Services.Checkout;

namespace Proyecto.Services.Recurrente;

public class RecurrenteService : IRecurrenteService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;

    public RecurrenteService(IHttpClientFactory httpClientFactory, AppDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public async Task<string> TestConnectionAsync()
    {
        var client = _httpClientFactory.CreateClient("Recurrente");
        var response = await client.GetAsync("test");
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Recurrente respondió con {(int)response.StatusCode}: {content}"
            );
        }

        return content;
    }

    public async Task<string> CreateConnectedAccountAsync(
        int bandaId,
        string email,
        string fullName,
        string phoneNumber,
        string name,
        IFormFile taxRegistrationDocument,
        IFormFile idCardImage,
        IFormFile idCardImageBack)
    {
        var banda = await _context.Bandas.FindAsync(bandaId);
        if (banda is null)
        {
            throw new KeyNotFoundException($"No se encontró la banda con id {bandaId}.");
        }

        var client = _httpClientFactory.CreateClient("Recurrente");

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(email), "email");
        form.Add(new StringContent(fullName), "full_name");
        form.Add(new StringContent(phoneNumber), "phone_number");
        form.Add(new StringContent(name), "name");
        form.Add(new StringContent("individual"), "account_type");
        form.Add(new StringContent("daily"), "withdrawals_schedule");

        form.Add(
            await CreateFileContentAsync(taxRegistrationDocument),
            "verification[tax_registration_document]",
            taxRegistrationDocument.FileName
        );

        form.Add(
            await CreateFileContentAsync(idCardImage),
            "verification[id_card_image]",
            idCardImage.FileName
        );

        form.Add(
            await CreateFileContentAsync(idCardImageBack),
            "verification[id_card_image_back]",
            idCardImageBack.FileName
        );

        var response = await client.PostAsync("connected_accounts", form);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Recurrente respondió con {(int)response.StatusCode}: {content}"
            );
        }

        var recurrenteAccountId = ExtractConnectedAccountId(content);
        banda.RecurrenteAccountId = recurrenteAccountId;
        banda.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return content;
    }

    public async Task<RecurrenteCheckoutResponse> CreateCheckoutAsync(
        CheckoutCalculation calculation,
        DateTime? expiresAt = null,
        string? successUrl = null,
        string? cancelUrl = null)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");

        var totalInCents = (int)Math.Round(
            calculation.Total * 100,
            MidpointRounding.AwayFromZero
        );

        var payload = new
        {
            items = new[]
            {
                new
                {
                    name = $"Compra Banda #{calculation.BandaId}",
                    amount_in_cents = totalInCents,
                    currency = "GTQ",
                    quantity = 1
                }
            },

            expires_at = expiresAt?.ToString("O"),
            success_url = successUrl,
            cancel_url = cancelUrl
        };

        Console.WriteLine(
            $"Recurrente checkout payload: {JsonSerializer.Serialize(payload)}");

        var response = await client.PostAsJsonAsync(
            "checkouts",
            payload
        );

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Recurrente respondió con {(int)response.StatusCode}: {content}"
            );
        }

        var checkout =
            JsonSerializer.Deserialize<RecurrenteCheckoutResponse>(
                content
            );

        if (checkout == null)
        {
            throw new Exception(
                "No se pudo interpretar la respuesta de Recurrente."
            );
        }

        return checkout;
    }

    public async Task<RecurrenteCheckoutResponse> GetCheckoutAsync(
        string checkoutId)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");

        var response = await client.GetAsync(
            $"checkouts/{checkoutId}");

        var content =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Error consultando checkout {checkoutId}: {content}");
        }

        var checkout =
            JsonSerializer.Deserialize<RecurrenteCheckoutResponse>(
                content);

        if (checkout == null)
        {
            throw new Exception(
                "No se pudo interpretar el checkout de Recurrente.");
        }

        return checkout;
    }

    private static string ExtractConnectedAccountId(string content)
    {
        using var document = JsonDocument.Parse(content);

        if (document.RootElement.TryGetProperty("id", out var idElement))
        {
            var accountId = idElement.GetString();
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                return accountId;
            }
        }

        throw new Exception("La respuesta de Recurrente no contiene un id de cuenta válido.");
    }

    private static async Task<ByteArrayContent> CreateFileContentAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var content = new ByteArrayContent(memoryStream.ToArray());

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        return content;
    }
}
