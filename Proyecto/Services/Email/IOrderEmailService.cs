namespace Proyecto.Services.Email;

public interface IOrderEmailService
{
    Task SendProductOrderToBandAsync(int ordenId);

    Task SendProductOrderConfirmationToCustomerAsync(int ordenId);

    // Después:
    // Task SendEventTicketsToCustomerAsync(int ordenId);
}
