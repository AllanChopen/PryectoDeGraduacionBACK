using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<Banda>> Create([FromBody] CreateBandaDto dto)
    {
        var item = new Banda
        {
            Uuid = Guid.NewGuid(),
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Biografia = dto.Biografia,
            ImagenUrl = dto.ImagenUrl,
            BiografiaImagenUrl = dto.BiografiaImagenUrl,
            Genero = dto.Genero,
            UsuarioId = dto.UsuarioId,
            Slug = dto.Slug,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.Bandas.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBandaDto dto)
    {
        var item = await _context.Bandas.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Nombre = dto.Nombre;
        item.Descripcion = dto.Descripcion;
        item.Biografia = dto.Biografia;
        item.ImagenUrl = dto.ImagenUrl;
        item.BiografiaImagenUrl = dto.BiografiaImagenUrl;
        item.Genero = dto.Genero;
        item.UsuarioId = dto.UsuarioId;
        item.Slug = dto.Slug;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
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

    [AllowAnonymous]
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
                    BiografiaImagenUrl = x.BiografiaImagenUrl,
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
