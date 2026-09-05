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
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IS3Service _s3Service;

    public ProductosController(AppDbContext context, IS3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductoDto>>> GetAll()
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var productos = await _context.Productos
            .AsNoTracking()
            .Where(producto => producto.BandaId == bandaId)
            .Select(producto => new ProductoDto
            {
                Id = producto.Id,
                Uuid = producto.Uuid,
                BandaId = producto.BandaId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Disponible = producto.Disponible,
                Stock = producto.Stock,
                TieneTalla = producto.TieneTalla,
                ImagenUrl = producto.ImagenUrl,
                CreatedAt = producto.CreatedAt,
                UpdatedAt = producto.UpdatedAt,
                Variaciones = _context.ProductoVariaciones
                    .Where(variacion => variacion.ProductoId == producto.Id)
                    .OrderBy(variacion => variacion.Id)
                    .Select(variacion => new ProductoVariacionDto
                    {
                        Id = variacion.Id,
                        Uuid = variacion.Uuid,
                        ProductoId = variacion.ProductoId,
                        Nombre = variacion.Nombre,
                        Stock = variacion.Stock,
                        Disponible = variacion.Disponible,
                        CreatedAt = variacion.CreatedAt,
                        UpdatedAt = variacion.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(productos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoDto>> GetById(int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var producto = await _context.Productos
            .AsNoTracking()
            .Where(item => item.Id == id && item.BandaId == bandaId)
            .Select(item => new ProductoDto
            {
                Id = item.Id,
                Uuid = item.Uuid,
                BandaId = item.BandaId,
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                Precio = item.Precio,
                Disponible = item.Disponible,
                Stock = item.Stock,
                TieneTalla = item.TieneTalla,
                ImagenUrl = item.ImagenUrl,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Variaciones = _context.ProductoVariaciones
                    .Where(variacion => variacion.ProductoId == item.Id)
                    .OrderBy(variacion => variacion.Id)
                    .Select(variacion => new ProductoVariacionDto
                    {
                        Id = variacion.Id,
                        Uuid = variacion.Uuid,
                        ProductoId = variacion.ProductoId,
                        Nombre = variacion.Nombre,
                        Stock = variacion.Stock,
                        Disponible = variacion.Disponible,
                        CreatedAt = variacion.CreatedAt,
                        UpdatedAt = variacion.UpdatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return producto is null ? NotFound("Producto no encontrado.") : Ok(producto);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Producto>> Create([FromForm] CreateProductoDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        if (!dto.TieneTalla && (dto.Stock is null || dto.Stock < 0))
        {
            return BadRequest("El producto debe tener stock.");
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
                $"productos/{bandaId}");
            imageUrl = _s3Service.GetPublicUrl(imageKey);
        }

        var producto = new Producto
        {
            Uuid = Guid.NewGuid(),
            BandaId = bandaId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Disponible = dto.Disponible,
            Stock = dto.TieneTalla ? null : dto.Stock,
            TieneTalla = dto.TieneTalla,
            ImagenUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _context.Productos.Add(producto);
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

        return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductoDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var producto = await _context.Productos.FirstOrDefaultAsync(item =>
            item.Id == id &&
            item.BandaId == bandaId);

        if (producto is null)
        {
            return NotFound("Producto no encontrado.");
        }

        if (!dto.TieneTalla && (dto.Stock is null || dto.Stock < 0))
        {
            return BadRequest("El producto debe tener stock.");
        }

        var imageError = ImageFileValidator.Validate(dto.Imagen);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        var previousImageUrl = producto.ImagenUrl;
        string? newImageKey = null;

        if (dto.Imagen is not null)
        {
            await using var stream = dto.Imagen.OpenReadStream();
            newImageKey = await _s3Service.UploadAsync(
                stream,
                Path.GetFileName(dto.Imagen.FileName),
                dto.Imagen.ContentType,
                $"productos/{bandaId}");
            producto.ImagenUrl = _s3Service.GetPublicUrl(newImageKey);
        }

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Disponible = dto.Disponible;
        producto.Stock = dto.TieneTalla ? null : dto.Stock;
        producto.TieneTalla = dto.TieneTalla;
        producto.UpdatedAt = DateTime.UtcNow;

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

        var producto = await _context.Productos.FirstOrDefaultAsync(item =>
            item.Id == id &&
            item.BandaId == bandaId);

        if (producto is null)
        {
            return NotFound("Producto no encontrado.");
        }

        if (!string.IsNullOrWhiteSpace(producto.ImagenUrl))
        {
            await _s3Service.DeleteByUrlAsync(producto.ImagenUrl);
        }

        var variaciones = await _context.ProductoVariaciones
            .Where(variacion => variacion.ProductoId == producto.Id)
            .ToListAsync();

        _context.ProductoVariaciones.RemoveRange(variaciones);
        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<IEnumerable<ProductoPublicDto>>> GetPublicBySlug(string slug)
    {
        var bandaId = await _context.Bandas
            .Where(banda => banda.Slug == slug)
            .Select(banda => banda.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var productos = await _context.Productos
            .AsNoTracking()
            .Where(producto => producto.BandaId == bandaId && producto.Disponible)
            .Select(producto => new ProductoPublicDto
            {
                Uuid = producto.Uuid,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Disponible = producto.Disponible,
                Stock = producto.Stock,
                TieneTalla = producto.TieneTalla,
                ImagenUrl = producto.ImagenUrl,
                Variaciones = _context.ProductoVariaciones
                    .Where(variacion =>
                        variacion.ProductoId == producto.Id &&
                        variacion.Disponible)
                    .OrderBy(variacion => variacion.Id)
                    .Select(variacion => new ProductoVariacionDto
                    {
                        Id = variacion.Id,
                        Uuid = variacion.Uuid,
                        ProductoId = variacion.ProductoId,
                        Nombre = variacion.Nombre,
                        Stock = variacion.Stock,
                        Disponible = variacion.Disponible,
                        CreatedAt = variacion.CreatedAt,
                        UpdatedAt = variacion.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(productos);
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}/{id:int}")]
    public async Task<ActionResult<ProductoPublicDto>> GetPublicById(string slug, int id)
    {
        var bandaId = await _context.Bandas
            .Where(banda => banda.Slug == slug)
            .Select(banda => banda.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var producto = await _context.Productos
            .AsNoTracking()
            .Where(item =>
                item.Id == id &&
                item.BandaId == bandaId &&
                item.Disponible)
            .Select(item => new ProductoPublicDto
            {
                Uuid = item.Uuid,
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                Precio = item.Precio,
                Disponible = item.Disponible,
                Stock = item.Stock,
                TieneTalla = item.TieneTalla,
                ImagenUrl = item.ImagenUrl,
                Variaciones = _context.ProductoVariaciones
                    .Where(variacion =>
                        variacion.ProductoId == item.Id &&
                        variacion.Disponible)
                    .OrderBy(variacion => variacion.Id)
                    .Select(variacion => new ProductoVariacionDto
                    {
                        Id = variacion.Id,
                        Uuid = variacion.Uuid,
                        ProductoId = variacion.ProductoId,
                        Nombre = variacion.Nombre,
                        Stock = variacion.Stock,
                        Disponible = variacion.Disponible,
                        CreatedAt = variacion.CreatedAt,
                        UpdatedAt = variacion.UpdatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return producto is null ? NotFound("Producto no encontrado.") : Ok(producto);
    }

    private bool TryGetBandaId(out int bandaId)
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        return int.TryParse(bandaIdClaim, out bandaId) && bandaId > 0;
    }
}
