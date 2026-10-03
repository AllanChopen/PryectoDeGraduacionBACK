using Microsoft.AspNetCore.Http;
using Proyecto.Models.Recurrente;
using Proyecto.Services.Checkout;

namespace Proyecto.Services.Recurrente;

public interface IRecurrenteService
{
    Task<string> TestConnectionAsync();

    Task<IReadOnlyList<RecurrenteBankDto>> GetBanksAsync(CancellationToken cancellationToken = default);

    Task<RecurrenteConnectedAccountResponse> CreateConnectedAccountAsync(
        string bandName,
        ConnectedAccountRequest request);

    Task<RecurrenteCheckoutResponse> CreateCheckoutAsync(
        CheckoutCalculation calculation,
        DateTime? expiresAt = null,
        string? successUrl = null,
        string? cancelUrl = null);

    Task<RecurrenteCheckoutResponse> GetCheckoutAsync(
        string checkoutId);

    Task<RecurrenteCheckoutResponse> CreateBackstageSubscriptionCheckoutAsync(
        string successUrl,
        string cancelUrl);

    Task<RecurrenteSubscriptionResponse> GetSubscriptionAsync(string subscriptionId);
}
