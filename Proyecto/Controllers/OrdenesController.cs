using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdenesController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdenesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Orden>>> GetAll()
    {
        return Ok(await _context.Ordenes.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Orden>> GetById(int id)
    {
        var item = await _context.Ordenes.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("checkout/{checkoutId}")]
    public async Task<IActionResult> GetByCheckout(string checkoutId)
    {
        var orden = await _context.Ordenes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.RecurrenteCheckoutId == checkoutId);

        if (orden is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Orden no encontrada."
            });
        }

        if (orden.EventoId.HasValue)
        {
            return BadRequest(new
            {
                success = false,
                message = "Este checkout corresponde a una compra de evento."
            });
        }

        var detalles = await _context.OrdenesDetalle
            .AsNoTracking()
            .Where(d => d.OrdenId == orden.Id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            estadoPago = orden.EstadoPago,
            orden = new
            {
                orden.Id,
                orden.Uuid,
                orden.CompradorNombre,
                orden.CompradorEmail,
                orden.CompradorTelefono,
                orden.DireccionEnvio,
                orden.Subtotal,
                orden.CostoEnvio,
                orden.TarifaServicio,
                orden.Total,
                orden.Estado,
                orden.EstadoPago
            },
            productos = detalles.Select(d => new
            {
                d.Uuid,
                d.ProductoNombre,
                d.VariacionNombre,
                d.ImagenUrl,
                d.Cantidad,
                d.PrecioUnitario,
                d.Subtotal
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<Orden>> Create([FromBody] CreateOrdenDto dto)
    {
        var item = new Orden
        {
            Uuid = Guid.NewGuid(),
            UsuarioId = dto.UsuarioId,
            Estado = dto.Estado,
            Total = dto.Total,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.Ordenes.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrdenDto dto)
    {
        var item = await _context.Ordenes.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.UsuarioId = dto.UsuarioId;
        item.Estado = dto.Estado;
        item.Total = dto.Total;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Ordenes.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Ordenes.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
