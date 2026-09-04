namespace Proyecto.Services.Email;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}
