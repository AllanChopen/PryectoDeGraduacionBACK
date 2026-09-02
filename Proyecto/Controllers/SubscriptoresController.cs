using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubscriptoresController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subscriptor>>> GetAll()
    {
        return Ok(await _context.Subscriptores.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Subscriptor>> GetById(int id)
    {
        var item = await _context.Subscriptores.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Subscriptor>> Create([FromBody] CreateSubscriptorDto dto)
    {
        var item = new Subscriptor
        {
            Uuid = Guid.NewGuid(),
            BandaId = dto.BandaId,
            UsuarioId = dto.UsuarioId,
            TipoSuscripcion = dto.TipoSuscripcion,
            Estado = dto.Estado,
            FechaInicio = DateTime.UtcNow,
            FechaFin = null,
            SuscripcionEn = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subscriptores.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubscriptorDto dto)
    {
        var item = await _context.Subscriptores.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.BandaId = dto.BandaId;
        item.UsuarioId = dto.UsuarioId;
        item.TipoSuscripcion = dto.TipoSuscripcion;
        item.Estado = dto.Estado;
        item.FechaFin = dto.FechaFin;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Subscriptores.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Subscriptores.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
