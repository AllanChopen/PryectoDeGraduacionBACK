using Microsoft.AspNetCore.Http;

namespace Proyecto.Services.Recurrente;

public class RecurrenteService : IRecurrenteService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RecurrenteService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
        string email,
        string fullName,
        string phoneNumber,
        string name,
        IFormFile taxRegistrationDocument,
        IFormFile idCardImage,
        IFormFile idCardImageBack)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(email), "email");
        form.Add(new StringContent(fullName), "full_name");
        form.Add(new StringContent(phoneNumber), "phone_number");
        form.Add(new StringContent(name), "name");
        form.Add(new StringContent("individual"), "account_type");
        form.Add(new StringContent("daily"), "withdrawals_schedule");

        form.Add(
            CreateFileContent(taxRegistrationDocument),
            "verification[tax_registration_document]",
            taxRegistrationDocument.FileName
        );

        form.Add(
            CreateFileContent(idCardImage),
            "verification[id_card_image]",
            idCardImage.FileName
        );

        form.Add(
            CreateFileContent(idCardImageBack),
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

        return content;
    }

    private static StreamContent CreateFileContent(IFormFile file)
    {
        var content = new StreamContent(file.OpenReadStream());

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        return content;
    }
}
