using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Usuario>>> GetAll()
    {
        return Ok(await _context.Usuarios.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Usuario>> GetById(int id)
    {
        var item = await _context.Usuarios.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create([FromBody] Usuario item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.Usuarios.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Usuario item)
    {
        if (id != item.Id)
        {
            return BadRequest("El id de la ruta no coincide con el cuerpo.");
        }

        var exists = await _context.Usuarios.AnyAsync(x => x.Id == id);
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
        var item = await _context.Usuarios.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Usuarios.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
