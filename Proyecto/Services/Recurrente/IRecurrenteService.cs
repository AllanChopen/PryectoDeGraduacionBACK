using Microsoft.AspNetCore.Http;
using Proyecto.Models.Recurrente;
using Proyecto.Services.Checkout;

namespace Proyecto.Services.Recurrente;

public interface IRecurrenteService
{
    Task<string> TestConnectionAsync();

    Task<string> CreateConnectedAccountAsync(
        int bandaId,
        string email,
        string fullName,
        string phoneNumber,
        string name,
        IFormFile taxRegistrationDocument,
        IFormFile idCardImage,
        IFormFile idCardImageBack
    );

    Task<RecurrenteCheckoutResponse> CreateCheckoutAsync(
        CheckoutCalculation calculation,
        DateTime? expiresAt = null);

    Task<RecurrenteCheckoutResponse> GetCheckoutAsync(
        string checkoutId);
}
