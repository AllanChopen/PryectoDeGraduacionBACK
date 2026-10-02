using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<RecurrenteBankDto>> GetBanksAsync(
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");
        using var response = await client.GetAsync("banks", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Recurrente no pudo listar los bancos ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var banks = await response.Content.ReadFromJsonAsync<List<RecurrenteBankDto>>(
            cancellationToken: cancellationToken);

        return banks?.Where(bank => !string.IsNullOrWhiteSpace(bank.Name)).ToList()
            ?? throw new InvalidOperationException("Recurrente no devolvió una lista de bancos válida.");
    }

    public async Task<RecurrenteConnectedAccountResponse> CreateConnectedAccountAsync(
        string bandName,
        ConnectedAccountRequest request)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");
        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(request.Email.Trim()), "email");
        form.Add(new StringContent(request.FullName.Trim()), "full_name");
        form.Add(new StringContent(bandName), "name");
        form.Add(new StringContent(request.AccountType), "account_type");
        form.Add(new StringContent("daily"), "withdrawals_schedule");

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            form.Add(new StringContent(request.PhoneNumber.Trim()), "phone_number");
        }

        form.Add(new StringContent(request.HolderName.Trim()), "bank_account[holder_name]");
        form.Add(new StringContent(request.BankAccountNumber.Trim()), "bank_account[number]");
        form.Add(new StringContent(request.BankName.Trim()), "bank_account[bank_name]");
        form.Add(new StringContent(request.Currency), "bank_account[currency]");
        form.Add(new StringContent(request.BankAccountType), "bank_account[account_type]");
        form.Add(new StringContent("true"), "bank_account[is_preferred]");

        AddFile(form, request.TaxRegistrationDocument, "verification[tax_registration_document]");
        AddFile(form, request.IdCardFront, "verification[id_card_image]");
        AddFile(form, request.IdCardBack, "verification[id_card_image_back]");

        using var response = await client.PostAsync("connected_accounts", form);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Recurrente rechazó la cuenta conectada ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var content = await response.Content.ReadAsStringAsync();
        var account = JsonSerializer.Deserialize<RecurrenteConnectedAccountResponse>(content);
        if (account is null || string.IsNullOrWhiteSpace(account.Id))
        {
            throw new InvalidOperationException("Recurrente no devolvió un ID de cuenta válido.");
        }

        return account;
    }

    public async Task<RecurrenteCheckoutResponse> CreateCheckoutAsync(
        CheckoutCalculation calculation,
        DateTime? expiresAt = null,
        string? successUrl = null,
        string? cancelUrl = null)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");

        var banda = await _context.Bandas
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == calculation.BandaId);

        if (banda is null)
        {
            throw new KeyNotFoundException(
                $"No se encontr\u00f3 la banda con id {calculation.BandaId}.");
        }

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
                    name = banda.Nombre,
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

    public async Task<RecurrenteCheckoutResponse> CreateBackstageSubscriptionCheckoutAsync(
        string successUrl,
        string cancelUrl)
    {
        const int backstageMonthlyPriceCents = 9_900;

        var client = _httpClientFactory.CreateClient("Recurrente");
        var payload = new
        {
            items = new[]
            {
                new
                {
                    name = "Backstage",
                    amount_in_cents = backstageMonthlyPriceCents,
                    currency = "GTQ",
                    quantity = 1,
                    charge_type = "recurring",
                    billing_interval = "month",
                    billing_interval_count = 1,
                    free_trial_interval = "month",
                    free_trial_interval_count = 1,
                    payment_method_types = new[] { "card" }
                }
            },
            success_url = successUrl,
            cancel_url = cancelUrl
        };

        using var response = await client.PostAsJsonAsync("checkouts", payload);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Recurrente rechazó el checkout de suscripción ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var checkout = await response.Content.ReadFromJsonAsync<RecurrenteCheckoutResponse>();
        if (checkout is null || string.IsNullOrWhiteSpace(checkout.Id) ||
            string.IsNullOrWhiteSpace(checkout.CheckoutUrl))
        {
            throw new InvalidOperationException("Recurrente no devolvió un checkout válido.");
        }

        return checkout;
    }

    public async Task<RecurrenteSubscriptionResponse> GetSubscriptionAsync(string subscriptionId)
    {
        var client = _httpClientFactory.CreateClient("Recurrente");
        using var response = await client.GetAsync($"subscriptions/{Uri.EscapeDataString(subscriptionId)}");
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"No se pudo consultar la suscripción ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var subscription = await response.Content.ReadFromJsonAsync<RecurrenteSubscriptionResponse>();
        if (subscription is null || subscription.Id != subscriptionId)
        {
            throw new InvalidOperationException("Recurrente no devolvió la suscripción solicitada.");
        }

        return subscription;
    }

    private static void AddFile(MultipartFormDataContent form, IFormFile file, string fieldName)
    {
        var content = new StreamContent(file.OpenReadStream());
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        }

        form.Add(content, fieldName, Path.GetFileName(file.FileName));
    }
}
