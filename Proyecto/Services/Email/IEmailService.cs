namespace Proyecto.Services.Email;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);

    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailInlineImage> inlineImages);
}
