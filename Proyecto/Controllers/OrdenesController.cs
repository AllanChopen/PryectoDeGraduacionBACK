using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpPost]
    public async Task<ActionResult<Orden>> Create([FromBody] Orden item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.Ordenes.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Orden item)
    {
        if (id != item.Id)
        {
            return BadRequest("El id de la ruta no coincide con el cuerpo.");
        }

        var exists = await _context.Ordenes.AnyAsync(x => x.Id == id);
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
