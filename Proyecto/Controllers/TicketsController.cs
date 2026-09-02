using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ticket>>> GetAll()
    {
        return Ok(await _context.Tickets.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Ticket>> GetById(int id)
    {
        var item = await _context.Tickets.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Ticket>> Create([FromBody] CreateTicketDto dto)
    {
        var item = new Ticket
        {
            Uuid = Guid.NewGuid(),
            EventoId = dto.EventoId,
            OrdenId = dto.OrdenId,
            Estado = dto.Estado,
            CodigoQr = Guid.NewGuid().ToString("N"),
            UsedDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.Tickets.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto dto)
    {
        var item = await _context.Tickets.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.EventoId = dto.EventoId;
        item.OrdenId = dto.OrdenId;
        item.Estado = dto.Estado;
        item.UsedDate = dto.UsedDate;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Tickets.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Tickets.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
