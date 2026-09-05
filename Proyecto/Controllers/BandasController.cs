using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.DTOs;
using PryectoDeGraduacionBACK.Services.S3;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BandasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IS3Service _s3Service;

    public BandasController(AppDbContext context, IS3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
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
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Banda>> Create([FromForm] CreateBandaDto dto)
    {
        var imageError = ImageFileValidator.Validate(dto.Imagen);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        var bioImageError = ImageFileValidator.Validate(dto.BiografiaImagen);
        if (bioImageError is not null)
        {
            return BadRequest(bioImageError);
        }

        string? imageKey = null;
        string? imageUrl = dto.ImagenUrl;

        if (dto.Imagen is not null)
        {
            await using var stream = dto.Imagen.OpenReadStream();
            imageKey = await _s3Service.UploadAsync(
                stream,
                Path.GetFileName(dto.Imagen.FileName),
                dto.Imagen.ContentType,
                "bandas");
            imageUrl = _s3Service.GetPublicUrl(imageKey);
        }

        string? bioImageKey = null;
        string? bioImageUrl = dto.BiografiaImagenUrl;

        if (dto.BiografiaImagen is not null)
        {
            await using var stream = dto.BiografiaImagen.OpenReadStream();
            bioImageKey = await _s3Service.UploadAsync(
                stream,
                Path.GetFileName(dto.BiografiaImagen.FileName),
                dto.BiografiaImagen.ContentType,
                "bandas");
            bioImageUrl = _s3Service.GetPublicUrl(bioImageKey);
        }

        var item = new Banda
        {
            Uuid = Guid.NewGuid(),
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Biografia = dto.Biografia,
            ImagenUrl = imageUrl,
            BiografiaImagenUrl = bioImageUrl,
            Genero = dto.Genero,
            UsuarioId = dto.UsuarioId,
            Slug = dto.Slug,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _context.Bandas.Add(item);
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (imageKey is not null)
            {
                await _s3Service.DeleteAsync(imageKey);
            }
            if (bioImageKey is not null)
            {
                await _s3Service.DeleteAsync(bioImageKey);
            }
            throw;
        }

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateBandaDto dto)
    {
        var item = await _context.Bandas.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var imageError = ImageFileValidator.Validate(dto.Imagen);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        var bioImageError = ImageFileValidator.Validate(dto.BiografiaImagen);
        if (bioImageError is not null)
        {
            return BadRequest(bioImageError);
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
                "bandas");
            item.ImagenUrl = _s3Service.GetPublicUrl(newImageKey);
        }
        else if (dto.ImagenUrl is not null)
        {
            item.ImagenUrl = dto.ImagenUrl;
        }

        var previousBioImageUrl = item.BiografiaImagenUrl;
        string? newBioImageKey = null;

        if (dto.BiografiaImagen is not null)
        {
            await using var stream = dto.BiografiaImagen.OpenReadStream();
            newBioImageKey = await _s3Service.UploadAsync(
                stream,
                Path.GetFileName(dto.BiografiaImagen.FileName),
                dto.BiografiaImagen.ContentType,
                "bandas");
            item.BiografiaImagenUrl = _s3Service.GetPublicUrl(newBioImageKey);
        }
        else if (dto.BiografiaImagenUrl is not null)
        {
            item.BiografiaImagenUrl = dto.BiografiaImagenUrl;
        }

        item.Nombre = dto.Nombre;
        item.Descripcion = dto.Descripcion;
        item.Biografia = dto.Biografia;
        item.Genero = dto.Genero;
        item.UsuarioId = dto.UsuarioId;
        item.Slug = dto.Slug;
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
            if (newBioImageKey is not null)
            {
                await _s3Service.DeleteAsync(newBioImageKey);
            }
            throw;
        }

        if (newImageKey is not null && !string.IsNullOrWhiteSpace(previousImageUrl))
        {
            await _s3Service.DeleteByUrlAsync(previousImageUrl);
        }

        if (newBioImageKey is not null && !string.IsNullOrWhiteSpace(previousBioImageUrl))
        {
            await _s3Service.DeleteByUrlAsync(previousBioImageUrl);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Bandas.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Bandas.Remove(item);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(item.ImagenUrl))
        {
            await _s3Service.DeleteByUrlAsync(item.ImagenUrl);
        }
        if (!string.IsNullOrWhiteSpace(item.BiografiaImagenUrl))
        {
            await _s3Service.DeleteByUrlAsync(item.BiografiaImagenUrl);
        }

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
