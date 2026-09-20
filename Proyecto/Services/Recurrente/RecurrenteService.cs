using System.Text.Json;
using Microsoft.AspNetCore.Http;

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
