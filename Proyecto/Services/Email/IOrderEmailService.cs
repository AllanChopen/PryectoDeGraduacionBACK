namespace Proyecto.Services.Email;

public interface IOrderEmailService
{
    Task SendProductOrderToBandAsync(int ordenId);

    Task SendProductOrderConfirmationToCustomerAsync(int ordenId);

    Task SendEventTicketsToCustomerAsync(int ordenId);
}
