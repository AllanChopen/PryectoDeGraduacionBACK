using Microsoft.AspNetCore.Mvc;
using Proyecto.DTOs;
using Proyecto.Services.Email;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTest([FromBody] SendTestEmailDto dto)
    {
        await _emailService.SendAsync(
            dto.To,
            "Correo de prueba",
            "<h1>Mailtrap configurado correctamente</h1><p>Este es un correo de prueba desde la API.</p>");

        return Ok(new
        {
            message = "Correo de prueba enviado."
        });
    }
}
