using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdenesDetalleController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdenesDetalleController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrdenDetalle>>> GetAll()
    {
        return Ok(await _context.OrdenesDetalle.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdenDetalle>> GetById(int id)
    {
        var item = await _context.OrdenesDetalle.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<OrdenDetalle>> Create([FromBody] CreateOrdenDetalleDto dto)
    {
        var item = new OrdenDetalle
        {
            Uuid = Guid.NewGuid(),
            OrdenId = dto.OrdenId,
            ProductoId = dto.ProductoId,
            Cantidad = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario,
            Subtotal = dto.Subtotal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrdenesDetalle.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrdenDetalleDto dto)
    {
        var item = await _context.OrdenesDetalle.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.OrdenId = dto.OrdenId;
        item.ProductoId = dto.ProductoId;
        item.Cantidad = dto.Cantidad;
        item.PrecioUnitario = dto.PrecioUnitario;
        item.Subtotal = dto.Subtotal;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.OrdenesDetalle.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.OrdenesDetalle.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
