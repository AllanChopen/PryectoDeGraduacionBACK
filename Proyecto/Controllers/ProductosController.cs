using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Producto>>> GetAll()
    {
        // If the caller is a bandadmin, limit to their banda using the BandaId claim
        if (User?.Identity?.IsAuthenticated == true && User.IsInRole("bandadmin"))
        {
            var bandaClaim = User.FindFirst("BandaId")?.Value;
            if (int.TryParse(bandaClaim, out var bandaId) && bandaId > 0)
            {
                var list = await _context.Productos
                    .AsNoTracking()
                    .Where(p => p.BandaId == bandaId)
                    .ToListAsync();
                return Ok(list);
            }
        }

        return Ok(await _context.Productos.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var item = await _context.Productos.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create([FromBody] CreateProductoDto dto)
    {
        var item = new Producto
        {
            Uuid = Guid.NewGuid(),
            BandaId = dto.BandaId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Disponible = dto.Disponible,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.Productos.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductoDto dto)
    {
        var item = await _context.Productos.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.BandaId = dto.BandaId;
        item.Nombre = dto.Nombre;
        item.Descripcion = dto.Descripcion;
        item.Precio = dto.Precio;
        item.Disponible = dto.Disponible;
        item.Stock = dto.Stock;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Productos.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Productos.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
        
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<IEnumerable<ProductoPublicDto>>> GetPublicBySlug(string slug)
    {
        var bandaId = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var productos = await _context.Productos
            .AsNoTracking()
            .Where(x => x.BandaId == bandaId && x.Disponible)
            .Select(x => new ProductoPublicDto
            {
                Uuid = x.Uuid,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Precio = x.Precio,
                Disponible = x.Disponible,
                Stock = x.Stock,

                Variaciones = _context.ProductoVariaciones
                    .Where(v =>
                        v.ProductoId == x.Id &&
                        v.Disponible)
                    .Select(v => new ProductoVariacionDto
                    {
                        Uuid = v.Uuid,
                        Nombre = v.Nombre,
                        Precio = v.Precio,
                        Stock = v.Stock,
                        Disponible = v.Disponible,
                        Atributos = v.Atributos
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(productos);
    }
    
    [HttpGet("public/{slug}/{id:int}")]
    public async Task<ActionResult<ProductoPublicDto>> GetPublicById(
        string slug,
        int id)
    {
        var bandaId = await _context.Bandas
            .Where(x => x.Slug == slug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (bandaId == 0)
        {
            return NotFound("Banda no encontrada.");
        }

        var producto = await _context.Productos
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BandaId == bandaId &&
                x.Disponible)
            .Select(x => new ProductoPublicDto
            {
                Uuid = x.Uuid,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Precio = x.Precio,
                Disponible = x.Disponible,
                Stock = x.Stock,

                Variaciones = _context.ProductoVariaciones
                    .Where(v =>
                        v.ProductoId == x.Id &&
                        v.Disponible)
                    .Select(v => new ProductoVariacionDto
                    {
                        Uuid = v.Uuid,
                        Nombre = v.Nombre,
                        Precio = v.Precio,
                        Stock = v.Stock,
                        Disponible = v.Disponible,
                        Atributos = v.Atributos
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return producto is null
            ? NotFound("Producto no encontrado.")
            : Ok(producto);
    }

}
