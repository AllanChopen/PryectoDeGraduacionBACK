using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "bandadmin")]
public class RedSocialController : ControllerBase
{
    private readonly AppDbContext _context;

    public RedSocialController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RedSocialDto>>> GetAll()
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var redes = await _context.RedesSociales
            .AsNoTracking()
            .Where(x => x.BandaId == bandaId)
            .OrderBy(x => x.Id)
            .Select(x => new RedSocialDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                Tipo = x.Tipo,
                Nombre = x.Nombre,
                Url = x.Url
            })
            .ToListAsync();

        return Ok(redes);
    }

    [HttpPost]
    public async Task<ActionResult<RedSocialDto>> Create([FromBody] CreateRedSocialDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var red = new RedSocial
        {
            Uuid = Guid.NewGuid(),
            BandaId = bandaId,
            Tipo = dto.Tipo.Trim(),
            Nombre = dto.Nombre.Trim(),
            Url = dto.Url.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RedesSociales.Add(red);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(red));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRedSocialDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var red = await _context.RedesSociales
            .FirstOrDefaultAsync(x => x.Id == id && x.BandaId == bandaId);

        if (red is null)
        {
            return NotFound("Red social no encontrada.");
        }

        red.Tipo = dto.Tipo.Trim();
        red.Nombre = dto.Nombre.Trim();
        red.Url = dto.Url.Trim();
        red.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var red = await _context.RedesSociales
            .FirstOrDefaultAsync(x => x.Id == id && x.BandaId == bandaId);

        if (red is null)
        {
            return NotFound("Red social no encontrada.");
        }

        _context.RedesSociales.Remove(red);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<IEnumerable<RedSocialDto>>> GetPublicBySlug(string slug)
    {
        var bandaId = await _context.Bandas
            .Where(banda => banda.Slug == slug)
            .Select(banda => banda.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var redes = await _context.RedesSociales
            .AsNoTracking()
            .Where(x => x.BandaId == bandaId)
            .OrderBy(x => x.Id)
            .Select(x => new RedSocialDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                Tipo = x.Tipo,
                Nombre = x.Nombre,
                Url = x.Url
            })
            .ToListAsync();

        return Ok(redes);
    }

    private bool TryGetBandaId(out int bandaId)
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        return int.TryParse(bandaIdClaim, out bandaId) && bandaId > 0;
    }

    private static RedSocialDto ToDto(RedSocial red)
    {
        return new RedSocialDto
        {
            Id = red.Id,
            Uuid = red.Uuid,
            Tipo = red.Tipo,
            Nombre = red.Nombre,
            Url = red.Url
        };
    }
}
