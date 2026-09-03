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
public class EventosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IS3Service _s3Service;

    public EventosController(AppDbContext context, IS3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Evento>>> GetAll()
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var eventos = await _context.Eventos
            .AsNoTracking()
            .Where(evento => evento.BandaId == bandaId)
            .ToListAsync();

        return Ok(eventos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Evento>> GetById(int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var item = await _context.Eventos
            .AsNoTracking()
            .FirstOrDefaultAsync(evento =>
                evento.Id == id &&
                evento.BandaId == bandaId);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Evento>> Create([FromForm] CreateEventoDto dto)
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
                $"eventos/{bandaId}");
            imageUrl = _s3Service.GetPublicUrl(imageKey);
        }

        var item = new Evento
        {
            Uuid = Guid.NewGuid(),
            BandaId = bandaId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Fecha = dto.Fecha,
            Hora = dto.Hora,
            Ubicacion = dto.Ubicacion,
            UbicacionUrl = dto.UbicacionUrl,
            ImagenUrl = imageUrl,
            Capacidad = dto.Capacidad,
            PrecioEntrada = dto.PrecioEntrada,
            Estado = dto.Estado,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _context.Eventos.Add(item);
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
    public async Task<IActionResult> Update(int id, [FromForm] UpdateEventoDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var item = await _context.Eventos.FirstOrDefaultAsync(evento =>
            evento.Id == id &&
            evento.BandaId == bandaId);

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
                $"eventos/{bandaId}");
            item.ImagenUrl = _s3Service.GetPublicUrl(newImageKey);
        }

        item.Nombre = dto.Nombre;
        item.Descripcion = dto.Descripcion;
        item.Fecha = dto.Fecha;
        item.Hora = dto.Hora;
        item.Ubicacion = dto.Ubicacion;
        item.UbicacionUrl = dto.UbicacionUrl;
        item.Capacidad = dto.Capacidad;
        item.PrecioEntrada = dto.PrecioEntrada;
        item.Estado = dto.Estado;
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

        var item = await _context.Eventos.FirstOrDefaultAsync(evento =>
            evento.Id == id &&
            evento.BandaId == bandaId);

        if (item is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(item.ImagenUrl))
        {
            await _s3Service.DeleteByUrlAsync(item.ImagenUrl);
        }

        _context.Eventos.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    [AllowAnonymous]
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
                ImagenUrl = x.ImagenUrl,
                Capacidad = x.Capacidad,
                PrecioEntrada = x.PrecioEntrada,
                Estado = x.Estado,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(eventos);
    }

    [AllowAnonymous]
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
                ImagenUrl = x.ImagenUrl,
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

    private bool TryGetBandaId(out int bandaId)
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        return int.TryParse(bandaIdClaim, out bandaId) && bandaId > 0;
    }
}
