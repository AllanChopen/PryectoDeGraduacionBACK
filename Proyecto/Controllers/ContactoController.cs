using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactoController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContactoController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpPost("public/{slug}")]
    public async Task<IActionResult> Create(string slug, [FromBody] CreateMensajeContactoDto dto)
    {
        var bandaId = await _context.Bandas
            .Where(banda => banda.Slug == slug)
            .Select(banda => banda.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var mensajeContacto = new MensajeContacto
        {
            Uuid = Guid.NewGuid(),
            BandaId = bandaId,
            Nombre = dto.Nombre.Trim(),
            Email = dto.Email.Trim(),
            Mensaje = dto.Mensaje.Trim(),
            Estado = "nuevo",
            CreatedAt = DateTime.UtcNow
        };

        if (mensajeContacto.Nombre.Length == 0 || mensajeContacto.Mensaje.Length == 0)
        {
            return BadRequest("El nombre y el mensaje son obligatorios.");
        }

        _context.MensajesContacto.Add(mensajeContacto);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            mensajeContacto.Id,
            mensajeContacto.Uuid,
            mensajeContacto.Estado
        });
    }
}
