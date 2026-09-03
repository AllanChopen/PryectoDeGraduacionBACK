using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;
using PryectoDeGraduacionBACK.Services.S3;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "bandadmin")]
public class PublicacionesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IS3Service _s3Service;

    public PublicacionesController(AppDbContext context, IS3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Publicacion>>> GetAll()
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var publicaciones = await _context.Publicaciones
            .AsNoTracking()
            .Where(publicacion => publicacion.BandaId == bandaId)
            .ToListAsync();

        return Ok(publicaciones);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Publicacion>> GetById(int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var item = await _context.Publicaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(publicacion =>
                publicacion.Id == id &&
                publicacion.BandaId == bandaId);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Publicacion>> Create([FromForm] CreatePublicacionDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var imageError = ImageFileValidator.Validate(dto.Imagen);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        string? imageKey = null;
        string? imageUrl = null;

        if (dto.Imagen is not null)
        {
            await using var stream = dto.Imagen.OpenReadStream();
            imageKey = await _s3Service.UploadAsync(
                stream,
                Path.GetFileName(dto.Imagen.FileName),
                dto.Imagen.ContentType,
                $"publicaciones/{bandaId}");
            imageUrl = _s3Service.GetPublicUrl(imageKey);
        }

        var item = new Publicacion
        {
            Uuid = Guid.NewGuid(),
            BandaId = bandaId,
            Titulo = dto.Titulo,
            Contenido = dto.Contenido,
            ImagenUrl = imageUrl,
            FechaPublicacion = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _context.Publicaciones.Add(item);
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (imageKey is not null)
            {
                await _s3Service.DeleteAsync(imageKey);
            }

            throw;
        }

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdatePublicacionDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var item = await _context.Publicaciones.FirstOrDefaultAsync(publicacion =>
            publicacion.Id == id &&
            publicacion.BandaId == bandaId);

        if (item is null)
        {
            return NotFound();
        }

        var imageError = ImageFileValidator.Validate(dto.Imagen);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        var previousImageUrl = item.ImagenUrl;
        string? newImageKey = null;

        if (dto.Imagen is not null)
        {
            await using var stream = dto.Imagen.OpenReadStream();
            newImageKey = await _s3Service.UploadAsync(
                stream,
                Path.GetFileName(dto.Imagen.FileName),
                dto.Imagen.ContentType,
                $"publicaciones/{bandaId}");
            item.ImagenUrl = _s3Service.GetPublicUrl(newImageKey);
        }

        item.Titulo = dto.Titulo;
        item.Contenido = dto.Contenido;
        item.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (newImageKey is not null)
            {
                await _s3Service.DeleteAsync(newImageKey);
            }

            throw;
        }

        if (newImageKey is not null && !string.IsNullOrWhiteSpace(previousImageUrl))
        {
            await _s3Service.DeleteByUrlAsync(previousImageUrl);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var item = await _context.Publicaciones.FirstOrDefaultAsync(publicacion =>
            publicacion.Id == id &&
            publicacion.BandaId == bandaId);

        if (item is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(item.ImagenUrl))
        {
            await _s3Service.DeleteByUrlAsync(item.ImagenUrl);
        }

        _context.Publicaciones.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [AllowAnonymous]
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
            .Where(x => x.BandaId == bandaId)
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
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(publicaciones);
    }

    [AllowAnonymous]
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
                x.BandaId == bandaId)
            .Select(x => new PublicacionDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                BandaId = x.BandaId,
                Titulo = x.Titulo,
                Contenido = x.Contenido,
                ImagenUrl = x.ImagenUrl,
                FechaPublicacion = x.FechaPublicacion,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return publicacion is null
            ? NotFound("Publicación no encontrada.")
            : Ok(publicacion);
    }

    private bool TryGetBandaId(out int bandaId)
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        return int.TryParse(bandaIdClaim, out bandaId) && bandaId > 0;
    }

}
