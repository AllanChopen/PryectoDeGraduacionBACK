using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublicacionesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicacionesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Publicacion>>> GetAll()
    {
        return Ok(await _context.Publicaciones.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Publicacion>> GetById(int id)
    {
        var item = await _context.Publicaciones.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Publicacion>> Create([FromBody] Publicacion item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.Publicaciones.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Publicacion item)
    {
        if (id != item.Id)
        {
            return BadRequest("El id de la ruta no coincide con el cuerpo.");
        }

        var exists = await _context.Publicaciones.AnyAsync(x => x.Id == id);
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
        var item = await _context.Publicaciones.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Publicaciones.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("public/{slug}")]
    public async Task<ActionResult<IEnumerable<PublicacionDto>>> GetPublicBySlug(string slug)
    {
        var bandaId = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var publicaciones = await _context.Publicaciones
            .AsNoTracking()
            .Where(x => x.BandaId == bandaId && x.Estado == "publicada")
            .OrderByDescending(x => x.FechaPublicacion)
            .Select(x => new PublicacionDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                BandaId = x.BandaId,
                Titulo = x.Titulo,
                Contenido = x.Contenido,
                ImagenUrl = x.ImagenUrl,
                FechaPublicacion = x.FechaPublicacion,
                Estado = x.Estado,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(publicaciones);
    }

    [HttpGet("public/{slug}/{id:int}")]
    public async Task<ActionResult<PublicacionDto>> GetPublicById(string slug, int id)
    {
        var bandaId = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var publicacion = await _context.Publicaciones
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BandaId == bandaId &&
                x.Estado == "publicada")
            .Select(x => new PublicacionDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                BandaId = x.BandaId,
                Titulo = x.Titulo,
                Contenido = x.Contenido,
                ImagenUrl = x.ImagenUrl,
                FechaPublicacion = x.FechaPublicacion,
                Estado = x.Estado,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return publicacion is null
            ? NotFound("Publicación no encontrada.")
            : Ok(publicacion);
    }
}
