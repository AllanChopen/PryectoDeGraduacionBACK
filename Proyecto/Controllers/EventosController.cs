using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Evento>>> GetAll()
    {
        return Ok(await _context.Eventos.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Evento>> GetById(int id)
    {
        var item = await _context.Eventos.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Evento>> Create([FromBody] Evento item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.Eventos.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Evento item)
    {
        if (id != item.Id)
        {
            return BadRequest("El id de la ruta no coincide con el cuerpo.");
        }

        var exists = await _context.Eventos.AnyAsync(x => x.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        item.UpdatedAt = DateTime.UtcNow;
        _context.Entry(item).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Eventos.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Eventos.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<IEnumerable<EventoDto>>> GetPublicBySlug(string slug)
    {
        var bandaId = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var eventos = await _context.Eventos
            .AsNoTracking()
            .Where(x => x.BandaId == bandaId && x.Estado == "programado")
            .OrderBy(x => x.Fecha)
            .Select(x => new EventoDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                BandaId = x.BandaId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Fecha = x.Fecha,
                Hora = x.Hora,
                Ubicacion = x.Ubicacion,
                UbicacionUrl = x.UbicacionUrl,
                Capacidad = x.Capacidad,
                PrecioEntrada = x.PrecioEntrada,
                Estado = x.Estado,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(eventos);
    }

    [HttpGet("public/{slug}/{id:int}")]
    public async Task<ActionResult<EventoDto>> GetPublicById(string slug, int id)
    {
        var bandaId = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var evento = await _context.Eventos
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BandaId == bandaId &&
                x.Estado == "programado")
            .Select(x => new EventoDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                BandaId = x.BandaId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Fecha = x.Fecha,
                Hora = x.Hora,
                Ubicacion = x.Ubicacion,
                UbicacionUrl = x.UbicacionUrl,
                Capacidad = x.Capacidad,
                PrecioEntrada = x.PrecioEntrada,
                Estado = x.Estado,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return evento is null
            ? NotFound("Evento no encontrado.")
            : Ok(evento);
    }
}
