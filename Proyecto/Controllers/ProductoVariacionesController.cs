using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/Productos/{productoId:int}/variaciones")]
[Authorize(Roles = "bandadmin")]
public class ProductoVariacionesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductoVariacionesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductoVariacionDto>>> GetAll(int productoId)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        if (!await ProductBelongsToBand(productoId, bandaId))
        {
            return NotFound("Producto no encontrado.");
        }

        var variaciones = await _context.ProductoVariaciones
            .AsNoTracking()
            .Where(variacion => variacion.ProductoId == productoId)
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
            .ToListAsync();

        return Ok(variaciones);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoVariacionDto>> GetById(int productoId, int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        if (!await ProductBelongsToBand(productoId, bandaId))
        {
            return NotFound("Producto no encontrado.");
        }

        var variacion = await _context.ProductoVariaciones
            .AsNoTracking()
            .Where(item => item.Id == id && item.ProductoId == productoId)
            .Select(item => new ProductoVariacionDto
            {
                Id = item.Id,
                Uuid = item.Uuid,
                ProductoId = item.ProductoId,
                Nombre = item.Nombre,
                Stock = item.Stock,
                Disponible = item.Disponible,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return variacion is null ? NotFound("Variación no encontrada.") : Ok(variacion);
    }

    [HttpPost]
    public async Task<ActionResult<ProductoVariacionDto>> Create(
        int productoId,
        [FromBody] CreateProductoVariacionDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        if (!await ProductBelongsToBand(productoId, bandaId))
        {
            return NotFound("Producto no encontrado.");
        }

        var variacion = new ProductoVariacion
        {
            Uuid = Guid.NewGuid(),
            ProductoId = productoId,
            Nombre = dto.Nombre,
            Stock = dto.Stock,
            Disponible = dto.Disponible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.ProductoVariaciones.Add(variacion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { productoId, id = variacion.Id },
            ToDto(variacion));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int productoId,
        int id,
        [FromBody] UpdateProductoVariacionDto dto)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        if (!await ProductBelongsToBand(productoId, bandaId))
        {
            return NotFound("Producto no encontrado.");
        }

        var variacion = await _context.ProductoVariaciones
            .FirstOrDefaultAsync(item => item.Id == id && item.ProductoId == productoId);

        if (variacion is null)
        {
            return NotFound("Variación no encontrada.");
        }

        variacion.Nombre = dto.Nombre;
        variacion.Stock = dto.Stock;
        variacion.Disponible = dto.Disponible;
        variacion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int productoId, int id)
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        if (!await ProductBelongsToBand(productoId, bandaId))
        {
            return NotFound("Producto no encontrado.");
        }

        var variacion = await _context.ProductoVariaciones
            .FirstOrDefaultAsync(item => item.Id == id && item.ProductoId == productoId);

        if (variacion is null)
        {
            return NotFound("Variación no encontrada.");
        }

        _context.ProductoVariaciones.Remove(variacion);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private Task<bool> ProductBelongsToBand(int productoId, int bandaId)
    {
        return _context.Productos.AnyAsync(producto =>
            producto.Id == productoId &&
            producto.BandaId == bandaId);
    }

    private bool TryGetBandaId(out int bandaId)
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        return int.TryParse(bandaIdClaim, out bandaId) && bandaId > 0;
    }

    private static ProductoVariacionDto ToDto(ProductoVariacion variacion)
    {
        return new ProductoVariacionDto
        {
            Id = variacion.Id,
            Uuid = variacion.Uuid,
            ProductoId = variacion.ProductoId,
            Nombre = variacion.Nombre,
            Stock = variacion.Stock,
            Disponible = variacion.Disponible,
            CreatedAt = variacion.CreatedAt,
            UpdatedAt = variacion.UpdatedAt
        };
    }
}
