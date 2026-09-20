using Microsoft.AspNetCore.Http;

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
}
