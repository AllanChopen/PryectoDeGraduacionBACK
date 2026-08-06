using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<ActionResult<OrdenDetalle>> Create([FromBody] OrdenDetalle item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.OrdenesDetalle.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrdenDetalle item)
    {
        if (id != item.Id)
        {
            return BadRequest("El id de la ruta no coincide con el cuerpo.");
        }

        var exists = await _context.OrdenesDetalle.AnyAsync(x => x.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        item.UpdatedAt = DateTime.UtcNow;
        _context.Entry(item).State = EntityState.Modified;
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
