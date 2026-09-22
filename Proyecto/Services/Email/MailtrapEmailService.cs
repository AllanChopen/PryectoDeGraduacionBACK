using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Proyecto.Services.Email;

public class MailtrapEmailService : IEmailService
{
    private readonly MailSettings _settings;

    public MailtrapEmailService(IOptions<MailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
        => await SendAsync(to, subject, htmlBody, Array.Empty<EmailInlineImage>());

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailInlineImage> inlineImages)
    {
        ValidateSettings();

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        var builder = new BodyBuilder { HtmlBody = htmlBody };
        foreach (var image in inlineImages)
        {
            var resource = builder.LinkedResources.Add(
                "inline-image.png",
                image.Content,
                ContentType.Parse(image.ContentType));
            resource.ContentId = image.ContentId;
            resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
        }
        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail) ||
            _settings.Port <= 0)
        {
            throw new InvalidOperationException("La configuración de MailSettings está incompleta.");
        }
    }
}
