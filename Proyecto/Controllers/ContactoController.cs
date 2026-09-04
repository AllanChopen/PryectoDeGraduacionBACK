using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services.Email;
using System.Net;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactoController> _logger;

    public ContactoController(
        AppDbContext context,
        IEmailService emailService,
        ILogger<ContactoController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("public/{slug}")]
    public async Task<IActionResult> Create(string slug, [FromBody] CreateMensajeContactoDto dto)
    {
        var banda = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Id, x.Nombre, x.UsuarioId })
            .FirstOrDefaultAsync();

        if (banda is null)
        {
            return NotFound("Banda no encontrada.");
        }

        var mensajeContacto = new MensajeContacto
        {
            Uuid = Guid.NewGuid(),
            BandaId = banda.Id,
            Nombre = dto.Nombre.Trim(),
            Email = dto.Email.Trim(),
            Mensaje = dto.Mensaje.Trim(),
            Estado = "Nuevo",
            CreatedAt = DateTime.UtcNow
        };

        if (mensajeContacto.Nombre.Length == 0 || mensajeContacto.Mensaje.Length == 0)
        {
            return BadRequest("El nombre y el mensaje son obligatorios.");
        }

        _context.MensajesContacto.Add(mensajeContacto);
        await _context.SaveChangesAsync();

        var bandaEmail = await _context.Usuarios
            .Where(x => x.Id == banda.UsuarioId)
            .Select(x => x.Email)
            .FirstOrDefaultAsync();

        var notificacionEnviada = false;

        if (!string.IsNullOrWhiteSpace(bandaEmail))
        {
            try
            {
                var mensajeHtml = WebUtility.HtmlEncode(mensajeContacto.Mensaje)
                    .Replace("\r\n", "<br/>")
                    .Replace("\n", "<br/>");

                var htmlBody = $@"<!doctype html>
<html lang=""es"">
  <body style=""margin:0;padding:24px 12px;background:#0d0107;background-image:linear-gradient(140deg,#0d0107,#100418 55%,#0e213a);font-family:Inter,system-ui,sans-serif;color:#e9e4e6;"">
    <div style=""max-width:640px;margin:0 auto;background:#141018;border:1px solid rgba(167,148,148,.22);border-radius:12px;padding:22px;"">
      <p style=""margin:0 0 8px 0;color:#a79494;font-size:12px;letter-spacing:.1em;text-transform:uppercase;"">Nuevo mensaje</p>
      <h1 style=""margin:0 0 14px 0;font-size:26px;line-height:1.2;font-weight:600;color:#e9e4e6;"">Nuevo Mensaje de contacto</h1>
      <div style=""width:86px;height:3px;border-radius:4px;background:linear-gradient(115deg,#5a4252,#0e213a 58%,#a79494);margin:0 0 18px 0;""></div>

      <p style=""margin:0 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;"">Nombre</p>
      <p style=""margin:0 0 14px 0;color:#e9e4e6;font-size:15px;word-break:break-word;"">{WebUtility.HtmlEncode(mensajeContacto.Nombre)}</p>

      <p style=""margin:0 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;"">Email</p>
      <p style=""margin:0 0 14px 0;color:#e9e4e6;font-size:15px;word-break:break-word;"">{WebUtility.HtmlEncode(mensajeContacto.Email)}</p>

      <p style=""margin:0 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;"">Mensaje</p>
      <div style=""margin-top:6px;padding:14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;line-height:1.6;color:#e9e4e6;"">{mensajeHtml}</div>
    </div>
  </body>
</html>";

                await _emailService.SendAsync(
                    bandaEmail,
                    "Nuevo mensaje de contacto",
                    htmlBody);

                notificacionEnviada = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de contacto para la banda {BandaId}.", banda.Id);
            }
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            mensajeContacto.Id,
            mensajeContacto.Uuid,
            mensajeContacto.Estado,
            notificacionEnviada
        });
    }
}
