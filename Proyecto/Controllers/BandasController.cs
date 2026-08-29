using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.DTOs;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BandasController : ControllerBase
{
    private readonly AppDbContext _context;

    public BandasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Banda>>> GetAll()
    {
        return Ok(await _context.Bandas.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Banda>> GetById(int id)
    {
        var item = await _context.Bandas.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Banda>> Create([FromBody] Banda item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.Bandas.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Banda item)
    {
        if (id != item.Id)
        {
            return BadRequest("El id de la ruta no coincide con el cuerpo.");
        }

        var exists = await _context.Bandas.AnyAsync(x => x.Id == id);
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
        var item = await _context.Bandas.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Bandas.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("public/{slug}")]
    public async Task<ActionResult<BandaDto>> GetPublicBySlug(string slug)
    {
        try
        {
            var banda = await _context.Bandas
                .AsNoTracking()
                .Where(x => x.Slug == slug)
                .Select(x => new BandaDto
                {
                    Uuid = x.Uuid,
                    Nombre = x.Nombre,
                    Slug = x.Slug,
                    Descripcion = x.Descripcion,
                    Biografia = x.Biografia,
                    ImagenUrl = x.ImagenUrl,
                    Genero = x.Genero
                })
                .FirstOrDefaultAsync();

            return banda is null ? NotFound() : Ok(banda);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message,
                innerException = ex.InnerException?.Message,
                innerInnerException = ex.InnerException?.InnerException?.Message
            });
        }
    }
}
